param(
    [string[]] $Refs = @(
        'origin/fix/fix-locale',
        'origin/feature/localization300925',
        'origin/feature/translation-not-all',
        'origin/bug/locale',
        'origin/fix/rule-loc',
        'origin/rebase/RuMC'
    )
)

$ErrorActionPreference = 'Continue'

function Test-EnglishishValue([string] $value) {
    return $value -match '[A-Za-z]{3,}' -and $value -notmatch '\p{IsCyrillic}'
}

function Test-RussianValue([string] $value) {
    return $value -match '\p{IsCyrillic}'
}

function Get-EntryKey([string] $line) {
    if ($line -match '^\s*([A-Za-z0-9_.-]+)\s*=') {
        return $Matches[1]
    }

    if ($line -match '^\s*\.([A-Za-z0-9_-]+)\s*=') {
        return '.' + $Matches[1]
    }

    return $null
}

function Read-FtlEntries([string[]] $lines) {
    $entries = @{}
    $currentKey = $null
    $start = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $key = Get-EntryKey $lines[$i]
        if ($null -ne $key) {
            if ($null -ne $currentKey) {
                $entries[$currentKey] = @{
                    Start = $start
                    End = $i - 1
                    Lines = $lines[$start..($i - 1)]
                }
            }

            $currentKey = $key
            $start = $i
        }
    }

    if ($null -ne $currentKey) {
        $entries[$currentKey] = @{
            Start = $start
            End = $lines.Count - 1
            Lines = $lines[$start..($lines.Count - 1)]
        }
    }

    return $entries
}

function Get-EntryValue([string[]] $lines) {
    return ($lines -join "`n") -replace '^[^=]*=', ''
}

$localeRoot = Join-Path (Get-Location) 'Resources/Locale/ru-RU'
$filesChanged = 0
$entriesChanged = 0

foreach ($file in Get-ChildItem -Recurse -LiteralPath $localeRoot -Filter '*.ftl') {
    $relative = $file.FullName.Substring((Get-Location).Path.Length + 1).Replace('\', '/')
    $currentLines = [System.IO.File]::ReadAllLines($file.FullName)
    $currentEntries = Read-FtlEntries $currentLines
    $fileChanged = $false

    foreach ($ref in $Refs) {
        git cat-file -e "$ref`:$relative" 2>$null
        if ($LASTEXITCODE -ne 0) {
            continue
        }

        $oldText = git show "$ref`:$relative"
        if ($null -eq $oldText) {
            continue
        }

        $oldLines = @($oldText)
        $oldEntries = Read-FtlEntries $oldLines

        foreach ($key in @($currentEntries.Keys)) {
            if (-not $oldEntries.ContainsKey($key)) {
                continue
            }

            $currentValue = Get-EntryValue $currentEntries[$key].Lines
            $oldValue = Get-EntryValue $oldEntries[$key].Lines

            if ((Test-EnglishishValue $currentValue) -and (Test-RussianValue $oldValue)) {
                $entry = $currentEntries[$key]
                $replacement = @($oldEntries[$key].Lines)
                $currentLines = @($currentLines[0..($entry.Start - 1)] + $replacement + $currentLines[($entry.End + 1)..($currentLines.Count - 1)])
                $currentEntries = Read-FtlEntries $currentLines
                $fileChanged = $true
                $entriesChanged++
            }
        }
    }

    if ($fileChanged) {
        [System.IO.File]::WriteAllLines($file.FullName, $currentLines, [System.Text.UTF8Encoding]::new($false))
        $filesChanged++
    }
}

Write-Output "Merged existing translations: files=$filesChanged entries=$entriesChanged"
