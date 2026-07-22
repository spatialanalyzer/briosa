using System.Text;
using System.Text.Json;
using Briosa.Generator;

return args switch
{
    ["catalog-generate", var catalogRoot, var outputRoot] =>
        GenerateCatalog(catalogRoot, outputRoot),
    ["catalog-validate", var catalogRoot] => ValidateCatalog(catalogRoot),
    ["interop-api", var assemblyPath, var outputPath] => WriteInteropApi(assemblyPath, outputPath),
    ["interop-api", var assemblyPath] => WriteInteropApi(assemblyPath, null),
    ["typelib-info", var typeLibraryPath] => WriteTypeLibraryInfo(typeLibraryPath),
    _ => ShowUsage()
};

static int GenerateCatalog(string catalogRoot, string outputRoot)
{
    var result = CommandCatalogGenerator.Generate(catalogRoot, outputRoot);
    foreach (var file in result.Files)
    {
        Console.WriteLine(file);
    }

    Console.WriteLine($"Generated {result.Files.Count} catalog artifact(s).");
    return 0;
}

static int ValidateCatalog(string catalogRoot)
{
    var result = CommandCatalogValidator.ValidateDirectory(catalogRoot);
    foreach (var error in result.Errors)
    {
        Console.Error.WriteLine(error);
    }

    if (!result.IsValid)
    {
        Console.Error.WriteLine($"Catalog validation failed with {result.Errors.Count} error(s).");
        return 1;
    }

    Console.WriteLine(
        $"Validated {result.OperationCount} operation(s) in {result.CatalogCount} exact-target catalog(s).");
    return 0;
}

static int WriteInteropApi(string assemblyPath, string? outputPath)
{
    var manifest = InteropApiManifest.Create(assemblyPath);

    if (outputPath is null)
    {
        Console.Write(manifest);
    }
    else
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, manifest, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    return 0;
}

static int WriteTypeLibraryInfo(string typeLibraryPath)
{
    if (!OperatingSystem.IsWindows())
    {
        Console.Error.WriteLine("Type-library inspection is supported only on Windows.");
        return 2;
    }

    var metadata = TypeLibraryMetadata.Read(typeLibraryPath);
    Console.WriteLine(JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

static int ShowUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Briosa.Generator catalog-generate <catalog-root> <output-root>");
    Console.Error.WriteLine("  Briosa.Generator catalog-validate <catalog-root>");
    Console.Error.WriteLine("  Briosa.Generator interop-api <assembly-path> [output-path]");
    Console.Error.WriteLine("  Briosa.Generator typelib-info <type-library-path>");
    return 1;
}
