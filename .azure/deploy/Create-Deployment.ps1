az deployment group create --name "Adt-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file "azuredeploy.bicep" --parameters '@azuredeploy.parameters.json'
