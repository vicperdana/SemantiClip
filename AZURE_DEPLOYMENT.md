# Azure Deployment Quick Reference

This document provides a quick reference for deploying SemantiClip to Azure using Azure Developer CLI (azd).

## Prerequisites

1. [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
2. Azure subscription with sufficient permissions
3. Azure OpenAI service deployed with Whisper and GPT-4o models

## Quick Start

```bash
# Clone the repository
git clone https://github.com/vicperdana/SemantiClip.git
cd SemantiClip

# Initialize azd environment
azd init

# Configure required environment variables
azd env set AZURE_OPENAI_ENDPOINT "https://your-service.openai.azure.com/"
azd env set AZURE_OPENAI_API_KEY "your-api-key"

# Deploy everything
azd up
```

## Environment Variables

### Required
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI service endpoint
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key

### Optional
- `AZURE_OPENAI_WHISPER_DEPLOYMENT_NAME`: Whisper model deployment name (default: "whisper")
- `AZURE_OPENAI_CONTENT_DEPLOYMENT_NAME`: GPT model deployment name (default: "gpt-4o")
- `AZURE_AI_AGENT_CONNECTION_STRING`: Azure AI Foundry connection string
- `GITHUB_PERSONAL_ACCESS_TOKEN`: GitHub token for blog publishing
- `FILE_UPLOAD_MAX_REQUEST_BODY_SIZE_IN_BYTES`: Max upload size (default: "30000000")
- `FILE_UPLOAD_ALLOWED_EXTENSIONS`: Allowed file extensions (default: ".mp4,.avi,.mov,.wmv,.mkv")

## Common Commands

```bash
# Set environment variable
azd env set VARIABLE_NAME "value"

# View all environment variables
azd env get-values

# Deploy only (without provisioning)
azd deploy

# Re-provision and deploy
azd up

# Clean up all resources
azd down
```

## Deployed Resources

- **Resource Group**: Contains all resources
- **App Service Plan**: Linux Basic B1 plan
- **App Service**: Hosts the SemantiClip API
- **Key Vault**: Stores secrets securely
- **Application Insights**: Monitoring and logging
- **Log Analytics Workspace**: Log storage

## Monitoring

- Access logs and metrics through Azure portal
- Application Insights provides detailed telemetry
- Health checks available at `https://your-app.azurewebsites.net/health`

## Troubleshooting

1. **Deployment fails**: Check that all required environment variables are set
2. **API errors**: Verify Azure OpenAI configuration in Azure portal
3. **Permission issues**: Ensure your Azure account has Contributor access
4. **Resource limits**: Check your subscription's quota limits

## Support

For issues and questions:
- GitHub Issues: https://github.com/vicperdana/SemantiClip/issues
- Documentation: https://github.com/vicperdana/SemantiClip/blob/main/README.md