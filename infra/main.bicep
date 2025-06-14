targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

// Optional parameters
@description('The name of the API service')
param apiServiceName string = ''

@description('The name of the web service')
param webServiceName string = ''

// Configuration parameters with defaults
@description('Azure AI Agent Max Evaluations')
param azureAiAgentMaxEvaluations string = '3'

@description('Azure AI Agent Chat Model ID')
param azureAiAgentChatModelId string = 'gpt-4o'

@description('Azure OpenAI Content Deployment Name')
param azureOpenAiContentDeploymentName string = 'gpt-4o'

@description('Azure OpenAI Whisper Deployment Name')
param azureOpenAiWhisperDeploymentName string = 'whisper'

@description('File Upload Max Request Body Size in Bytes')
param fileUploadMaxRequestBodySizeInBytes string = '30000000'

@description('File Upload Allowed Extensions')
param fileUploadAllowedExtensions string = '.mp4,.avi,.mov,.wmv,.mkv'

// Load abbreviations for consistent resource naming
var abbrs = loadJsonContent('./abbreviations.json')

// Generate a unique token for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
}

// Monitor application with Azure Application Insights
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

// App service plan to host the web app
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

// API service
module api './core/host/appservice.bicep' = {
  name: 'api'
  scope: rg
  params: {
    name: !empty(apiServiceName) ? apiServiceName : '${abbrs.webSitesAppService}api-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'api' })
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'dotnetcore'
    runtimeVersion: '9.0'
    appSettings: {
      AzureAIAgent__MaxEvaluations: azureAiAgentMaxEvaluations
      AzureAIAgent__ChatModelId: azureAiAgentChatModelId
      AzureOpenAI__ContentDeploymentName: azureOpenAiContentDeploymentName
      AzureOpenAI__WhisperDeploymentName: azureOpenAiWhisperDeploymentName
      FileUpload__MaxRequestBodySizeInBytes: fileUploadMaxRequestBodySizeInBytes
      FileUpload__AllowedExtensions: fileUploadAllowedExtensions
      APPLICATIONINSIGHTS_CONNECTION_STRING: monitoring.outputs.applicationInsightsConnectionString
      ApplicationInsights__ConnectionString: monitoring.outputs.applicationInsightsConnectionString
    }
  }
}

// Web service (Blazor WebAssembly)
module web './core/host/appservice.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: !empty(webServiceName) ? webServiceName : '${abbrs.webSitesAppService}web-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'web' })
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'dotnetcore'
    runtimeVersion: '9.0'
    appSettings: {
      ApiBaseAddress: 'https://${api.outputs.uri}'
      APPLICATIONINSIGHTS_CONNECTION_STRING: monitoring.outputs.applicationInsightsConnectionString
    }
  }
}

// Data outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId

// App outputs
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output API_BASE_URL string = api.outputs.uri
output WEB_BASE_URL string = web.outputs.uri

// Tags to apply to all resources
var tags = {
  'azd-env-name': environmentName
}