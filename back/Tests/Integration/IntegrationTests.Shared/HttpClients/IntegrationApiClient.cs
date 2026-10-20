using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<TResp> GetAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.GetAsync(BuildUrl(url, data));
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<TResp> PostAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PostAsJsonAsync(url, data, _options);
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<TResp> PutAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PutAsJsonAsync(url, data, _options);
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<TResp> PatchAsync<TReq, TResp>(string url, TReq data)
    {
        var response = await _apiHttpClient.PatchAsJsonAsync(url, data, _options);
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<TResp> DeleteAsync<TResp>(string url)
    {
        var response = await _apiHttpClient.DeleteAsync(url);
        return await ReadSuccessAsync<TResp>(response);
    }

    public async Task<ApiErrorResponse> GetProblemAsync(string url)
    {
        var response = await _apiHttpClient.GetAsync(url);
        return await ReadProblemAsync(response);
    }

    public async Task<ApiErrorResponse> GetProblemAsync<TReq>(string url, TReq data)
    {
        var response = await _apiHttpClient.GetAsync(BuildUrl(url, data));
        return await ReadProblemAsync(response);
    }

    public async Task<ApiErrorResponse> PostProblemAsync<TReq>(string url, TReq data)
    {
        var response = await _apiHttpClient.PostAsJsonAsync(url, data, _options);
        return await ReadProblemAsync(response);
    }

    public async Task<ApiErrorResponse> PutProblemAsync<TReq>(string url, TReq data)
    {
        var response = await _apiHttpClient.PutAsJsonAsync(url, data, _options);
        return await ReadProblemAsync(response);
    }

    public async Task<ApiErrorResponse> PatchProblemAsync<TReq>(string url, TReq data)
    {
        var response = await _apiHttpClient.PatchAsJsonAsync(url, data, _options);
        return await ReadProblemAsync(response);
    }

    public async Task<ApiErrorResponse> DeleteProblemAsync(string url)
    {
        var response = await _apiHttpClient.DeleteAsync(url);
        return await ReadProblemAsync(response);
    }

    public void SetAuth(AuthResponse authResponse)
    {
        _apiHttpClient.SetAuth(authResponse);
    }

    public void SetLocale(string locale)
    {
        _apiHttpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
        _apiHttpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
    }

    private async Task<TResp> ReadSuccessAsync<TResp>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response);
            throw new ApiTestException(problem.StatusCode, problem.ProblemDetails);
        }

        return (await response.Content.ReadFromJsonAsync<TResp>(_options))!;
    }

    private async Task<ApiErrorResponse> ReadProblemAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Expected an error response, but got success.");
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(_options);
        return new ApiErrorResponse(response.StatusCode, problem!);
    }

    private static string BuildUrl<T>(string url, T data)
    {
        var queryString = BuildQueryString(data);
        return string.IsNullOrEmpty(queryString)
            ? url
            : $"{url.TrimEnd('?')}{(url.Contains('?') ? "&" : "?")}{queryString}";
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

public sealed record ApiErrorResponse(
    HttpStatusCode StatusCode,
    ProblemDetails ProblemDetails);

public sealed class ApiTestException(
    HttpStatusCode statusCode,
    ProblemDetails problem)
    : Exception(
        $"API request failed: {statusCode}. " +
        $"{problem.Detail} (code: {problem.GetCode()})");

public static class TestHttpClientExtensions
{
    public static void SetAuth(this HttpClient client, AuthResponse authResponse)
    {
        client.DefaultRequestHeaders
            .Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
    }
}
