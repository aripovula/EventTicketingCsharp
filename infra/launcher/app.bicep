// The launcher's Function App (Flex Consumption, .NET 10 isolated) and what it needs, in the
// long-lived shared resource group. It signs in to everything with its managed identity: its own
// storage account doesn't even allow keys.

param location string
param allowedOrigins array
param demoIdentityName string

@description('Demo__* app settings for EventTicketing.DemoLauncher\'s DemoOptions.')
param demoSettings object

var suffix = uniqueString(resourceGroup().id, 'demo-launcher')
var packageContainer = 'app-package'

// Built-in role ids.
var storageBlobDataOwner = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributor = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributor = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
var monitoringMetricsPublisher = '3913510d-42f4-4e42-8a64-420c390055eb'
var managedIdentityOperator = 'f1a07417-d97a-45cb-824c-7a7467783830'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'stlauncher${suffix}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }

  resource blobs 'blobServices' = {
    name: 'default'

    resource package 'containers' = {
      name: packageContainer
    }
  }

  // TableExpiryStore keeps the demo's expiry here.
  resource tables 'tableServices' = {
    name: 'default'

    resource demo 'tables' = {
      name: 'demo'
    }
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-demo-launcher'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-demo-launcher'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    DisableLocalAuth: true
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-demo-launcher'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: 'func-demo-launcher-${suffix}'
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${packageContainer}'
          authentication: { type: 'SystemAssignedIdentity' }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 512
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      cors: { allowedOrigins: allowedOrigins }
      appSettings: concat(
        [
          { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
          { name: 'AzureWebJobsStorage__blobServiceUri', value: storage.properties.primaryEndpoints.blob }
          { name: 'AzureWebJobsStorage__queueServiceUri', value: storage.properties.primaryEndpoints.queue }
          { name: 'AzureWebJobsStorage__tableServiceUri', value: storage.properties.primaryEndpoints.table }
          { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
          { name: 'APPLICATIONINSIGHTS_AUTHENTICATION_STRING', value: 'Authorization=AAD' }
          { name: 'Demo__TableServiceUri', value: storage.properties.primaryEndpoints.table }
        ],
        map(items(demoSettings), setting => { name: 'Demo__${setting.key}', value: setting.value })
      )
    }
  }
}

// The Functions host keeps its package, timer locks and queues in the storage account; the app keeps
// the demo's expiry in its table.
resource storageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for role in [storageBlobDataOwner, storageQueueDataContributor, storageTableDataContributor]: {
    name: guid(storage.id, app.id, role)
    scope: storage
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
      principalId: app.identity.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

resource insightsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(insights.id, app.id, monitoringMetricsPublisher)
  scope: insights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisher)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Lets the launcher attach the demo VMs' identity (and read its client id), but not use it itself.
resource demoIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: demoIdentityName
}

resource identityRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(demoIdentity.id, app.id, managedIdentityOperator)
  scope: demoIdentity
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', managedIdentityOperator)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output name string = app.name
output principalId string = app.identity.principalId
output demoEndpoint string = 'https://${app.properties.defaultHostName}/api/demo'
