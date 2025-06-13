targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string

// Optional parameters
@description('Name of the resource group. Generated if not provided.')
param resourceGroupName string = ''

@description('Name of the container registry. Generated if not provided.')
param containerRegistryName string = ''

@description('Name of the container apps environment. Generated if not provided.')
param containerAppsEnvironmentName string = ''

@description('Name of the container app for the API. Generated if not provided.')
param apiContainerAppName string = ''

@description('Name of the Log Analytics workspace. Generated if not provided.')
param logAnalyticsName string = ''

@description('Name of Application Insights. Generated if not provided.')
param applicationInsightsName string = ''

@description('Name of the Key Vault. Generated if not provided.')
param keyVaultName string = ''

// Variables
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Create container registry
module registry './core/host/container-registry.bicep' = {
  name: 'registry'
  scope: rg
  params: {
    name: !empty(containerRegistryName) ? containerRegistryName : '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: tags
  }
}

// Create log analytics workspace
module logAnalytics './core/monitor/loganalytics.bicep' = {
  name: 'loganalytics'
  scope: rg
  params: {
    name: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    location: location
    tags: tags
  }
}

// Create application insights
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    location: location
    tags: tags
  }
}

// Create Key Vault
module keyVault './core/security/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: !empty(keyVaultName) ? keyVaultName : '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: tags
    principalId: principalId
  }
}

// Create container apps environment
module containerAppsEnvironment './core/host/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  scope: rg
  params: {
    name: !empty(containerAppsEnvironmentName) ? containerAppsEnvironmentName : '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceName: logAnalytics.outputs.name
    applicationInsightsName: monitoring.outputs.applicationInsightsName
  }
}

// Create API container app
module api './app/api.bicep' = {
  name: 'api'
  scope: rg
  params: {
    name: !empty(apiContainerAppName) ? apiContainerAppName : '${abbrs.appContainerApps}api-${resourceToken}'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: registry.outputs.name
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    exists: false
  }
}

// App outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

// Container registry outputs
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = registry.outputs.name

// Container app outputs
output API_BASE_URL string = api.outputs.uri
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name

// Monitoring outputs
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalytics.outputs.name

// Security outputs  
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint