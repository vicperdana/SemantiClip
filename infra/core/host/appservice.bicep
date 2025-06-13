param name string
param location string = resourceGroup().location
param tags object = {}

param appServicePlanId string
param runtimeName string 
param runtimeVersion string
param keyVaultName string = ''
param appSettings object = {}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      linuxFxVersion: '${runtimeName}|${runtimeVersion}'
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      appSettings: [for item in items(appSettings): {
        name: item.key
        value: item.value
      }]
    }
    httpsOnly: true
  }
}

// Grant the app access to KeyVault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = if (!empty(keyVaultName)) {
  name: keyVaultName
}

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = if (!empty(keyVaultName)) {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        objectId: appService.identity.principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

output id string = appService.id
output name string = appService.name
output uri string = 'https://${appService.properties.defaultHostName}'
output identityPrincipalId string = appService.identity.principalId