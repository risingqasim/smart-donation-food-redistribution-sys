# Script to fix GitHub repository access issues
# Run this script in PowerShell

Write-Host "=== GitHub Repository Access Fix ===" -ForegroundColor Cyan
Write-Host ""

$repoUrl = "https://github.com/risingqasim/smart-donation-food-redistribution-sys.git"

Write-Host "Current remote URL: $repoUrl" -ForegroundColor Yellow
Write-Host ""

Write-Host "Possible issues and solutions:" -ForegroundColor Green
Write-Host ""

Write-Host "1. Repository doesn't exist on GitHub" -ForegroundColor Yellow
Write-Host "   Solution: Create a new repository on GitHub with the same name" -ForegroundColor Cyan
Write-Host "   Steps:" -ForegroundColor White
Write-Host "   - Go to https://github.com/new" -ForegroundColor Gray
Write-Host "   - Repository name: smart-donation-food-redistribution-sys" -ForegroundColor Gray
Write-Host "   - Make it Public or Private" -ForegroundColor Gray
Write-Host "   - DO NOT initialize with README, .gitignore, or license" -ForegroundColor Gray
Write-Host "   - Click 'Create repository'" -ForegroundColor Gray
Write-Host ""

Write-Host "2. Repository was renamed" -ForegroundColor Yellow
Write-Host "   Solution: Update the remote URL" -ForegroundColor Cyan
Write-Host "   Command: git remote set-url origin <NEW_URL>" -ForegroundColor Gray
Write-Host ""

Write-Host "3. Authentication issue" -ForegroundColor Yellow
Write-Host "   Solution: Use Personal Access Token (PAT)" -ForegroundColor Cyan
Write-Host "   Steps:" -ForegroundColor White
Write-Host "   - Go to https://github.com/settings/tokens" -ForegroundColor Gray
Write-Host "   - Generate new token (classic)" -ForegroundColor Gray
Write-Host "   - Select scopes: repo (all)" -ForegroundColor Gray
Write-Host "   - Copy the token" -ForegroundColor Gray
Write-Host "   - Use: git remote set-url origin https://<TOKEN>@github.com/risingqasim/smart-donation-food-redistribution-sys.git" -ForegroundColor Gray
Write-Host ""

Write-Host "4. Repository is private and you don't have access" -ForegroundColor Yellow
Write-Host "   Solution: Request access from repository owner" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Quick Fix Options ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Option A: Create new repository on GitHub" -ForegroundColor Green
Write-Host "   After creating, run these commands:" -ForegroundColor White
Write-Host "   git remote set-url origin https://github.com/risingqasim/smart-donation-food-redistribution-sys.git" -ForegroundColor Gray
Write-Host "   git push -u origin main" -ForegroundColor Gray
Write-Host ""

Write-Host "Option B: Update remote URL (if repository was renamed)" -ForegroundColor Green
Write-Host "   git remote set-url origin <NEW_REPOSITORY_URL>" -ForegroundColor Gray
Write-Host ""

Write-Host "Option C: Remove remote and add new one" -ForegroundColor Green
Write-Host "   git remote remove origin" -ForegroundColor Gray
Write-Host "   git remote add origin <NEW_REPOSITORY_URL>" -ForegroundColor Gray
Write-Host "   git push -u origin main" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Check Current Status ===" -ForegroundColor Cyan
Write-Host ""

# Try to check if Git is available
$gitPath = Get-Command git -ErrorAction SilentlyContinue
if ($gitPath) {
    Write-Host "Git is available. Checking remote..." -ForegroundColor Green
    git remote -v
    Write-Host ""
    Write-Host "Checking local branches..." -ForegroundColor Green
    git branch
} else {
    Write-Host "Git is not in PATH. Please:" -ForegroundColor Yellow
    Write-Host "1. Install Git from https://git-scm.com/download/win" -ForegroundColor Cyan
    Write-Host "2. Or use GitHub Desktop: https://desktop.github.com/" -ForegroundColor Cyan
    Write-Host "3. Or use Visual Studio's built-in Git tools" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Verify repository exists: https://github.com/risingqasim/smart-donation-food-redistribution-sys" -ForegroundColor White
Write-Host "2. If it doesn't exist, create it on GitHub" -ForegroundColor White
Write-Host "3. Then try pushing again" -ForegroundColor White

