using Azure;
using Azure.Core;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

public class TwinsClient : ITwinsClient
{
    private readonly ILogger _logger;
    private readonly DigitalTwinsClient _client;
    private readonly Options _options;

    public class Options
    {
        public enum CredentialType { Invalid = 0, DefaultAzureCredential, ClientSecretCredential };

        public const string Section = "DigitalTwins";
        public string? Url { get; set; }
        public CredentialType Credential { get; set; } = CredentialType.DefaultAzureCredential;
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }

    public TwinsClient(IOptions<Options> options, ILoggerFactory logfact)
    {
        _logger = logfact.CreateLogger(nameof(TwinsClient));
        _options = options.Value;
        try
        {
            if (_options is null) 
                throw new ApplicationException("Missing DigitalTwins configuration");
            
            if (_options.Url is null) 
                throw new ApplicationException("Missing DigitalTwins.Url configuration");

            // Authenticate with Digital Twins
            TokenCredential? credential = null;

            if (_options.Credential == Options.CredentialType.DefaultAzureCredential)
            {
                credential = new DefaultAzureCredential();
            }
            else if (_options.Credential == Options.CredentialType.ClientSecretCredential)
            {
                credential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
            }
            else
                throw new NotImplementedException($"Crendential type {_options.Credential} is not implemented");

            if (credential is null)
                throw new ApplicationException("Missing DigitalTwins credential type");

            _client = new DigitalTwinsClient(new Uri(_options.Url), credential);

            _logger.LogInformation("Created client OK on {url}", _options.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create client failed");
            throw;
        }
    }
    public async Task<Response> UpdateDigitalTwinAsync(string digitalTwinId, JsonPatchDocument jsonPatchDocument, ETag? ifMatch = null, CancellationToken cancellationToken = default)
    {
        var result = await _client.UpdateDigitalTwinAsync(digitalTwinId, jsonPatchDocument, ifMatch, cancellationToken);

        _logger.LogInformation("OK. Updated digital twin {twin}. Result: {result}/{phrase}", digitalTwinId, result.Status, result.ReasonPhrase);

        return result;
    }

    public async Task<IEnumerable<string>> QueryDevicesOfModel(string model)
    {
        var result = new List<string>();

        var query = $"SELECT * FROM digitaltwins WHERE IS_OF_MODEL('{model}')";

        AsyncPageable<BasicDigitalTwin> qresult = _client.QueryAsync<BasicDigitalTwin>(query);
        var reslist = new List<BasicDigitalTwin>();
        await foreach (BasicDigitalTwin item in qresult)
            result.Add(item.Id);
        return result;
    }

}