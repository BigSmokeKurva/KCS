using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using KCS.Server.Database.Models;
using Proxy = KCS.Server.Database.Models.Proxy;

namespace KCS.Server.Services;

public class CloudflareBackgroundSolverService
{
    private static string? _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    public static string Url = "";
    private static readonly HttpClient client = new();

    public static string? UserAgent
    {
        get => _userAgent;
        set => _userAgent = value ??
                            _userAgent;
    }

    public static async Task<string> SolveCfClearance(CancellationToken stoppingToken, Proxy proxy)
    {
        var data = new
        {
            userAgent = UserAgent,
            url = "https://kick.com/",
            proxyAddress = $"{proxy.Type}://{proxy.Host}:{proxy.Port}",
            proxyUsername = proxy.Credentials.Value.Username,
            proxyPassword = proxy.Credentials.Value.Password
        };
        var response = await client.PostAsJsonAsync(Url + "/createTask", data, stoppingToken);

        if (response.StatusCode != HttpStatusCode.OK)
            throw new Exception("Failed to create task");

        var id = await response.Content.ReadAsStringAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            response = await client.GetAsync(Url + $"/getTaskResult/{id}", stoppingToken);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to get task result");

            var result = await response.Content.ReadAsStringAsync();
            var resultResponse = JsonSerializer.Deserialize<Dictionary<string, string?>>(result);

            switch (resultResponse["status"])
            {
                case "Failed":
                    throw new Exception(resultResponse["error"]);
                case "In Progress":
                    await Task.Delay(1000, stoppingToken);
                    continue;
                case "Completed":
                    return resultResponse["cfClearance"]!;
                default:
                    throw new Exception("Unknown status");
            }
        }

        throw new Exception("Unknown status");
    }
}