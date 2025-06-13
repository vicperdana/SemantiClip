param name string
param location string = resourceGroup().location
param tags object = {}

@description('Name of the container apps environment')
param containerAppsEnvironmentName string

@description('Name of the container registry')
param containerRegistryName string

@description('Name of the key vault')
param keyVaultName string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string

@description('Specifies if the resource already exists')
param exists bool = false

@description('CPU cores allocated to each replica')
param cpuCore string = '0.5'

@description('Memory allocated to each replica')
param memory string = '1.0Gi'

@description('Minimum number of replicas')
param minReplicas int = 1

@description('Maximum number of replicas')
param maxReplicas int = 10

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-02-01-preview' existing = {
  name: containerRegistryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource apiContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'applicationinsights-connection-string'
          value: applicationInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          image: exists ? '${containerRegistry.properties.loginServer}/semanticlip-api:latest' : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'api'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'applicationinsights-connection-string'
            }
            {
              name: 'AzureKeyVault__VaultUri'
              value: keyVault.properties.vaultUri
            }
            {
              name: 'FileUpload__MaxRequestBodySizeInBytes'
              value: '30000000'
            }
            {
              name: 'FileUpload__AllowedExtensions'
              value: '.mp4,.avi,.mov,.wmv,.mkv'
            }
            {
              name: 'FFmpeg__Path'
              value: 'ffmpeg'
            }
            {
              name: 'FFmpeg__TimeoutMinutes'
              value: '10'
            }
            {
              name: 'FFmpeg__AudioSampleRate'
              value: '16000'
            }
            {
              name: 'FFmpeg__AudioChannels'
              value: '1'
            }
            {
              name: 'LocalSLM__ModelId'
              value: 'phi4-mini'
            }
            {
              name: 'LocalSLM__Endpoint'
              value: 'http://localhost:11434'
            }
          ]
          resources: {
            cpu: json(cpuCore)
            memory: memory
          }
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// Grant the container app access to the container registry
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, apiContainerApp.id, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: apiContainerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant the container app access to the key vault
resource keyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, apiContainerApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: apiContainerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output id string = apiContainerApp.id
output name string = apiContainerApp.name
output uri string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
output principalId string = apiContainerApp.identity.principalId