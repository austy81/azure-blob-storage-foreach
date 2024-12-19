using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureBlobStorageForeach;

public class MWorkRestClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    public MWorkRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task AuthenticateAsync(string tenantCode, string username, string password, string deviceIdentifier)
    {
        var loginPayload = new
        {
            tenantCode,
            username,
            password,
            deviceInfo = new { deviceIdentifier }
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://app.mwork365.com/main/api/public/auth/LogIn", requestContent);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent);

        if (authResponse?.Success == true && authResponse.AccessToken != null)
        {
            _accessToken = authResponse.AccessToken;
        }
        else
        {
            throw new Exception("Authentication failed. Please check your credentials.");
        }
    }

    public async Task<string> MakeRequestAsync(string url, HttpMethod method, string payload)
    {
        using var request = new HttpRequestMessage(method, url);
        if (_accessToken != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        if (payload != null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/yaml");
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return string.Empty;
        }
    }

    public async Task<string> MakeMultipartRequestAsync(string url, HttpMethod method, string data, string template)
    {
        using var request = new HttpRequestMessage(method, url);

        // Add Authorization header if the access token is available
        if (_accessToken != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        // Create multipart content
        var multipartContent = new MultipartFormDataContent();

        if (!string.IsNullOrEmpty(data))
        {
            var dataContent = new StringContent(data, Encoding.UTF8, "application/json");
            multipartContent.Add(dataContent, "data");
        }

        if (!string.IsNullOrEmpty(template))
        {
            var templateContent = new StringContent(template, Encoding.UTF8, "application/yaml");
            multipartContent.Add(templateContent, "template");
        }

        request.Content = multipartContent;

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return string.Empty;
        }
    }


    private class AuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }
    }
}
