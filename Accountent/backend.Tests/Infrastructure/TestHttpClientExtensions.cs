using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.Data;
using backend.Models.DTOs;

namespace backend.Tests.Infrastructure;

public static class TestHttpClientExtensions
{
    public static async Task<HttpClient> AsRoleAsync(
        this HttpClient client,
        string email,
        string password = DatabaseSeed.DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            email = email,
            password = password,
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Авторизация {email} не удалась: {(int)response.StatusCode} {response.StatusCode}. {body}");
        }
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default)
            ?? throw new InvalidOperationException("Пустой ответ авторизации.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.jwt_token);

        return client;
    }

    public static HttpClient WithoutAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }
}
