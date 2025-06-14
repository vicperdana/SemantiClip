param name string
param location string = resourceGroup().location
param tags object = {}

param sku object = {
  name: 'B1'
  tier: 'Basic'
}

param kind string = 'linux'
param reserved bool = true

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: name
  location: location
  tags: tags
  sku: sku
  kind: kind
  properties: {
    reserved: reserved
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name