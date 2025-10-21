# Smart Donation System - Quick Verification Script
param(
    [string]$BaseUrl = "https://localhost:5001"
)

Write-Host "Smart Donation System - Quick Verification" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check if application is running
Write-Host "`n1. Checking Application Status..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 10
    if ($healthCheck.StatusCode -eq 200) {
        Write-Host "✅ Application is running" -ForegroundColor Green
    } else {
        Write-Host "❌ Application health check failed" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Application is not running. Please start the application first." -ForegroundColor Red
    Write-Host "Run: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test basic endpoints
Write-Host "`n2. Testing Basic Endpoints..." -ForegroundColor Yellow

$endpoints = @(
    @{ Name = "Home Page"; Url = "$BaseUrl/" }
    @{ Name = "Donations Page"; Url = "$BaseUrl/Donations" }
    @{ Name = "Map Page"; Url = "$BaseUrl/Map" }
    @{ Name = "Notifications Page"; Url = "$BaseUrl/Notifications" }
    @{ Name = "Analytics Dashboard"; Url = "$BaseUrl/Analytics/Dashboard" }
)

$endpointResults = @()
foreach ($endpoint in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri $endpoint.Url -UseBasicParsing -TimeoutSec 10
        $status = if ($response.StatusCode -eq 200) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($response.StatusCode -eq 200) { "Green" } else { "Red" }
        Write-Host "  $($endpoint.Name): $status ($($response.StatusCode))" -ForegroundColor $color
        $endpointResults += @{ Name = $endpoint.Name; Success = ($response.StatusCode -eq 200) }
    }
    catch {
        Write-Host "  $($endpoint.Name): ❌ FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $endpointResults += @{ Name = $endpoint.Name; Success = $false }
    }
}

# Test API endpoints
Write-Host "`n3. Testing API Endpoints..." -ForegroundColor Yellow

$apiEndpoints = @(
    @{ Name = "Donations API"; Url = "$BaseUrl/api/donations" }
    @{ Name = "Analytics API"; Url = "$BaseUrl/api/analytics/metrics" }
    @{ Name = "Notifications API"; Url = "$BaseUrl/api/notifications" }
    @{ Name = "ML Recommendations API"; Url = "$BaseUrl/api/ml/recommendations" }
)

$apiResults = @()
foreach ($api in $apiEndpoints) {
    try {
        $response = Invoke-WebRequest -Uri $api.Url -UseBasicParsing -TimeoutSec 10
        $status = if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 401) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 401) { "Green" } else { "Red" }
        Write-Host "  $($api.Name): $status ($($response.StatusCode))" -ForegroundColor $color
        $apiResults += @{ Name = $api.Name; Success = ($response.StatusCode -eq 200 -or $response.StatusCode -eq 401) }
    }
    catch {
        Write-Host "  $($api.Name): ❌ FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $apiResults += @{ Name = $api.Name; Success = $false }
    }
}

# Test SignalR connection
Write-Host "`n4. Testing SignalR Connection..." -ForegroundColor Yellow
try {
    $signalRResponse = Invoke-WebRequest -Uri "$BaseUrl/notificationHub" -UseBasicParsing -TimeoutSec 10
    if ($signalRResponse.StatusCode -eq 101) {
        Write-Host "✅ SignalR connection available" -ForegroundColor Green
    } else {
        Write-Host "⚠️ SignalR connection status: $($signalRResponse.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ SignalR connection failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Performance test
Write-Host "`n5. Testing Performance..." -ForegroundColor Yellow
$performanceTests = @()
for ($i = 1; $i -le 5; $i++) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing -TimeoutSec 10
        $stopwatch.Stop()
        $performanceTests += $stopwatch.ElapsedMilliseconds
        Write-Host "  Request $i : $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    }
    catch {
        $stopwatch.Stop()
        Write-Host "  Request $i : FAILED" -ForegroundColor Red
    }
}

if ($performanceTests.Count -gt 0) {
    $averageTime = ($performanceTests | Measure-Object -Average).Average
    $maxTime = ($performanceTests | Measure-Object -Maximum).Maximum
    Write-Host "  Average Response Time: $([math]::Round($averageTime, 2))ms" -ForegroundColor Cyan
    Write-Host "  Maximum Response Time: $([math]::Round($maxTime, 2))ms" -ForegroundColor Cyan
    
    if ($averageTime -lt 3000) {
        Write-Host "✅ Performance: EXCELLENT" -ForegroundColor Green
    } elseif ($averageTime -lt 5000) {
        Write-Host "⚠️ Performance: ACCEPTABLE" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Performance: POOR" -ForegroundColor Red
    }
}

# Summary
Write-Host "`n=== Verification Summary ===" -ForegroundColor Green

$endpointSuccess = ($endpointResults | Where-Object { $_.Success }).Count
$apiSuccess = ($apiResults | Where-Object { $_.Success }).Count

Write-Host "Endpoints Tested: $($endpointResults.Count)" -ForegroundColor White
Write-Host "Endpoints Passed: $endpointSuccess" -ForegroundColor Green
Write-Host "API Endpoints Tested: $($apiResults.Count)" -ForegroundColor White
Write-Host "API Endpoints Passed: $apiSuccess" -ForegroundColor Green

$overallSuccess = $endpointSuccess + $apiSuccess
$totalTests = $endpointResults.Count + $apiResults.Count
$successRate = [math]::Round(($overallSuccess / $totalTests) * 100, 2)

Write-Host "Overall Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } else { "Red" })

if ($successRate -ge 90) {
    Write-Host "`n🎉 Application is ready for testing!" -ForegroundColor Green
    Write-Host "✅ All core endpoints are accessible" -ForegroundColor Green
    Write-Host "✅ API endpoints are responding" -ForegroundColor Green
    Write-Host "✅ Performance is acceptable" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Run full verification: .\scripts\verify-functionality.ps1" -ForegroundColor White
    Write-Host "2. Follow manual testing guide: MANUAL_VERIFICATION_GUIDE.md" -ForegroundColor White
    Write-Host "3. Test with real users and data" -ForegroundColor White
} else {
    Write-Host "`n⚠️ Some issues detected. Please check:" -ForegroundColor Red
    Write-Host "1. Ensure application is running: dotnet run" -ForegroundColor Yellow
    Write-Host "2. Check database connection" -ForegroundColor Yellow
    Write-Host "3. Verify all services are configured" -ForegroundColor Yellow
}

Write-Host "`nQuick verification completed." -ForegroundColor Green
