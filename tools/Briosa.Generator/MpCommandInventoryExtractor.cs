using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Briosa.Generator;

internal static partial class MpCommandInventoryExtractor
{
    private const string DocumentationKind = "installed_mp_documentation";
    private const string SdkCodeKind = "generated_sdk_sample";

    private static readonly JsonSerializerOptions CompactSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    internal static MpCommandInventoryExtraction Extract(
        string spatialAnalyzerTarget,
        string documentationRoot,
        string sdkCodeRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spatialAnalyzerTarget);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentationRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sdkCodeRoot);

        var documentationFiles = ReadSourceFiles(documentationRoot, IsDocumentationFile);
        var sdkCodeFiles = ReadSourceFiles(sdkCodeRoot, IsSdkCodeFile);

        var documents = documentationFiles
            .Select(ParseDocument)
            .Where(static document => document is not null)
            .Cast<DocumentCommand>()
            .OrderBy(static document => document.Reference, StringComparer.Ordinal)
            .ToList();
        var sdkSteps = sdkCodeFiles
            .SelectMany(ParseSdkSteps)
            .OrderBy(static step => step.Reference, StringComparer.Ordinal)
            .ThenBy(static step => step.Occurrence)
            .ToList();

        var matches = MatchEvidence(documents, sdkSteps);
        var commands = new List<MpCommandInventoryCommand>(documents.Count + sdkSteps.Count);
        foreach (var document in documents)
        {
            matches.ByDocument.TryGetValue(document.InventoryKey, out var matchingSteps);
            commands.Add(CreateDocumentCommand(document, matchingSteps ?? []));
        }

        foreach (var sdkStep in sdkSteps.Where(step => !matches.ReferencedSdkKeys.Contains(step.Key)))
        {
            commands.Add(CreateSdkOnlyCommand(sdkStep));
        }

        commands.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.InventoryKey, right.InventoryKey));

        var inventory = new MpCommandInventory
        {
            Schema = "../../schemas/v1/inventory.schema.json",
            SchemaVersion = 1,
            SpatialAnalyzerTarget = spatialAnalyzerTarget,
            Provenance = new MpCommandInventoryProvenance
            {
                Documentation = CreateSource(
                    DocumentationKind,
                    documentationFiles,
                    documents.Count),
                SdkCode = CreateSource(SdkCodeKind, sdkCodeFiles, sdkSteps.Count),
            },
            Summary = CreateSummary(commands),
            Commands = commands,
        };

        var json = SerializeInventory(inventory);
        var report = CreateReport(inventory);
        return new MpCommandInventoryExtraction(inventory, json, report);
    }

    internal static IReadOnlyList<string> Write(
        string spatialAnalyzerTarget,
        string documentationRoot,
        string sdkCodeRoot,
        string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var extraction = Extract(spatialAnalyzerTarget, documentationRoot, sdkCodeRoot);
        Directory.CreateDirectory(outputDirectory);

        var inventoryPath = Path.Combine(outputDirectory, "inventory.json");
        var reportPath = Path.Combine(outputDirectory, "report.md");
        File.WriteAllText(
            inventoryPath,
            extraction.InventoryJson,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(
            reportPath,
            extraction.ReportMarkdown,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return [inventoryPath, reportPath];
    }

    private static List<SourceFile> ReadSourceFiles(
        string root,
        Func<string, bool> predicate)
    {
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException($"Evidence directory was not found: {fullRoot}");
        }

        return Directory
            .EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
            .Where(predicate)
            .Select(path =>
            {
                var bytes = File.ReadAllBytes(path);
                return new SourceFile(
                    NormalizePath(Path.GetRelativePath(fullRoot, path)),
                    Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    File.ReadAllText(path));
            })
            .OrderBy(static file => file.Reference, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsDocumentationFile(string path) =>
        string.Equals(Path.GetExtension(path), ".htm", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Path.GetExtension(path), ".html", StringComparison.OrdinalIgnoreCase);

    private static bool IsSdkCodeFile(string path) =>
        string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Path.GetExtension(path), ".vb", StringComparison.OrdinalIgnoreCase);

    private static DocumentCommand? ParseDocument(SourceFile file)
    {
        var document = XDocument.Parse(file.Text, LoadOptions.PreserveWhitespace);
        var title = document
            .Descendants()
            .FirstOrDefault(static element => HasLocalName(element, "h1"));
        if (title is null)
        {
            return null;
        }

        var headings = document
            .Descendants()
            .Where(static element => HasLocalName(element, "h2"))
            .ToList();
        var inputHeading = FindHeading(headings, "Input Arguments");
        var returnHeading = FindHeading(headings, "Return Arguments");
        var statusHeading = FindHeading(headings, "Returned Status");
        if (inputHeading is null && returnHeading is null && statusHeading is null)
        {
            return null;
        }

        var findings = new List<string>();
        var arguments = new List<DocumentArgument>();
        if (inputHeading is null)
        {
            findings.Add("missing_input_arguments_section");
        }
        else
        {
            arguments.AddRange(ParseDocumentArguments(inputHeading, "input", findings));
        }

        if (returnHeading is null)
        {
            findings.Add("missing_return_arguments_section");
        }
        else
        {
            arguments.AddRange(ParseDocumentArguments(returnHeading, "output", findings));
        }

        if (statusHeading is null)
        {
            findings.Add("missing_returned_status_section");
        }

        var mergedArguments = MergeDocumentArguments(arguments, findings);
        var reference = file.Reference;
        var categoryPath = reference
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1)
            .ToList();

        return new DocumentCommand(
            $"documentation:{reference}",
            NormalizeText(title.Value),
            categoryPath,
            reference,
            file.Sha256,
            inputHeading is not null,
            returnHeading is not null,
            statusHeading is not null,
            mergedArguments,
            findings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList());
    }

    private static XElement? FindHeading(IEnumerable<XElement> headings, string text) =>
        headings.FirstOrDefault(
            heading => string.Equals(NormalizeText(heading.Value), text, StringComparison.Ordinal));

    private static IEnumerable<DocumentArgument> ParseDocumentArguments(
        XElement heading,
        string direction,
        List<string> findings)
    {
        var tables = heading
            .ElementsAfterSelf()
            .TakeWhile(static element => !HasLocalName(element, "h2"))
            .SelectMany(static element =>
                HasLocalName(element, "table")
                    ? [element]
                    : element.Descendants().Where(descendant => HasLocalName(descendant, "table")));

        foreach (var row in tables.SelectMany(
                     static table => table.Descendants().Where(element => HasLocalName(element, "tr"))))
        {
            var cells = row
                .Elements()
                .Where(static element => HasLocalName(element, "td"))
                .Select(static element => NormalizeText(element.Value))
                .ToList();
            if (cells.Count == 0)
            {
                continue;
            }

            if (cells.Count < 4 || !int.TryParse(cells[0], out var ordinal))
            {
                findings.Add("unparsed_documentation_argument_row");
                continue;
            }

            var name = cells[2];
            var description = string.Join(' ', cells.Skip(3));
            yield return new DocumentArgument(
                ordinal,
                name,
                cells[1],
                direction,
                IsOptional(name, description));
        }
    }

    private static List<DocumentArgument> MergeDocumentArguments(
        IEnumerable<DocumentArgument> arguments,
        List<string> findings)
    {
        var result = new List<DocumentArgument>();
        foreach (var group in arguments
                     .GroupBy(static argument => argument.Ordinal)
                     .OrderBy(static group => group.Key))
        {
            var values = group.ToList();
            if (values.Count == 2 &&
                values.Select(static value => value.Direction).ToHashSet(StringComparer.Ordinal)
                    .SetEquals(["input", "output"]) &&
                string.Equals(values[0].Name, values[1].Name, StringComparison.Ordinal) &&
                string.Equals(values[0].DocumentedType, values[1].DocumentedType, StringComparison.Ordinal))
            {
                result.Add(values[0] with
                {
                    Direction = "input_output",
                    IsOptional = values.Any(static value => value.IsOptional),
                });
                continue;
            }

            if (values.Count > 1)
            {
                findings.Add("conflicting_documented_ordinal");
            }

            result.AddRange(values.OrderBy(static value => value.Direction, StringComparer.Ordinal));
        }

        return result;
    }

    private static bool IsOptional(string name, string description) =>
        name.Contains("optional", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("optional", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<SdkStep> ParseSdkSteps(SourceFile file)
    {
        var matches = SetStepRegex().Matches(file.Text);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var end = index + 1 < matches.Count ? matches[index + 1].Index : file.Text.Length;
            var block = file.Text[match.Index..end];
            var executeMatch = ExecuteStepRegex().Match(block);
            var calls = new List<SdkCall>();
            var callOrder = 0;
            foreach (Match callMatch in SdkCallRegex().Matches(block))
            {
                var method = callMatch.Groups["method"].Value;
                var argument = callMatch.Groups["argument"].Value.Replace("\"\"", "\"", StringComparison.Ordinal);
                var phase = !executeMatch.Success || callMatch.Index < executeMatch.Index ? "input" : "output";
                calls.Add(new SdkCall(method, argument, phase, callOrder++));
            }

            var findings = executeMatch.Success ? new List<string>() : ["missing_execute_step"];
            yield return new SdkStep(
                $"{file.Reference}#{index + 1}",
                match.Groups["name"].Value.Replace("\"\"", "\"", StringComparison.Ordinal),
                file.Reference,
                file.Sha256,
                index + 1,
                calls,
                findings);
        }
    }

    private static EvidenceMatches MatchEvidence(
        IReadOnlyCollection<DocumentCommand> documents,
        IReadOnlyCollection<SdkStep> sdkSteps)
    {
        var byDocument = new Dictionary<string, List<SdkStep>>(StringComparer.Ordinal);
        var referencedSdkKeys = new HashSet<string>(StringComparer.Ordinal);
        var documentGroups = documents.GroupBy(static document => document.MpStep, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
        var sdkGroups = sdkSteps.GroupBy(static step => step.MpStep, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);

        foreach (var (mpStep, documentGroup) in documentGroups)
        {
            if (!sdkGroups.TryGetValue(mpStep, out var sdkGroup))
            {
                continue;
            }

            var remainingDocuments = documentGroup
                .OrderBy(static document => document.Reference, StringComparer.Ordinal)
                .ToList();
            var remainingSteps = sdkGroup
                .OrderBy(static step => step.Reference, StringComparer.Ordinal)
                .ThenBy(static step => step.Occurrence)
                .ToList();

            while (remainingDocuments.Count > 0 && remainingSteps.Count > 0)
            {
                if (remainingDocuments.Count == 1 && remainingSteps.Count == 1)
                {
                    AddMatch(remainingDocuments[0], remainingSteps[0], byDocument, referencedSdkKeys);
                    remainingDocuments.Clear();
                    remainingSteps.Clear();
                    break;
                }

                var best = FindBestMatch(remainingDocuments, remainingSteps);
                if (best is null)
                {
                    break;
                }

                AddMatch(best.Value.Document, best.Value.Step, byDocument, referencedSdkKeys);
                remainingDocuments.Remove(best.Value.Document);
                remainingSteps.Remove(best.Value.Step);
            }

            if (remainingDocuments.Count > 0 && remainingSteps.Count > 0)
            {
                foreach (var document in remainingDocuments)
                {
                    byDocument[document.InventoryKey] = [.. remainingSteps];
                }

                foreach (var step in remainingSteps)
                {
                    referencedSdkKeys.Add(step.Key);
                }
            }
        }

        var normalizedDocumentGroups = documents
            .Where(document => !byDocument.ContainsKey(document.InventoryKey))
            .GroupBy(document => NormalizeIdentifier(document.MpStep), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
        var normalizedSdkGroups = sdkSteps
            .Where(step => !referencedSdkKeys.Contains(step.Key))
            .GroupBy(step => NormalizeIdentifier(step.MpStep), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
        foreach (var (normalizedStep, documentGroup) in normalizedDocumentGroups)
        {
            if (normalizedStep.Length == 0 ||
                !normalizedSdkGroups.TryGetValue(normalizedStep, out var sdkGroup))
            {
                continue;
            }

            var remainingDocuments = documentGroup
                .OrderBy(static document => document.Reference, StringComparer.Ordinal)
                .ToList();
            var remainingSteps = sdkGroup
                .OrderBy(static step => step.Reference, StringComparer.Ordinal)
                .ThenBy(static step => step.Occurrence)
                .ToList();
            while (remainingDocuments.Count > 0 && remainingSteps.Count > 0)
            {
                if (remainingDocuments.Count == 1 && remainingSteps.Count == 1)
                {
                    AddMatch(remainingDocuments[0], remainingSteps[0], byDocument, referencedSdkKeys);
                    remainingDocuments.Clear();
                    remainingSteps.Clear();
                    break;
                }

                var best = FindBestMatch(remainingDocuments, remainingSteps);
                if (best is null)
                {
                    break;
                }

                AddMatch(best.Value.Document, best.Value.Step, byDocument, referencedSdkKeys);
                remainingDocuments.Remove(best.Value.Document);
                remainingSteps.Remove(best.Value.Step);
            }

            if (remainingDocuments.Count > 0 && remainingSteps.Count > 0)
            {
                foreach (var document in remainingDocuments)
                {
                    byDocument[document.InventoryKey] = [.. remainingSteps];
                }

                foreach (var step in remainingSteps)
                {
                    referencedSdkKeys.Add(step.Key);
                }
            }
        }
        return new EvidenceMatches(byDocument, referencedSdkKeys);
    }

    private static (DocumentCommand Document, SdkStep Step)? FindBestMatch(
        IReadOnlyCollection<DocumentCommand> documents,
        IReadOnlyCollection<SdkStep> sdkSteps)
    {
        var candidates = (
            from document in documents
            from step in sdkSteps
            let score = Affinity(document, step)
            where score >= 1000
            orderby score descending,
                document.Reference ascending,
                step.Reference ascending,
                step.Occurrence ascending
            select (Document: document, Step: step, Score: score)).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates[0];
        var tiedForDocument = candidates.Count(candidate =>
            candidate.Document == best.Document && candidate.Score == best.Score);
        var tiedForStep = candidates.Count(candidate =>
            candidate.Step == best.Step && candidate.Score == best.Score);
        return tiedForDocument == 1 && tiedForStep == 1 ? (best.Document, best.Step) : null;
    }

    private static int Affinity(DocumentCommand document, SdkStep step)
    {
        var documentCategory = NormalizeIdentifier(string.Concat(document.CategoryPath));
        var sdkReference = NormalizeIdentifier(Path.GetFileNameWithoutExtension(step.Reference));
        if (documentCategory.Length == 0 || sdkReference.Length == 0)
        {
            return 0;
        }

        if (sdkReference.StartsWith(documentCategory, StringComparison.Ordinal) ||
            documentCategory.StartsWith(sdkReference, StringComparison.Ordinal))
        {
            return 1000 + Math.Min(documentCategory.Length, sdkReference.Length);
        }

        return 0;
    }

    private static void AddMatch(
        DocumentCommand document,
        SdkStep step,
        IDictionary<string, List<SdkStep>> byDocument,
        HashSet<string> referencedSdkKeys)
    {
        byDocument[document.InventoryKey] = [step];
        referencedSdkKeys.Add(step.Key);
    }

    private static MpCommandInventoryCommand CreateDocumentCommand(
        DocumentCommand document,
        IReadOnlyCollection<SdkStep> sdkSteps)
    {
        var findings = new List<string>(document.Findings);
        if (sdkSteps.Count == 0)
        {
            findings.Add("sdk_step_missing");
        }
        else if (sdkSteps.Count > 1)
        {
            findings.Add("ambiguous_sdk_match");
        }

        foreach (var step in sdkSteps)
        {
            findings.AddRange(step.Findings);
        }

        var selectedStep = sdkSteps.Count == 1 ? sdkSteps.Single() : null;
        if (selectedStep is not null &&
            !string.Equals(document.MpStep, selectedStep.MpStep, StringComparison.Ordinal))
        {
            findings.Add("mp_step_text_difference");
        }

        var arguments = BuildArguments(document.Arguments, selectedStep, findings);
        return new MpCommandInventoryCommand
        {
            InventoryKey = document.InventoryKey,
            MpStep = document.MpStep,
            CategoryPath = document.CategoryPath,
            Documentation = new MpCommandInventoryDocumentEvidence
            {
                Reference = document.Reference,
                Sha256 = document.Sha256,
                HasInputArgumentsSection = document.HasInputArgumentsSection,
                HasReturnArgumentsSection = document.HasReturnArgumentsSection,
                HasReturnedStatusSection = document.HasReturnedStatusSection,
            },
            SdkEvidence = sdkSteps
                .Select(CreateSdkEvidence)
                .OrderBy(static evidence => evidence.Reference, StringComparer.Ordinal)
                .ThenBy(static evidence => evidence.Occurrence)
                .ToList(),
            OverallOutcome = document.HasReturnedStatusSection ? "documented" : "missing",
            Arguments = arguments,
            Findings = findings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
        };
    }

    private static MpCommandInventoryCommand CreateSdkOnlyCommand(SdkStep sdkStep)
    {
        var findings = new List<string>(sdkStep.Findings) { "documentation_command_missing" };
        return new MpCommandInventoryCommand
        {
            InventoryKey = $"sdk:{sdkStep.Key}",
            MpStep = sdkStep.MpStep,
            CategoryPath = CategoryPathFromSdkReference(sdkStep.Reference),
            Documentation = null,
            SdkEvidence = [CreateSdkEvidence(sdkStep)],
            OverallOutcome = "unknown",
            Arguments = BuildArguments([], sdkStep, findings),
            Findings = findings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
        };
    }

    private static List<string> CategoryPathFromSdkReference(string reference)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(reference);
        return withoutExtension.Split('_', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static MpCommandInventorySdkEvidence CreateSdkEvidence(SdkStep step) => new()
    {
        Reference = step.Reference,
        Sha256 = step.Sha256,
        Occurrence = step.Occurrence,
        MpStep = step.MpStep,
    };

    private static List<MpCommandInventoryArgument> BuildArguments(
        IReadOnlyCollection<DocumentArgument> documentedArguments,
        SdkStep? sdkStep,
        List<string> commandFindings)
    {
        var result = new List<MpCommandInventoryArgument>();
        var calls = sdkStep?.Calls ?? [];
        var consumedCallOrders = new HashSet<int>();
        foreach (var argument in documentedArguments)
        {
            var matchingCalls = calls
                .Where(call => string.Equals(call.Argument, argument.Name, StringComparison.Ordinal))
                .ToList();
            var argumentFindings = new List<string>();
            if (matchingCalls.Count == 0)
            {
                var normalizedName = NormalizeIdentifier(argument.Name);
                var normalizedMatches = calls
                    .Where(call =>
                        normalizedName.Length > 0 &&
                        string.Equals(
                            NormalizeIdentifier(call.Argument),
                            normalizedName,
                            StringComparison.Ordinal))
                    .GroupBy(static call => call.Argument, StringComparer.Ordinal)
                    .ToList();
                if (normalizedMatches.Count == 1)
                {
                    matchingCalls = normalizedMatches[0].ToList();
                    argumentFindings.Add("argument_name_text_difference");
                }
                else if (normalizedMatches.Count > 1)
                {
                    argumentFindings.Add("ambiguous_sdk_argument_match");
                }
            }

            foreach (var call in matchingCalls)
            {
                consumedCallOrders.Add(call.Order);
            }
            var binding = CreateBinding(matchingCalls);
            AddExpectedBindingFindings(argument.Direction, binding, argumentFindings);
            AddDirectionDisagreementFindings(argument.Direction, binding, argumentFindings);
            commandFindings.AddRange(argumentFindings);

            result.Add(new MpCommandInventoryArgument
            {
                Ordinal = argument.Ordinal,
                SdkOrder = matchingCalls.Count == 0 ? null : matchingCalls.Min(static call => call.Order),
                MpName = argument.Name,
                DocumentedType = argument.DocumentedType,
                Direction = argument.Direction,
                ResultOnly = argument.Direction == "output" ? "yes" : "no",
                Presence = argument.Direction is "input" or "input_output"
                    ? argument.IsOptional ? "optional" : "unknown"
                    : "not_applicable",
                SdkBinding = binding,
                Findings = argumentFindings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
            });
        }

        foreach (var callGroup in calls
                     .Where(call => !consumedCallOrders.Contains(call.Order))
                     .GroupBy(static call => call.Argument, StringComparer.Ordinal)
                     .OrderBy(static group => group.Min(call => call.Order)))
        {
            commandFindings.Add("sdk_argument_not_documented");
            var groupedCalls = callGroup.ToList();
            var binding = CreateBinding(groupedCalls);
            var direction = InferDirection(binding);
            result.Add(new MpCommandInventoryArgument
            {
                Ordinal = null,
                SdkOrder = groupedCalls.Min(static call => call.Order),
                MpName = callGroup.Key,
                DocumentedType = "unknown",
                Direction = direction,
                ResultOnly = direction == "output" ? "yes" : direction == "unknown" ? "unknown" : "no",
                Presence = direction is "input" or "input_output" ? "unknown" : "not_applicable",
                SdkBinding = binding,
                Findings = ["not_documented"],
            });
        }

        return result
            .OrderBy(static argument => argument.Ordinal ?? int.MaxValue)
            .ThenBy(static argument => argument.SdkOrder ?? int.MaxValue)
            .ThenBy(static argument => argument.MpName, StringComparer.Ordinal)
            .ToList();
    }

    private static MpCommandInventorySdkBinding CreateBinding(IReadOnlyCollection<SdkCall> calls) => new()
    {
        Setter = CreateBindingEvidence(calls, setter: true),
        Getter = CreateBindingEvidence(calls, setter: false),
    };

    private static MpCommandInventoryBindingEvidence CreateBindingEvidence(
        IReadOnlyCollection<SdkCall> calls,
        bool setter)
    {
        var expectedPrefix = setter ? "Set" : "Get";
        var expectedPhase = setter ? "input" : "output";
        var methods = calls
            .Where(call => call.Method.StartsWith(expectedPrefix, StringComparison.Ordinal))
            .Select(static call => call.Method)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var unavailable = calls.Any(call =>
            string.Equals(call.Method, "NOT_SUPPORTED", StringComparison.Ordinal) &&
            string.Equals(call.Phase, expectedPhase, StringComparison.Ordinal));
        var argumentNames = calls
            .Where(call =>
                call.Method.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
                (string.Equals(call.Method, "NOT_SUPPORTED", StringComparison.Ordinal) &&
                 string.Equals(call.Phase, expectedPhase, StringComparison.Ordinal)))
            .Select(static call => call.Argument)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return (methods.Count, unavailable, argumentNames.Count) switch
        {
            (0, false, _) => new MpCommandInventoryBindingEvidence
            {
                Status = "not_observed",
                Method = null,
                ArgumentName = null,
            },
            (0, true, 1) => new MpCommandInventoryBindingEvidence
            {
                Status = "unavailable",
                Method = null,
                ArgumentName = argumentNames[0],
            },
            (1, false, 1) => new MpCommandInventoryBindingEvidence
            {
                Status = "available",
                Method = methods[0],
                ArgumentName = argumentNames[0],
            },
            _ => new MpCommandInventoryBindingEvidence
            {
                Status = "ambiguous",
                Method = null,
                ArgumentName = null,
            },
        };
    }

    private static void AddExpectedBindingFindings(
        string direction,
        MpCommandInventorySdkBinding binding,
        List<string> findings)
    {
        if (direction is "input" or "input_output")
        {
            AddBindingFinding("setter", binding.Setter.Status, findings);
        }

        if (direction is "output" or "input_output")
        {
            AddBindingFinding("getter", binding.Getter.Status, findings);
        }
    }

    private static void AddBindingFinding(
        string bindingKind,
        string status,
        List<string> findings)
    {
        if (status == "not_observed")
        {
            findings.Add($"sdk_{bindingKind}_missing");
        }
        else if (status is "unavailable" or "ambiguous")
        {
            findings.Add($"sdk_{bindingKind}_{status}");
        }
    }

    private static void AddDirectionDisagreementFindings(
        string direction,
        MpCommandInventorySdkBinding binding,
        List<string> findings)
    {
        if (direction == "input" && binding.Getter.Status == "available")
        {
            findings.Add("direction_disagreement_sdk_getter_observed");
        }

        if (direction == "output" && binding.Setter.Status == "available")
        {
            findings.Add("direction_disagreement_sdk_setter_observed");
        }
    }

    private static string InferDirection(MpCommandInventorySdkBinding binding)
    {
        var setterObserved = binding.Setter.Status is "available" or "unavailable" or "ambiguous";
        var getterObserved = binding.Getter.Status is "available" or "unavailable" or "ambiguous";
        return (setterObserved, getterObserved) switch
        {
            (true, true) => "input_output",
            (true, false) => "input",
            (false, true) => "output",
            _ => "unknown",
        };
    }

    private static MpCommandInventorySource CreateSource(
        string kind,
        IReadOnlyCollection<SourceFile> files,
        int recordCount) => new()
        {
            Kind = kind,
            FileCount = files.Count,
            RecordCount = recordCount,
            AggregateSha256 = AggregateHash(files),
            SourceMaterialCommitted = false,
        };

    private static string AggregateHash(IEnumerable<SourceFile> files)
    {
        var manifest = string.Concat(files
            .OrderBy(static file => file.Reference, StringComparer.Ordinal)
            .Select(static file => $"{file.Reference}\0{file.Sha256}\n"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));
    }

    private static MpCommandInventorySummary CreateSummary(
        List<MpCommandInventoryCommand> commands)
    {
        var findings = commands
            .SelectMany(static command => command.Findings.Concat(
                command.Arguments.SelectMany(static argument => argument.Findings)))
            .GroupBy(static finding => finding, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => new MpCommandInventoryFindingCount
            {
                Finding = group.Key,
                Count = group.Count(),
            })
            .ToList();

        return new MpCommandInventorySummary
        {
            CommandCount = commands.Count,
            MatchedCommandCount = commands.Count(command =>
                command.Documentation is not null &&
                command.SdkEvidence.Count == 1 &&
                !command.Findings.Contains("ambiguous_sdk_match", StringComparer.Ordinal)),
            DocumentationOnlyCommandCount = commands.Count(command =>
                command.Documentation is not null && command.SdkEvidence.Count == 0),
            SdkOnlyCommandCount = commands.Count(static command => command.Documentation is null),
            AmbiguousCommandCount = commands.Count(command =>
                command.Findings.Contains("ambiguous_sdk_match", StringComparer.Ordinal)),
            FindingCounts = findings,
        };
    }

    private static string SerializeInventory(MpCommandInventory inventory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.Append("  \"$schema\": ")
            .Append(JsonSerializer.Serialize(inventory.Schema))
            .AppendLine(",");
        builder.Append("  \"schema_version\": ")
            .Append(inventory.SchemaVersion.ToString(CultureInfo.InvariantCulture))
            .AppendLine(",");
        builder.Append("  \"spatial_analyzer_target\": ")
            .Append(JsonSerializer.Serialize(inventory.SpatialAnalyzerTarget))
            .AppendLine(",");
        builder.Append("  \"provenance\": ")
            .Append(JsonSerializer.Serialize(inventory.Provenance, CompactSerializerOptions))
            .AppendLine(",");
        builder.Append("  \"summary\": ")
            .Append(JsonSerializer.Serialize(inventory.Summary, CompactSerializerOptions))
            .AppendLine(",");
        builder.AppendLine("  \"commands\": [");
        for (var index = 0; index < inventory.Commands.Count; index++)
        {
            builder.Append("    ")
                .Append(JsonSerializer.Serialize(inventory.Commands[index], CompactSerializerOptions));
            if (index + 1 < inventory.Commands.Count)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString().ReplaceLineEndings("\n");
    }

    private static string CreateReport(MpCommandInventory inventory)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"# SA {inventory.SpatialAnalyzerTarget} extracted MP command inventory");
        builder.AppendLine();
        builder.AppendLine("This report summarizes derived facts only. Installed HTML and generated SDK samples remain local evidence and are not committed.");
        builder.AppendLine();
        builder.AppendLine("## Evidence");
        builder.AppendLine();
        builder.AppendLine("| Source | Files | Records | Aggregate SHA-256 |");
        builder.AppendLine("| --- | ---: | ---: | --- |");
        AppendSourceRow(builder, "Installed MP documentation", inventory.Provenance.Documentation);
        AppendSourceRow(builder, "View SDK Code (VB)", inventory.Provenance.SdkCode);
        builder.AppendLine();
        builder.AppendLine("## Coverage");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Inventory commands: {inventory.Summary.CommandCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Documentation and SDK matched: {inventory.Summary.MatchedCommandCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Documentation only: {inventory.Summary.DocumentationOnlyCommandCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- SDK only: {inventory.Summary.SdkOnlyCommandCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Ambiguous evidence matches: {inventory.Summary.AmbiguousCommandCount}");
        builder.AppendLine();
        builder.AppendLine("## Finding counts");
        builder.AppendLine();
        builder.AppendLine("| Finding | Count |");
        builder.AppendLine("| --- | ---: |");
        foreach (var finding in inventory.Summary.FindingCounts)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"| `{finding.Finding}` | {finding.Count} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Metadata gaps requiring review or Hexagon input");
        builder.AppendLine();
        builder.AppendLine("- The installed command reference is useful evidence but is not an authoritative machine-readable contract.");
        builder.AppendLine("- Generated SDK sample values do not establish whether inputs are required or whether sample values are meaningful defaults.");
        builder.AppendLine("- `NOT_SUPPORTED` establishes that View SDK Code has no binding; it does not establish that a generic or undocumented SDK binding is safe.");
        builder.AppendLine("- Setter/getter presence is compared with documented direction, but mismatches remain unresolved evidence rather than silently corrected metadata.");
        builder.AppendLine("- No compatibility or semantic equivalence is inferred for any other SpatialAnalyzer release.");
        builder.AppendLine();
        builder.AppendLine("## Commands with findings");
        builder.AppendLine();
        builder.AppendLine("The inventory JSON contains exact argument-level evidence. This table intentionally excludes vendor prose and raw SDK code.");
        builder.AppendLine();
        builder.AppendLine("| Inventory key | MP step | Findings |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var command in inventory.Commands.Where(HasFindings))
        {
            var findings = command.Findings
                .Concat(command.Arguments.SelectMany(static argument => argument.Findings))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(static finding => $"`{finding}`");
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {EscapeMarkdown(command.InventoryKey)} | {EscapeMarkdown(command.MpStep)} | {string.Join(", ", findings)} |");
        }

        return builder.ToString().ReplaceLineEndings("\n");
    }

    private static bool HasFindings(MpCommandInventoryCommand command) =>
        command.Findings.Count > 0 || command.Arguments.Any(static argument => argument.Findings.Count > 0);

    private static void AppendSourceRow(
        StringBuilder builder,
        string label,
        MpCommandInventorySource source) =>
        builder.AppendLine(CultureInfo.InvariantCulture, $"| {label} | {source.FileCount} | {source.RecordCount} | `{source.AggregateSha256}` |");

    private static string EscapeMarkdown(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static bool HasLocalName(XElement element, string localName) =>
        string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeText(string value) =>
        WhitespaceRegex().Replace(WebUtility.HtmlDecode(value), " ").Trim();

    private static string NormalizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    [GeneratedRegex("""NrkSdk\s*\.\s*SetStep\s*\(\s*\x22(?<name>(?:\x22\x22|[^\x22])*)\x22\s*\)""", RegexOptions.CultureInvariant)]
    private static partial Regex SetStepRegex();

    [GeneratedRegex(@"NrkSdk\s*\.\s*ExecuteStep\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex ExecuteStepRegex();

    [GeneratedRegex("""NrkSdk\s*\.\s*(?<method>NOT_SUPPORTED|(?:Set|Get)[A-Za-z0-9]+Arg[0-9]*)\s*\(\s*\x22(?<argument>(?:\x22\x22|[^\x22])*)\x22""", RegexOptions.CultureInvariant)]
    private static partial Regex SdkCallRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    private sealed record SourceFile(
        string Reference,
        string Sha256,
        string Text);

    private sealed record DocumentCommand(
        string InventoryKey,
        string MpStep,
        List<string> CategoryPath,
        string Reference,
        string Sha256,
        bool HasInputArgumentsSection,
        bool HasReturnArgumentsSection,
        bool HasReturnedStatusSection,
        List<DocumentArgument> Arguments,
        List<string> Findings);

    private sealed record DocumentArgument(
        int Ordinal,
        string Name,
        string DocumentedType,
        string Direction,
        bool IsOptional);

    private sealed record SdkStep(
        string Key,
        string MpStep,
        string Reference,
        string Sha256,
        int Occurrence,
        List<SdkCall> Calls,
        List<string> Findings);

    private sealed record SdkCall(string Method, string Argument, string Phase, int Order);

    private sealed record EvidenceMatches(
        Dictionary<string, List<SdkStep>> ByDocument,
        HashSet<string> ReferencedSdkKeys);
}
