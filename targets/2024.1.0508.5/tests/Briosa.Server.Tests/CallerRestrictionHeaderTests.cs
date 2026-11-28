using Briosa.Server.Security;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Briosa.Server.Tests;

/// <summary>
/// The strict grammar of the <c>briosa-caller-restriction</c> request header (#443):
/// every accepted form, and every malformed form rejected rather than ignored.
/// </summary>
public sealed class CallerRestrictionHeaderTests
{
    /// <summary>
    /// Malformed values a gRPC client can send. Each one rejects the call before
    /// policy evaluation or dispatch.
    /// </summary>
    internal static IReadOnlyList<(string Name, string Value)> MalformedValues { get; } =
    [
        ("empty", ""),
        ("no separator space", "profile=read-only;deny=interactive_ui"),
        ("two separator spaces", "profile=read-only;  deny=interactive_ui"),
        ("space before separator", "profile=read-only ; deny=interactive_ui"),
        ("space before equals", "profile =read-only"),
        ("space after equals", "profile= read-only"),
        ("space after comma", "deny=interactive_ui, filesystem_write"),
        ("leading space", " profile=read-only"),
        ("trailing space", "profile=read-only "),
        ("tab separator", "profile=read-only;\tdeny=interactive_ui"),
        ("trailing separator", "profile=read-only; "),
        ("trailing semicolon", "profile=read-only;"),
        ("leading separator", "; profile=read-only"),
        ("empty clause", "profile=read-only; ; deny=interactive_ui"),
        ("uppercase key", "Profile=read-only"),
        ("uppercase profile", "profile=Read-Only"),
        ("uppercase flag", "deny=Interactive_UI"),
        ("all uppercase", "DENY=INTERACTIVE_UI"),
        ("unknown profile", "profile=readonly"),
        ("profile with underscore", "profile=read_only"),
        ("unknown flag", "deny=unknown_flag"),
        ("hyphenated flag", "deny=interactive-ui"),
        ("unknown key", "allow=interactive_ui"),
        ("unknown key beside valid", "profile=read-only; allow=interactive_ui"),
        ("repeated profile key", "profile=read-only; profile=standard"),
        ("repeated equal profile key", "profile=read-only; profile=read-only"),
        ("repeated deny key", "deny=interactive_ui; deny=destructive"),
        ("repeated flag", "deny=interactive_ui,interactive_ui"),
        ("key without value", "profile"),
        ("empty profile", "profile="),
        ("empty deny list", "deny="),
        ("trailing comma", "deny=interactive_ui,"),
        ("leading comma", "deny=,interactive_ui"),
        ("double comma", "deny=interactive_ui,,destructive"),
        ("second equals", "profile=read-only=standard"),
        ("value without key", "=read-only"),
        ("comma between clauses", "profile=read-only,deny=interactive_ui"),
        ("joined repeated header", "profile=read-only, deny=interactive_ui"),
        ("joined fragment", "deny=interactive_ui, filesystem_write"),
        ("digit", "profile=read-only1"),
        ("punctuation", "profile=read-only."),
        ("quoted", "profile=\"read-only\""),
        ("over the length bound", "deny=" + string.Join(',', Enumerable.Repeat("interactive_ui", 20)))
    ];

    public static TheoryData<string, string> Malformed()
    {
        var data = new TheoryData<string, string>();
        foreach (var (name, value) in MalformedValues)
        {
            data.Add(name, value);
        }

        // Characters a gRPC ASCII header cannot carry, for the parser alone.
        data.Add("non-ASCII letter", "profile=read-onlyé");
        data.Add("non-breaking space", "profile=read-only; deny=interactive_ui");
        data.Add("control character", "profile=read-only\u0000");
        return data;
    }

    [Theory]
    [MemberData(nameof(Malformed))]
    public void AMalformedValueParsesToNothing(string name, string value)
    {
        Assert.True(OperationCallerRestriction.Parse(value) is null, name);
        if (value.All(char.IsAscii))
        {
            var read = OperationCallerRestriction.Read(new Metadata { { OperationCallerRestriction.HeaderName, value } });
            Assert.True(read.IsMalformed, name);
            Assert.Null(read.Restriction);
        }
    }

    [Theory]
    [InlineData("profile=read-only", "read-only", (int)OperationRisks.None)]
    [InlineData("profile=standard", "standard", (int)OperationRisks.None)]
    [InlineData("profile=device", "device", (int)OperationRisks.None)]
    [InlineData("profile=full", "full", (int)OperationRisks.None)]
    [InlineData("deny=interactive_ui", null, (int)OperationRisks.InteractiveUi)]
    [InlineData("profile=read-only; deny=interactive_ui", "read-only", (int)OperationRisks.InteractiveUi)]
    [InlineData("profile=read-only; deny=interactive_ui,filesystem_write", "read-only",
        (int)(OperationRisks.InteractiveUi | OperationRisks.FilesystemWrite))]
    [InlineData("deny=filesystem_write,interactive_ui; profile=read-only", "read-only",
        (int)(OperationRisks.InteractiveUi | OperationRisks.FilesystemWrite))]
    public void AValidValueParsesToItsProfileAndFlags(string value, string? profile, int flags)
    {
        var restriction = OperationCallerRestriction.Parse(value);

        Assert.NotNull(restriction);
        Assert.Equal(profile, restriction.Profile?.Name);
        Assert.Equal((OperationRisks)flags, restriction.DeniedFlags);
        Assert.Equal(restriction, OperationCallerRestriction.Parse(restriction.Format()));
    }

    [Fact]
    public void EveryProfileAndEveryFlagSetRoundTripsThroughTheCanonicalForm()
    {
        var profiles = OperationAdmissionProfile.All.Cast<OperationAdmissionProfile?>().Prepend(null).ToArray();
        var count = 0;
        foreach (var profile in profiles)
        {
            for (var mask = 0; mask < 1 << OperationRiskVocabulary.Members.Count; mask++)
            {
                var flags = OperationRiskVocabulary.Members
                    .Where((_, index) => (mask & (1 << index)) != 0)
                    .Aggregate(OperationRisks.None, (all, flag) => all | flag);
                if (profile is null && flags == OperationRisks.None)
                {
                    Assert.Throws<ArgumentException>(() => OperationCallerRestriction.Create(profile, flags));
                    continue;
                }

                var restriction = OperationCallerRestriction.Create(profile, flags);
                var text = restriction.Format();
                Assert.True(text.Length <= OperationCallerRestriction.MaximumLength, text);
                Assert.Equal(restriction, OperationCallerRestriction.Parse(text));

                // The flags in reverse order and the clauses swapped mean the same.
                var reversed = string.Join("; ", new[]
                {
                    flags == OperationRisks.None
                        ? null
                        : "deny=" + string.Join(',', OperationRiskVocabulary.GetNames(flags).Reverse()),
                    profile is null ? null : $"profile={profile.Name}"
                }.OfType<string>());
                Assert.Equal(restriction, OperationCallerRestriction.Parse(reversed));
                count++;
            }
        }

        // Five profile choices, each with every subset of the eleven flags, less the empty restriction.
        Assert.Equal((5 * 2048) - 1, count);
        Assert.Equal(193, OperationCallerRestriction.Create(
            OperationAdmissionProfile.ReadOnly, OperationRiskVocabulary.All).Format().Length);
    }

    [Fact]
    public void NoInputMakesTheParserThrow()
    {
        // A fixed linear congruential sequence: the same inputs on every run.
        var state = 443u;
        int Next(int bound)
        {
            state = unchecked((state * 1664525u) + 1013904223u);
            return (int)((state >> 8) % (uint)bound);
        }

        const string alphabet = "abcdefghijklmnopqrstuvwxyz_-=;, \tA0é";
        var accepted = 0;
        for (var index = 0; index < 20_000; index++)
        {
            var seed = index % 2 == 0
                ? "profile=read-only; deny=interactive_ui,filesystem_write"
                : string.Empty;
            var characters = seed.ToList();
            for (var edit = 1 + Next(3); edit > 0; edit--)
            {
                var position = Next(characters.Count + 1);
                switch (Next(3))
                {
                    case 0:
                        characters.Insert(position, alphabet[Next(alphabet.Length)]);
                        break;
                    case 1 when position < characters.Count:
                        characters.RemoveAt(position);
                        break;
                    default:
                        if (position < characters.Count)
                        {
                            characters[position] = alphabet[Next(alphabet.Length)];
                        }

                        break;
                }
            }

            // Anything accepted is a real restriction whose canonical form parses back.
            if (OperationCallerRestriction.Parse(new string([.. characters])) is { } restriction)
            {
                Assert.Equal(restriction, OperationCallerRestriction.Parse(restriction.Format()));
                accepted++;
            }
        }

        Assert.True(accepted < 20_000);
    }

    [Fact]
    public void ReadDistinguishesAbsentValidAndMalformed()
    {
        Assert.Equal(OperationCallerRestrictionHeader.Absent, OperationCallerRestriction.Read((Metadata?)null));
        Assert.Equal(OperationCallerRestrictionHeader.Absent, OperationCallerRestriction.Read([]));
        var unrelated = OperationCallerRestriction.Read(new Metadata
        {
            { "briosa-caller-restrictions", "profile=read-only" },
            { "x-caller-restriction", "profile=read-only" }
        });
        Assert.Equal(OperationCallerRestrictionHeader.Absent, unrelated);
        Assert.False(unrelated.IsMalformed);
        Assert.Null(unrelated.Restriction);

        var valid = OperationCallerRestriction.Read(new Metadata
        {
            { "user-agent", "test" },
            { OperationCallerRestriction.HeaderName, "profile=read-only; deny=interactive_ui" }
        });
        Assert.False(valid.IsMalformed);
        Assert.Equal(OperationAdmissionProfile.ReadOnly, valid.Restriction!.Profile);
        Assert.Equal(OperationRisks.InteractiveUi, valid.Restriction.DeniedFlags);

        // More than one entry, even two equal valid ones, and a binary entry are malformed.
        foreach (var headers in new[]
        {
            new Metadata
            {
                { OperationCallerRestriction.HeaderName, "profile=read-only" },
                { OperationCallerRestriction.HeaderName, "profile=read-only" }
            },
            new Metadata
            {
                { OperationCallerRestriction.HeaderName, "profile=read-only" },
                { OperationCallerRestriction.HeaderName, "deny=interactive_ui" }
            },
            new Metadata { { OperationCallerRestriction.HeaderName + "-bin", [1, 2, 3] } },
            new Metadata
            {
                { OperationCallerRestriction.HeaderName, "profile=read-only" },
                { OperationCallerRestriction.HeaderName + "-bin", [1] }
            }
        })
        {
            var read = OperationCallerRestriction.Read(headers);
            Assert.True(read.IsMalformed);
            Assert.Null(read.Restriction);
        }
    }

    [Fact]
    public void RawHeadersAreReadByNameWithoutDecodingAnything()
    {
        Assert.Equal(OperationCallerRestrictionHeader.Absent, OperationCallerRestriction.Read(new HeaderDictionary()));

        // Unrelated headers, including a binary one that is not base64, are never read.
        Assert.Equal(OperationCallerRestrictionHeader.Absent, OperationCallerRestriction.Read(new HeaderDictionary
        {
            ["x-unrelated-bin"] = "!!not base64!!",
            ["x-unrelated"] = new StringValues(["a", "b"])
        }));

        var valid = OperationCallerRestriction.Read(new HeaderDictionary
        {
            ["x-unrelated-bin"] = "!!not base64!!",
            [OperationCallerRestriction.HeaderName] = "profile=read-only; deny=interactive_ui"
        });
        Assert.False(valid.IsMalformed);
        Assert.Equal(OperationAdmissionProfile.ReadOnly, valid.Restriction!.Profile);
        Assert.Equal(OperationRisks.InteractiveUi, valid.Restriction.DeniedFlags);

        foreach (var headers in new[]
        {
            // Two header fields are two values, even when joining them would parse.
            new HeaderDictionary
            {
                [OperationCallerRestriction.HeaderName] =
                    new StringValues(["profile=read-only; deny=interactive_ui", "filesystem_write"])
            },
            new HeaderDictionary { [OperationCallerRestriction.HeaderName] = string.Empty },
            // The binary name is malformed by its presence; its value is never decoded.
            new HeaderDictionary { [OperationCallerRestriction.HeaderName + "-bin"] = "!!not base64!!" },
            new HeaderDictionary
            {
                [OperationCallerRestriction.HeaderName] = "profile=read-only",
                [OperationCallerRestriction.HeaderName + "-bin"] = "cHJvZmlsZT1yZWFkLW9ubHk="
            }
        })
        {
            var read = OperationCallerRestriction.Read(headers);
            Assert.True(read.IsMalformed);
            Assert.Null(read.Restriction);
        }
    }

    [Fact]
    public void ACallContextNotHostedByAspNetCoreIsReadFromItsMetadata()
    {
        var absent = new InMemoryServerCallContext("/briosa.FileOperations/GetWorkingDirectory", null, CancellationToken.None);
        Assert.Equal(OperationCallerRestrictionHeader.Absent, OperationCallerRestriction.Read(absent));

        var restricted = new InMemoryServerCallContext("/briosa.FileOperations/GetWorkingDirectory", null, CancellationToken.None);
        restricted.RequestHeaders.Add(OperationCallerRestriction.HeaderName, "deny=interactive_ui");
        Assert.Equal(OperationRisks.InteractiveUi, OperationCallerRestriction.Read(restricted).Restriction!.DeniedFlags);

        var repeated = new InMemoryServerCallContext("/briosa.FileOperations/GetWorkingDirectory", null, CancellationToken.None);
        repeated.RequestHeaders.Add(OperationCallerRestriction.HeaderName, "deny=interactive_ui");
        repeated.RequestHeaders.Add(OperationCallerRestriction.HeaderName, "deny=interactive_ui");
        Assert.True(OperationCallerRestriction.Read(repeated).IsMalformed);
    }

    [Fact]
    public void CreateRejectsAnEmptyOrUnreviewedRestriction()
    {
        Assert.Throws<ArgumentException>(() => OperationCallerRestriction.Create(null, OperationRisks.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationCallerRestriction.Create(null, (OperationRisks)(1 << 20)));
        Assert.Throws<ArgumentException>(() => OperationCallerRestriction.Create(
            new OperationAdmissionProfile("read-only", OperationRisks.FilesystemMetadata | OperationRisks.InteractiveUi,
                ReadOnlyEffectOnly: true),
            OperationRisks.None));
    }
}
