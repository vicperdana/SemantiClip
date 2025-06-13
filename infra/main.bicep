targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

// Optional parameters
@description('Id of the user or app to assign application roles')
param principalId string = ''

// Configuration parameters
@description('Azure OpenAI endpoint')
param azureOpenAiEndpoint string = ''

@description('Azure OpenAI API key')
@secure()
param azureOpenAiApiKey string = ''

@description('Azure OpenAI Whisper deployment name')
param azureOpenAiWhisperDeploymentName string = 'whisper'

@description('Azure OpenAI content deployment name')
param azureOpenAiContentDeploymentName string = 'gpt-4o'

@description('Azure AI Agent connection string')
@secure()
param azureAiAgentConnectionString string = ''

@description('Azure AI Agent chat model ID')
param azureAiAgentChatModelId string = 'gpt-4o'

@description('Azure AI Agent vector store ID')
param azureAiAgentVectorStoreId string = 'semanticclipproject'

@description('Azure AI Agent max evaluations')
param azureAiAgentMaxEvaluations string = '3'

@description('GitHub personal access token')
@secure()
param githubPersonalAccessToken string = ''

@description('File upload max request body size in bytes')
param fileUploadMaxRequestBodySizeInBytes string = '30000000'

@description('File upload allowed extensions')
param fileUploadAllowedExtensions string = '.mp4,.avi,.mov,.wmv,.mkv'

// Variables
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// App Service Plan
module appServicePlan './core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: rg
  params: {
    name: '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    sku: {
      name: 'B1'
      tier: 'Basic'
    }
    kind: 'linux'
    reserved: true
  }
}

// Key Vault
module keyVault './core/security/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: tags
    principalId: principalId
  }
}

// Application Insights
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

// App Service
module api './core/host/appservice.bicep' = {
  name: 'api'
  scope: rg
  params: {
    name: '${abbrs.webSitesAppService}api-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'api' })
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'dotnetcore'
    runtimeVersion: '9.0'
    appSettings: {
      APPLICATIONINSIGHTS_CONNECTION_STRING: monitoring.outputs.applicationInsightsConnectionString
      ApplicationInsights__ConnectionString: monitoring.outputs.applicationInsightsConnectionString
      AzureKeyVault__VaultUri: keyVault.outputs.endpoint
      
      // Azure OpenAI Configuration
      AzureOpenAI__Endpoint: azureOpenAiEndpoint
      AzureOpenAI__ApiKey: azureOpenAiApiKey
      AzureOpenAI__WhisperDeploymentName: azureOpenAiWhisperDeploymentName
      AzureOpenAI__ContentDeploymentName: azureOpenAiContentDeploymentName
      AzureOpenAI__UseKeyVault: 'false'
      
      // Azure AI Agent Configuration
      AzureAIAgent__ConnectionString: azureAiAgentConnectionString
      AzureAIAgent__ChatModelId: azureAiAgentChatModelId
      AzureAIAgent__VectorStoreId: azureAiAgentVectorStoreId
      AzureAIAgent__MaxEvaluations: azureAiAgentMaxEvaluations
      
      // GitHub Configuration
      GitHub__PersonalAccessToken: githubPersonalAccessToken
      
      // File Upload Configuration
      FileUpload__MaxRequestBodySizeInBytes: fileUploadMaxRequestBodySizeInBytes
      FileUpload__AllowedExtensions: fileUploadAllowedExtensions
      
      // FFmpeg Configuration
      FFmpeg__Path: 'ffmpeg'
      FFmpeg__TimeoutMinutes: '10'
      FFmpeg__AudioSampleRate: '16000'
      FFmpeg__AudioChannels: '1'
      
      // CORS Configuration
      Cors__AllowedOrigins__0: 'https://${abbrs.webSitesAppService}api-${resourceToken}.azurewebsites.net'
    }
    keyVaultName: keyVault.outputs.name
  }
}

// App Service outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

// Service outputs
output SERVICE_API_IDENTITY_PRINCIPAL_ID string = api.outputs.identityPrincipalId
output SERVICE_API_NAME string = api.outputs.name
output SERVICE_API_URI string = api.outputs.uri

// Shared outputs
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString