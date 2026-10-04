[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TargetDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Configuration = 'Release'
)

# Dumps the compiled operation registry of one exact-SA target as
# descriptors-<target>.json, the input of classify.py. Build the target first.
# The reflection reader is a throwaway console project created in a temporary
# directory outside the repository, so no project is added to the repository and
# no target references another.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$target = Get-Item -LiteralPath $TargetDirectory
$serverOutput = Join-Path $target.FullName "src/Briosa.Server/bin/$Configuration/net10.0-windows"
if (-not (Test-Path -LiteralPath (Join-Path $serverOutput 'Briosa.Server.dll') -PathType Leaf)) {
    throw "Build '$($target.Name)' in $Configuration first; '$serverOutput' has no Briosa.Server.dll."
}
$null = New-Item -ItemType Directory -Force -Path $OutputDirectory
$output = Join-Path (Resolve-Path -LiteralPath $OutputDirectory) "descriptors-$($target.Name).json"

$reader = Join-Path ([IO.Path]::GetTempPath()) "briosa-descriptor-export-$([guid]::NewGuid().ToString('n'))"
$null = New-Item -ItemType Directory -Path $reader
try {
    Set-Content -LiteralPath (Join-Path $reader 'reader.csproj') -Encoding utf8 -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
'@
    Set-Content -LiteralPath (Join-Path $reader 'Program.cs') -Encoding utf8 -Value @'
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

var directory = args[0];
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    var path = Path.Combine(directory, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, "Briosa.Server.dll"));
var api = assembly.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", throwOnError: true)!;
var operations = (System.Collections.IEnumerable)api
    .GetProperty("Operations", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
    .GetValue(null)!;
var rows = new List<Dictionary<string, object?>>();
foreach (var operation in operations)
{
    object? Get(string property) => operation.GetType().GetProperty(property)!.GetValue(operation);
    rows.Add(new()
    {
        ["id"] = Get("OperationId"),
        ["step"] = Get("MpStep"),
        ["service"] = Get("GrpcService"),
        ["rpc"] = Get("Rpc"),
        ["effect"] = Get("EffectLabel"),
        ["scope"] = Get("ExecutionScope")!.ToString(),
        ["replay"] = Get("ReplaySafety")!.ToString(),
        ["flags"] = ((IEnumerable<string>)Get("RiskFlags")!).ToArray(),
    });
}
File.WriteAllText(args[1], JsonSerializer.Serialize(rows));
Console.WriteLine($"Wrote {rows.Count} descriptors to {args[1]}.");
'@
    & dotnet run --project (Join-Path $reader 'reader.csproj') --configuration Release -- $serverOutput $output
    if ($LASTEXITCODE -ne 0) {
        throw "The descriptor reader failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item -LiteralPath $reader -Recurse -Force -ErrorAction SilentlyContinue
}
