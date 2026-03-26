using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
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

    public async Task<TResp> GetAsync<TResp>(string url)
    {
        var response = await _apiHttpClient.GetAsync(url);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public async Task<TResp> GetAsync<TReq, TResp>(string url, TReq data)
    {
        var queryString = BuildQueryString(data);
        var fullUrl = string.IsNullOrEmpty(queryString)
            ? url
            : $"{url.TrimEnd('?')}{(url.Contains('?') ? "&" : "?")}{queryString}";

        var response = await _apiHttpClient.GetAsync(fullUrl);
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

    public async Task<TResp> PatchAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PatchAsJsonAsync(url, data, _options);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public async Task<TResp> DeleteAsync<TResp>(string url)
    {
        var response = await _apiHttpClient.DeleteAsync(url);
        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    public void SetAuth(AuthResponse authResponse)
    {
        _apiHttpClient.SetAuth(authResponse);
    }

    private static string BuildQueryString<T>(T obj)
    {
        if (obj == null) return string.Empty;

        var dict = new Dictionary<string, string?>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            if (value == null) continue;

            var strValue = value switch
            {
                float f => f.ToString("F6", CultureInfo.InvariantCulture),
                double d => d.ToString("F6", CultureInfo.InvariantCulture),
                decimal m => m.ToString("F6", CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            dict[prop.Name] = strValue;
        }

        return QueryHelpers.AddQueryString("", dict).TrimStart('?');
    }
}

public static class TestHttpClientExtensions
{
    public static void SetAuth(this HttpClient client, AuthResponse authResponse)
    {
        client.DefaultRequestHeaders
            .Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
    }
}