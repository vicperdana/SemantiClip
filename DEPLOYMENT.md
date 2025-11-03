# SemantiClip - Azure Deployment Guide

This guide provides complete instructions for deploying SemantiClip to Azure using Azure Developer CLI (`azd`).

## Prerequisites

### 1. Install Required Tools

#### Azure CLI
```bash
# macOS
brew install azure-cli

# Windows
# Download from https://aka.ms/installazurecliwindows

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

#### Azure Developer CLI (azd)
```bash
# macOS/Linux
curl -fsSL https://aka.ms/install-azd.sh | bash

# Windows (PowerShell)
powershell -ex AllSigned -c "Invoke-RestMethod 'https://aka.ms/install-azd.ps1' | Invoke-Expression"

# Verify installation
azd version
```

#### .NET 8 SDK
```bash
# macOS
brew install dotnet-sdk

# Windows/Linux
# Download from https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. Azure Resources Setup

Before deploying, you need to set up the following Azure resources:

#### Azure OpenAI Service
1. Create an Azure OpenAI resource in Azure Portal
2. Deploy two models:
   - **gpt-4o** (or gpt-4) - for content generation
   - **whisper** - for audio transcription
3. Note down:
   - Endpoint URL: `https://<your-service>.openai.azure.com/`
   - API Key (from Keys and Endpoint section)
   - Deployment names

#### Azure AI Agent Service
1. Create an Azure Machine Learning workspace or Azure AI Studio project
2. Set up vector store/index for semantic search
3. Note down the connection string format:
   ```
   <region>.api.azureml.ms;<subscription-id>;<resource-group>;<workspace-name>
   ```

#### GitHub Personal Access Token
1. Go to https://github.com/settings/tokens
2. Generate a new token (classic)
3. Required scopes:
   - `repo` (for private repositories)
   - `public_repo` (for public repositories only)
4. Copy and save the token securely

## Deployment Steps

### Step 1: Clone and Navigate to Repository

```bash
git clone https://github.com/vicperdana/SemantiClip.git
cd SemantiClip
```

### Step 2: Login to Azure

```bash
# Login to Azure
az login

# Set your subscription
az account set --subscription "your-subscription-id"

# Login to Azure Developer CLI
azd auth login
```

### Step 3: Configure Environment Variables

```bash
# Initialize azd environment (if not already done)
azd env new semanticlip-env

# Or select existing environment
azd env select semanticlip-env

# Set required environment variables
azd env set AZURE_LOCATION "eastus"
azd env set AZURE_OPENAI_ENDPOINT "https://your-openai.openai.azure.com/"
azd env set AZURE_OPENAI_API_KEY "your-api-key"
azd env set AZURE_OPENAI_CONTENT_DEPLOYMENT_NAME "gpt-4o"
azd env set AZURE_OPENAI_WHISPER_DEPLOYMENT_NAME "whisper"
azd env set AZURE_AI_AGENT_CONNECTION_STRING "region.api.azureml.ms;sub-id;rg;workspace"
azd env set AZURE_AI_AGENT_CHAT_MODEL_ID "gpt-4o"
azd env set AZURE_AI_AGENT_VECTOR_STORE_ID "semanticclipproject"
azd env set GITHUB_PERSONAL_ACCESS_TOKEN "your-github-pat"

# Optional: Set custom values (these have defaults)
azd env set AZURE_AI_AGENT_MAX_EVALUATIONS "3"
azd env set FILE_UPLOAD_MAX_REQUEST_BODY_SIZE "1073741824"
azd env set FILE_UPLOAD_ALLOWED_EXTENSIONS ".mp4,.avi,.mov,.wmv,.mkv"
```

**Alternative:** Copy and edit the environment template:
```bash
cp .azure/semanticlip-env/.env.template .azure/semanticlip-env/.env
# Edit .env file with your values
```

### Step 4: Deploy to Azure

```bash
# Full deployment (provision + deploy)
azd up

# This will:
# 1. Create a resource group
# 2. Provision all infrastructure (Key Vault, App Services, etc.)
# 3. Build and deploy the applications
# 4. Configure all app settings and secrets
```

**Step-by-step deployment (optional):**
```bash
# 1. Provision infrastructure only
azd provision

# 2. Deploy applications only
azd deploy

# 3. Deploy specific service
azd deploy api
azd deploy client
```

### Step 5: Verify Deployment

```bash
# Check deployment status
azd show

# View all environment variables and outputs
azd env get-values

# Open the application in browser
azd browse
```

## Post-Deployment

### Access Your Application

After deployment, you'll receive URLs for:
- **API Service:** `https://app-api-<unique-id>.azurewebsites.net`
- **Client Service:** `https://app-client-<unique-id>.azurewebsites.net`

### Verify Configuration

1. **Check Key Vault Secrets:**
   ```bash
   # Get Key Vault name
   KEY_VAULT_NAME=$(azd env get-values | grep KEY_VAULT_NAME | cut -d'=' -f2)
   
   # List secrets
   az keyvault secret list --vault-name $KEY_VAULT_NAME
   ```
   Expected secrets:
   - `azure-openai-api-key`
   - `github-personal-access-token`
   - `azure-ai-agent-connection-string`

2. **Check API App Settings:**
   ```bash
   # Get API app name
   API_APP_NAME=$(azd env get-values | grep API_BASE_URL | cut -d'/' -f3 | cut -d'.' -f1)
   
   # List app settings
   az webapp config appsettings list --name $API_APP_NAME --resource-group <resource-group> --output table
   ```

3. **Test the Application:**
   - Open the Client URL in browser
   - Upload a test video file
   - Verify transcription works
   - Test blog post generation
   - Test GitHub publishing (if configured)

## Infrastructure Details

### Deployed Resources

The deployment creates the following Azure resources:

| Resource | Purpose | SKU/Tier |
|----------|---------|----------|
| **App Service Plan** | Hosts API and Client apps | P1V2 (PremiumV2) |
| **API App Service** | Backend API (.NET 8) | P1V2 instance |
| **Client App Service** | Blazor WebAssembly frontend | P1V2 instance |
| **Key Vault** | Stores secrets securely | Standard |
| **Application Insights** | Monitoring and telemetry | Standard |
| **Log Analytics Workspace** | Centralized logging | Pay-as-you-go |
| **User Assigned Managed Identity** | Secure Key Vault access | N/A |

### Cost Estimate

**Monthly costs (approximate):**
- App Service Plan (P1V2): ~$75/month
- Key Vault: ~$0.03/10k operations
- Application Insights: Pay-as-you-go (first 5GB free)
- Log Analytics: Pay-as-you-go (first 5GB free)

**Total:** ~$75-80/month (may vary based on usage)

**Cost optimization options:**
- Development/testing: Use B1 Basic tier (~$13/month)
- Production with autoscaling: Add more P1V2 instances as needed

### Configuration Details

#### API App Service Settings

The API is configured with 18 app settings:

| Setting | Source | Purpose |
|---------|--------|---------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Auto-generated | Monitoring |
| `AzureOpenAI__Endpoint` | Parameter | OpenAI service endpoint |
| `AzureOpenAI__ApiKey` | Key Vault | OpenAI authentication |
| `AzureOpenAI__ContentDeploymentName` | Parameter | Content generation model |
| `AzureOpenAI__WhisperDeploymentName` | Parameter | Audio transcription model |
| `AzureAIAgent__ConnectionString` | Key Vault | AI Agent connection |
| `AzureAIAgent__ChatModelId` | Parameter | Chat model identifier |
| `AzureAIAgent__VectorStoreId` | Parameter | Vector store/index name |
| `AzureAIAgent__BingConnectionId` | Parameter | Bing search (optional) |
| `AzureAIAgent__MaxEvaluations` | Parameter | Evaluation iterations |
| `FileUpload__MaxRequestBodySizeInBytes` | Parameter | Max upload size (1GB) |
| `FileUpload__AllowedExtensions` | Parameter | Allowed video formats |
| `FFmpeg__Path` | Static | FFmpeg executable path |
| `FFmpeg__TimeoutMinutes` | Static | Processing timeout (5 min) |
| `FFmpeg__AudioSampleRate` | Static | Audio sample rate (16kHz) |
| `FFmpeg__AudioChannels` | Static | Audio channels (mono) |
| `Cors__AllowedOrigins__0` | Dynamic | Client app URL |
| `GitHub__PersonalAccessToken` | Key Vault | GitHub publishing |

#### FFmpeg Configuration

The application uses **Xabe.FFmpeg** library which:
- Automatically downloads FFmpeg binaries at startup
- No manual FFmpeg installation required
- Requires ~100MB disk space
- Configured for optimal audio extraction (16kHz mono)

## Maintenance

### Update Application

```bash
# Deploy latest code changes
azd deploy

# Deploy specific service
azd deploy api
azd deploy client
```

### Update Infrastructure

```bash
# Update infrastructure without redeploying code
azd provision

# Full update (infrastructure + code)
azd up
```

### Update Environment Variables

```bash
# Update a single variable
azd env set AZURE_OPENAI_API_KEY "new-api-key"

# Re-provision to apply changes
azd provision
```

### View Logs

```bash
# API logs
az webapp log tail --name <api-app-name> --resource-group <resource-group>

# Client logs
az webapp log tail --name <client-app-name> --resource-group <resource-group>
```

### Scale Application

```bash
# Scale App Service Plan
az appservice plan update --name <plan-name> --resource-group <resource-group> --sku P2V2

# Scale out (add instances)
az appservice plan update --name <plan-name> --resource-group <resource-group> --number-of-workers 2
```

## Troubleshooting

### Common Issues

#### 1. Deployment Fails with "Parameter validation failed"
- Verify all required environment variables are set
- Check `.azure/semanticlip-env/.env` file
- Ensure values match expected formats

#### 2. Application doesn't start
- Check application logs in Azure Portal
- Verify FFmpeg has sufficient disk space
- Check Key Vault access permissions

#### 3. Video upload fails
- Verify file size is under 1GB limit
- Check App Service Plan has sufficient resources
- Review `FileUpload__MaxRequestBodySizeInBytes` setting

#### 4. Transcription fails
- Verify Whisper deployment exists in Azure OpenAI
- Check API key is valid in Key Vault
- Review Application Insights for detailed errors

#### 5. CORS errors in browser
- Verify Client app URL is in CORS allowed origins
- Check API app settings for `Cors__AllowedOrigins__0`
- Clear browser cache and retry

### Debug Locally

```bash
# Run API locally
cd SemanticClip.API
dotnet run

# Run Client locally (separate terminal)
cd SemanticClip.Client
dotnet run

# Configure local appsettings
# Edit SemanticClip.API/appsettings.Development.json
# Edit SemanticClip.Client/wwwroot/appsettings.json
```

## Clean Up

### Delete Deployment

```bash
# Delete all resources
azd down

# Delete resource group manually
az group delete --name <resource-group-name> --yes
```

### Remove Environment

```bash
# List environments
azd env list

# Delete environment
azd env delete semanticlip-env
```

## Security Best Practices

1. **Never commit `.env` files to source control**
   - Use `.env.template` for documentation only
   - `.env` is already in `.gitignore`

2. **Rotate secrets regularly**
   - Update Key Vault secrets periodically
   - Regenerate GitHub tokens yearly

3. **Use Managed Identity**
   - Already configured for Key Vault access
   - No hardcoded credentials in code

4. **Enable HTTPS only**
   - Already enforced in Bicep configuration
   - Do not disable HTTPS redirects

5. **Monitor access logs**
   - Review Application Insights regularly
   - Set up alerts for anomalies

## Additional Resources

- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)

## Support

For issues or questions:
- GitHub Issues: https://github.com/vicperdana/SemantiClip/issues
- Azure Support: https://azure.microsoft.com/support/

---

**Last Updated:** November 3, 2025
