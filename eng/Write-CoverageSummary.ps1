[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ResultsDirectory,
    [string[]]$RequiredAssemblies = @('Briosa.Desktop', 'Briosa.Server', 'Briosa.Worker', 'Briosa.Worker.Control')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter coverage.cobertura.xml -File -Recurse)
if ($reports.Count -eq 0) {
    throw 'No Coverlet coverage reports were produced.'
}

# Union source lines across test assemblies; adding report totals would count
# shared production assemblies several times. Keep each SA target separate.
$modules = @{}
foreach ($report in $reports) {
    [xml]$coverage = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($package in $coverage.SelectNodes('/coverage/packages/package')) {
        $moduleName = [string]$package.name
        if (-not $modules.ContainsKey($moduleName)) {
            $modules[$moduleName] = @{}
        }
        $lines = $modules[$moduleName]
        foreach ($class in $package.SelectNodes('classes/class')) {
            foreach ($line in $class.SelectNodes('lines/line')) {
                $key = '{0}:{1}' -f $class.filename, $line.number
                $covered = [long]$line.hits -gt 0
                $lines[$key] = $covered -or ($lines.ContainsKey($key) -and $lines[$key])
            }
        }
    }
}

foreach ($requiredAssembly in $RequiredAssemblies) {
    if (-not $modules.ContainsKey($requiredAssembly) -or $modules[$requiredAssembly].Count -eq 0) {
        throw "No measured source lines for required assembly '$requiredAssembly'. Check coverage instrumentation."
    }
}

'### Handwritten in-process line coverage'
''
'| Assembly | Covered lines | Measured lines | Coverage |'
'| --- | ---: | ---: | ---: |'
foreach ($moduleName in @($modules.Keys | Sort-Object)) {
    $lines = $modules[$moduleName]
    $covered = @($lines.Values | Where-Object { $_ }).Count
    $percentage = if ($lines.Count -eq 0) { 'n/a' } else {
        (100.0 * $covered / $lines.Count).ToString('F1', [Globalization.CultureInfo]::InvariantCulture) + '%'
    }
    '| {0} | {1} | {2} | {3} |' -f $moduleName, $covered, $lines.Count, $percentage
}
''
'Generated protocol code and test helpers are excluded. Child-process and licensed-SA execution are not measured by this collector; assemblies absent from the reports are not evidence of coverage. No minimum coverage threshold is enforced.'
