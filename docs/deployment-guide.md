# Financial Aid Platform - Deployment Guide

## Overview
This guide covers the deployment of the Financial Aid Platform infrastructure and application to Azure cloud services.

## Prerequisites

### Required Tools
- Azure CLI (v2.50+)
- .NET 8.0 SDK
- Git
- PowerShell or Bash

### Required Azure Permissions
- Contributor role on target subscription
- Key Vault Administrator role
- Application Developer role in Azure AD

### Required Secrets
The following secrets must be configured in GitHub Secrets for automated deployment:

#### Repository Secrets
```
AZURE_CLIENT_ID=<service-principal-client-id>
AZURE_TENANT_ID=<azure-tenant-id>
AZURE_SUBSCRIPTION_ID=<azure-subscription-id>
DEV_SQL_ADMIN_PASSWORD=<development-sql-admin-password>
```

## Infrastructure Deployment

### Automated Deployment via GitHub Actions

1. **Trigger Infrastructure Deployment**
   ```bash
   # Navigate to GitHub Actions tab in repository
   # Select "Deploy Infrastructure" workflow
   # Click "Run workflow"
   # Select target environment (dev/prod)
   ```

2. **Monitor Deployment**
   - Check GitHub Actions logs for progress
   - Verify resource creation in Azure Portal
   - Validate connectivity tests

### Manual Deployment

1. **Login to Azure**
   ```bash
   az login
   az account set --subscription <subscription-id>
   ```

2. **Create Resource Group**
   ```bash
   az group create \
     --name rg-finaid-dev \
     --location eastus2 \
     --tags Environment=development Application=finaid
   ```

3. **Deploy Infrastructure**
   ```bash
   az deployment group create \
     --resource-group rg-finaid-dev \
     --template-file Infrastructure/azure-resources.bicep \
     --parameters environmentName=dev
   ```

## Post-Deployment Configuration

### Key Vault Secret Configuration

#### Development Environment
```bash
# SQL Database
az keyvault secret set \
  --vault-name kv-finaid-dev \
  --name sql-connection-string \
  --value "Server=tcp:sql-finaid-dev.database.windows.net,1433;Database=sqldb-finaid-dev;Authentication=Active Directory Default;"

# Redis Cache
REDIS_KEY=$(az redis list-keys --resource-group rg-finaid-dev --name redis-finaid-dev --query primaryKey -o tsv)
az keyvault secret set \
  --vault-name kv-finaid-dev \
  --name redis-connection-string \
  --value "redis-finaid-dev.redis.cache.windows.net:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

# Storage Account
STORAGE_KEY=$(az storage account keys list --resource-group rg-finaid-dev --account-name stfinaiddev --query [0].value -o tsv)
az keyvault secret set \
  --vault-name kv-finaid-dev \
  --name storage-connection-string \
  --value "DefaultEndpointsProtocol=https;AccountName=stfinaiddev;AccountKey=${STORAGE_KEY};EndpointSuffix=core.windows.net"
```

#### Production Environment
Production secrets must be configured manually with proper values:

```bash
# Required production secrets
az keyvault secret set --vault-name kv-finaid-prod --name sql-admin-password --value "<secure-password>"
az keyvault secret set --vault-name kv-finaid-prod --name aad-client-id --value "<azure-ad-app-id>"
az keyvault secret set --vault-name kv-finaid-prod --name aad-client-secret --value "<azure-ad-app-secret>"
az keyvault secret set --vault-name kv-finaid-prod --name openai-endpoint --value "<azure-openai-endpoint>"
az keyvault secret set --vault-name kv-finaid-prod --name openai-api-key --value "<azure-openai-key>"
az keyvault secret set --vault-name kv-finaid-prod --name federal-api-key --value "<federal-studentaid-api-key>"
```

### Application Service Configuration

1. **Configure App Service Identity**
   ```bash
   az webapp identity assign \
     --resource-group rg-finaid-dev \
     --name app-finaid-dev
   ```

2. **Grant Key Vault Access**
   ```bash
   PRINCIPAL_ID=$(az webapp identity show --resource-group rg-finaid-dev --name app-finaid-dev --query principalId -o tsv)
   az keyvault set-policy \
     --name kv-finaid-dev \
     --object-id $PRINCIPAL_ID \
     --secret-permissions get list
   ```

### Database Setup

1. **Run Entity Framework Migrations**
   ```bash
   # From application directory
   dotnet ef database update --environment Development
   ```

2. **Verify Database Connection**
   ```bash
   # Test application startup
   dotnet run --environment Development
   ```

## Validation and Testing

### Infrastructure Validation Checklist

- [ ] All Azure resources deployed successfully
- [ ] App Service can start without errors
- [ ] SQL Database is accessible and migrations applied
- [ ] Redis Cache connection working
- [ ] Key Vault secrets accessible by App Service
- [ ] Application Insights receiving telemetry
- [ ] Storage Account containers created

### Connectivity Tests

```bash
# Test App Service
curl -k https://app-finaid-dev.azurewebsites.net/health

# Test SQL Database (from App Service console)
# Navigate to App Service -> Development Tools -> Console
sqlcmd -S sql-finaid-dev.database.windows.net -d sqldb-finaid-dev -G

# Test Redis (from App Service console)
redis-cli -h redis-finaid-dev.redis.cache.windows.net -p 6380 -a <redis-key> --tls ping
```

## Monitoring and Maintenance

### Key Metrics to Monitor
- App Service response times and error rates
- SQL Database DTU/CPU utilization
- Redis Cache hit rates
- Application Insights custom metrics
- Key Vault access patterns

### Regular Maintenance Tasks
- Review and rotate Key Vault secrets quarterly
- Update SSL certificates annually
- Monitor and optimize SQL Database performance
- Review Application Insights logs weekly
- Update infrastructure templates as needed

### Backup and Recovery
- SQL Database: Automated backups enabled (7-day retention)
- Storage Account: Enable soft delete and versioning
- Key Vault: Soft delete enabled (90-day retention)
- Infrastructure: Version-controlled Bicep templates

## Troubleshooting

### Common Issues

1. **App Service Won't Start**
   - Check Application Insights logs
   - Verify Key Vault access permissions
   - Validate connection strings in configuration

2. **Database Connection Failures**
   - Verify firewall rules allow Azure services
   - Check SQL Server authentication method
   - Validate connection string format

3. **Key Vault Access Denied**
   - Verify App Service managed identity is assigned
   - Check Key Vault access policies
   - Validate secret names and URIs

### Support Contacts
- Infrastructure Issues: Cloud Platform Team
- Application Issues: Development Team
- Security Issues: Security Operations Center

## Rollback Procedures

### Emergency Rollback
1. **Application Rollback**
   ```bash
   # Restore previous deployment slot
   az webapp deployment slot swap \
     --resource-group rg-finaid-prod \
     --name app-finaid-prod \
     --slot staging \
     --target-slot production
   ```

2. **Database Rollback**
   ```bash
   # Restore from point-in-time backup
   az sql db restore \
     --resource-group rg-finaid-prod \
     --server sql-finaid-prod \
     --name sqldb-finaid-prod \
     --dest-name sqldb-finaid-prod-restored \
     --time "2024-01-15T10:30:00"
   ```

3. **Complete Infrastructure Rollback**
   ```bash
   # Only use in extreme cases - will delete all resources
   az group delete --name rg-finaid-prod --yes
   ```

This deployment guide ensures reliable, secure, and maintainable infrastructure for the Financial Aid Platform.