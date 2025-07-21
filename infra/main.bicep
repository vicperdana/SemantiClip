// SemanticClip - Main Infrastructure Template
// This template deploys the complete infrastructure for SemanticClip application
// including two App Services (API and Client), Key Vault, Log Analytics, and Application Insights

targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment used to generate a short unique hash for resource names.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@secure()
@description('Azure OpenAI API Key')
param azureOpenAIApiKey string

@description('Azure OpenAI Endpoint')
param azureOpenAIEndpoint string

@description('Azure OpenAI Content Deployment Name')
param azureOpenAIContentDeploymentName string

@secure()
@description('GitHub Personal Access Token')
param gitHubPersonalAccessToken string

@description('Custom domain name for the API service')
param customDomainName string = 'semanticlip.vicperdana.com'

@description('Custom domain name for the Client service')
param clientCustomDomainName string = 'semanticlipweb.vicperdana.com'

// Generate a unique suffix from environmentName, subscription, and resource group
var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var tags = {
  'azd-env-name': environmentName
}

// Create User Assigned Managed Identity
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${resourceToken}'
  location: location
  tags: tags
}

// Create Log Analytics Workspace for monitoring
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Create Application Insights for monitoring
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Create Key Vault for storing secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    enabledForTemplateDeployment: true
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        objectId: userAssignedIdentity.properties.principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get']
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Store Azure OpenAI API Key in Key Vault
resource azureOpenAIApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azure-openai-api-key'
  properties: {
    value: azureOpenAIApiKey
  }
}

// Store GitHub Personal Access Token in Key Vault
resource gitHubTokenSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'github-personal-access-token'
  properties: {
    value: gitHubPersonalAccessToken
  }
}

// Create App Service Plan for both applications
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'asp-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

// Create API App Service
resource apiAppService 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-api-${resourceToken}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'api'
  })
  kind: 'app'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: userAssignedIdentity.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: 'v8.0'
      webSocketsEnabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: azureOpenAIEndpoint
        }
        {
          name: 'AzureOpenAI__ContentDeploymentName'
          value: azureOpenAIContentDeploymentName
        }
        {
          name: 'AzureOpenAI__ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${azureOpenAIApiKeySecret.name})'
        }
        {
          name: 'GitHub__PersonalAccessToken'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${gitHubTokenSecret.name})'
        }
      ]
      cors: {
        allowedOrigins: [
          'https://${clientCustomDomainName}'
          '*'
        ]
        supportCredentials: false
      }
    }
  }
}

// Custom domain configuration commented out for initial deployment
// Uncomment and configure after manual SSL certificate setup

/*
// Add custom domain to API App Service
resource apiCustomDomain 'Microsoft.Web/sites/hostNameBindings@2024-04-01' = {
  parent: apiAppService
  name: customDomainName
  properties: {
    hostNameType: 'Verified'
    sslState: 'Disabled'
  }
}
*/

// Create Client App Service
resource clientAppService 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-client-${resourceToken}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'client'
  })
  kind: 'app'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: userAssignedIdentity.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApiBaseAddress'
          value: 'https://${apiAppService.properties.defaultHostName}'
        }
      ]
    }
  }
}

// Custom domain configuration commented out for initial deployment
// Uncomment and configure after manual SSL certificate setup

/*
// Add custom domain to Client App Service
resource clientCustomDomain 'Microsoft.Web/sites/hostNameBindings@2024-04-01' = {
  parent: clientAppService
  name: clientCustomDomainName
  properties: {
    hostNameType: 'Verified'
    sslState: 'Disabled'
  }
}
*/

// Configure diagnostic settings for API App Service
resource apiAppServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: apiAppService
  name: 'api-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Configure diagnostic settings for Client App Service
resource clientAppServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: clientAppService
  name: 'client-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Outputs
output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output API_BASE_URL string = 'https://${apiAppService.properties.defaultHostName}'
output CLIENT_BASE_URL string = 'https://${clientCustomDomainName}'
output KEY_VAULT_NAME string = keyVault.name
output RESOURCE_GROUP_NAME string = resourceGroup().name
output RESOURCE_GROUP_ID string = resourceGroup().id
