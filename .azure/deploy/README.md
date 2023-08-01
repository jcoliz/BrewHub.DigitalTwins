# BrewHub Azure Digital Twins Deployment

## Steps

1. Copy env.template.ps1 to .env.ps1
2. Choose a resource group name. Update $env:RESOURCEGROUP
3. Open Powershell
4. Source .env.ps1
5. Run Create-ResourceGroup.ps1
6. Create azuredeploy.parameters.json with the principal ID of your user account you'll use to log into twins explorer 
7. Run Create-Deployment.ps1
8. Update .env.ps1 with STORNAME, TWINSNAME, TWINSURL based on deployment
9. Visit https://explorer.digitaltwins.azure.net/ . Connect to the new TWINSURL there.

Next: See ./scripts/CreateTwins.ps1 to get the models and twins set up