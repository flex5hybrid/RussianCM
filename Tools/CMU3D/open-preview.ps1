$ErrorActionPreference = 'Stop'
$preview = Join-Path $PSScriptRoot 'Preview\index.html'
if (-not (Test-Path $preview)) { throw "CMU3D preview not found: $preview" }
Start-Process $preview
