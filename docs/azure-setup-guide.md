# Azure Resources Setup Guide

This document provides step-by-step instructions for setting up the required Azure resources for the FinAid Assistant application.

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed and configured
- Access to domain management for custom domains

## Resource Overview

The following Azure resources are required for production deployment:

### Resource Group
- **Name**: `rg-finaid-prod`
- **Location**: East US (or preferred region)
- **Purpose**: Container for all FinAid Assistant resources

### App Service Plan
- **Name**: `asp-finaid-prod`
- **Tier**: Standard S1
- **Operating System**: Linux
- **Purpose**: Hosting platform for the Blazor Server application

### App Service
- **Name**: `app-finaid-prod`
- **Runtime Stack**: .NET 8
- **Purpose**: Hosts the main FinAid Assistant application

### Azure SQL Database
- **Server Name**: `sql-finaid-prod`
- **Database Name**: `sqldb-finaid-prod`
- **Tier**: Basic (50 DTU)
- **Purpose**: Primary data storage for user profiles, applications, and documents

### Redis Cache
- **Name**: `redis-finaid-prod`
- **Tier**: Basic C0 (250MB)
- **Purpose**: Session state, application state, and caching

### Application Insights
- **Name**: `appi-finaid-prod`
- **Purpose**: Application performance monitoring and logging

### Key Vault
- **Name**: `kv-finaid-prod`
- **Purpose**: Secure storage for connection strings, API keys, and secrets

### Storage Account
- **Name**: `stfinaidprod`
- **Type**: StorageV2 (general purpose v2)
- **Performance**: Standard
- **Replication**: LRS (Locally Redundant Storage)
- **Purpose**: Document storage for uploaded files

## Setup Instructions

### 1. Create Resource Group

```bash
az group create \
  --name rg-finaid-prod \
  --location eastus
```

### 2. Create App Service Plan

```bash
az appservice plan create \
  --name asp-finaid-prod \
  --resource-group rg-finaid-prod \
  --sku S1 \
  --is-linux
```

### 3. Create App Service

```bash
az webapp create \
  --name app-finaid-prod \
  --resource-group rg-finaid-prod \
  --plan asp-finaid-prod \
  --runtime "DOTNETCORE:8.0"
```

### 4. Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name sql-finaid-prod \
  --resource-group rg-finaid-prod \
  --location eastus \
  --admin-user sqladmin \
  --admin-password <SecurePassword>

# Create Database
az sql db create \
  --resource-group rg-finaid-prod \
  --server sql-finaid-prod \
  --name sqldb-finaid-prod \
  --service-objective Basic
```

### 5. Create Redis Cache

```bash
az redis create \
  --location eastus \
  --name redis-finaid-prod \
  --resource-group rg-finaid-prod \
  --sku Basic \
  --vm-size c0
```

### 6. Create Application Insights

```bash
az extension add -n application-insights

az monitor app-insights component create \
  --app appi-finaid-prod \
  --location eastus \
  --resource-group rg-finaid-prod
```

### 7. Create Key Vault

```bash
az keyvault create \
  --name kv-finaid-prod \
  --resource-group rg-finaid-prod \
  --location eastus \
  --sku standard
```

### 8. Create Storage Account

```bash
az storage account create \
  --name stfinaidprod \
  --resource-group rg-finaid-prod \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2
```

## Configuration Steps

### 1. Configure App Service Settings

```bash
# Set connection strings and app settings
az webapp config appsettings set \
  --resource-group rg-finaid-prod \
  --name app-finaid-prod \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    WEBSITE_TIME_ZONE="Eastern Standard Time"

# Configure connection strings to use Key Vault references
az webapp config connection-string set \
  --resource-group rg-finaid-prod \
  --name app-finaid-prod \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="@Microsoft.KeyVault(SecretUri=https://kv-finaid-prod.vault.azure.net/secrets/sql-connection-string/)"
```

### 2. Store Secrets in Key Vault

```bash
# SQL Connection String
az keyvault secret set \
  --vault-name kv-finaid-prod \
  --name "sql-connection-string" \
  --value "Server=tcp:sql-finaid-prod.database.windows.net,1433;Database=sqldb-finaid-prod;..."

# Redis Connection String
az keyvault secret set \
  --vault-name kv-finaid-prod \
  --name "redis-connection-string" \
  --value "redis-finaid-prod.redis.cache.windows.net:6380,password=..."

# Storage Connection String
az keyvault secret set \
  --vault-name kv-finaid-prod \
  --name "storage-connection-string" \
  --value "DefaultEndpointsProtocol=https;AccountName=stfinaidprod;..."
```

### 3. Configure Managed Identity

```bash
# Enable system-assigned managed identity for the App Service
az webapp identity assign \
  --name app-finaid-prod \
  --resource-group rg-finaid-prod

# Grant Key Vault access to the App Service
az keyvault set-policy \
  --name kv-finaid-prod \
  --object-id <app-service-identity-principal-id> \
  --secret-permissions get list
```

### 4. Configure Custom Domain and SSL

```bash
# Map custom domain (requires DNS configuration)
az webapp config hostname add \
  --webapp-name app-finaid-prod \
  --resource-group rg-finaid-prod \
  --hostname yourdomain.com

# Enable HTTPS only
az webapp update \
  --name app-finaid-prod \
  --resource-group rg-finaid-prod \
  --https-only true
```

## Security Configuration

### 1. Network Security

- Configure SQL Server firewall rules
- Enable Private Endpoints for sensitive resources
- Set up Virtual Network integration if required

### 2. Data Protection

- Enable Azure SQL Database Transparent Data Encryption (TDE)
- Configure backup retention policies
- Enable geo-redundant backups for critical data

### 3. Access Control

- Implement Role-Based Access Control (RBAC)
- Configure Azure AD authentication for management access
- Enable diagnostic logging for all resources

## Monitoring and Alerting

### 1. Application Insights Configuration

- Configure custom telemetry
- Set up availability tests
- Create performance alerts

### 2. Azure Monitor Alerts

- Database performance alerts
- App Service health alerts
- Storage capacity alerts

## Cost Optimization

### Resource Tagging

```bash
# Apply consistent tags for cost tracking
az resource tag \
  --tags Environment=Production Project=FinAid-Assistant \
  --ids $(az resource list --resource-group rg-finaid-prod --query '[].id' -o tsv)
```

### Scaling Considerations

- Configure auto-scaling for App Service Plan
- Monitor and optimize database DTU usage
- Implement Redis cache expiration policies

## Backup and Disaster Recovery

### 1. Database Backups

- Configure automated backups with point-in-time restore
- Set up geo-redundant backup storage
- Test restore procedures regularly

### 2. Application Backups

- Enable App Service backup for configuration and content
- Configure backup to separate storage account
- Document restoration procedures

## Troubleshooting

### Common Issues

1. **Key Vault Access Denied**
   - Verify managed identity is enabled
   - Check Key Vault access policies
   - Confirm secret URIs are correct

2. **Database Connection Failures**
   - Verify firewall rules
   - Check connection string format
   - Test network connectivity

3. **App Service Deployment Issues**
   - Review deployment logs
   - Check runtime stack configuration
   - Verify app settings and connection strings

## Next Steps

After completing the Azure resources setup:

1. Deploy the application using CI/CD pipeline
2. Configure Azure AD B2C for authentication
3. Set up monitoring dashboards
4. Perform security assessment
5. Execute disaster recovery tests

For deployment instructions, see [CI/CD Pipeline Setup Guide](./cicd-setup-guide.md).
For authentication setup, see [Azure AD B2C Setup Guide](./authentication-setup-guide.md).