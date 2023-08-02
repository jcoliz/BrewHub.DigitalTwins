using Azure;
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
        public const string Section = "DigitalTwins";
        public string? Url { get; set; }
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
            var cred = new DefaultAzureCredential();
            _client = new DigitalTwinsClient(new Uri(_options.Url), cred);

            _logger.LogInformation("Created client OK on {url}", _options.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create client failed");
            throw;
        }
    }
    public Task<Response> UpdateDigitalTwinAsync(string digitalTwinId, JsonPatchDocument jsonPatchDocument, ETag? ifMatch = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

}