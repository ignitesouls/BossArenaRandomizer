param(
    [string]$BossesPath = "C:\Users\xbox1\OneDrive - Angelo State University\Documents\IGNITE\BAR Fire Giant\Data\bosses.json",
    [string]$ArenasPath = "C:\Users\xbox1\OneDrive - Angelo State University\Documents\IGNITE\BAR Fire Giant\Data\arenas.json",
    [string]$OutputDirectory = "BossArenaRandomizer\Data\Pairings"
)

$ErrorActionPreference = "Stop"

function Read-JsonObject {
    param([string]$Path)

    $json = Get-Content -Raw -LiteralPath $Path
    return $json | ConvertFrom-Json
}

function Get-IntValue {
    param(
        [object]$Object,
        [string]$Name,
        [int]$Default = 0
    )

    if ($null -eq $Object.PSObject.Properties[$Name] -or $null -eq $Object.$Name) {
        return $Default
    }

    return [int]$Object.$Name
}

function Test-SpecialCompatibility {
    param(
        [object]$Arena,
        [object]$Boss
    )

    if ((Get-IntValue $Arena "twoPhaseNotAllowed") -eq 1 -and (Get-IntValue $Boss "isTwoPhase") -eq 1) { return $false }
    if ((Get-IntValue $Arena "dragonNotAllowed") -eq 1 -and (Get-IntValue $Boss "isDragon") -eq 1) { return $false }
    if ((Get-IntValue $Arena "npcNotAllowed") -eq 1 -and (Get-IntValue $Boss "isNPC") -eq 1) { return $false }
    if ((Get-IntValue $Arena "isEscapable") -eq 1 -and (Get-IntValue $Boss "canEscape") -eq 1) { return $false }
    if ((Get-IntValue $Arena "messmerNotAllowed") -eq 1 -and (Get-IntValue $Boss "isMessmer") -eq 1) { return $false }
    if ((Get-IntValue $Arena "malikethNotAllowed") -eq 1 -and (Get-IntValue $Boss "isMaliketh") -eq 1) { return $false }
    if ((Get-IntValue $Arena "godskinduoNotAllowed") -eq 1 -and (Get-IntValue $Boss "isGodskinDuo") -eq 1) { return $false }
    if ((Get-IntValue $Arena "firegiantNotAllowed") -eq 1 -and (Get-IntValue $Boss "isFiregiant") -eq 1) { return $false }

    $arenaType = Get-IntValue $Arena "arenaType"
    if ($arenaType -eq 7 -and (Get-IntValue $Boss "isEvergaolIncompatible") -eq 1) { return $false }
    if ($arenaType -eq 3 -and (Get-IntValue $Boss "isOpenworldIncompatible") -eq 1) { return $false }

    return $true
}

function Test-Placement {
    param(
        [object]$Arena,
        [object]$Boss,
        [bool]$UseArenaSizeRestriction,
        [bool]$UseBossRushDifficultyCurve,
        [bool]$UseLooseDifficulty
    )

    if (-not (Test-SpecialCompatibility $Arena $Boss)) { return $false }

    if ($UseArenaSizeRestriction) {
        if ((Get-IntValue $Boss "bossSize") -gt (Get-IntValue $Arena "arenaSize")) { return $false }
    }

    if ($UseBossRushDifficultyCurve) {
        if ((Get-IntValue $Arena "hardNotAllowed") -eq 1 -and (Get-IntValue $Boss "isHard") -eq 1) { return $false }
    }

    if ($UseLooseDifficulty) {
        if ((Get-IntValue $Boss "baseDifficulty" 1) -gt (Get-IntValue $Arena "difficultyPassThrough" 5)) { return $false }
    }

    return $true
}

$bosses = Read-JsonObject -Path $BossesPath
$arenas = Read-JsonObject -Path $ArenasPath

$bossEntries = @($bosses.PSObject.Properties)
$arenaEntries = @($arenas.PSObject.Properties)

$ruleSets = @(
    @{ FileName = "no_arena_size_restrictions.json"; Size = $false; Rush = $false; Loose = $false },
    @{ FileName = "arena_size_restrictions.json"; Size = $true; Rush = $false; Loose = $false },
    @{ FileName = "boss_rush_difficulty_curve.json"; Size = $false; Rush = $true; Loose = $false },
    @{ FileName = "no_arena_size_restrictions_loose_difficulty.json"; Size = $false; Rush = $false; Loose = $true },
    @{ FileName = "arena_size_restrictions_boss_rush_difficulty_curve.json"; Size = $true; Rush = $true; Loose = $false },
    @{ FileName = "arena_size_restrictions_loose_difficulty.json"; Size = $true; Rush = $false; Loose = $true },
    @{ FileName = "boss_rush_difficulty_curve_loose_difficulty.json"; Size = $false; Rush = $true; Loose = $true },
    @{ FileName = "arena_size_restrictions_boss_rush_difficulty_curve_loose_difficulty.json"; Size = $true; Rush = $true; Loose = $true }
)

$resolvedOutputDirectory = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
} else {
    Join-Path (Get-Location) $OutputDirectory
}

New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

foreach ($ruleSet in $ruleSets) {
    $placements = [ordered]@{}
    $totalPlacements = 0

    foreach ($arenaEntry in $arenaEntries) {
        $validBossIds = New-Object System.Collections.Generic.List[string]

        foreach ($bossEntry in $bossEntries) {
            if (Test-Placement `
                    -Arena $arenaEntry.Value `
                    -Boss $bossEntry.Value `
                    -UseArenaSizeRestriction $ruleSet.Size `
                    -UseBossRushDifficultyCurve $ruleSet.Rush `
                    -UseLooseDifficulty $ruleSet.Loose) {
                $validBossIds.Add([string]$bossEntry.Value.id)
            }
        }

        $placements[[string]$arenaEntry.Value.id] = $validBossIds.ToArray()
        $totalPlacements += $validBossIds.Count
    }

    $outputPath = Join-Path $resolvedOutputDirectory $ruleSet.FileName
    $json = $placements | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($outputPath, $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "$($ruleSet.FileName): $totalPlacements placements"
}
