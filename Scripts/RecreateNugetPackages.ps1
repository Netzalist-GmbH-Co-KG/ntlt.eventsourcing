# NuGet Package Recreation Script
# Builds and pushes packages to local NuGet feed

$ErrorActionPreference = "Stop"

$localNugetSource = "G:\Meine Ablage\local-nuget"
$projects = @(
    "ntlt.eventsourcing.core\ntlt.eventsourcing.core.csproj",
    "ntlt.eventsourcing.autx\ntlt.eventsourcing.autx.csproj"
)

Write-Host "`n=== NuGet Package Recreation ===" -ForegroundColor Cyan
Write-Host "Local feed: $localNugetSource`n" -ForegroundColor Gray

# Check if local NuGet source exists
if (-not (Test-Path $localNugetSource)) {
    Write-Host "ERROR: Local NuGet source not found: $localNugetSource" -ForegroundColor Red
    exit 1
}

$successCount = 0
$failCount = 0

# Build packages
Write-Host "--- Building Packages ---" -ForegroundColor Yellow
foreach ($project in $projects) {
    $projectName = Split-Path $project -Leaf
    Write-Host "Building: $projectName" -ForegroundColor White

    if (-not (Test-Path $project)) {
        Write-Host "  ERROR: Project file not found!" -ForegroundColor Red
        $failCount++
        continue
    }

    dotnet pack $project -c Release --nologo

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Success" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "  [ERROR] Failed (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""

# Push packages
Write-Host "--- Pushing to Local NuGet ---" -ForegroundColor Yellow
foreach ($project in $projects) {
    $projectDir = Split-Path $project -Parent
    $packagePattern = ".\$projectDir\bin\Release\*.nupkg"

    $packages = Get-ChildItem $packagePattern -ErrorAction SilentlyContinue

    if ($packages) {
        Write-Host "Pushing: $projectDir" -ForegroundColor White
        dotnet nuget push $packagePattern --source $localNugetSource --skip-duplicate

        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] Pushed" -ForegroundColor Green
        } else {
            Write-Host "  [ERROR] Push failed (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        }
    } else {
        Write-Host "No packages found for: $projectDir" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n--- Summary ---" -ForegroundColor Cyan
Write-Host "Built: $successCount succeeded, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })

if ($failCount -gt 0) {
    exit 1
}
