using System.Text.Json;
using System.Xml.Linq;

namespace Briosa.Server.Tests;

public sealed class LocalDevelopmentHostContractTests
{
    [Fact]
    public void SpatialAnalyzerIsTheOnlyAndDefaultLaunchProfile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var settingsPath = Path.Combine(
            repositoryRoot,
            "src",
            "Briosa.Server",
            "Properties",
            "launchSettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var profiles = document.RootElement.GetProperty("profiles");
        var profileProperties = profiles.EnumerateObject().ToArray();

        var profile = Assert.Single(profileProperties);
        Assert.Equal("SpatialAnalyzer", profile.Name);
        Assert.Equal("Project", profile.Value.GetProperty("commandName").GetString());
        Assert.False(profile.Value.TryGetProperty("applicationUrl", out _));
        Assert.False(profile.Value.TryGetProperty("commandLineArgs", out _));
        var profileNames = profile.Value.EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();
        Assert.Equal(["commandName", "environmentVariables"], profileNames);

        var variables = profile.Value.GetProperty("environmentVariables")
            .EnumerateObject()
            .ToArray();
        var variable = Assert.Single(variables);
        Assert.Equal("ASPNETCORE_ENVIRONMENT", variable.Name);
        Assert.Equal("Development", variable.Value.GetString());
    }

    [Fact]
    public void WorkerCompositionAndUserSecretsAreDebugOnly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "Briosa.Server",
            "Briosa.Server.csproj");
        var project = XDocument.Load(projectPath);
        var userSecrets = project.Descendants("UserSecretsId").ToArray();
        var workerReferences = project.Descendants("ProjectReference")
            .Where(element => string.Equals(
                Path.GetFileName(element.Attribute("Include")?.Value),
                "Briosa.Worker.csproj",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var copyTargets = project.Descendants("Target")
            .Where(element => string.Equals(
                element.Attribute("Name")?.Value,
                "CopyDevelopmentWorkerCohort",
                StringComparison.Ordinal))
            .ToArray();

        AssertDebugOnly(Assert.Single(userSecrets).Parent);
        AssertDebugOnly(Assert.Single(workerReferences).Parent);
        AssertDebugOnly(Assert.Single(copyTargets));
        Assert.False(File.Exists(Path.Combine(
            repositoryRoot,
            "src",
            "Briosa.Server",
            "appsettings.Development.json")));
    }

    private static void AssertDebugOnly(XElement? element)
    {
        Assert.NotNull(element);
        Assert.Equal(
            "'$(Configuration)' == 'Debug'",
            element.Attribute("Condition")?.Value);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("The Briosa repository root was not found.");
    }
}
