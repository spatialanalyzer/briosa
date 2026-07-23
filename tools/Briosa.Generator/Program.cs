using System.Text;
using System.Text.Json;
using Briosa.Generator;

return args switch
{
    ["catalog-generate", var catalogRoot, var outputRoot] =>
        GenerateCatalog(catalogRoot, outputRoot),
    ["mp-inventory-extract", var saTarget, var documentationRoot, var sdkCodeRoot, var outputRoot] =>
        ExtractMpInventory(saTarget, documentationRoot, sdkCodeRoot, outputRoot),
    ["disposition-sync", var inventoryPath, var targetDirectory] =>
        SyncDispositions(inventoryPath, targetDirectory),
    ["disposition-validate", var inventoryPath, var targetDirectory] =>
        ValidateDispositions(inventoryPath, targetDirectory),
    ["binding-registry-sync", var inventoryPath, var dispositionDirectory,
        var interopDirectory, var targetDirectory] =>
        SyncBindingRegistry(
            inventoryPath,
            dispositionDirectory,
            interopDirectory,
            targetDirectory),
    ["binding-registry-validate", var inventoryPath, var dispositionDirectory,
        var interopDirectory, var targetDirectory] =>
        ValidateBindingRegistry(
            inventoryPath,
            dispositionDirectory,
            interopDirectory,
            targetDirectory),
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

static int ExtractMpInventory(
    string spatialAnalyzerTarget,
    string documentationRoot,
    string sdkCodeRoot,
    string outputRoot)
{
    var files = MpCommandInventoryExtractor.Write(
        spatialAnalyzerTarget,
        documentationRoot,
        sdkCodeRoot,
        outputRoot);
    foreach (var file in files)
    {
        Console.WriteLine(file);
    }

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

static int SyncDispositions(string inventoryPath, string targetDirectory)
{
    var result = CommandDispositionLedger.Sync(inventoryPath, targetDirectory);
    foreach (var file in result.Files)
    {
        Console.WriteLine(file);
    }

    Console.WriteLine(
        $"Synchronized {result.EntryCount} disposition(s): " +
        $"{result.NewEntryCount} new, {result.ReReviewCount} requiring re-review.");
    return 0;
}

static int ValidateDispositions(string inventoryPath, string targetDirectory)
{
    var result = CommandDispositionLedger.Validate(inventoryPath, targetDirectory);
    foreach (var error in result.Errors)
    {
        Console.Error.WriteLine(error);
    }

    if (!result.IsValid)
    {
        Console.Error.WriteLine(
            $"Disposition validation failed with {result.Errors.Count} error(s).");
        return 1;
    }

    Console.WriteLine(
        $"Validated {result.EntryCount} disposition(s) in " +
        $"{result.TargetCount} exact-target ledger(s).");
    return 0;
}

static int SyncBindingRegistry(
    string inventoryPath,
    string dispositionDirectory,
    string interopDirectory,
    string targetDirectory)
{
    var result = SdkBindingRegistry.Sync(
        inventoryPath,
        dispositionDirectory,
        interopDirectory,
        targetDirectory);
    foreach (var file in result.Files)
    {
        Console.WriteLine(file);
    }

    Console.WriteLine(
        $"Synchronized {result.BindingCount} binding(s) across " +
        $"{result.ValueFamilyCount} semantic value families.");
    return 0;
}

static int ValidateBindingRegistry(
    string inventoryPath,
    string dispositionDirectory,
    string interopDirectory,
    string targetDirectory)
{
    var result = SdkBindingRegistry.Validate(
        inventoryPath,
        dispositionDirectory,
        interopDirectory,
        targetDirectory);
    foreach (var error in result.Errors)
    {
        Console.Error.WriteLine(error);
    }

    if (!result.IsValid)
    {
        Console.Error.WriteLine(
            $"Binding registry validation failed with {result.Errors.Count} error(s).");
        return 1;
    }

    Console.WriteLine(
        $"Validated {result.BindingCount} binding(s) across " +
        $"{result.ValueFamilyCount} semantic value families.");
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
    Console.Error.WriteLine(
        "  Briosa.Generator disposition-sync <inventory-path> <target-directory>");
    Console.Error.WriteLine(
        "  Briosa.Generator disposition-validate <inventory-path> <target-directory>");
    Console.Error.WriteLine(
        "  Briosa.Generator binding-registry-sync <inventory-path> " +
        "<disposition-directory> <interop-directory> <target-directory>");
    Console.Error.WriteLine(
        "  Briosa.Generator binding-registry-validate <inventory-path> " +
        "<disposition-directory> <interop-directory> <target-directory>");
    Console.Error.WriteLine("  Briosa.Generator mp-inventory-extract <sa-target> <documentation-root> <sdk-code-root> <output-root>");
    Console.Error.WriteLine("  Briosa.Generator interop-api <assembly-path> [output-path]");
    Console.Error.WriteLine("  Briosa.Generator typelib-info <type-library-path>");
    return 1;
}
