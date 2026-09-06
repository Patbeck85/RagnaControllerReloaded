#!/usr/bin/env pwsh
# Sync-Localization.ps1
# Synchronisiert alle 41 Locale-Dateien mit en.json auf exakt 655 Keys
# Usage: .\Sync-Localization.ps1 [-Root "src/RagnaController/Resources/Localization"]

param(
    [string]$Root = "src/RagnaController/Resources/Localization"
)

Write-Host "=== Localization Sync Script ===" -ForegroundColor Cyan
Write-Host "Root: $Root" -ForegroundColor Yellow

# Master file als JSON laden
$masterPath = Join-Path $Root "en.json"
if (-not (Test-Path $masterPath)) {
    Write-Host "❌ Fehler: Master-Datei '$masterPath' nicht gefunden!" -ForegroundColor Red
    exit 1
}

$master = Get-Content $masterPath | ConvertFrom-Json
$masterKeys = $master.PSObject.Properties.Name | Sort-Object

Write-Host "`n📦 Master file loaded: $($masterKeys.Count) keys" -ForegroundColor Green

# Alle .json Dateien außer en.json verarbeiten
Get-ChildItem "$Root/*.json" -Exclude "en.json" | ForEach-Object {
    $file = $_
    $fileName = $file.Name
    Write-Host "`nProcessing: $fileName" -ForegroundColor Yellow
    
    try {
        $curr = Get-Content $file.FullName | ConvertFrom-Json
        $currKeys = $curr.PSObject.Properties.Name | Sort-Object
        
        # Missing Keys identifizieren (existieren in master, aber nicht im aktuellen File)
        $missing = Compare-Object $currKeys $masterKeys -SyncWindow 0 | 
                    Where-Object SideIndicator -eq '=>' | 
                    Select-Object -ExpandProperty InputObject
        
        # Extra Keys identifizieren (existieren im aktuellen File, aber nicht im master)
        $extra = Compare-Object $currKeys $masterKeys -SyncWindow 0 | 
                  Where-Object SideIndicator -eq '<=' | 
                  Select-Object -ExpandProperty InputObject
        
        # Missing Keys hinzufügen (mit EN-Wert + TODO-Marker)
        if ($missing) {
            foreach ($k in $missing) {
                $curr | Add-Member NoteProperty $k "$($master.$k) // TODO: translate" -Force
            }
            Write-Host "  ➕ Added $($missing.Count) missing keys" -ForegroundColor Green
        } else {
            Write-Host "  ✓ No missing keys" -ForegroundColor Gray
        }
        
        # Extra Keys entfernen
        if ($extra) {
            foreach ($k in $extra) {
                $curr.PSObject.Properties.Remove($k)
            }
            Write-Host "  ➖ Removed $($extra.Count) extra keys" -ForegroundColor Yellow
        } else {
            Write-Host "  ✓ No extra keys" -ForegroundColor Gray
        }
        
        # Objekt in ordered hashtable umwandeln und sortiert zurückschreiben
        $ordered = [ordered]@{}
        $masterKeys | ForEach-Object { 
            $ordered[$_] = $curr.$_ 
        }
        
        # JSON formatieren (4 Spaces Indentation)
        $jsonContent = $ordered | ConvertTo-Json -Depth 10 -Compress:$false
        
        # UTF-8 ohne BOM schreiben
        [System.IO.File]::WriteAllText(
            $file.FullName, 
            $jsonContent, 
            [System.Text.UTF8Encoding]::new($false)
        )
        
        Write-Host "  ✅ Saved: $fileName" -ForegroundColor Green
        
    } catch {
        Write-Host "  ❌ Error processing $fileName: $_" -ForegroundColor Red
    }
}

Write-Host "`n=== Sync Complete ===" -ForegroundColor Cyan
Write-Host "Alle Dateien sind nun mit en.json synchronisiert." -ForegroundColor Green
