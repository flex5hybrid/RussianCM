$ErrorActionPreference = 'Stop'

$expectedRobustCommit = '03e28a812104b70761244fca084245e0dab75d2a'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$robustRoot = Join-Path $repoRoot 'RobustToolbox'
$patchRoot = Join-Path $PSScriptRoot 'Patches'

if (-not (Test-Path (Join-Path $robustRoot '.git'))) {
    throw 'RobustToolbox submodule is not initialized. Run: git submodule update --init --recursive'
}

$currentCommit = (git -C $robustRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to read RobustToolbox HEAD.'
}

if ($currentCommit -ne $expectedRobustCommit) {
    throw "Unexpected RobustToolbox commit: $currentCommit. Expected: $expectedRobustCommit"
}

$dirty = git -C $robustRoot status --porcelain
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to inspect RobustToolbox worktree.'
}

if ($dirty) {
    throw 'RobustToolbox worktree is not clean. Commit/stash/reset local changes before applying CMU3D patches.'
}

$patches = Get-ChildItem -Path $patchRoot -Filter '*.patch' | Sort-Object Name
if ($patches.Count -eq 0) {
    throw 'No CMU3D engine patches found.'
}

foreach ($patch in $patches) {
    Write-Host "Applying $($patch.Name)..."
    git -C $robustRoot apply --check $patch.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "Patch check failed: $($patch.Name)"
    }

    git -C $robustRoot apply $patch.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "Patch application failed: $($patch.Name)"
    }
}

Write-Host 'CMU3D RobustToolbox bootstrap patches applied successfully.'
Write-Host 'Next: build Robust.Shared/Content.Shared and commit the resulting engine changes in a dedicated RobustToolbox fork.'
