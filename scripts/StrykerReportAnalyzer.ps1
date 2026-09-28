<#
.SYNOPSIS
    TEST-013: Per-file mutation score analysis for Stryker.NET mutation-report.json.

.DESCRIPTION
    Groups all mutations from a Stryker mutation-report.json by file, computes the
    per-file mutation score (killed / total) and emits:
      - a Markdown table (all files, sorted by score ascending),
      - a Top-N survivor list (files with the most surviving mutants).
    This identifies the Top-N survivor files for targeted test hardening
    (purpose: Stryker scoping in CI, Kanban TEST-013).

    NOTE: This file is intentionally pure ASCII. Windows PowerShell 5.1 reads
    no-BOM files as ANSI/Windows-1252, so any non-ASCII byte would corrupt a
    string literal (an em-dash decodes into a phantom quote and breaks the
    parser with "ExpressionsMustBeFirstInPipeline").

.PARAMETER ReportPath
    Path to the mutation-report.json (Stryker json reporter).

.PARAMETER TopN
    Number of survivor files in the Top-N list (default: 10).

.PARAMETER FailBelow
    Optional per-file score floor: fails (exit 1) if any file is below it.
    Default: -1 = no gate action (report only).

.EXAMPLE
    powershell -File ./scripts/StrykerReportAnalyzer.ps1 -ReportPath StrykerOutput/.../mutation-report.json -TopN 10
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [int]$TopN = 10,

    [double]$FailBelow = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReportPath)) {
    Write-Error "mutation-report.json not found: $ReportPath"
    exit 2
}

$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json

# --- Group mutations by file -------------------------------------------------
# Stryker JSON schema: .results[] with .file (path relative to solution) and
# .status ("Killed" | "Survived" | "NoCoverage" | "Timeout" | ...).
$byFile = @{}
foreach ($m in $report.results) {
    $file = $m.file
    if (-not $byFile.ContainsKey($file)) {
        $byFile[$file] = @{ Total = 0; Killed = 0; Survived = 0; NoCoverage = 0 }
    }
    $byFile[$file].Total++
    switch ($m.status) {
        'Killed'     { $byFile[$file].Killed++ }
        'Survived'   { $byFile[$file].Survived++ }
        'NoCoverage' { $byFile[$file].NoCoverage++ }
    }
}

if ($byFile.Count -eq 0) {
    Write-Warning "No mutations found in report - nothing to analyze."
    exit 0
}

# --- Per-file scores ---------------------------------------------------------
# Explicit array build (robust under PowerShell 5.1 + StrictMode Latest).
$entries = @()
foreach ($file in $byFile.Keys) {
    $s = $byFile[$file]
    if ($s.Total -gt 0) {
        $score = [math]::Round(100.0 * $s.Killed / $s.Total, 1)
    } else {
        $score = 100.0
    }
    $obj = [PSCustomObject]@{
        File         = $file
        Total        = $s.Total
        Killed       = $s.Killed
        Survived     = $s.Survived
        NoCoverage   = $s.NoCoverage
        ScorePercent = $score
    }
    $entries += , $obj
}

# Sort: worst score first (those are the hardening candidates).
$byScoreAsc = $entries | Sort-Object -Property @{ Expression = { $_.ScorePercent } }, @{ Expression = { $_.File } }

# --- Markdown table ----------------------------------------------------------
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('## Stryker Per-File Mutation Score (TEST-013)')
[void]$md.AppendLine('')
[void]$md.AppendLine('| File | Mutants | Killed | Survived | NoCoverage | Score % |')
[void]$md.AppendLine('|---|---:|---:|---:|---:|---:|')
foreach ($e in $byScoreAsc) {
    # Column vars up front so no subexpression appears inside the cell string.
    $f = $e.File; $t = $e.Total; $k = $e.Killed; $s = $e.Survived; $n = $e.NoCoverage; $sc = $e.ScorePercent
    [void]$md.AppendLine("| $f | $t | $k | $s | $n | $sc |")
}

# --- Top-N survivor list -----------------------------------------------------
# Most surviving mutants first - those files need the hardest tests.
$topSurvivors = $entries |
    Sort-Object -Property @{ Expression = { $_.Survived }; Descending = $true }, @{ Expression = { $_.ScorePercent } } |
    Select-Object -First $TopN

[void]$md.AppendLine('')
[void]$md.AppendLine("### Top-$TopN Survivor Files (hardening candidates)")
if ($topSurvivors.Count -eq 0) {
    [void]$md.AppendLine('_No surviving mutants - all files clean._')
} else {
    foreach ($e in $topSurvivors) {
        $f = $e.File; $sv = $e.Survived; $t = $e.Total; $sc = $e.ScorePercent
        [void]$md.AppendLine("- **$f**: $sv survivors of $t mutants (score $sc%)")
    }
}

# --- Output ------------------------------------------------------------------
Write-Host $md.ToString()

$outDir = Split-Path -Parent $ReportPath
$mdPath = Join-Path $outDir 'stryker-per-file-report.md'
$md.ToString() | Set-Content -LiteralPath $mdPath -Encoding utf8
Write-Host "Markdown report written: $mdPath"

# --- Optional per-file gate --------------------------------------------------
if ($FailBelow -ge 0) {
    $violations = @($byScoreAsc | Where-Object { $_.ScorePercent -lt $FailBelow })
    if ($violations.Count -gt 0) {
        Write-Host "Per-file gate violated (floor ${FailBelow}%):" -ForegroundColor Red
        foreach ($v in $violations) {
            $vf = $v.File; $vs = $v.ScorePercent
            Write-Host "   - ${vf}: ${vs}%" -ForegroundColor Red
        }
        exit 1
    }
    Write-Host "Per-file gate passed (floor ${FailBelow}%)"
}

exit 0
