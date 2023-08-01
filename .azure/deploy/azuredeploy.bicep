@description('Unique suffix for all resources in this deployment')
param suffix string = uniqueString(resourceGroup().id)

@description('Location for all resources.')
param location string = resourceGroup().location

@description('The id that will be given data owner permission for the Digital Twins resource')
param principalId string

@description('The type of the given principal id')
param principalType string = 'User'

module twins '../../AzDeploy.Bicep/DigitalTwins/digitaltwins.bicep' = {
  name: 'twins'
  params: {
    suffix: suffix
    location: location
  }
}

module dataowner '../../AzDeploy.Bicep/DigitalTwins/dataownerrole.bicep' = {
  name: 'dataowner'
  params: {
    digitalTwinsName: twins.outputs.result.name
    principalId: principalId
    principalType: principalType
  }
}

module storage '../../AzDeploy.Bicep/Storage/storage.bicep' = {
  name: 'storage'
  params: {
    suffix: suffix
    location: location
  }
}

module blobs '../../AzDeploy.Bicep/DigitalTwins/storageblobservicecors.bicep' = {
  name: 'blobs'
  params: {
    account: storage.outputs.result.name
  }
}

var containername = 'scenes'
module container '../../AzDeploy.Bicep/Storage/storcontainer.bicep' = {
  name: containername
  params: {
    name: containername
    account: storage.outputs.result.name
  }
}

module blobcontributor '../../AzDeploy.Bicep/Storage/blobdatacontribrole.bicep' = {
  name: 'blobcontributor'
  params: {
    containerFullName: container.outputs.result.name
    principalId: principalId
    principalType: principalType
  }

}
