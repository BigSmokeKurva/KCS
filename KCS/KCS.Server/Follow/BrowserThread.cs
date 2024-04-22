using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using KCS.Server.Database;
using Microsoft.Playwright;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;
using Cookie = Microsoft.Playwright.Cookie;

namespace KCS.Server.Follow;

public class BrowserThread
{
    private static readonly SemaphoreSlim Semaphore = FollowBot.semaphore;

    private static readonly HttpClient Client = new(new HttpClientHandler()
    {
        UseCookies = false
    });

    private bool _isReleased = true;
    private Item? _item;

    private async Task Lock()
    {
        if (_isReleased)
        {
            await Semaphore.WaitAsync();
            _isReleased = false;
        }
    }

    private void Unlock()
    {
        if (_isReleased) return;
        Semaphore.Release();
        _isReleased = true;
    }

    private async Task Bot()
    {
        using var playwright = await Playwright.CreateAsync();
        IBrowser? browser = null;
        IBrowserContext? context = null;
        ProxyServer? proxyServer = null;
        var returned = false;

        try
        {
            // Проверка нужно ли запускать браузер
            var state = await CheckFollow();
            switch (_item!.Action)
            {
                case Actions.Follow when state == ThreadState.Followed:
                    _item.State = ThreadState.Followed;
                    await AddFollow();
                    await Lock();
                    FollowBot.Queue.Remove(_item);
                    Unlock();
                    return;
                case Actions.Unfollow when state == ThreadState.Unfollowed:
                    _item.State = ThreadState.Unfollowed;
                    await RemoveFollow();
                    await Lock();
                    FollowBot.Queue.Remove(_item);
                    Unlock();
                    return;
            }

            if (_item.Proxy.Type == "socks5")
            {
                proxyServer = new ProxyServer();
                var socks5Proxy = new ExternalProxy
                {
                    HostName = _item.Proxy.Host,
                    Port = int.Parse(_item.Proxy.Port),
                    ProxyType = ExternalProxyType.Socks5,
                    UserName = _item.Proxy.Credentials?.Username,
                    Password = _item.Proxy.Credentials?.Password
                };
                proxyServer.UpStreamHttpProxy = socks5Proxy;
                proxyServer.UpStreamHttpsProxy = socks5Proxy;
                proxyServer.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 0));
                proxyServer.Start();
            }

            var proxy = _item.Proxy.Type == "socks5"
                ? new Proxy { Server = $"http://127.0.0.1:{proxyServer!.ProxyEndPoints[0].Port}" }
                : new Proxy
                {
                    Server = $"http://{_item.Proxy.Host}:{_item.Proxy.Port}",
                    Username = _item.Proxy.Credentials!.Value.Username,
                    Password = _item.Proxy.Credentials.Value.Password
                };
            // Запуск браузера
            BrowserTypeLaunchOptions options = new()
            {
                Headless = false,
                Args =
                [
                    "--disable-blink-features=AutomationControlled",
                    "--mute-audio",
                    //"--headless=new",
                    //"--blink-settings=imagesEnabled=false"
                ],
                Proxy = proxy,
                //ExecutablePath = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
            };
            browser = await playwright.Firefox.LaunchAsync(options);
            //context = await browser.NewContextAsync(new BrowserNewContextOptions
            //{
            //    //UserAgent =
            //    //    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            //});
            Console.WriteLine(browser.Contexts.Count);
            context = await browser.NewContextAsync();
            Console.WriteLine(browser.Contexts.Count);
            var page = await context.NewPageAsync();
            page.RequestFinished += async (sender, e) =>
            {
                if (e.Url != "https://kick.com/api/v2/channels/bigsmokekurva/follow" || e.Method != "POST" ||
                    returned) return;
                try
                {
                    Console.WriteLine(await (await e.ResponseAsync()).TextAsync());
                    returned = true;
                }
                catch
                {
                    // ignored
                }
            };
            await context.AddCookiesAsync(
            [
                new Cookie
                {
                    Name = "kick_session",
                    Value = _item.Token1,
                    Domain = ".kick.com",
                    Path = "/"
                },
                new Cookie
                {
                    Name = _item.Token2,
                    Value = _item.Token3,
                    Domain = ".kick.com",
                    Path = "/"
                },
                new Cookie
                {
                    Name = "XSRF-TOKEN",
                    Value = _item.Token4,
                    Domain = ".kick.com",
                    Path = "/"
                }
            ]);
            Task task = page.GotoAsync($"https://kick.com/{_item.StreamerUsername}");
            for (var i = 0; i < 900 && !returned; i++) await Task.Delay(100);
            task.Exception?.Handle(x => true);
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();
            context = null;
            browser = null;
            _item.State = await CheckFollow();
            switch (_item.State)
            {
                case ThreadState.Followed:
                    await AddFollow();
                    break;
                case ThreadState.Unfollowed:
                    await RemoveFollow();
                    break;
            }

            await Lock();
            FollowBot.Queue.Remove(_item);
            Unlock();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            await Lock();
            FollowBot.Queue.Remove(_item);
            Unlock();
        }
        finally
        {
            Unlock();
            proxyServer?.Stop();
            try
            {
                if (context is not null)
                    await context.CloseAsync();
                if (browser is not null)
                    await browser.CloseAsync();
            }
            catch
            {
                // ignored
            }

            proxyServer = null;
            context = null;
            browser = null;
            _item = null;
        }
    }

    private async Task<ThreadState> CheckFollow()
    {
        var message = new HttpRequestMessage
        {
            RequestUri = new Uri($"https://kick.com/api/v2/channels/{_item!.StreamerUsername}/me"),
            Method = HttpMethod.Get,
        };
        message.Headers.Add("x-xsrf-token", HttpUtility.UrlDecode(_item.Token4));
        message.Headers.Add("referer", "https://kick.com/");
        message.Headers.Add("cookie", $"kick_session={_item.Token1}; {_item.Token2}={_item.Token3}");

        var response = await Client.SendAsync(message);
        try
        {
            var json = await response.Content.ReadFromJsonAsync<GetMe>();
            return json.IsFollowing ? ThreadState.Followed : ThreadState.Unfollowed;
        }
        catch
        {
            return ThreadState.Error;
        }
    }

    private async Task AddFollow()
    {
        var serviceProvider = ServiceProviderAccessor.ServiceProvider;
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var token = await db.Bots.FindAsync(_item.Username);
        token.Followed.Add(_item.StreamerUsername);
        db.Entry(token).Property(x => x.Followed).IsModified = true;
        await db.SaveChangesAsync();
    }

    private async Task RemoveFollow()
    {
        var serviceProvider = ServiceProviderAccessor.ServiceProvider;
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var token = await db.Bots.FindAsync(_item.Username);
        token.Followed.Remove(_item.StreamerUsername);
        db.Entry(token).Property(x => x.Followed).IsModified = true;
        await db.SaveChangesAsync();
    }

    internal async Task Polling()
    {
        while (true)
        {
            await Lock();
            if (!FollowBot.Queue.Any(x => x.State == ThreadState.Waiting && x.Date < TimeHelper.GetUnspecifiedUtc()))
            {
                Unlock();
                await Task.Delay(1000);
                continue;
            }

            _item = FollowBot.Queue.First(x =>
                x.State == ThreadState.Waiting && x.Date < TimeHelper.GetUnspecifiedUtc());
            _item.State = ThreadState.InProgress;
            Unlock();
            await Bot();
        }
    }
}