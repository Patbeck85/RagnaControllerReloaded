#!/usr/bin/env pwsh
# Verify-LocalizationParity.ps1
# Prüft ob alle 41 Locale-Dateien exakt 655 Keys haben (Parität zu en.json)
# Exit-Code 0 = OK, Exit-Code 1 = Drift detected

param(
    [string]$Root = "src/RagnaController/Resources/Localization"
)

$masterPath = Join-Path $Root "en.json"
if (-not (Test-Path $masterPath)) {
    Write-Host "❌ Master-Datei '$masterPath' nicht gefunden!" -ForegroundColor Red
    exit 1
}

$master = Get-Content $masterPath | ConvertFrom-Json
$masterKeys = $master.PSObject.Properties.Name | Sort-Object
$expectedCount = $masterKeys.Count

Write-Host "Checking localization parity against en.json ($expectedCount keys expected)..." -ForegroundColor Yellow

$driftDetected = $false
$filesWithDrift = @()

Get-ChildItem "$Root/*.json" -Exclude "en.json" | ForEach-Object {
    $file = $_
    $fileName = $file.Name
    
    try {
        $curr = Get-Content $file.FullName | ConvertFrom-Json
        $currKeys = $curr.PSObject.Properties.Name | Sort-Object
        
        # Missing Keys
        $missing = Compare-Object $currKeys $masterKeys -SyncWindow 0 | 
                    Where-Object SideIndicator -eq '=>' | 
                    Select-Object -ExpandProperty InputObject | Measure-Object | Select-Object -ExpandProperty Count
        
        # Extra Keys
        $extra = Compare-Object $currKeys $masterKeys -SyncWindow 0 | 
                  Where-Object SideIndicator -eq '<=' | 
                  Select-Object -ExpandProperty InputObject | Measure-Object | Select-Object -ExpandProperty Count
        
        $totalCount = $missing + $extra
        
        if ($totalCount -gt 0) {
            Write-Host "❌ $fileName: Drift detected! Missing: $missing, Extra: $extra" -ForegroundColor Red
            $driftDetected = $true
            $filesWithDrift += "$fileName (missing: $missing, extra: $extra)"
        } else {
            Write-Host "✅ $fileName: OK ($expectedCount keys)" -ForegroundColor Green
        }
        
    } catch {
        Write-Host "❌ $fileName: Error - $_" -ForegroundColor Red
        $driftDetected = $true
    }
}

if (-not $driftDetected) {
    Write-Host "`n✅ All files have exact parity with en.json!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Localization drift detected. Run Sync-Localization.ps1 to fix." -ForegroundColor Red
    Write-Host "Files with drift:"
    $filesWithDrift | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
