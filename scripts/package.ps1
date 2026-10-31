param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'outputs')
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'validate-package.ps1') -RepositoryRoot $RepositoryRoot -RequireBinaries

$artifactsRoot = Join-Path $RepositoryRoot 'artifacts'
$stagingRoot = Join-Path $artifactsRoot 'package'
$modRoot = Join-Path $stagingRoot 'CatMortar'
$resolvedRepository = [System.IO.Path]::GetFullPath($RepositoryRoot)
$resolvedStaging = [System.IO.Path]::GetFullPath($stagingRoot)
if (-not $resolvedStaging.StartsWith($resolvedRepository, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing to clean a staging directory outside the repository.' }
if (Test-Path -LiteralPath $stagingRoot) { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }

New-Item -ItemType Directory -Path $modRoot -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$runtimeEntries = @('About', 'Defs', 'Languages', '1.6', '1.7', 'LoadFolders.xml', 'LICENSE', 'README.md', 'CHANGELOG.md')
foreach ($entry in $runtimeEntries) { Copy-Item -LiteralPath (Join-Path $RepositoryRoot $entry) -Destination $modRoot -Recurse -Force }

Get-ChildItem -LiteralPath $modRoot -Recurse -File | Where-Object { $_.DirectoryName -like '*\Assemblies' -and $_.Extension -eq '.pdb' } | Remove-Item -Force
Get-ChildItem -LiteralPath $modRoot -Recurse -File | Where-Object { $_.Name -like 'CatLauncher*.dll' -or $_.Name -like 'CatLauncher*.pdb' } | Remove-Item -Force
$zipPath = Join-Path $OutputDirectory 'CatMortar.zip'
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -LiteralPath $modRoot -DestinationPath $zipPath -CompressionLevel Optimal
$checksum = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $OutputDirectory 'CatMortar.zip.sha256'
Set-Content -LiteralPath $checksumPath -Value "$checksum  CatMortar.zip" -Encoding ascii
Write-Output "Created $zipPath"
Write-Output "SHA-256 $checksum"
