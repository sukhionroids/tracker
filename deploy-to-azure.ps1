#!/usr/bin/env pwsh

# This script creates the necessary Azure resources and deploys your app
# Make sure you've logged into Azure CLI with 'az login' before running this script

# Configuration variables - change these as needed
$resourceGroupName = "lifetrack-app-rg"
$location = "eastus"
$storageAccountName = "lifetrackdata$(Get-Random -Minimum 10000 -Maximum 99999)"
$staticWebAppName = "lifetrack-app"
$containerName = "lifetrack-data"

Write-Host "Creating Azure resources for LifeTrack app..." -ForegroundColor Green

# Create resource group
Write-Host "Creating resource group: $resourceGroupName..."
az group create --name $resourceGroupName --location $location

# Create storage account
Write-Host "Creating storage account: $storageAccountName..."
az storage account create `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS `
    --kind StorageV2 `
    --enable-hierarchical-namespace false

# Get storage account connection string
$connectionString = $(az storage account show-connection-string `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --query connectionString `
    --output tsv)

# Create storage container
Write-Host "Creating storage container: $containerName..."
az storage container create `
    --name $containerName `
    --account-name $storageAccountName `
    --connection-string $connectionString `
    --public-access off

# Create Static Web App
Write-Host "Creating Static Web App: $staticWebAppName..."
$staticWebApp = $(az staticwebapp create `
    --name $staticWebAppName `
    --resource-group $resourceGroupName `
    --location $location `
    --source https://github.com/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME `
    --branch main `
    --app-location "/" `
    --output-location "wwwroot" `
    --api-location "" `
    --login-with-github)

# Add application settings (connection string)
Write-Host "Adding storage connection string to Static Web App settings..."
az staticwebapp appsettings set `
    --name $staticWebAppName `
    --resource-group $resourceGroupName `
    --setting-names AZURE_STORAGE_CONNECTION_STRING="$connectionString"

Write-Host "`nAzure resources created successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Push your code to GitHub repository" -ForegroundColor Yellow
Write-Host "2. Get the deployment token from your Static Web App in the Azure Portal" -ForegroundColor Yellow
Write-Host "3. Add the token as a GitHub secret named AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Yellow
Write-Host "4. Update your GitHub repository URL in this script if you want to run it again" -ForegroundColor Yellow

Write-Host "`nStorage account name: $storageAccountName" -ForegroundColor Cyan
Write-Host "Static Web App name: $staticWebAppName" -ForegroundColor Cyan
Write-Host "Resource group: $resourceGroupName" -ForegroundColor Cyan