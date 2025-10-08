# NuGet.org Package Push Script
# Pushes packages to official NuGet.org repository

$ErrorActionPreference = "Stop"

$nugetSource = "https://api.nuget.org/v3/index.json"
$projects = @(
    "ntlt.eventsourcing.core",
    "ntlt.eventsourcing.autx"
)

Write-Host "`n=== Push Packages to NuGet.org ===" -ForegroundColor Cyan
Write-Host "Target: $nugetSource`n" -ForegroundColor Gray

# Check API key
$apiKey = $env:NUGET_API_KEY
if (-not $apiKey) {
    Write-Host "ERROR: Environment variable NUGET_API_KEY is not set!" -ForegroundColor Red
    Write-Host "Set it with: " -ForegroundColor Yellow -NoNewline
    Write-Host "`$env:NUGET_API_KEY = 'your-api-key'" -ForegroundColor White
    exit 1
}

Write-Host "API Key: " -ForegroundColor Gray -NoNewline
Write-Host "****$($apiKey.Substring([Math]::Max(0, $apiKey.Length - 4)))" -ForegroundColor Green
Write-Host ""

$successCount = 0
$failCount = 0

# Push packages
Write-Host "--- Pushing Packages ---" -ForegroundColor Yellow
foreach ($project in $projects) {
    $packagePattern = ".\$project\bin\Release\*.nupkg"
    $packages = Get-ChildItem $packagePattern -ErrorAction SilentlyContinue

    if (-not $packages) {
        Write-Host "$project`: " -ForegroundColor White -NoNewline
        Write-Host "No packages found" -ForegroundColor Yellow
        $failCount++
        continue
    }

    Write-Host "$project`: " -ForegroundColor White -NoNewline
    Write-Host "$($packages.Count) package(s)" -ForegroundColor Gray

    dotnet nuget push $packagePattern --api-key $apiKey --source $nugetSource --skip-duplicate 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Pushed successfully" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "  [ERROR] Push failed (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        $failCount++
    }
}

# Summary
Write-Host "`n--- Summary ---" -ForegroundColor Cyan
Write-Host "Pushed: $successCount succeeded, $failCount failed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

if ($failCount -gt 0) {
    Write-Host "`nSome packages failed to push. Check the output above for details." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "`nAll packages pushed successfully!" -ForegroundColor Green
}
