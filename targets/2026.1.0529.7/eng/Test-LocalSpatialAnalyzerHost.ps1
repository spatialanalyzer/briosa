[CmdletBinding()]
param(
    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $repositoryRoot "src\Briosa.Server\Briosa.Server.csproj"
$serverTests = Join-Path $repositoryRoot "tests\Briosa.Server.Tests\Briosa.Server.Tests.csproj"
$serverOutput = Join-Path $repositoryRoot "src\Briosa.Server\bin\Debug\net10.0-windows"
$workerOutput = Join-Path $repositoryRoot "src\Briosa.Worker\bin\Debug\net10.0-windows"
$launchSettingsPath = Join-Path $repositoryRoot "src\Briosa.Server\Properties\launchSettings.json"

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$launchSettings = Get-Content -LiteralPath $launchSettingsPath -Raw | ConvertFrom-Json
$profileNames = @($launchSettings.profiles.PSObject.Properties.Name)
Assert-Condition `
    -Condition ($profileNames.Count -eq 1 -and $profileNames[0] -ceq "SpatialAnalyzer") `
    -Message "SpatialAnalyzer must be the first and only local launch profile."
$profile = $launchSettings.profiles.SpatialAnalyzer
$profilePropertyNames = @($profile.PSObject.Properties.Name)
Assert-Condition `
    -Condition (($profilePropertyNames -join ',') -ceq "commandName,environmentVariables") `
    -Message "The SpatialAnalyzer profile may contain only commandName and environmentVariables."
Assert-Condition `
    -Condition ($profile.commandName -ceq "Project") `
    -Message "The SpatialAnalyzer profile must use the Project command."
$environmentNames = @($profile.environmentVariables.PSObject.Properties.Name)
Assert-Condition `
    -Condition ($environmentNames.Count -eq 1 -and $environmentNames[0] -ceq "ASPNETCORE_ENVIRONMENT") `
    -Message "The SpatialAnalyzer profile may set only ASPNETCORE_ENVIRONMENT."
Assert-Condition `
    -Condition ($profile.environmentVariables.ASPNETCORE_ENVIRONMENT -ceq "Development") `
    -Message "The SpatialAnalyzer profile must select the Development environment."

if (-not $NoRestore) {
    Invoke-DotNet @("restore", $serverProject, "--locked-mode")
    Invoke-DotNet @("restore", $serverTests, "--locked-mode")
}

Invoke-DotNet @("build", $serverProject, "-c", "Debug", "--no-restore")

$requiredWorkerFiles = @(
    "Briosa.Worker.exe",
    "Briosa.Worker.dll",
    "Briosa.Worker.deps.json",
    "Briosa.Worker.runtimeconfig.json",
    "Briosa.Worker.Control.dll",
    "Briosa.SpatialAnalyzer.Interop.dll"
)
foreach ($fileName in $requiredWorkerFiles) {
    Assert-Condition `
        -Condition (Test-Path -LiteralPath (Join-Path $serverOutput $fileName) -PathType Leaf) `
        -Message "The Debug server output is missing required worker cohort file '$fileName'."
}

$workerSettings = @(Get-ChildItem -LiteralPath $workerOutput -Filter "appsettings*.json" -File -Recurse)
Assert-Condition `
    -Condition ($workerSettings.Count -eq 0) `
    -Message "Worker appsettings files require explicit review before source-host composition."

$workerFiles = @(Get-ChildItem -LiteralPath $workerOutput -File -Recurse)
Assert-Condition `
    -Condition ($workerFiles.Count -gt 0) `
    -Message "The Debug worker output cohort is empty."
foreach ($workerFile in $workerFiles) {
    $relativePath = [IO.Path]::GetRelativePath($workerOutput, $workerFile.FullName)
    $serverFile = Join-Path $serverOutput $relativePath
    Assert-Condition `
        -Condition (Test-Path -LiteralPath $serverFile -PathType Leaf) `
        -Message "The Debug server output omitted worker cohort file '$relativePath'."
    $workerHash = (Get-FileHash -LiteralPath $workerFile.FullName -Algorithm SHA256).Hash
    $serverHash = (Get-FileHash -LiteralPath $serverFile -Algorithm SHA256).Hash
    Assert-Condition `
        -Condition ($workerHash -ceq $serverHash) `
        -Message "The Debug server worker cohort differs at '$relativePath'."
}

$workerOutputVariable = "BRIOSA_SOURCE_WORKER_OUTPUT"
$previousWorkerOutput = [Environment]::GetEnvironmentVariable($workerOutputVariable)
try {
    [Environment]::SetEnvironmentVariable($workerOutputVariable, $serverOutput)
    Invoke-DotNet @(
        "test",
        $serverTests,
        "-c", "Debug",
        "--no-restore",
        "--filter", "FullyQualifiedName~ProductionWorkerCompletesControlLifecycleWithoutSpatialAnalyzer")
}
finally {
    [Environment]::SetEnvironmentVariable($workerOutputVariable, $previousWorkerOutput)
}

Write-Host (
    "The default launch profile, complete Debug worker cohort, SDK-disabled source worker " +
    "lifecycle, and graceful cleanup contracts passed without contacting SpatialAnalyzer.")
