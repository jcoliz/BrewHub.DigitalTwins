
# Create all the models at once using exported models from Still6UnitController
# TODO: Make this a shared project, and get it via submodules
az dt model create -n $env:TWINSNAME --models ..\..\BrewHub.Devices.Still6UnitController\dtmi\export.json
az dt model create -n $env:TWINSNAME --models .\distilateur.json

# Create twins of the physical device & components
$Device = "west-1"
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:devices:BaseDevice;1" --twin-id "$Device-Device" --properties '@initialstate-device.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:sensors:TH;1" --twin-id "$Device-amb" --properties '@initialstate-th.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:controls:Thermostat;1" --twin-id "$Device-ct" --properties '@initialstate-thermostat.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:controls:Thermostat;1" --twin-id "$Device-rt" --properties '@initialstate-thermostat.json'

# Create twin of of the overall machinery
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:com:brewhub:machinery:distilateur;1" --twin-id $Device

# Establish relationships
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_ambient --relationship rel_has_ambient --twin-id $Device --target "$Device-amb"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_device --relationship rel_has_device --twin-id $Device --target "$Device-Device"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_thermostat_ct --relationship rel_has_thermostat --twin-id $Device --target "$Device-ct"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_thermostat_rt --relationship rel_has_thermostat --twin-id $Device --target "$Device-rt"

# Again for another device
$Device = "west-2"
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:devices:BaseDevice;1" --twin-id "$Device-Device" --properties '@initialstate-device.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:sensors:TH;1" --twin-id "$Device-amb" --properties '@initialstate-th.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:controls:Thermostat;1" --twin-id "$Device-ct" --properties '@initialstate-thermostat.json'
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:brewhub:controls:Thermostat;1" --twin-id "$Device-rt" --properties '@initialstate-thermostat.json'

# Create twin of of the overall machinery
az dt twin create -n $env:TWINSNAME --dtmi "dtmi:com:brewhub:machinery:distilateur;1" --twin-id $Device

# Establish relationships
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_ambient --relationship rel_has_ambient --twin-id $Device --target "$Device-amb"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_device --relationship rel_has_device --twin-id $Device --target "$Device-Device"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_thermostat_ct --relationship rel_has_thermostat --twin-id $Device --target "$Device-ct"
az dt twin relationship create -n $env:TWINSNAME --relationship-id has_thermostat_rt --relationship rel_has_thermostat --twin-id $Device --target "$Device-rt"
