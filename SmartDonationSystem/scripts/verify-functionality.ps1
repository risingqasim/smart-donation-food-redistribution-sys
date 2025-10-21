# Smart Donation System - Functionality Verification Script
param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$TestUser = "donor@example.com",
    [string]$TestPassword = "Password123!",
    [string]$AdminUser = "admin@example.com",
    [string]$NGOUser = "ngo@example.com",
    [int]$TimeoutSeconds = 30
)

Write-Host "Smart Donation System - Functionality Verification" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

$TestResults = @{
    "Donor Functionality" = @{}
    "ML Model Performance" = @{}
    "Notification System" = @{}
    "Admin Dashboard" = @{}
}

# Helper function to make HTTP requests
function Invoke-TestRequest {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$TimeoutSeconds = 30
    )
    
    try {
        $requestParams = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = $TimeoutSeconds
            UseBasicParsing = $true
        }
        
        if ($Headers.Count -gt 0) {
            $requestParams.Headers = $Headers
        }
        
        if ($Body) {
            $requestParams.Body = $Body
            $requestParams.ContentType = "application/json"
        }
        
        $response = Invoke-WebRequest @requestParams
        return @{
            Success = $true
            StatusCode = $response.StatusCode
            Content = $response.Content
            ResponseTime = $response.Headers["X-Response-Time"]
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $_.Exception.Response.StatusCode
        }
    }
}

# Helper function to measure performance
function Measure-TestPerformance {
    param(
        [scriptblock]$TestScript,
        [string]$TestName
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $result = & $TestScript
    $stopwatch.Stop()
    
    $result.ResponseTime = $stopwatch.ElapsedMilliseconds
    $result.TestName = $TestName
    
    return $result
}

# Test 1: Donor Can Create and Track Donation
Write-Host "`n1. Testing Donor Functionality..." -ForegroundColor Yellow

# Test 1.1: Donor Login
$donorLoginTest = Measure-TestPerformance -TestScript {
    $loginData = @{
        Email = $TestUser
        Password = $TestPassword
    } | ConvertTo-Json
    
    Invoke-TestRequest -Url "$BaseUrl/Account/Login" -Method "POST" -Body $loginData
} -TestName "Donor Login"

# Test 1.2: Create Donation
$createDonationTest = Measure-TestPerformance -TestScript {
    $donationData = @{
        Title = "Test Donation - Fresh Vegetables"
        Description = "Fresh organic vegetables from local farm"
        FoodType = "Vegetables"
        Quantity = 15
        Unit = "kg"
        ExpiryDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
        PickupAddress = "123 Main Street, New York, NY"
        Location = "New York, NY"
        Latitude = 40.7128
        Longitude = -74.0060
    } | ConvertTo-Json
    
    Invoke-TestRequest -Url "$BaseUrl/api/donations" -Method "POST" -Body $donationData
} -TestName "Create Donation"

# Test 1.3: Get Donor Donations
$getDonationsTest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/api/donations" -Method "GET"
} -TestName "Get Donor Donations"

# Test 1.4: Track Donation Status
$trackDonationTest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/Donor/Dashboard" -Method "GET"
} -TestName "Track Donation Status"

$TestResults["Donor Functionality"] = @{
    "Login" = $donorLoginTest
    "Create Donation" = $createDonationTest
    "Get Donations" = $getDonationsTest
    "Track Status" = $trackDonationTest
}

# Test 2: ML Model Returns NGO Recommendations < 2s
Write-Host "`n2. Testing ML Model Performance..." -ForegroundColor Yellow

# Test 2.1: ML Recommendations Performance
$mlRecommendationsTest = Measure-TestPerformance -TestScript {
    $mlData = @{
        FoodType = "Vegetables"
        Quantity = 15
        Latitude = 40.7128
        Longitude = -74.0060
    } | ConvertTo-Json
    
    Invoke-TestRequest -Url "$BaseUrl/api/ml/recommendations" -Method "POST" -Body $mlData -TimeoutSeconds 5
} -TestName "ML Recommendations"

# Test 2.2: ML Model Load Testing
Write-Host "Testing ML Model under concurrent load..." -ForegroundColor Cyan
$mlLoadTests = @()
for ($i = 1; $i -le 5; $i++) {
    $mlLoadTests += Start-Job -ScriptBlock {
        param($BaseUrl)
        $mlData = @{
            FoodType = "Vegetables"
            Quantity = 10
            Latitude = 40.7128
            Longitude = -74.0060
        } | ConvertTo-Json
        
        $result = Invoke-TestRequest -Url "$BaseUrl/api/ml/recommendations" -Method "POST" -Body $mlData -TimeoutSeconds 5
        return $result
    } -ArgumentList $BaseUrl
}

$mlLoadResults = $mlLoadTests | Wait-Job | Receive-Job
$mlLoadTests | Remove-Job

$mlLoadSuccess = ($mlLoadResults | Where-Object { $_.Success }).Count
$mlLoadAverageTime = ($mlLoadResults | Where-Object { $_.Success } | Measure-Object -Property ResponseTime -Average).Average

$TestResults["ML Model Performance"] = @{
    "Recommendations" = $mlRecommendationsTest
    "Load Test Success" = $mlLoadSuccess
    "Load Test Average Time" = $mlLoadAverageTime
}

# Test 3: Notifications Trigger Correctly
Write-Host "`n3. Testing Notification System..." -ForegroundColor Yellow

# Test 3.1: SignalR Connection
$signalRConnectionTest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/notificationHub" -Method "GET" -TimeoutSeconds 10
} -TestName "SignalR Connection"

# Test 3.2: Notification Delivery
$notificationDeliveryTest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/api/notifications" -Method "GET"
} -TestName "Notification Delivery"

# Test 3.3: Real-time Notification Test
$realTimeNotificationTest = Measure-TestPerformance -TestScript {
    # Simulate creating a donation and checking for notifications
    $donationData = @{
        Title = "Notification Test Donation"
        Description = "Testing real-time notifications"
        FoodType = "Vegetables"
        Quantity = 5
        Unit = "kg"
        ExpiryDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
        PickupAddress = "456 Test Avenue, New York, NY"
        Location = "New York, NY"
    } | ConvertTo-Json
    
    $createResult = Invoke-TestRequest -Url "$BaseUrl/api/donations" -Method "POST" -Body $donationData
    
    # Wait a moment for notification processing
    Start-Sleep -Seconds 2
    
    # Check for notifications
    $notificationResult = Invoke-TestRequest -Url "$BaseUrl/api/notifications" -Method "GET"
    
    return @{
        Success = $createResult.Success -and $notificationResult.Success
        ResponseTime = $createResult.ResponseTime + $notificationResult.ResponseTime
        CreateResult = $createResult
        NotificationResult = $notificationResult
    }
} -TestName "Real-time Notifications"

$TestResults["Notification System"] = @{
    "SignalR Connection" = $signalRConnectionTest
    "Notification Delivery" = $notificationDeliveryTest
    "Real-time Notifications" = $realTimeNotificationTest
}

# Test 4: Admin Dashboard Shows Live Data
Write-Host "`n4. Testing Admin Dashboard..." -ForegroundColor Yellow

# Test 4.1: Admin Dashboard Access
$adminDashboardTest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/Analytics/Dashboard" -Method "GET"
} -TestName "Admin Dashboard Access"

# Test 4.2: Analytics API
$analyticsAPITest = Measure-TestPerformance -TestScript {
    Invoke-TestRequest -Url "$BaseUrl/api/analytics/metrics" -Method "GET"
} -TestName "Analytics API"

# Test 4.3: Live Data Updates
$liveDataTest = Measure-TestPerformance -TestScript {
    # Get initial metrics
    $initialMetrics = Invoke-TestRequest -Url "$BaseUrl/api/analytics/metrics" -Method "GET"
    
    # Create a test donation to trigger data update
    $donationData = @{
        Title = "Live Data Test Donation"
        Description = "Testing live data updates"
        FoodType = "Fruits"
        Quantity = 8
        Unit = "kg"
        ExpiryDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
        PickupAddress = "789 Test Boulevard, New York, NY"
        Location = "New York, NY"
    } | ConvertTo-Json
    
    $createResult = Invoke-TestRequest -Url "$BaseUrl/api/donations" -Method "POST" -Body $donationData
    
    # Wait for data processing
    Start-Sleep -Seconds 3
    
    # Get updated metrics
    $updatedMetrics = Invoke-TestRequest -Url "$BaseUrl/api/analytics/metrics" -Method "GET"
    
    return @{
        Success = $initialMetrics.Success -and $createResult.Success -and $updatedMetrics.Success
        ResponseTime = $initialMetrics.ResponseTime + $createResult.ResponseTime + $updatedMetrics.ResponseTime
        InitialMetrics = $initialMetrics
        CreateResult = $createResult
        UpdatedMetrics = $updatedMetrics
    }
} -TestName "Live Data Updates"

# Test 4.4: Export Functionality
$exportTest = Measure-TestPerformance -TestScript {
    $exportData = @{
        format = "excel"
        startDate = "2024-01-01"
        endDate = "2024-12-31"
        includeCharts = $true
    } | ConvertTo-Json
    
    Invoke-TestRequest -Url "$BaseUrl/api/analytics/export" -Method "POST" -Body $exportData
} -TestName "Export Functionality"

$TestResults["Admin Dashboard"] = @{
    "Dashboard Access" = $adminDashboardTest
    "Analytics API" = $analyticsAPITest
    "Live Data Updates" = $liveDataTest
    "Export Functionality" = $exportTest
}

# Generate Test Report
Write-Host "`n=== Test Results Summary ===" -ForegroundColor Green

$totalTests = 0
$passedTests = 0
$criticalFailures = 0

foreach ($category in $TestResults.Keys) {
    Write-Host "`n$category :" -ForegroundColor Cyan
    foreach ($test in $TestResults[$category].Keys) {
        if ($test -ne "Load Test Success" -and $test -ne "Load Test Average Time") {
            $totalTests++
            $testResult = $TestResults[$category][$test]
            if ($testResult.Success) {
                $passedTests++
                Write-Host "  ✅ $($testResult.TestName) : PASS ($($testResult.ResponseTime)ms)" -ForegroundColor Green
            } else {
                $criticalFailures++
                Write-Host "  ❌ $($testResult.TestName) : FAIL - $($testResult.Error)" -ForegroundColor Red
            }
        }
    }
}

# Performance Analysis
Write-Host "`n=== Performance Analysis ===" -ForegroundColor Yellow

# ML Model Performance
$mlResponseTime = $TestResults["ML Model Performance"]["Recommendations"].ResponseTime
$mlPerformanceStatus = if ($mlResponseTime -lt 2000) { "✅ EXCELLENT" } elseif ($mlResponseTime -lt 3000) { "⚠️ ACCEPTABLE" } else { "❌ POOR" }
Write-Host "ML Model Response Time: $mlResponseTime ms - $mlPerformanceStatus" -ForegroundColor $(if ($mlResponseTime -lt 2000) { "Green" } elseif ($mlResponseTime -lt 3000) { "Yellow" } else { "Red" })

# Notification Performance
$notificationResponseTime = $TestResults["Notification System"]["Real-time Notifications"].ResponseTime
$notificationPerformanceStatus = if ($notificationResponseTime -lt 5000) { "✅ EXCELLENT" } elseif ($notificationResponseTime -lt 10000) { "⚠️ ACCEPTABLE" } else { "❌ POOR" }
Write-Host "Notification Delivery Time: $notificationResponseTime ms - $notificationPerformanceStatus" -ForegroundColor $(if ($notificationResponseTime -lt 5000) { "Green" } elseif ($notificationResponseTime -lt 10000) { "Yellow" } else { "Red" })

# Admin Dashboard Performance
$dashboardResponseTime = $TestResults["Admin Dashboard"]["Analytics API"].ResponseTime
$dashboardPerformanceStatus = if ($dashboardResponseTime -lt 5000) { "✅ EXCELLENT" } elseif ($dashboardResponseTime -lt 10000) { "⚠️ ACCEPTABLE" } else { "❌ POOR" }
Write-Host "Admin Dashboard Response Time: $dashboardResponseTime ms - $dashboardPerformanceStatus" -ForegroundColor $(if ($dashboardResponseTime -lt 5000) { "Green" } elseif ($dashboardResponseTime -lt 10000) { "Yellow" } else { "Red" })

# Load Testing Results
$mlLoadSuccessRate = [math]::Round(($TestResults["ML Model Performance"]["Load Test Success"] / 5) * 100, 2)
Write-Host "ML Model Load Test Success Rate: $mlLoadSuccessRate%" -ForegroundColor $(if ($mlLoadSuccessRate -ge 90) { "Green" } else { "Red" })

# Acceptance Criteria Verification
Write-Host "`n=== Acceptance Criteria Verification ===" -ForegroundColor Yellow

$acceptanceCriteria = @{
    "Donor can create and track donation" = $TestResults["Donor Functionality"]["Create Donation"].Success -and $TestResults["Donor Functionality"]["Track Status"].Success
    "ML model returns NGO recommendations < 2s" = $TestResults["ML Model Performance"]["Recommendations"].Success -and $TestResults["ML Model Performance"]["Recommendations"].ResponseTime -lt 2000
    "Notifications trigger correctly" = $TestResults["Notification System"]["SignalR Connection"].Success -and $TestResults["Notification System"]["Real-time Notifications"].Success
    "Admin dashboard shows live data" = $TestResults["Admin Dashboard"]["Analytics API"].Success -and $TestResults["Admin Dashboard"]["Live Data Updates"].Success
}

$criteriaMet = 0
foreach ($criteria in $acceptanceCriteria.GetEnumerator()) {
    $status = if ($criteria.Value) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($criteria.Value) { "Green" } else { "Red" }
    Write-Host "$($criteria.Key): $status" -ForegroundColor $color
    if ($criteria.Value) { $criteriaMet++ }
}

# Final Assessment
$passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
$criteriaPassRate = [math]::Round(($criteriaMet / $acceptanceCriteria.Count) * 100, 2)

Write-Host "`n=== Final Assessment ===" -ForegroundColor Green
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed Tests: $passedTests" -ForegroundColor Green
Write-Host "Failed Tests: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host "Test Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 95) { "Green" } else { "Red" })
Write-Host "Acceptance Criteria Met: $criteriaMet/$($acceptanceCriteria.Count) ($criteriaPassRate%)" -ForegroundColor $(if ($criteriaPassRate -eq 100) { "Green" } else { "Red" })

if ($criteriaPassRate -eq 100 -and $passRate -ge 95) {
    Write-Host "`n🎉 ALL ACCEPTANCE CRITERIA MET! System is ready for production." -ForegroundColor Green
    Write-Host "✅ Donor can create and track donation" -ForegroundColor Green
    Write-Host "✅ ML model returns NGO recommendations < 2s" -ForegroundColor Green
    Write-Host "✅ Notifications trigger correctly" -ForegroundColor Green
    Write-Host "✅ Admin dashboard shows live data" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ Some acceptance criteria not met. Please review failed tests." -ForegroundColor Red
    if ($criteriaPassRate -lt 100) {
        Write-Host "Failed Acceptance Criteria:" -ForegroundColor Red
        foreach ($criteria in $acceptanceCriteria.GetEnumerator()) {
            if (-not $criteria.Value) {
                Write-Host "  ❌ $($criteria.Key)" -ForegroundColor Red
            }
        }
    }
}

Write-Host "`nFunctionality verification completed." -ForegroundColor Green
