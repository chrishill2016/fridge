// The API: a Flex Consumption Function App (.NET isolated) with a system-assigned managed identity.
// The identity reaches storage and Foundry through role assignments. There are no keys or connection strings.

param location string
param name string
param storageAccountName string
param tags object = {}

@description('Name of the existing Foundry (AIServices) account the API calls.')
param foundryAccountName string
param foundryProjectEndpoint string
param ingredientsModel string
param recipesModel string
param extraModels string[] = []

param authClientId string
param allowedObjectIds string[]

@description('Origins allowed to call the API from a browser.')
param corsOrigins string[]

param appInsightsConnectionString string

var deploymentContainer = 'app-package'

// Built-in role IDs.
var roles = {
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  azureAiUser: '53ca6127-db72-4b80-b1b0-d745d6d5456d'
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    allowSharedKeyAccess: false
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = {
  parent: storage
  name: 'default'
}

resource packageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  parent: blobService
  name: deploymentContainer
}

resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

var foundryAppSettings = [
  { name: 'Foundry__ProjectEndpoint', value: foundryProjectEndpoint }
  { name: 'Foundry__IngredientsModel', value: ingredientsModel }
  { name: 'Foundry__RecipesModel', value: recipesModel }
]

var allowedModelSettings = [for (m, i) in extraModels: { name: 'Foundry__AllowedModels__${i}', value: m }]
var allowedUserSettings = [for (id, i) in allowedObjectIds: { name: 'Auth__AllowedObjectIds__${i}', value: id }]

resource app 'Microsoft.Web/sites@2024-11-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainer}'
          authentication: { type: 'SystemAssignedIdentity' }
        }
      }
      scaleAndConcurrency: {
        // The lowest allowed ceiling: a personal app never needs more, and it bounds cost.
        maximumInstanceCount: 40
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: corsOrigins
        supportCredentials: false
      }
      appSettings: concat([
        // Host storage through the managed identity (no connection string).
        { name: 'AzureWebJobsStorage__accountName', value: storage.name }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'Auth__TenantId', value: tenant().tenantId }
        { name: 'Auth__ClientId', value: authClientId }
      ], foundryAppSettings, allowedModelSettings, allowedUserSettings)
    }
  }
}

// Deploy with Entra ID only: no FTP or basic-auth publishing credentials.
resource ftpPolicy 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-11-01' = {
  parent: app
  name: 'ftp'
  properties: { allow: false }
}

resource scmPolicy 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-11-01' = {
  parent: app
  name: 'scm'
  properties: { allow: false }
}

resource storageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for role in [roles.storageBlobDataOwner, roles.storageQueueDataContributor, roles.storageTableDataContributor]: {
    scope: storage
    name: guid(storage.id, app.id, role)
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
      principalId: app.identity.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: foundryAccountName
}

resource foundryRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: foundry
  name: guid(foundry.id, app.id, roles.azureAiUser)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.azureAiUser)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output name string = app.name
output baseUrl string = 'https://${app.properties.defaultHostName}/api/'
