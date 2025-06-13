param name string
param location string = resourceGroup().location
param tags object = {}

@description('Indicates whether admin user is enabled')
param adminUserEnabled bool = true

@description('Indicates whether anonymous pull is enabled')
param anonymousPullEnabled bool = false

@description('Indicates whether data endpoint is enabled')
param dataEndpointEnabled bool = false

@description('Encryption settings')
param encryption object = {
  status: 'disabled'
}

@description('Network rule set for the registry')
param networkRuleSet object = {
  defaultAction: 'Allow'
}

@description('Policies for the registry')
param policies object = {
  quarantinePolicy: {
    status: 'disabled'
  }
  trustPolicy: {
    type: 'Notary'
    status: 'disabled'
  }
  retentionPolicy: {
    days: 7
    status: 'disabled'
  }
}

@description('SKU of the registry')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'

@description('Zone redundancy setting')
@allowed([
  'Enabled'
  'Disabled'
])
param zoneRedundancy string = 'Disabled'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-02-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    anonymousPullEnabled: anonymousPullEnabled
    dataEndpointEnabled: dataEndpointEnabled
    encryption: encryption
    networkRuleSet: networkRuleSet
    policies: policies
    zoneRedundancy: zoneRedundancy
  }
}

output id string = containerRegistry.id
output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name