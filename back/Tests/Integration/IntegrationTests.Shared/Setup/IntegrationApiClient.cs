using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationTests.Shared.Setup;

public class IntegrationApiClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public IntegrationApiClient(HttpClient client) => _client = client;

    public async Task<TResp> GetAsync<TResp>(string url)
    {
        return (await _client.GetFromJsonAsync<TResp>(url, _options))!;
    }

    public async Task<TResp> PostAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _client.PostAsJsonAsync(url, data, _options);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public async Task<TResp> PutAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _client.PutAsJsonAsync(url, data, _options);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;
}