#
# Local environment configuration
#   > Copy this file to .env.ps1, and update with your details
#

# You set this value manually

$env:RESOURCEGROUP = "Name of resource group to deploy into"

# These values are returned in the `outputs` section of the ARM deployment

$env:STORNAME = "Name of storage account"
$env:TWINSNAME = "Name of Digital Twins resource"
$env:TWINSURL = "Digital Twins service URL"

# Find this value by running: `az storage account show-connection-string --name $env:STORNAME`

$env:STORCSTR = "Connection string to the storage account"
