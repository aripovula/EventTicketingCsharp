// One short-lived demo of the app: a single VM running everything (see the README section
// "Azure demo deployment"), plus its own storage account for event images. Deploy it into a fresh
// resource group; deleting the group removes everything.

@description('DNS label for the public address: the demo is served at https://<label>.<region>.cloudapp.azure.com.')
param dnsLabel string

@description('URL of the release tarball built by .github/workflows/release.yml.')
param releaseUrl string

@description('URI of the persistent Key Vault that holds the EventTicketing--* secrets.')
param keyVaultUri string

@description('Name of the pre-created user-assigned managed identity that may read the vault\'s secrets.')
param identityName string

@description('Resource group of that identity (the long-lived shared group).')
param identityResourceGroup string

@description('Public key for the VM\'s admin account. Azure requires one, but the network security group keeps SSH closed.')
param adminSshPublicKey string

@description('When the orchestrator will delete this demo, as an ISO 8601 time; a sweeper uses it to catch failed teardowns.')
param expiresAt string

param location string = resourceGroup().location
param vmSize string = 'Standard_B2s'
param adminUsername string = 'azureuser'

var tags = { app: 'event-ticketing', expiresAt: expiresAt }
var fqdn = '${dnsLabel}.${location}.cloudapp.azure.com'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
  scope: resourceGroup(identityResourceGroup)
}

// Images uploaded during this demo live and die with it. Single blobs are publicly readable, so
// <img> tags work; the app creates the container itself on startup.
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'etdemo${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Only the web ports are open: no SSH on a public demo box (use az vm run-command if needed).
resource nsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-event-ticketing'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'allow-http'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: 'Internet'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '80'
        }
      }
      {
        name: 'allow-https'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: 'Internet'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '443'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-event-ticketing'
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.0.0.0/16'] }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: '10.0.0.0/24'
          networkSecurityGroup: { id: nsg.id }
        }
      }
    ]
  }
}

resource publicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-event-ticketing'
  location: location
  tags: tags
  sku: { name: 'Standard' }
  properties: {
    publicIPAllocationMethod: 'Static'
    dnsSettings: { domainNameLabel: dnsLabel }
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: 'nic-event-ticketing'
  location: location
  tags: tags
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: { id: vnet.properties.subnets[0].id }
          publicIPAddress: { id: publicIp.id }
        }
      }
    ]
  }
}

var blobConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

var cloudInit = reduce(
  [
    { key: '__FQDN__', value: fqdn }
    { key: '__RELEASE_URL__', value: releaseUrl }
    { key: '__KEY_VAULT_URI__', value: keyVaultUri }
    { key: '__AZURE_CLIENT_ID__', value: identity.properties.clientId }
    { key: '__BLOB_CONNECTION_STRING__', value: blobConnectionString }
  ],
  loadTextContent('cloud-init.yaml'),
  (text, placeholder) => replace(text, placeholder.key, placeholder.value)
)

resource vm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: 'vm-event-ticketing'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    hardwareProfile: { vmSize: vmSize }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        deleteOption: 'Delete'
        managedDisk: { storageAccountType: 'StandardSSD_LRS' }
      }
    }
    osProfile: {
      computerName: 'event-ticketing'
      adminUsername: adminUsername
      customData: base64(cloudInit)
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: adminSshPublicKey
            }
          ]
        }
      }
    }
    networkProfile: {
      networkInterfaces: [{ id: nic.id, properties: { deleteOption: 'Delete' } }]
    }
  }
}

output url string = 'https://${fqdn}'
