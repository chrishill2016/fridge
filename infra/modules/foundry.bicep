// Foundry resource (AI Services account), one project and its model deployments.
// Key-based auth is disabled: callers use Entra ID (az login locally, managed identity in Azure).

@description('Azure region for the Foundry resource.')
param location string

@description('Name of the Foundry resource. Also used as its custom subdomain, so it must be globally unique.')
param accountName string

@description('Name of the Foundry project.')
param projectName string

@description('Model deployments to create. Add an entry to make another model available.')
param deployments deploymentConfig[]

@description('Principals (users or managed identities) that may call models and agents in the project.')
param aiUsers principalConfig[] = []

param tags object = {}

@export()
type deploymentConfig = {
  @description('Deployment name the API uses to pick the model, e.g. "gpt-4.1-mini".')
  name: string
  @description('Model publisher format, e.g. "OpenAI".')
  format: string
  model: string
  version: string
  @description('GlobalStandard, DataZoneStandard or Standard.')
  sku: string
  @description('Capacity in thousands of tokens per minute.')
  capacity: int
}

@export()
type principalConfig = {
  principalId: string
  principalType: 'User' | 'Group' | 'ServicePrincipal'
}

// Built-in "Azure AI User" role: use models and build agents in Foundry projects, no management rights.
var azureAiUserRoleId = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: accountName
    allowProjectManagement: true
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: account
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: projectName
    description: 'Fridge Chef proof of concept'
  }
}

// One at a time: parallel deployment writes on the same account fail with a conflict.
@batchSize(1)
resource modelDeployments 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = [
  for d in deployments: {
    parent: account
    name: d.name
    sku: {
      name: d.sku
      capacity: d.capacity
    }
    properties: {
      model: {
        format: d.format
        name: d.model
        version: d.version
      }
      versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    }
  }
]

resource aiUserAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for p in aiUsers: {
    scope: account
    name: guid(account.id, p.principalId, azureAiUserRoleId)
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureAiUserRoleId)
      principalId: p.principalId
      principalType: p.principalType
    }
  }
]

output accountName string = account.name
output accountEndpoint string = account.properties.endpoint
output projectName string = project.name
output projectEndpoint string = 'https://${accountName}.services.ai.azure.com/api/projects/${projectName}'
output deploymentNames string[] = [for d in deployments: d.name]
