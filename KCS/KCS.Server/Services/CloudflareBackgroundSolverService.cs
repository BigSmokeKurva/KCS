using System.Text.Json;
using System.Text.Json.Serialization;
using KCS.Server.Database.Models;
using Microsoft.Playwright;

namespace KCS.Server.Services;

public class CloudflareBackgroundSolverService(HttpClient client) : BackgroundService
{
    public static string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    public static string ApiKey;

    private DateTime _lastSolveTime = DateTime.MinValue;
    public static string CfClearance = string.Empty;

    /// <summary>
    /// True if the Cloudflare clearance is valid
    /// False if the Cloudflare clearance is invalid
    /// </summary>
    /// <returns></returns>
    private async Task<bool> CheckCfClearance(CancellationToken stoppingToken)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://kick.com/api/");
        requestMessage.Headers.Add("Accept", "application/json, text/plain, */*");
        requestMessage.Headers.Add("Cookie", $"cf_clearance={CfClearance}");
        try
        {
            var response = await client.SendAsync(requestMessage, stoppingToken);
            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(stoppingToken);

            return json != null && json.ContainsKey("message");
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> SolveCaptcha(Dictionary<string, string> param)
    {
        try
        {
            var data = new
            {
                key = ApiKey,
                sitekey = param["sitekey"],
                pageurl = param["pageurl"],
                pagedata = param["pagedata"],
                method = "turnstile",
                data = param["data"],
                action = param["action"],
                useragent = param["userAgent"],
                json = "1"
            };

            var response = await client.PostAsJsonAsync("http://rucaptcha.com/in.php", data);
            var task = await response.Content.ReadFromJsonAsync<_2CaptchaResponse>();
            // TODO check task.Status
            if (task.Status != 1)
                return null;
            while (true)
            {
                await Task.Delay(1000);
                response = await client.GetAsync(
                    $"http://rucaptcha.com/res.php?key={ApiKey}&action=get&json=1&id={task.Request}");

                var result = await response.Content.ReadFromJsonAsync<_2CaptchaResultResponse>();
                if (result.Request == "CAPCHA_NOT_READY")
                    continue;
                if (result.Request != "CAPCHA_NOT_READY" && result.Status == 1)
                    return result.Request;
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private async Task SolveCfClearance(CancellationToken stoppingToken)
    {
        var isCompleted = false;
        BrowserTypeLaunchOptions options = new()
        {
            Headless = false,
            Args =
            [
                "--disable-blink-features=AutomationControlled",
                "--mute-audio",
                "--headless=new",
                "--blink-settings=imagesEnabled=false",
                //"--proxy-server=http://159.69.74.159:60171" // TODO
            ],
        };
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(options);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = UserAgent
        });
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            console.clear = () => console.log('Console was cleared')
            const i = setInterval(() => {
                if (window.turnstile) {
                    clearInterval(i)
                    window.turnstile.render = (a, b) => {
                        let params = {
                            sitekey: b.sitekey,
                            pageurl: window.location.href,
                            data: b.cData,
                            pagedata: b.chlPageData,
                            action: b.action,
                            userAgent: navigator.userAgent
                        }
                        // we will intercept the message in puppeeter
                        console.log('intercepted-params:' + JSON.stringify(params))
                        window.cfCallback = b.callback
                        return
                    }
                }
            }, 50)
            """);
        page.Console += async (_, msg) =>
        {
            var txt = msg.Text;
            if (!txt.Contains("intercepted-params:"))
                return;
            txt = txt.Replace("intercepted-params:", string.Empty);

            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(txt);
            var token = await SolveCaptcha(json!);
            if (token is null)
            {
                isCompleted = true;
                return;
            }

            await page.EvaluateAsync($"cfCallback('{token}')");
        };

        page.RequestFinished += async (_, e) =>
        {
            if (e.Url != "https://kick.com/")
                return;
            var cookies = await page.Context.CookiesAsync();
            var cfClearance = cookies.FirstOrDefault(c => c!.Name == "cf_clearance", null);
            if (cfClearance is null)
            {
                return;
            }

            CfClearance = cfClearance.Value;
            _lastSolveTime = DateTime.Now;
            isCompleted = true;
        };
        await page.GotoAsync("https://kick.com", new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await page.ReloadAsync();
        for (int i = 0; i < 240; i++)
        {
            await Task.Delay(500, stoppingToken);
            if (isCompleted)
                break;
        }

        try
        {
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();
        }
        catch
        {
            // ignored
        }
    }

    private async Task Check(CancellationToken stoppingToken)
    {
        if (!client.DefaultRequestHeaders.Contains("User-Agent") ||
            client.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault(defaultValue: null) != UserAgent)
        {
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        if ((DateTime.Now - _lastSolveTime) > TimeSpan.FromMinutes(25) || !await CheckCfClearance(stoppingToken))
        {
            try
            {
                await SolveCfClearance(stoppingToken);
            }
            catch
            {
                // ignored
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await Check(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch
            {
                // ignored
            }

            await Task.Delay(3000, stoppingToken);
        }
    }
}

internal struct _2CaptchaResponse
{
    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("request")] public string Request { get; set; }
}

public struct _2CaptchaResultResponse
{
    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("request")] public string? Request { get; set; }
}