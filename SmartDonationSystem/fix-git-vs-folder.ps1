# Script to remove .vs folder from Git tracking
# Run this script in PowerShell

Write-Host "Removing .vs folder from Git tracking..." -ForegroundColor Yellow

# Check if Git is available
$gitPath = Get-Command git -ErrorAction SilentlyContinue
if (-not $gitPath) {
    Write-Host "Git is not found in PATH. Please install Git or add it to your PATH." -ForegroundColor Red
    Write-Host "You can download Git from: https://git-scm.com/download/win" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternatively, you can:" -ForegroundColor Yellow
    Write-Host "1. Close Visual Studio completely" -ForegroundColor Cyan
    Write-Host "2. Manually delete the .vs folder" -ForegroundColor Cyan
    Write-Host "3. Use GitHub Desktop or another Git client to commit" -ForegroundColor Cyan
    exit 1
}

# Remove .vs folder from Git cache (but keep it locally)
git rm -r --cached .vs/

# If that doesn't work, try removing specific files
if ($LASTEXITCODE -ne 0) {
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
    git rm -r --cached "SmartDonationSystem/.vs/" 2>$null
}

Write-Host ""
Write-Host "Done! Now you can commit your changes." -ForegroundColor Green
Write-Host "The .vs folder will be ignored in future commits." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. git add .gitignore" -ForegroundColor Cyan
Write-Host "2. git commit -m 'Add .gitignore and remove .vs folder'" -ForegroundColor Cyan
Write-Host "3. git push" -ForegroundColor Cyan

