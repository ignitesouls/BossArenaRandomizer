param(
    [string]$BossesPath = "C:\Users\xbox1\OneDrive - Angelo State University\Documents\IGNITE\BAR Fire Giant\Data\bosses.json",
    [string]$ArenasPath = "C:\Users\xbox1\OneDrive - Angelo State University\Documents\IGNITE\BAR Fire Giant\Data\arenas.json",
    [string]$OutputPath = "valid_boss_placements_by_rules.txt"
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

$bossEntries = $bosses.PSObject.Properties | Sort-Object Name
$arenaEntries = $arenas.PSObject.Properties | Sort-Object Name

$ruleSets = @(
    @{ Name = "No Arena Size Restrictions"; Size = $false; Rush = $false; Loose = $false },
    @{ Name = "Arena Size Restrictions"; Size = $true; Rush = $false; Loose = $false },
    @{ Name = "Boss Rush Difficulty Curve"; Size = $false; Rush = $true; Loose = $false },
    @{ Name = "No Arena Size Restrictions + Loose Difficulty"; Size = $false; Rush = $false; Loose = $true },
    @{ Name = "Arena Size Restrictions + Boss Rush Difficulty Curve"; Size = $true; Rush = $true; Loose = $false },
    @{ Name = "Arena Size Restrictions + Loose Difficulty"; Size = $true; Rush = $false; Loose = $true },
    @{ Name = "Boss Rush Difficulty Curve + Loose Difficulty"; Size = $false; Rush = $true; Loose = $true },
    @{ Name = "Arena Size Restrictions + Boss Rush Difficulty Curve + Loose Difficulty"; Size = $true; Rush = $true; Loose = $true }
)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("Valid Boss Placements by Rule Combination")
$lines.Add("Bosses source: $BossesPath")
$lines.Add("Arenas source: $ArenasPath")
$lines.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
$lines.Add("")
$lines.Add("Rules:")
$lines.Add("- Special incompatibility flags always apply.")
$lines.Add("- Arena Size Restrictions: bossSize must be less than or equal to arenaSize.")
$lines.Add("- Boss Rush Difficulty Curve: arenas with hardNotAllowed=1 reject bosses with isHard=1.")
$lines.Add("- Loose Difficulty: boss baseDifficulty must be less than or equal to arena difficultyPassThrough.")
$lines.Add("")

foreach ($ruleSet in $ruleSets) {
    $lines.Add("============================================================")
    $lines.Add($ruleSet.Name)
    $lines.Add("Arena Size Restrictions: $($ruleSet.Size)")
    $lines.Add("Boss Rush Difficulty Curve: $($ruleSet.Rush)")
    $lines.Add("Loose Difficulty: $($ruleSet.Loose)")
    $lines.Add("")

    $totalPlacements = 0

    foreach ($arenaEntry in $arenaEntries) {
        $arenaName = $arenaEntry.Name
        $arena = $arenaEntry.Value
        $validBosses = New-Object System.Collections.Generic.List[string]

        foreach ($bossEntry in $bossEntries) {
            if (Test-Placement `
                    -Arena $arena `
                    -Boss $bossEntry.Value `
                    -UseArenaSizeRestriction $ruleSet.Size `
                    -UseBossRushDifficultyCurve $ruleSet.Rush `
                    -UseLooseDifficulty $ruleSet.Loose) {
                $validBosses.Add("$($bossEntry.Name) (ID: $($bossEntry.Value.id))")
            }
        }

        $totalPlacements += $validBosses.Count
        $lines.Add("$arenaName (ID: $($arena.id)) - $($validBosses.Count) valid bosses")
        foreach ($bossLine in $validBosses) {
            $lines.Add("  - $bossLine")
        }
        $lines.Add("")
    }

    $lines.Add("Total valid arena/boss placements: $totalPlacements")
    $lines.Add("")
}

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path (Get-Location) $OutputPath
}

[System.IO.File]::WriteAllLines($resolvedOutput, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $resolvedOutput"
