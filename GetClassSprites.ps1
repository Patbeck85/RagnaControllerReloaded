# RagnaController - Class Sprite Downloader
# Fetches official RO class artworks for the MainWindow Hero UI
# Run: .\GetClassSprites.ps1

$dest = Join-Path $PSScriptRoot "Assets\Classes"
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }

# Dictionary mapping local filenames to public wiki image URLs
$sprites = @{
    "novice.png"          = "https://irowiki.org/cl/images/1/15/Novice.png"
    "swordsman.png"       = "https://irowiki.org/cl/images/3/36/Swordman.png"
    "knight.png"          = "https://irowiki.org/cl/images/1/10/Knight.png"
    "lord_knight.png"     = "https://irowiki.org/cl/images/1/1a/Lord_Knight.png"
    "crusader.png"        = "https://irowiki.org/cl/images/7/7b/Crusader.png"
    "paladin.png"         = "https://irowiki.org/cl/images/b/be/Paladin.png"
    "mage.png"            = "https://irowiki.org/cl/images/7/7f/Mage.png"
    "wizard.png"          = "https://irowiki.org/cl/images/9/90/Wizard.png"
    "high_wizard.png"     = "https://irowiki.org/cl/images/9/95/High_Wizard.png"
    "sage.png"            = "https://irowiki.org/cl/images/1/1f/Sage.png"
    "professor.png"       = "https://irowiki.org/cl/images/3/3e/Scholar.png"
    "archer.png"          = "https://irowiki.org/cl/images/c/c9/Archer.png"
    "hunter.png"          = "https://irowiki.org/cl/images/3/35/Hunter.png"
    "sniper.png"          = "https://irowiki.org/cl/images/6/69/Sniper.png"
    "bard.png"            = "https://irowiki.org/cl/images/6/6b/Bard.png"
    "dancer.png"          = "https://irowiki.org/cl/images/2/22/Dancer.png"
    "clown.png"           = "https://irowiki.org/cl/images/b/b3/Clown.png"
    "gypsy.png"           = "https://irowiki.org/cl/images/6/6c/Gypsy.png"
    "thief.png"           = "https://irowiki.org/cl/images/4/4d/Thief.png"
    "assassin.png"        = "https://irowiki.org/cl/images/2/23/Assassin.png"
    "assassin_cross.png"  = "https://irowiki.org/cl/images/a/ab/Assassin_Cross.png"
    "rogue.png"           = "https://irowiki.org/cl/images/3/35/Rogue.png"
    "stalker.png"         = "https://irowiki.org/cl/images/0/07/Stalker.png"
    "merchant.png"        = "https://irowiki.org/cl/images/5/5f/Merchant.png"
    "blacksmith.png"      = "https://irowiki.org/cl/images/8/86/Blacksmith.png"
    "whitesmith.png"      = "https://irowiki.org/cl/images/e/ed/Whitesmith.png"
    "alchemist.png"       = "https://irowiki.org/cl/images/0/0f/Alchemist.png"
    "creator.png"         = "https://irowiki.org/cl/images/5/5d/Creator.png"
    "acolyte.png"         = "https://irowiki.org/cl/images/2/2a/Acolyte.png"
    "priest.png"          = "https://irowiki.org/cl/images/5/53/Priest.png"
    "high_priest.png"     = "https://irowiki.org/cl/images/6/6b/High_Priest.png"
    "monk.png"            = "https://irowiki.org/cl/images/4/4b/Monk.png"
    "champion.png"        = "https://irowiki.org/cl/images/2/2e/Champion.png"
    "ninja.png"           = "https://irowiki.org/cl/images/7/7b/Ninja.png"
    "gunslinger.png"      = "https://irowiki.org/cl/images/e/ef/Gunslinger.png"
    "taekwon.png"         = "https://irowiki.org/cl/images/c/ca/Taekwon_Boy.png"
    "star_gladiator.png"  = "https://irowiki.org/cl/images/e/ea/Star_Gladiator.png"
    "soul_linker.png"     = "https://irowiki.org/cl/images/2/27/Soul_Linker.png"
}

# Create a fallback/unknown image locally if it doesn't exist
$unknownFile = Join-Path $dest "unknown.png"
if (-not (Test-Path $unknownFile)) {
    # Generate a simple transparent placeholder using PowerShell
    $bytes = New-Object byte[] (4, 0)
    [System.IO.File]::WriteAllBytes($unknownFile, $bytes)
    Write-Host "Created fallback 'unknown.png' (transparent placeholder)" -ForegroundColor Cyan
}

$ok = 0; $fail = 0
foreach ($entry in $sprites.GetEnumerator()) {
    $url  = $entry.Value
    $file = Join-Path $dest $entry.Key
    
    if (Test-Path $file) { 
        Write-Host "  SKIP (Exists) $($entry.Key)" -ForegroundColor DarkGray
        $ok++
        continue 
    }
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $file -UseBasicParsing -TimeoutSec 15
        Write-Host "  OK  $($entry.Key)" -ForegroundColor Green
        $ok++
    } catch {
        Write-Host "FAIL  $($entry.Key): $_" -ForegroundColor Red
        $fail++
    }
    Start-Sleep -Milliseconds 200 # Be polite to the server
}

Write-Host ""
Write-Host "Finished! $ok images ready in $dest" -ForegroundColor Cyan
if ($fail -gt 0) { Write-Host "$fail images failed to download." -ForegroundColor Red }
