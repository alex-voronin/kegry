using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecurityModule.API.Dto;

namespace IntegrationTests.Shared.HttpClients;

public class IntegrationApiClient
{
    private readonly HttpClient _apiHttpClient;
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public IntegrationApiClient(HttpClient apiHttpClient) => _apiHttpClient = apiHttpClient;

    private HttpRequestHeaders DefaultRequestHeaders => _apiHttpClient.DefaultRequestHeaders;

    public async Task<TResp> GetAsync<TResp>(string url)
    {
        var response = await _apiHttpClient.GetAsync(url);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public async Task<TResp> PostAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PostAsJsonAsync(url, data, _options);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public async Task<TResp> PutAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PutAsJsonAsync(url, data, _options);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public void SetAuth(AuthResponse authResponse)
    {
        DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
    }
}