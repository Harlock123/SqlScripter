#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publishes self-contained, single-file SqlScripter executables for every
    supported platform: Intel/AMD (x64) and ARM64 on Windows, macOS and Linux.

.DESCRIPTION
    Produces one standalone native executable per runtime (no .NET install needed
    on the target machine). Cross-platform: run it with PowerShell 7+ (pwsh) on
    Windows, macOS or Linux.

.PARAMETER OutRoot
    Output root directory. Each runtime lands in <OutRoot>/<rid>/. Default: ./publish

.PARAMETER Rids
    Override the list of runtime identifiers to build.

.EXAMPLE
    pwsh ./publish-all.ps1
    pwsh ./publish-all.ps1 -OutRoot ./dist -Rids win-x64,linux-x64
#>
param(
    [string]   $OutRoot = "./publish",
    [string[]] $Rids = @("win-x64", "win-arm64", "osx-x64", "osx-arm64", "linux-x64", "linux-arm64")
)

$ErrorActionPreference = "Stop"

# Resolve the project next to this script so it can be run from any directory,
# and so 'dotnet publish' targets the project (not the .sln, which rejects -o).
$project = Join-Path $PSScriptRoot "SqlScripter.csproj"

foreach ($rid in $Rids) {
    Write-Host "==> Publishing $rid" -ForegroundColor Cyan
    $dest = Join-Path $OutRoot $rid

    dotnet publish $project -c Release -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $dest

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for $rid (exit code $LASTEXITCODE)."
    }

    $exe = if ($rid -like "win-*") { "SqlScripter.exe" } else { "SqlScripter" }
    Write-Host "    -> $(Join-Path $dest $exe)" -ForegroundColor DarkGray
}

Write-Host "Done. Single-file executables are under $OutRoot/<rid>/" -ForegroundColor Green
