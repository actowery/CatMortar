param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot), [switch]$RequireBinaries)
$ErrorActionPreference = 'Stop'
function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

$aboutPath = Join-Path $RepositoryRoot 'About\About.xml'
$defsPath = Join-Path $RepositoryRoot 'Defs\ThingDefs\CatMortar.xml'
$coatsPath = Join-Path $RepositoryRoot 'Defs\HediffDefs\CatMortar_LaunchCoats.xml'
$jobsPath = Join-Path $RepositoryRoot 'Defs\JobDefs\CatMortar_Jobs.xml'
$previewPath = Join-Path $RepositoryRoot 'About\Preview.png'
$loadFoldersPath = Join-Path $RepositoryRoot 'LoadFolders.xml'
[xml]$about = Get-Content -Raw $aboutPath; [xml]$defs = Get-Content -Raw $defsPath; [xml]$coats = Get-Content -Raw $coatsPath; [xml]$loadFolders = Get-Content -Raw $loadFoldersPath

Assert-Condition ($about.ModMetaData.packageId -eq 'konduct.catmortar') 'Unexpected package ID.'
Assert-Condition (@($about.ModMetaData.supportedVersions.li) -contains '1.6') 'RimWorld 1.6 missing.'
Assert-Condition (@($about.ModMetaData.supportedVersions.li) -contains '1.7') 'RimWorld 1.7 missing.'
Assert-Condition ((Get-Item $previewPath).Length -lt 1MB) 'Workshop preview must be smaller than 1 MB.'
Assert-Condition ($defs.Defs.ThingDef.defName -contains 'Building_CatMortar') 'Mortar definition missing.'
Assert-Condition (($defs.Defs.ThingDef | Where-Object { $_.defName -eq 'Building_CatMortar' }).thingClass -eq 'CatMortar.Building_CatMortar') 'Mortar class missing.'
Assert-Condition (@(($defs.Defs.ThingDef | Where-Object { $_.defName -eq 'Building_CatMortar' }).thingCategories.li) -contains 'BuildingsSecurity') 'Mortar must appear in RimWorld security building lists.'
Assert-Condition ($defs.Defs.ThingDef.defName -contains 'Projectile_CatMortarShell') 'Projectile definition missing.'
Assert-Condition (Test-Path $jobsPath) 'Cat loading job definition missing.'
Assert-Condition (@($coats.Defs.HediffDef).Count -eq 4) 'Expected four launch coat definitions.'
Assert-Condition (@($coats.Defs.HediffDef | Where-Object { $_.description -and $_.skinColorOverride }).Count -eq 4) 'Launch coats need descriptions and native skin-color overrides.'
Assert-Condition (Test-Path (Join-Path $RepositoryRoot 'Textures\Things\Building\Security\CatMortar.png')) 'Mortar texture missing.'
foreach ($version in 'v1.6','v1.7') { Assert-Condition (@($loadFolders.loadFolders.$version.li) -contains '/') "Shared root is not loaded for $version." }
if ($RequireBinaries) { foreach ($version in '1.6','1.7') { Assert-Condition (Test-Path (Join-Path $RepositoryRoot "$version\Assemblies\CatMortar.dll")) "Missing $version assembly." } }
Write-Output 'Package validation passed.'
