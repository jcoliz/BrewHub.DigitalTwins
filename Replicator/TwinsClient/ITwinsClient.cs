using Azure;

public interface ITwinsClient
{
    public Task<Response> UpdateDigitalTwinAsync(string digitalTwinId, JsonPatchDocument jsonPatchDocument, ETag? ifMatch = null, CancellationToken cancellationToken = default);
}
