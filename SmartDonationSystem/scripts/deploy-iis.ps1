# IIS Deployment Script for Smart Donation System
param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName = "SmartDonationSystem",
    
    [Parameter(Mandatory=$true)]
    [string]$PhysicalPath,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlDatabaseName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlUsername,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlPassword,
    
    [Parameter(Mandatory=$true)]
    [string]$JwtSecretKey,
    
    [Parameter(Mandatory=$true)]
    [string]$GoogleMapsApiKey,
    
    [int]$Port = 80,
    [int]$HttpsPort = 443
)

Write-Host "Starting IIS deployment for Smart Donation System..." -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Please run as Administrator." -ForegroundColor Red
    exit 1
}

# Import WebAdministration module
Import-Module WebAdministration -Force

# Create Application Pool
Write-Host "Creating Application Pool: $SiteName" -ForegroundColor Yellow
if (Get-IISAppPool -Name $SiteName -ErrorAction SilentlyContinue) {
    Remove-WebAppPool -Name $SiteName
}
New-WebAppPool -Name $SiteName -Force
Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name processModel.identityType -Value ApplicationPoolIdentity
Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name managedRuntimeVersion -Value ""

# Create Website
Write-Host "Creating Website: $SiteName" -ForegroundColor Yellow
if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $SiteName
}

New-Website -Name $SiteName -Port $Port -PhysicalPath $PhysicalPath -ApplicationPool $SiteName

# Configure HTTPS (if certificate is available)
if ($HttpsPort -ne 443) {
    Write-Host "Configuring HTTPS on port $HttpsPort..." -ForegroundColor Yellow
    New-WebBinding -Name $SiteName -Protocol https -Port $HttpsPort
}

# Configure Application Settings
Write-Host "Configuring Application Settings..." -ForegroundColor Yellow
$connectionString = "Server=$SqlServerName;Database=$SqlDatabaseName;User Id=$SqlUsername;Password=[SECURE];TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"

# Set web.config transformations
$webConfigPath = Join-Path $PhysicalPath "web.config"
if (Test-Path $webConfigPath) {
    Write-Host "Updating web.config with connection string..." -ForegroundColor Yellow
    $webConfig = [xml](Get-Content $webConfigPath)
    
    # Update connection string
    $connectionStringNode = $webConfig.configuration.connectionStrings.add | Where-Object { $_.name -eq "DefaultConnection" }
    if ($connectionStringNode) {
        $connectionStringNode.connectionString = $connectionString
    } else {
        $newConnectionString = $webConfig.CreateElement("add")
        $newConnectionString.SetAttribute("name", "DefaultConnection")
        $newConnectionString.SetAttribute("connectionString", $connectionString)
        $webConfig.configuration.connectionStrings.AppendChild($newConnectionString)
    }
    
    # Update app settings
    $appSettings = @{
        "JwtSettings:SecretKey" = $JwtSecretKey
        "GoogleMaps:ApiKey" = $GoogleMapsApiKey
        "ASPNETCORE_ENVIRONMENT" = "Production"
    }
    
    foreach ($setting in $appSettings.GetEnumerator()) {
        $appSettingNode = $webConfig.configuration.appSettings.add | Where-Object { $_.key -eq $setting.Key }
        if ($appSettingNode) {
            $appSettingNode.value = $setting.Value
        } else {
            $newAppSetting = $webConfig.CreateElement("add")
            $newAppSetting.SetAttribute("key", $setting.Key)
            $newAppSetting.SetAttribute("value", $setting.Value)
            $webConfig.configuration.appSettings.AppendChild($newAppSetting)
        }
    }
    
    $webConfig.Save($webConfigPath)
}

# Configure URL Rewrite (if needed)
Write-Host "Configuring URL Rewrite..." -ForegroundColor Yellow
$urlRewritePath = Join-Path $PhysicalPath "web.config"
if (Test-Path $urlRewritePath) {
    # Add URL rewrite rules for SPA routing
    $webConfig = [xml](Get-Content $urlRewritePath)
    $rewriteNode = $webConfig.configuration.'system.webServer'.rewrite
    if (-not $rewriteNode) {
        $rewriteNode = $webConfig.CreateElement("rewrite")
        $webConfig.configuration.'system.webServer'.AppendChild($rewriteNode)
    }
    
    $rulesNode = $rewriteNode.rules
    if (-not $rulesNode) {
        $rulesNode = $webConfig.CreateElement("rules")
        $rewriteNode.AppendChild($rulesNode)
    }
    
    # Add SPA fallback rule
    $fallbackRule = $webConfig.CreateElement("rule")
    $fallbackRule.SetAttribute("name", "SPA Fallback")
    $fallbackRule.SetAttribute("stopProcessing", "true")
    
    $matchNode = $webConfig.CreateElement("match")
    $matchNode.SetAttribute("url", ".*")
    $fallbackRule.AppendChild($matchNode)
    
    $conditionsNode = $webConfig.CreateElement("conditions")
    $conditionNode = $webConfig.CreateElement("add")
    $conditionNode.SetAttribute("input", "{REQUEST_FILENAME}")
    $conditionNode.SetAttribute("matchType", "IsFile")
    $conditionsNode.AppendChild($conditionNode)
    $conditionNode2 = $webConfig.CreateElement("add")
    $conditionNode2.SetAttribute("input", "{REQUEST_FILENAME}")
    $conditionNode2.SetAttribute("matchType", "IsDirectory")
    $conditionsNode.AppendChild($conditionNode2)
    $fallbackRule.AppendChild($conditionsNode)
    
    $actionNode = $webConfig.CreateElement("action")
    $actionNode.SetAttribute("type", "Rewrite")
    $actionNode.SetAttribute("url", "/")
    $fallbackRule.AppendChild($actionNode)
    
    $rulesNode.AppendChild($fallbackRule)
    $webConfig.Save($urlRewritePath)
}

# Set permissions
Write-Host "Setting folder permissions..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path $PhysicalPath -AclObject $acl

# Configure logging
Write-Host "Configuring logging..." -ForegroundColor Yellow
Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name logFile.directory -Value "C:\inetpub\logs\LogFiles\$SiteName"
Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name logFile.logFormat -Value "W3C"
Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name logFile.logExtFileFlags -Value "Date,Time,ClientIP,UserName,ServerIP,Method,UriStem,UriQuery,HttpStatus,Win32Status,TimeTaken,ServerPort,UserAgent,Referer"

# Start Application Pool
Write-Host "Starting Application Pool..." -ForegroundColor Yellow
Start-WebAppPool -Name $SiteName

# Start Website
Write-Host "Starting Website..." -ForegroundColor Yellow
Start-Website -Name $SiteName

Write-Host "IIS deployment completed successfully!" -ForegroundColor Green
Write-Host "Website URL: http://localhost:$Port" -ForegroundColor Cyan
if ($HttpsPort -ne 443) {
    Write-Host "HTTPS URL: https://localhost:$HttpsPort" -ForegroundColor Cyan
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Run database migrations: dotnet ef database update" -ForegroundColor White
Write-Host "2. Configure SSL certificate for HTTPS" -ForegroundColor White
Write-Host "3. Set up monitoring and logging" -ForegroundColor White
Write-Host "4. Configure firewall rules if needed" -ForegroundColor White
