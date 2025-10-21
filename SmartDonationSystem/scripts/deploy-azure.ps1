# Azure Deployment Script for Smart Donation System
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$AppServiceName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlDatabaseName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlAdminUsername,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlAdminPassword,
    
    [Parameter(Mandatory=$true)]
    [string]$JwtSecretKey,
    
    [Parameter(Mandatory=$true)]
    [string]$GoogleMapsApiKey,
    
    [string]$Location = "East US",
    [string]$AppServicePlanName = "SmartDonationSystem-Plan"
)

Write-Host "Starting Azure deployment for Smart Donation System..." -ForegroundColor Green

# Login to Azure (if not already logged in)
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$context = Get-AzContext
if (-not $context) {
    Write-Host "Please login to Azure first using: Connect-AzAccount" -ForegroundColor Red
    exit 1
}

# Create Resource Group
Write-Host "Creating Resource Group: $ResourceGroupName" -ForegroundColor Yellow
New-AzResourceGroup -Name $ResourceGroupName -Location $Location -Force

# Create App Service Plan
Write-Host "Creating App Service Plan: $AppServicePlanName" -ForegroundColor Yellow
New-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlanName -Location $Location -Tier "Standard" -NumberofWorkers 1 -WorkerSize "Small"

# Create App Service
Write-Host "Creating App Service: $AppServiceName" -ForegroundColor Yellow
New-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -AppServicePlan $AppServicePlanName

# Create SQL Server
Write-Host "Creating SQL Server: $SqlServerName" -ForegroundColor Yellow
New-AzSqlServer -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -Location $Location -SqlAdministratorCredentials (New-Object System.Management.Automation.PSCredential($SqlAdminUsername, $SqlAdminPassword))

# Create SQL Database
Write-Host "Creating SQL Database: $SqlDatabaseName" -ForegroundColor Yellow
New-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -Edition "Standard" -RequestedServiceObjectiveName "S0"

# Configure App Service Settings
Write-Host "Configuring App Service settings..." -ForegroundColor Yellow
$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$SqlDatabaseName;Persist Security Info=False;User ID=$SqlAdminUsername;Password=[SECURE];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$appSettings = @{
    "ConnectionStrings__DefaultConnection" = $connectionString
    "JwtSettings__SecretKey" = $JwtSecretKey
    "GoogleMaps__ApiKey" = $GoogleMapsApiKey
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "ASPNETCORE_URLS" = "https://+:443;http://+:80"
}

Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -AppSettings $appSettings

# Enable Application Insights
Write-Host "Enabling Application Insights..." -ForegroundColor Yellow
$appInsights = New-AzApplicationInsights -ResourceGroupName $ResourceGroupName -Name "$AppServiceName-insights" -Location $Location
$instrumentationKey = $appInsights.InstrumentationKey
Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -AppSettings @{"APPINSIGHTS_INSTRUMENTATIONKEY" = $instrumentationKey}

# Configure CORS
Write-Host "Configuring CORS..." -ForegroundColor Yellow
$corsRules = @{
    "AllowedOrigins" = @("https://$AppServiceName.azurewebsites.net", "https://www.$AppServiceName.com")
}
Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -CorsRules $corsRules

# Configure SSL
Write-Host "Configuring SSL..." -ForegroundColor Yellow
Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -HttpsOnly $true

Write-Host "Azure deployment completed successfully!" -ForegroundColor Green
Write-Host "App Service URL: https://$AppServiceName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "SQL Server: $SqlServerName.database.windows.net" -ForegroundColor Cyan
Write-Host "Database: $SqlDatabaseName" -ForegroundColor Cyan
Write-Host "Application Insights Key: $instrumentationKey" -ForegroundColor Cyan

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Publish your application to the App Service" -ForegroundColor White
Write-Host "2. Run database migrations: dotnet ef database update" -ForegroundColor White
Write-Host "3. Configure custom domain (optional)" -ForegroundColor White
Write-Host "4. Set up monitoring and alerts" -ForegroundColor White
