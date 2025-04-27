# Azure Deployment Guide for LifeTrack App

This guide will walk you through the steps to deploy your LifeTrack app to Azure using Static Web Apps with Azure Storage for data persistence.

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- [GitHub](https://github.com/) account
- Git installed on your machine

## Step 1: Create a GitHub Repository

1. Log in to GitHub and create a new repository
2. Note your repository URL (https://github.com/YOUR_USERNAME/YOUR_REPO_NAME)

## Step 2: Commit Your Code to Git

1. Make sure you're in the WebApp directory
2. Run the following commands:

```bash
# Initialize Git repository (already done)
# git init

# Add all files to the repository
git add .

# Commit the files
git commit -m "Initial commit"

# Add your GitHub repository as a remote
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git

# Push the code to GitHub
git push -u origin main
```

## Step 3: Create Azure Resources

### Option 1: Using the Azure Portal

1. Log in to the [Azure Portal](https://portal.azure.com)
2. Create a new Resource Group called `lifetrack-app-rg`
3. Create a new Static Web App:
   - Name: `lifetrack-app`
   - Link it to your GitHub repository
   - Build preset: Custom
   - App location: `/`
   - API location: Leave empty
   - Output location: `published/wwwroot`
4. Create a new Storage Account:
   - Name: `lifetrackdata` (add some random numbers for uniqueness)
   - Performance: Standard
   - Redundancy: Locally-redundant storage (LRS)
5. In the Storage Account:
   - Create a new container named `lifetrack-data`
   - Get the connection string from "Access keys"
6. In the Static Web App:
   - Go to Configuration
   - Add an application setting:
     - Name: `AZURE_STORAGE_CONNECTION_STRING`
     - Value: Paste the connection string you copied

### Option 2: Using the PowerShell Script

1. Update the GitHub repository URL in `deploy-to-azure.ps1`
2. Open a PowerShell terminal
3. Log in to Azure CLI by running:
   ```
   az login
   ```
4. Run the deployment script:
   ```
   ./deploy-to-azure.ps1
   ```

## Step 4: Configure GitHub Actions

1. In the Azure Portal, navigate to your Static Web App
2. Go to "Deployment token" and copy the token
3. In your GitHub repository:
   - Go to "Settings" > "Secrets and variables" > "Actions"
   - Add a new secret:
     - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
     - Value: Paste the deployment token you copied

## Step 5: Verify Deployment

1. GitHub Actions will automatically build and deploy your app
2. You can check the status in the "Actions" tab of your GitHub repository
3. Once deployed, access your app at the URL provided in the Static Web App overview page

## Troubleshooting

- If the deployment fails, check the GitHub Actions logs for errors
- Make sure the connection string is correctly set in the application settings
- Verify that the container name in `appsettings.json` matches the one created in Azure Storage
- If changes to your code are not reflecting in the deployed app, try pushing a new commit to trigger a redeployment