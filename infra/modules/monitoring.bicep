// Log Analytics + workspace-based Application Insights for the Function (and later Foundry tracing).

param location string
param name string
param tags object = {}

resource workspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: '${name}-logs'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    // Cap ingestion so a runaway loop can't run up a bill.
    workspaceCapping: { dailyQuotaGb: 1 }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${name}-insights'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

output connectionString string = insights.properties.ConnectionString
output insightsName string = insights.name
