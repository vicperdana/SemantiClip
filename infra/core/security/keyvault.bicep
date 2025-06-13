param name string
param location string = resourceGroup().location
param tags object = {}

@description('Id of the user or app to assign application roles')
param principalId string

@description('The pricing tier of the vault')
@allowed([
  'Premium'
  'Standard'
])
param sku string = 'Standard'

@description('Specifies whether the vault is enabled for deployment scripts')
param enabledForDeployment bool = false

@description('Specifies whether the vault is enabled for disk encryption')
param enabledForDiskEncryption bool = true

@description('Specifies whether the vault is enabled for template deployment')
param enabledForTemplateDeployment bool = true

@description('Specifies whether RBAC authorization is enabled')
param enableRbacAuthorization bool = true

@description('Specifies whether soft delete is enabled')
param enableSoftDelete bool = true

@description('Soft delete retention in days')
param softDeleteRetentionInDays int = 90

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: sku
    }
    enabledForDeployment: enabledForDeployment
    enabledForDiskEncryption: enabledForDiskEncryption
    enabledForTemplateDeployment: enabledForTemplateDeployment
    enableRbacAuthorization: enableRbacAuthorization
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    accessPolicies: []
  }
}

// Assign Key Vault Secrets User role to the principal
resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, principalId, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: principalId
    principalType: 'User'
  }
}

output id string = keyVault.id
output name string = keyVault.name
output endpoint string = keyVault.properties.vaultUri