# Synchronized Package Version Bump Script
# Bumps all packages to the same version to keep them in sync

param(
    [Parameter(Mandatory=$false)]
    [switch]$Major,

    [Parameter(Mandatory=$false)]
    [switch]$Minor
)

$ErrorActionPreference = "Stop"

# All packages that should be versioned together
$projects = @(
    "ntlt.eventsourcing.core\ntlt.eventsourcing.core.csproj",
    "ntlt.eventsourcing.autx\ntlt.eventsourcing.autx.csproj"
)

Write-Host "`n=== Synchronized Version Bump ===" -ForegroundColor Cyan

# Check for mutual exclusivity
if ($Major -and $Minor) {
    Write-Host "ERROR: Cannot specify both -Major and -Minor flags" -ForegroundColor Red
    exit 1
}

# Determine bump type
$bumpType = if ($Major) { "Major" } elseif ($Minor) { "Minor" } else { "Patch" }
Write-Host "Bump type: $bumpType`n" -ForegroundColor Gray

# Check all projects exist and collect versions
$projectData = @()
foreach ($project in $projects) {
    if (-not (Test-Path $project)) {
        Write-Host "ERROR: Project file not found: $project" -ForegroundColor Red
        exit 1
    }

    [xml]$projectXml = Get-Content $project
    $versionText = [string]$projectXml.Project.PropertyGroup.Version

    if (-not $versionText) {
        Write-Host "ERROR: No <Version> element found in $project" -ForegroundColor Red
        exit 1
    }

    if ($versionText -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
        Write-Host "ERROR: Invalid version format in ${project}: $versionText (expected X.Y.Z)" -ForegroundColor Red
        exit 1
    }

    $projectData += @{
        Path = $project
        Name = Split-Path $project -Leaf
        Xml = $projectXml
        CurrentVersion = $versionText
    }
}

# Check version consistency
Write-Host "--- Current Versions ---" -ForegroundColor Yellow
$versions = $projectData | ForEach-Object { $_.CurrentVersion } | Select-Object -Unique

foreach ($data in $projectData) {
    Write-Host "$($data.Name): " -ForegroundColor White -NoNewline
    Write-Host $data.CurrentVersion -ForegroundColor Cyan
}

if ($versions.Count -gt 1) {
    Write-Host "`nWARNING: Packages have different versions!" -ForegroundColor Yellow
    Write-Host "They will all be synchronized to the new version.`n" -ForegroundColor Yellow
}

# Use first project's version as base
$currentVersion = $projectData[0].CurrentVersion
$currentVersion -match '^(\d+)\.(\d+)\.(\d+)$' | Out-Null

$majorNum = [int]$matches[1]
$minorNum = [int]$matches[2]
$patchNum = [int]$matches[3]

# Calculate new version
if ($Major) {
    $newVersion = "$($majorNum + 1).0.0"
} elseif ($Minor) {
    $newVersion = "$majorNum.$($minorNum + 1).0"
} else {
    $newVersion = "$majorNum.$minorNum.$($patchNum + 1)"
}

Write-Host "`n--- Version Update ---" -ForegroundColor Yellow
Write-Host "Old: " -ForegroundColor Gray -NoNewline
Write-Host $currentVersion -ForegroundColor Red -NoNewline
Write-Host " -> New: " -ForegroundColor Gray -NoNewline
Write-Host $newVersion -ForegroundColor Green
Write-Host ""

# Update all projects
Write-Host "--- Updating Projects ---" -ForegroundColor Yellow
foreach ($data in $projectData) {
    Write-Host "Updating: $($data.Name)" -ForegroundColor White

    # Update version in XML
    $data.Xml.Project.PropertyGroup.Version = $newVersion

    # Save the file
    $data.Xml.Save((Resolve-Path $data.Path))

    Write-Host "  [OK] $($data.CurrentVersion) -> $newVersion" -ForegroundColor Green
}

Write-Host "`n--- Summary ---" -ForegroundColor Cyan
Write-Host "All $($projectData.Count) packages synchronized to version: " -ForegroundColor White -NoNewline
Write-Host $newVersion -ForegroundColor Green

# Output new version for scripting
Write-Output $newVersion
