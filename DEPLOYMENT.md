# Deployment Guide - Azure App Service

This guide provides step-by-step instructions for deploying the Fatigue Monitoring Dashboard to Azure App Service.

## Prerequisites

- Azure subscription
- Azure CLI installed (`az` command)
- Access to SQL Server database (Azure SQL or on-premises)
- Git repository access

## 1. Prepare Azure Resources

### 1.1 Create Resource Group

```bash
az group create \
  --name rg-fatigue-monitoring \
  --location southeastasia
```

### 1.2 Create App Service Plan

```bash
az appservice plan create \
  --name asp-fatigue-monitoring \
  --resource-group rg-fatigue-monitoring \
  --sku B1 \
  --is-linux
```

### 1.3 Create Backend Web App

```bash
az webapp create \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --plan asp-fatigue-monitoring \
  --runtime "DOTNETCORE:9.0"
```

### 1.4 Create Frontend Web App (Optional - for separate frontend hosting)

```bash
az webapp create \
  --name fatigue-monitoring-web \
  --resource-group rg-fatigue-monitoring \
  --plan asp-fatigue-monitoring \
  --runtime "NODE:20-lts"
```

## 2. Configure Backend API

### 2.1 Set Application Settings

```bash
# Database Connection String
az webapp config connection-string set \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=your-server.database.windows.net;Database=SHE_DB;User Id=your-user;Password=your-password;Encrypt=True"

# External API Credentials
az webapp config appsettings set \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --settings \
    ExternalApi__Username="sis@mdvr" \
    ExternalApi__Password="Sis@mdvr12345"
```

### 2.2 Configure CORS

```bash
az webapp cors add \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --allowed-origins "https://fatigue-monitoring-web.azurewebsites.net" "http://localhost:5173"
```

### 2.3 Enable Always On (for background jobs)

```bash
az webapp config set \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --always-on true
```

## 3. Deploy Backend API

### Option A: Deploy from Local

```bash
cd FatigueMonitoring.Web.Api

# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --src deploy.zip
```

### Option B: Deploy from GitHub

```bash
# Configure GitHub deployment
az webapp deployment source config \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --repo-url https://github.com/usep-ai-code/FatigueMonitoring \
  --branch cursor/dashboard-monitoring-kelelahan-6e63 \
  --manual-integration
```

## 4. Initialize Database

### 4.1 Run Migrations

After deploying the API, you need to run the database migrations:

**Option 1: From local machine (recommended for first deployment)**

```bash
cd FatigueMonitoring.Web.Api

# Update appsettings.json with Azure SQL connection string temporarily
# Then run:
dotnet ef database update

# Don't forget to revert appsettings.json changes!
```

**Option 2: Using Azure App Service SSH**

1. Go to Azure Portal
2. Navigate to your App Service
3. Go to SSH / Advanced Tools
4. Run:
   ```bash
   cd /home/site/wwwroot
   dotnet ef database update --project FatigueMonitoring.Web.Api.dll
   ```

## 5. Deploy Frontend

### 5.1 Update Environment Variables

Update `fatigue-monitoring.react/.env.production`:

```env
VITE_API_URL=https://fatigue-monitoring-api.azurewebsites.net
```

### 5.2 Build Frontend

```bash
cd fatigue-monitoring.react

# Install dependencies
npm install

# Build for production
npm run build
```

### 5.3 Deploy Frontend

**Option A: Deploy to Azure Static Web Apps (Recommended)**

```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Deploy
az staticwebapp create \
  --name fatigue-monitoring-frontend \
  --resource-group rg-fatigue-monitoring \
  --source https://github.com/usep-ai-code/FatigueMonitoring \
  --location southeastasia \
  --branch cursor/dashboard-monitoring-kelelahan-6e63 \
  --app-location "fatigue-monitoring.react" \
  --output-location "dist"
```

**Option B: Deploy to App Service**

```bash
cd fatigue-monitoring.react/dist

# Create deployment package
zip -r ../frontend-deploy.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --name fatigue-monitoring-web \
  --resource-group rg-fatigue-monitoring \
  --src frontend-deploy.zip
```

### 5.4 Configure SPA Routing (if using App Service)

Create `web.config` in `fatigue-monitoring.react/public/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="React Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

## 6. Verify Deployment

### 6.1 Check API Health

```bash
curl https://fatigue-monitoring-api.azurewebsites.net/api/dashboard/stats?area=All
```

### 6.2 Check SSE Endpoint

```bash
curl -N https://fatigue-monitoring-api.azurewebsites.net/api/sse/stream
```

### 6.3 Access Frontend

Open browser and navigate to:
- Static Web App: `https://fatigue-monitoring-frontend.azurestaticapps.net`
- App Service: `https://fatigue-monitoring-web.azurewebsites.net`

## 7. Monitoring and Logs

### 7.1 Enable Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app fatigue-monitoring-insights \
  --location southeastasia \
  --resource-group rg-fatigue-monitoring

# Get instrumentation key
INSIGHTS_KEY=$(az monitor app-insights component show \
  --app fatigue-monitoring-insights \
  --resource-group rg-fatigue-monitoring \
  --query instrumentationKey -o tsv)

# Configure API to use Application Insights
az webapp config appsettings set \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSIGHTS_KEY"
```

### 7.2 View Logs

```bash
# Stream logs
az webapp log tail \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring

# Or download logs
az webapp log download \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --log-file logs.zip
```

## 8. Scaling

### 8.1 Scale Up (Vertical Scaling)

```bash
az appservice plan update \
  --name asp-fatigue-monitoring \
  --resource-group rg-fatigue-monitoring \
  --sku S1
```

### 8.2 Scale Out (Horizontal Scaling)

```bash
az appservice plan update \
  --name asp-fatigue-monitoring \
  --resource-group rg-fatigue-monitoring \
  --number-of-workers 3
```

### 8.3 Auto-scaling (for Production)

```bash
az monitor autoscale create \
  --resource-group rg-fatigue-monitoring \
  --resource fatigue-monitoring-api \
  --resource-type Microsoft.Web/sites \
  --name autoscale-fatigue-monitoring \
  --min-count 1 \
  --max-count 5 \
  --count 2

# Add CPU rule
az monitor autoscale rule create \
  --resource-group rg-fatigue-monitoring \
  --autoscale-name autoscale-fatigue-monitoring \
  --scale out 1 \
  --condition "CpuPercentage > 70 avg 5m"
```

## 9. Security Best Practices

### 9.1 Use Managed Identity for Database Access

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --query principalId -o tsv)

# Grant SQL access (run in SQL Server)
# CREATE USER [fatigue-monitoring-api] FROM EXTERNAL PROVIDER;
# ALTER ROLE db_datareader ADD MEMBER [fatigue-monitoring-api];
# ALTER ROLE db_datawriter ADD MEMBER [fatigue-monitoring-api];
```

### 9.2 Store Secrets in Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name kv-fatigue-monitoring \
  --resource-group rg-fatigue-monitoring \
  --location southeastasia

# Add secrets
az keyvault secret set \
  --vault-name kv-fatigue-monitoring \
  --name ExternalApiPassword \
  --value "Sis@mdvr12345"

# Reference in App Service
az webapp config appsettings set \
  --name fatigue-monitoring-api \
  --resource-group rg-fatigue-monitoring \
  --settings ExternalApi__Password="@Microsoft.KeyVault(SecretUri=https://kv-fatigue-monitoring.vault.azure.net/secrets/ExternalApiPassword/)"
```

## 10. Backup and Disaster Recovery

### 10.1 Configure Backup

```bash
# Create storage account for backups
az storage account create \
  --name stfatiguemonitoringbak \
  --resource-group rg-fatigue-monitoring \
  --location southeastasia \
  --sku Standard_LRS

# Configure backup
az webapp config backup create \
  --resource-group rg-fatigue-monitoring \
  --webapp-name fatigue-monitoring-api \
  --container-url "https://stfatiguemonitoringbak.blob.core.windows.net/backups" \
  --backup-name daily-backup \
  --frequency 1d \
  --retention 30
```

## 11. Troubleshooting

### Common Issues

**SSE Not Working:**
- Check if "Always On" is enabled
- Verify CORS settings
- Check firewall rules

**Background Job Not Running:**
- Check application logs
- Verify "Always On" is enabled
- Check External API credentials

**Slow Performance:**
- Enable Application Insights
- Scale up/out as needed
- Check database performance

## 12. Cost Optimization

- Use B1 or S1 tier for development/testing
- Use P1V2 or higher for production
- Enable auto-scaling to optimize costs
- Use Azure Reserved Instances for long-term deployments
- Monitor with Azure Cost Management

---

**Support:** Contact DevOps team for deployment assistance
**Last Updated:** February 1, 2026
