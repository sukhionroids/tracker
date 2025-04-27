# Azure AD Authentication Setup Guide

This document guides you through setting up Azure AD authentication for the LifeTrack application.

## 1. Register an Application in Azure AD

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Search for and select "Azure Active Directory"
3. In the left navigation, select "App registrations" and click "New registration"
4. Enter a name for your application (e.g., "LifeTrack")
5. For "Supported account types", select "Accounts in any organizational directory and personal Microsoft accounts"
6. For "Redirect URI":
   - Select "Single-page application (SPA)" from the dropdown
   - Enter your application's redirect URI (e.g., `https://yourapp.azurewebsites.net/authentication/login-callback` for production or `https://localhost:5001/authentication/login-callback` for development)
7. Click "Register"

## 2. Configure the Application

1. After registration, note your "Application (client) ID" - you'll need this for your app configuration
2. In the left navigation of your app registration, go to "Authentication"
3. Under "Implicit grant and hybrid flows", check both:
   - Access tokens
   - ID tokens
4. Add any additional redirect URIs if needed (e.g., for both production and development environments)
5. Click "Save"

## 3. Configure API Permissions (if needed)

1. In the left navigation of your app registration, go to "API permissions"
2. Click "Add a permission"
3. Select "Microsoft Graph"
4. Choose "Delegated permissions"
5. Add the permissions your application needs (at minimum, "User.Read")
6. Click "Add permissions"

## 4. Update Application Configuration

In your application's `appsettings.json`, update the Azure AD configuration:

```json
"AzureAd": {
  "Authority": "https://login.microsoftonline.com/common",
  "ClientId": "YOUR_CLIENT_ID_HERE",  // Replace with the Application (client) ID from step 2
  "ValidateAuthority": true
}
```

## 5. Deploying to Azure

When deploying to Azure, make sure to:

1. Update the redirect URIs in your Azure AD app registration to include your production URL
2. Set the appropriate `AzureAd` configuration in your deployed application settings

## Troubleshooting

* If authentication fails, ensure your redirect URIs are correctly configured in both the app and Azure AD
* Check browser console for any CORS errors
* Verify the ClientId in your application matches the Application ID in Azure AD
* For local development, ensure you're using HTTPS