# StrykerReportAnalyzer.ps1 — Per-File & Per-Engine Mutation Score Analysis (TEST-013 / GEX-014)
#
# Parses Stryker.NET mutation-report.json and:
# 1. Computes mutation score per file (Killed / Total mutants)
# 2. Groups files by engine (Combat, Movement, InputRouter, etc.)
# 3. Enforces per-engine floor gates (Combat ≥80%, Movement ≥80%, InputRouter ≥85%)
# 4. Generates "Hardening Candidates" report (Top N survivors by file)
#
# Exit codes:
#   0 — All per-engine floors met
#   1 — One or more per-engine floors violated

param(
    [Parameter(Mandatory=$true)]
    [string]$ReportPath,

    [Parameter(Mandatory=$false)]
    [int]$TopN = 10,

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "StrykerOutput/stryker-per-file-report.md"
)

$ErrorActionPreference = "Stop"

# ── 1. Load mutation report ───────────────────────────────────────────────
if (-not (Test-Path $ReportPath)) {
    Write-Error "Mutation report not found: $ReportPath"
    exit 1
}

$report = Get-Content $ReportPath -Raw | ConvertFrom-Json

# ── 2. Engine classification from file path ───────────────────────────────
function Get-EngineFromPath($filePath) {
    $lower = $filePath.ToLower()
    if ($lower -match "combate|mage|autotarget|combo|groundspell|skillorchestrator|buffmanager|cooldownmanager|itemmanager|partymanager|mobsweep") {
        return "Combat"
    }
    if ($lower -match "movement|kite|cursor|smartcursor") {
        return "Movement"
    }
    if ($lower -match "inputrouter|parsedinput|buttonstate|snapshotbuilder|inputcommandqueue") {
        return "InputRouter"
    }
    return "Other"
}

# Per-engine floor gates (GEX-014 DoD)
$engineFloors = @{
    "Combat"     = 80
    "Movement"   = 80
    "InputRouter" = 85
}

# ── 3. Collect per-file results ───────────────────────────────────────────
$fileResults = @{}
$mutants = $report.mutants

if ($null -eq $mutants -or $mutants.Count -eq 0) {
    Write-Warning "No mutants found in report"
    exit 0
}

foreach ($m in $mutants) {
    $file = $m.SourceFilePath
    if (-not $file) { continue }

    if (-not $fileResults.ContainsKey($file)) {
        $fileResults[$file] = @{
            Killed = 0
            Total  = 0
            Survivors = @()
        }
    }

    $fileResults[$file].Total++

    if ($m.Status -eq "Killed") {
        $fileResults[$file].Killed++
    } elseif ($m.Status -eq "Survived" -or $m.Status -eq "Timeout") {
        $fileResults[$file].Survivors += @{
            MutantId = $m.Id
            Description = $m.Description
            Line = $m.Lines.FirstOrDefault()
            Replacement = $m.Replacement
        }
    }
}

# ── 4. Compute per-engine scores & check floors ───────────────────────────
$engineStats = @{}
$floorViolations = @()

foreach ($file in $fileResults.Keys) {
    $engine = Get-EngineFromPath $file
    $stats = $fileResults[$file]
    $score = if ($stats.Total -gt 0) { [math]::Round(($stats.Killed / $stats.Total) * 100, 2) } else { 0 }

    if (-not $engineStats.ContainsKey($engine)) {
        $engineStats[$engine] = @{ Killed = 0; Total = 0; Files = @() }
    }
    $engineStats[$engine].Killed += $stats.Killed
    $engineStats[$engine].Total += $stats.Total
    $engineStats[$engine].Files += @{ Path = $file; Score = $score; Survivors = $stats.Survivors.Count }

    # Check floor gate
    if ($engineFloors.ContainsKey($engine)) {
        $floor = $engineFloors[$engine]
        if ($score -lt $floor) {
            $floorViolations += "$file ($engine): ${score}% < floor ${floor}%"
        }
    }
}

# ── 5. Generate report ────────────────────────────────────────────────────
$reportLines = @()
$reportLines += "# Stryker Per-File Mutation Report"
$reportLines += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$reportLines += ""

$reportLines += "## Per-Engine Mutation Scores & Floor Gates"
$reportLines += "| Engine | Killed | Total | Score | Floor | Status |"
$reportLines += "|--------|--------|-------|-------|-------|--------|"

$allFloorsMet = $true
foreach ($engine in $engineStats.Keys) {
    $e = $engineStats[$engine]
    $score = if ($e.Total -gt 0) { [math]::Round(($e.Killed / $e.Total) * 100, 2) } else { 0 }
    $floor = if ($engineFloors.ContainsKey($engine)) { $engineFloors[$engine] } else { "—" }
    $status = if ($floor -ne "—" -and $score -lt $floor) { "❌ VIOLATION" } else { "✅ OK" }
    if ($status -eq "❌ VIOLATION") { $allFloorsMet = $false }

    $reportLines += "| $engine | $($e.Killed) | $($e.Total) | $score% | $floor | $status |"
}

$reportLines += ""
$reportLines += "## Per-File Details (sorted by score, ascending)"
$reportLines += "| File | Engine | Killed | Total | Score | Survivors |"
$reportLines += "|------|--------|--------|-------|-------|-----------|"

# Sort files by score ascending
$sortedFiles = $fileResults.Keys | Sort-Object {
    $stats = $fileResults[$_]
    if ($stats.Total -gt 0) { $stats.Killed / $stats.Total } else { 0 }
}

foreach ($file in $sortedFiles) {
    $stats = $fileResults[$file]
    $engine = Get-EngineFromPath $file
    $score = if ($stats.Total -gt 0) { [math]::Round(($stats.Killed / $stats.Total) * 100, 2) } else { 0 }
    $reportLines += "| $file | $engine | $($stats.Killed) | $($stats.Total) | $score% | $($stats.Survivors.Count) |"
}

# ── 6. Hardening Candidates (Top N survivors) ─────────────────────────────
$reportLines += ""
$reportLines += "## Hardening Candidates — Top $TopN Survivors (by file)"
$reportLines += "| File | Mutant | Description | Line | Replacement |"
$reportLines += "|------|--------|-------------|------|-------------|"

$allSurvivors = @()
foreach ($file in $fileResults.Keys) {
    foreach ($s in $fileResults[$file].Survivors) {
        $allSurvivors += @{
            File = $file
            MutantId = $s.MutantId
            Description = $s.Description
            Line = $s.Line
            Replacement = $s.Replacement
        }
    }
}

# Sort by file then mutant id, take Top N
$topSurvivors = $allSurvivors | Select-Object -First $TopN
foreach ($s in $topSurvivors) {
    $reportLines += "| $($s.File) | $($s.MutantId) | $($s.Description) | $($s.Line) | $($s.Replacement) |"
}

$reportLines += ""
if ($allFloorsMet) {
    $reportLines += "## ✅ Result: All per-engine floor gates met"
} else {
    $reportLines += "## ❌ Result: Floor gate violations detected"
    $reportLines += ""
    $reportLines += "### Violations:"
    foreach ($v in $floorViolations) {
        $reportLines += "- $v"
    }
}

$reportContent = $reportLines -join "`n"

# Ensure output directory exists
$outDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

Set-Content -Path $OutputPath -Value $reportContent -Encoding UTF8
Write-Host "Per-file mutation report written to: $OutputPath"

# ── 7. Exit code ──────────────────────────────────────────────────────────
if (-not $allFloorsMet) {
    Write-Error "Per-engine floor gates violated. See $OutputPath for details."
    exit 1
}

Write-Host "✅ All per-engine floor gates met."
exit 0