# BrewHub.Net Digital Twins

This repository contains the code and configuration needed to drive an Azure
Digital Twins instance from a site
running the BrewHub.Net IoT Reference Architecture.

![IoT Reference Architecture Twins Replicator](./docs/images/IoT%20Reference%20Architecture%20Twins%20Replicator.png)

Currently, I am working on a Digital Twins Replicator containerized application,
which will replicate metrics from the local site directly into an Azure Digital
Twins instance. 

This bypasses the Event Grid and Function App components
described in the reference architecture. When those components are implemented,
the code here will run in the Function App instead of in the Edge Layer.

## Getting Started

1. Deploy an Azure Digital Twins instance. See [Deployment Readme](/.azure/deploy/README.md) for how I do this.
2. Set up the instance with models, twins, and relationships. See [Twins Setup Script](/scripts/CreateTwins.ps1).
3. Run the console test app to populate test data. See [Console Readme](/Console/README.md).

That's as far as I've gotten! Next up: Querying data from InfluxDB and translating it to digital twin properties.