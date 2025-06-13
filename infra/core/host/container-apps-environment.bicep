param name string
param location string = resourceGroup().location
param tags object = {}

@description('Name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('Name of the Application Insights instance')
param applicationInsightsName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    daprAIInstrumentationKey: applicationInsights.properties.InstrumentationKey
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output domain string = containerAppsEnvironment.properties.defaultDomain