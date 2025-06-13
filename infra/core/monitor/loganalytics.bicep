param name string
param location string = resourceGroup().location
param tags object = {}

@description('The pricing tier of this particular SKU')
@allowed([
  'Free'
  'PerGB2018'
  'Standalone'
  'CapacityReservation'
])
param sku string = 'PerGB2018'

@description('Number of days to retain data in workspace')
param retentionInDays int = 30

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

output id string = logAnalyticsWorkspace.id
output name string = logAnalyticsWorkspace.name
output customerId string = logAnalyticsWorkspace.properties.customerId