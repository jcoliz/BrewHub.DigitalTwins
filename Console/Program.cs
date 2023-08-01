using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;

try
{
    Console.WriteLine("BrewHub Digital Twins CLI");

    string? adtInstanceUrl = Environment.GetEnvironmentVariable("TWINSURL");
    HttpClient httpClient = new HttpClient();

    if (adtInstanceUrl == null) 
        throw new ApplicationException("ERROR: Application setting \"ADT_SERVICE_URL\" not set");

    // Authenticate with Digital Twins
    var cred = new DefaultAzureCredential();
    var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), cred);
    Console.WriteLine($"OK. ADT service client connection created.");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.GetType().Name} {ex.Message}");
}

