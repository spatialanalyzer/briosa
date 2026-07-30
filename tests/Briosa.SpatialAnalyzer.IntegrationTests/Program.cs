using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Briosa.SpatialAnalyzer.IntegrationTests;
using ObjectiveSA;
using ObjectiveSA.Exceptions.SDKStatus;
using ObjectiveSA.Types;
using ObjectiveSA.Types.Options;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            Emit(new Result(
                "test_setup",
                true,
                "definitely_not_started",
                false,
                false,
                "not_checked",
                "invalid_arguments"));
            return 2;
        }

        var root = Path.GetFullPath(args[0]);
        var scenario = args[1];
        FixtureDescriptor? fixture = null;
        Directory.CreateDirectory(root);

        try
        {
            fixture = args.Length == 3
                ? FixtureDescriptorReader.Read(args[2])
                : null;
            var client = new ObjectiveSAClient();
            client.Start(new ClientStartOptions
            {
                LaunchNewSAWindow = false,
                CloseOtherSDKEngineWindows = false,
                KeepNewSDKEngineWindow = true
            });

            if (!client.CheckSDKConnection())
            {
                Emit(new Result(
                    scenario,
                    true,
                    "definitely_not_started",
                    false,
                    false,
                    "not_checked",
                    "connection_not_ready"));
                return 3;
            }

            if (scenario is "export_event_ref_list" or
                "merge_measurements_xml" or
                "import_polyworks")
            {
                OpenDisposableFixtureJob(client, RequireFixture(fixture), root);
            }

            var result = scenario switch
            {
                "save" => TestSave(client, root),
                "save_as" => TestSaveAs(client, root),
                "save_as_read_only_template" => TestTemplate(client, root),
                "export_ascii_points" => TestAsciiPoints(client, root),
                "export_ascii_point_set_wrong_type" => TestAsciiPointSetWrongType(client, root),
                "export_event_ref_list" => TestEventExport(client, root, RequireFixture(fixture)),
                "import_nominals_xml" => TestImportNominals(client, RequireFixture(fixture)),
                "merge_measurements_xml" => TestMergeMeasurements(client, root, RequireFixture(fixture)),
                "output_report_pdf" => TestPdf(client, root),
                "import_vstars_cameras" => TestVstars(client, RequireFixture(fixture)),
                "import_polyworks" => TestPolyworks(client, RequireFixture(fixture)),
                _ => new Result(
                    scenario,
                    true,
                    "definitely_not_started",
                    false,
                    false,
                    "not_checked",
                    "unknown_scenario")
            };
            Emit(result);
            return result.ExpectationMet ? 0 : 1;
        }
        catch (Exception)
        {
            Emit(new Result(
                scenario,
                true,
                "definitely_not_started",
                false,
                false,
                "not_checked",
                "test_setup_failed"));
            return 1;
        }
    }

    private static Result TestSave(ObjectiveSAClient client, string root)
    {
        var group = CreatePointGroup(client);
        var file = Path.Combine(root, "named-job.xit64");
        client.SaveAs(file);
        var beforeSave = HashFile(file);
        client.ConstructPointInWorkingCoordinates(
            new PointName(group.CollectionName, group.ObjectName, "P3"),
            new Vector3(7.25f, 8.5f, 9.75f));
        return ExecuteTarget(
            "save",
            client.Save,
            () => IsNonemptyFile(file) && !beforeSave.SequenceEqual(HashFile(file)),
            "saved_job_did_not_change");
    }

    private static Result TestSaveAs(ObjectiveSAClient client, string root)
    {
        CreatePointGroup(client);
        var file = Path.Combine(root, "save-as.xit64");
        return ExecuteTarget(
            "save_as",
            () => client.SaveAs(file, 42),
            () =>
            {
                var files = Directory.GetFiles(root, "*.xit64");
                return files.Length == 1 && files.All(IsNonemptyFile);
            },
            "unexpected_output_count");
    }

    private static Result TestTemplate(ObjectiveSAClient client, string root)
    {
        CreatePointGroup(client);
        var file = Path.Combine(root, "template.xit64");
        return ExecuteTarget(
            "save_as_read_only_template",
            () => client.SaveAsReadOnlyTemplate(file),
            () => IsNonemptyFile(file),
            "output_not_created");
    }

    private static Result TestAsciiPoints(ObjectiveSAClient client, string root)
    {
        var group = CreatePointGroup(client);
        var file = Path.Combine(root, "points.txt");
        return ExecuteTarget(
            "export_ascii_points",
            () => client.ExportASCIIPoints(
                file,
                group,
                ExportDelimiterSpec.Space,
                TargetNameFormat.Target,
                CoordinateSystemType.Cartesian,
                decimalPrecision: 6,
                append: false),
            () => IsNonemptyFile(file),
            "output_not_created");
    }

    private static Result TestAsciiPointSetWrongType(
        ObjectiveSAClient client,
        string root)
    {
        var pointGroup = CreatePointGroup(client);
        var file = Path.Combine(root, "point-set.txt");
        return ExecuteExpectedFailure(
            "export_ascii_point_set_wrong_type",
            () => client.ExportASCIIPointSet(
                file,
                pointGroup,
                ExportDelimiterSpec.Space,
                TargetNameFormat.Target,
                CoordinateSystemType.Cartesian,
                decimalPrecision: 6,
                append: false),
            () => !File.Exists(file),
            "unexpected_output_created");
    }

    private static Result TestEventExport(
        ObjectiveSAClient client,
        string root,
        FixtureDescriptor fixture)
    {
        if (fixture.Items is not { Length: > 0 })
        {
            throw new InvalidDataException("event_fixture_required");
        }

        var events = fixture.Items.Select(ToCollectionItem).ToArray();
        if (events.Any(value => value.ItemType != ItemType.Event))
        {
            throw new InvalidDataException("event_item_type_required");
        }
        var file = Path.Combine(root, "events.txt");
        return ExecuteTarget(
            "export_event_ref_list",
            () => client.ExportEventRefList(
                events,
                file,
                decimalPrecision: 6,
                overwriteExisting: false),
            () => IsNonemptyFile(file),
            "output_not_created");
    }

    private static Result TestImportNominals(ObjectiveSAClient client, FixtureDescriptor fixture)
    {
        var input = RequireInputFile(fixture);
        return ExecuteTarget(
            "import_nominals_xml",
            () => client.ImportNominalsFromXMLFile(input),
            null,
            null);
    }

    private static Result TestMergeMeasurements(
        ObjectiveSAClient client,
        string root,
        FixtureDescriptor fixture)
    {
        var input = RequireInputFile(fixture);
        var originalHash = HashFile(input);
        var disposableInput = Path.Combine(
            root,
            $"merge-measurements-input{Path.GetExtension(input)}");
        File.Copy(input, disposableInput, overwrite: false);
        var pointGroup = ToCollectionObject(RequireObject(fixture));
        if (pointGroup.ObjectType != ObjectType.Point_Group)
        {
            throw new InvalidDataException("point_group_fixture_required");
        }
        return ExecuteTarget(
            "merge_measurements_xml",
            () => client.MergeMeasurementsIntoXMLFile(
                disposableInput,
                pointGroup),
            () => originalHash.SequenceEqual(HashFile(input)) &&
                IsNonemptyFile(disposableInput),
            "fixture_copy_validation_failed");
    }

    private static Result TestPdf(ObjectiveSAClient client, string root)
    {
        var report = new CollectionItemName("", "BriosaDisposableReport", ItemType.SA_Report);
        client.MakeNewSAReport(report);
        var file = Path.Combine(root, "report.pdf");
        return ExecuteTarget(
            "output_report_pdf",
            () => client.OutputSAReportToPDF(report, file, showPDF: false),
            () => IsNonemptyFile(file),
            "output_not_created");
    }

    private static Result TestVstars(ObjectiveSAClient client, FixtureDescriptor fixture)
    {
        var input = RequireInputFile(fixture);
        return ExecuteTarget(
            "import_vstars_cameras",
            () => client.ImportVSTARSCameras(input),
            null,
            null);
    }

    private static Result TestPolyworks(ObjectiveSAClient client, FixtureDescriptor fixture)
    {
        var cloud = ToCollectionObject(RequireObject(fixture));
        if (cloud.ObjectType != ObjectType.Cloud)
        {
            throw new InvalidDataException("cloud_fixture_required");
        }
        var input = RequireInputFile(fixture);
        return ExecuteTarget(
            "import_polyworks",
            () => client.ImportPolyworksFile(cloud, input),
            null,
            null);
    }

    private static CollectionObjectName CreatePointGroup(ObjectiveSAClient client)
    {
        const string groupName = "BriosaDisposableFileTest";
        client.ConstructPointInWorkingCoordinates(
            new PointName("", groupName, "P1"),
            new Vector3(1.25f, 2.5f, 3.75f));
        client.ConstructPointInWorkingCoordinates(
            new PointName("", groupName, "P2"),
            new Vector3(4.5f, 5.75f, 6.125f));
        return new CollectionObjectName("", groupName, ObjectType.Point_Group);
    }

    private static void OpenDisposableFixtureJob(
        ObjectiveSAClient client,
        FixtureDescriptor fixture,
        string root)
    {
        if (string.IsNullOrWhiteSpace(fixture.JobPath))
        {
            throw new InvalidDataException("job_fixture_required");
        }

        var source = Path.GetFullPath(fixture.JobPath);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("job_fixture_not_found");
        }

        var disposableJob = Path.Combine(root, $"fixture-job{Path.GetExtension(source)}");
        File.Copy(source, disposableJob, overwrite: false);
        client.OpenSAFile(disposableJob);
    }

    private static byte[] HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    private static bool IsNonemptyFile(string file) =>
        File.Exists(file) && new FileInfo(file).Length > 0;

    private static Result ExecuteTarget(
        string scenario,
        Action command,
        Func<bool>? validate,
        string? validationFailure)
    {
        try
        {
            command();
        }
        catch (SDKFailureException)
        {
            return new Result(scenario, true, "completed", false, false, "not_checked", "mp_failure");
        }
        catch (SDKPartialSuccessException)
        {
            return new Result(scenario, true, "completed", false, false, "not_checked", "mp_partial_success");
        }
        catch (Exception)
        {
            return new Result(
                scenario,
                true,
                "completion_unknown",
                false,
                false,
                "not_checked",
                "target_completion_unknown");
        }

        if (validate is null)
        {
            return new Result(scenario, true, "completed", true, true, "not_checked", null);
        }

        try
        {
            var validationSucceeded = validate();
            return new Result(
                scenario,
                true,
                "completed",
                true,
                validationSucceeded,
                validationSucceeded ? "passed" : "failed",
                validationSucceeded ? null : validationFailure);
        }
        catch (Exception)
        {
            return new Result(
                scenario,
                true,
                "completed",
                true,
                false,
                "failed",
                "validation_failed");
        }
    }

    private static Result ExecuteExpectedFailure(
        string scenario,
        Action command,
        Func<bool> validate,
        string validationFailure)
    {
        try
        {
            command();
            return new Result(
                scenario,
                true,
                "completed",
                true,
                false,
                "failed",
                "unexpected_mp_success");
        }
        catch (SDKFailureException)
        {
            try
            {
                var validationSucceeded = validate();
                return new Result(
                    scenario,
                    true,
                    "completed",
                    false,
                    validationSucceeded,
                    validationSucceeded ? "passed" : "failed",
                    validationSucceeded ? "expected_mp_failure" : validationFailure);
            }
            catch (Exception)
            {
                return new Result(
                    scenario,
                    true,
                    "completed",
                    false,
                    false,
                    "failed",
                    "validation_failed");
            }
        }
        catch (SDKPartialSuccessException)
        {
            return new Result(
                scenario,
                true,
                "completed",
                false,
                false,
                "not_checked",
                "unexpected_mp_partial_success");
        }
        catch (Exception)
        {
            return new Result(
                scenario,
                true,
                "completion_unknown",
                false,
                false,
                "not_checked",
                "target_completion_unknown");
        }
    }

    private static FixtureDescriptor RequireFixture(FixtureDescriptor? fixture) =>
        fixture ?? throw new InvalidDataException("fixture_descriptor_required");

    private static FixtureObject RequireObject(FixtureDescriptor fixture) =>
        fixture.Object ?? throw new InvalidDataException("object_fixture_required");

    private static string RequireInputFile(FixtureDescriptor fixture)
    {
        if (string.IsNullOrWhiteSpace(fixture.InputPath))
        {
            throw new InvalidDataException("input_file_fixture_required");
        }

        var input = Path.GetFullPath(fixture.InputPath);
        if (!File.Exists(input))
        {
            throw new FileNotFoundException("input_file_fixture_not_found");
        }

        return input;
    }

    private static CollectionObjectName ToCollectionObject(FixtureObject value) =>
        new(
            value.CollectionName ?? "",
            RequireName(value),
            ParseDefinedEnum<ObjectType>(RequireType(value)));

    private static CollectionItemName ToCollectionItem(FixtureObject value) =>
        new(
            value.CollectionName ?? "",
            RequireName(value),
            ParseDefinedEnum<ItemType>(RequireType(value)));

    private static T ParseDefinedEnum<T>(string value)
        where T : struct, Enum
    {
        if (long.TryParse(value, out _) ||
            !Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new InvalidDataException("fixture_object_type_invalid");
        }

        return parsed;
    }

    private static string RequireName(FixtureObject value) =>
        !string.IsNullOrWhiteSpace(value.Name)
            ? value.Name
            : throw new InvalidDataException("fixture_object_name_required");

    private static string RequireType(FixtureObject value) =>
        !string.IsNullOrWhiteSpace(value.Type)
            ? value.Type
            : throw new InvalidDataException("fixture_object_type_required");

    private static void Emit(Result value)
    {
        Console.WriteLine(JsonSerializer.Serialize(value));
        Console.Out.Flush();
    }

    private sealed record Result(
        string Scenario,
        bool RunnerCompleted,
        string ExecutionDisposition,
        bool MpSucceeded,
        bool ExpectationMet,
        string PostconditionStatus,
        string? Diagnostic);

}
