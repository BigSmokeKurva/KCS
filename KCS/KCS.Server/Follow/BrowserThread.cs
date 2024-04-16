using Microsoft.Playwright;
using System.Net;
using System.Text.RegularExpressions;
using KCS.Server.Database;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;
using Cookie = Microsoft.Playwright.Cookie;

namespace KCS.Server.Follow
{
    public class BrowserThread()
    {
        private static readonly SemaphoreSlim semaphore = FollowBot.semaphore;
        private static readonly string CurrentDirectory = Directory.GetCurrentDirectory();
        private static readonly Regex channelIdRegex = new("\"id\":\"(.*?)\",");
        private static readonly HttpClient httpClient = new();
        private static readonly Random random = new();
        private bool isReleased = true;
        private Item Item;
        private async Task Lock()
        {
            if (isReleased)
            {
                await semaphore.WaitAsync();
                isReleased = false;
            }
        }
        private void Unlock()
        {
            if (!isReleased)
            {
                semaphore.Release();
                isReleased = true;
            }
        }
        private async Task Bot()
        {
            using var playwright = await Playwright.CreateAsync();
            IBrowser browser = null;
            IBrowserContext context = null;
            ProxyServer proxyServer = null;
            string useragent = null;
            string authorization = null;
            string clientid = null;
            string deviceid = null;
            string integrity = null;
            bool returned = false;

            try
            {
                // Проверка нужно ли запускать браузер
                var state = await CheckFollow();
                if (Item.Action == Actions.Follow && state == ThreadState.Followed)
                {
                    Item.State = ThreadState.Followed;
                    await AddFollow();
                    await Lock();
                    FollowBot.Queue.Remove(Item);
                    Unlock();
                    return;
                }
                if (Item.Action == Actions.Unfollow && state == ThreadState.Unfollowed)
                {
                    Item.State = ThreadState.Unfollowed;
                    await RemoveFollow();
                    await Lock();
                    FollowBot.Queue.Remove(Item);
                    Unlock();
                    return;
                }
                if (Item.Proxy.Type == "socks5")
                {
                    proxyServer = new ProxyServer();
                    var socks5Proxy = new ExternalProxy
                    {
                        HostName = Item.Proxy.Host,
                        Port = int.Parse(Item.Proxy.Port),
                        ProxyType = ExternalProxyType.Socks5,
                        UserName = Item.Proxy.Credentials.Value.Username,
                        Password = Item.Proxy.Credentials.Value.Password,
                    };
                    proxyServer.UpStreamHttpProxy = socks5Proxy;
                    proxyServer.UpStreamHttpsProxy = socks5Proxy;
                    proxyServer.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 0, true));
                    proxyServer.Start();
                }
                Proxy proxy = Item.Proxy.Type == "socks5" ?
                    new Proxy { Server = $"http://127.0.0.1:{proxyServer.ProxyEndPoints[0].Port}" } :
                    new Proxy { Server = $"http://{Item.Proxy.Host}:{Item.Proxy.Port}", Username = Item.Proxy.Credentials.Value.Username, Password = Item.Proxy.Credentials.Value.Password };
                // Запуск браузера
                BrowserTypeLaunchOptions options = new()
                {
                    Headless = false,
                    Args = new string[] {
                                    "--disable-blink-features=AutomationControlled",
                                    "--mute-audio",
                                    "--headless=new",
                                    "--blink-settings=imagesEnabled=false",
                                },
                    Proxy = proxy,
                };
                browser = await playwright.Chromium.LaunchAsync(options);
                context = await browser.NewContextAsync(new() { UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" });
                var page = await context.NewPageAsync();
                page.RequestFinished += async (sender, e) =>
                {
                    if (e.Url == "https://gql.twitch.tv/integrity" && e.Method == "POST" && !returned)
                    {
                        try
                        {
                            using HttpClient httpClient = new(new HttpClientHandler()
                            {
                                Proxy = new WebProxy($"{Item.Proxy.Type}://{Item.Proxy.Host}:{Item.Proxy.Port}", false, new string[] { }, Item.Proxy.Credentials.Value),
                            });
                            integrity = (await (await e.ResponseAsync()).JsonAsync<Integrity>()).token;
                            useragent = e.Headers["user-agent"];
                            authorization = e.Headers["authorization"];
                            clientid = e.Headers["client-id"];
                            deviceid = e.Headers["x-device-id"];
                            var message = new HttpRequestMessage()
                            {
                                Method = HttpMethod.Post,
                                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                                Content = Item.Action switch
                                {
                                    Actions.Unfollow => new StringContent("[{\"operationName\": \"FollowButton_UnfollowUser\", \"variables\": {\"input\": {\"targetID\": \"" + Item.TargetId + "\"}}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"f7dae976ebf41c755ae2d758546bfd176b4eeb856656098bb40e0a672ca0d880\"}}}]"),
                                    Actions.Follow => new StringContent("[{\"operationName\": \"FollowButton_FollowUser\", \"variables\": {\"input\": {\"disableNotifications\": false, \"targetID\": \"" + Item.TargetId + "\"}}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"800e7346bdf7e5278a3c1d3f21b2b56e2639928f86815677a7126b093b2fdd08\"}}}]")
                                }
                            };
                            message.Headers.Add("User-Agent", useragent);
                            message.Headers.Add("Authorization", authorization);
                            message.Headers.Add("Client-Id", clientid);
                            message.Headers.Add("X-Device-Id", deviceid);
                            message.Headers.Add("Client-Integrity", integrity);
                            var r = await httpClient.SendAsync(message);
                            //Console.WriteLine(await r.Content.ReadAsStringAsync());
                            returned = true;
                        }
                        catch { }
                    }
                };
                await context.AddCookiesAsync(new Cookie[]
                {
                    new()
                    {
                        Name = "auth-token",
                        Value = Item.Token,
                        Domain = ".twitch.tv",
                        Path = "/",
                    }
                });
                Task task = null;
                task = page.GotoAsync($"https://www.twitch.tv/{Item.Channel}");
                for (var i = 0; i < 900 && !returned; i++)
                {
                    await Task.Delay(100);
                }
                task.Exception?.Handle(x => true);
                await page.CloseAsync();
                await context.CloseAsync();
                await browser.CloseAsync();
                context = null;
                browser = null;
                Item.State = await CheckFollow();
                if (Item.State == ThreadState.Followed)
                {
                    await AddFollow();
                }
                else if (Item.State == ThreadState.Unfollowed)
                {
                    await RemoveFollow();
                }
                await Lock();
                FollowBot.Queue.Remove(Item);
                Unlock();
            }
            catch
            {
                await Lock();
                FollowBot.Queue.Remove(Item);
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
                catch { }
                proxyServer = null;
                context = null;
                browser = null;
                Item = null;
            }
        }
        private async Task<ThreadState> CheckFollow()
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://gql.twitch.tv/gql"),
                Method = HttpMethod.Post,
                Content = new StringContent("[{\"operationName\": \"PersonalSections\", \"variables\": {\"input\": {\"sectionInputs\": [\"RECS_FOLLOWED_SECTION\"], \"recommendationContext\": {\"platform\": \"web\", \"clientApp\": \"twilight\", \"channelName\": null, \"categorySlug\": null, \"lastChannelName\": null, \"lastCategorySlug\": null, \"pageviewContent\": null, \"pageviewContentType\": null, \"pageviewLocation\": null, \"pageviewMedium\": null, \"previousPageviewContent\": null, \"previousPageviewContentType\": null, \"previousPageviewLocation\": null, \"previousPageviewMedium\": null}}, \"creatorAnniversariesFeature\": false}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"9efca8021f763cb383328559575f0af14882b53ab9f4c7b03a620828f46f2316\"}}}]"),
            };
            message.Headers.Add("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            message.Headers.Add("Authorization", $"OAuth {Item.Token}");
            message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/119.0");
            message.Headers.Add("X-Device-Id", random.Next(1000, 1000000000).ToString());
            var personalSections = (await (await httpClient.SendAsync(message)).Content.ReadFromJsonAsync<PersonalSectionsJson[]>())[0];
            if (personalSections.data is null)
            {
                return ThreadState.Error;
            }
            if (personalSections.data.personalSections[0].items is not null && personalSections.data.personalSections[0].items.Any(x =>
            {
                if (x.user is null)
                {
                    return false;
                }
                return x.user.id == Item.TargetId;
            }))
            {
                return ThreadState.Followed;
            }
            return ThreadState.Unfollowed;
        }
        private async Task AddFollow()
        {
            var serviceProvider = ServiceProviderAccessor.ServiceProvider;
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var token = await db.Bots.FindAsync(Item.Username);
            token.Followed.Add(Item.Channel);
            db.Entry(token).Property(x => x.Followed).IsModified = true;
            await db.SaveChangesAsync();
        }
        private async Task RemoveFollow()
        {
            var serviceProvider = ServiceProviderAccessor.ServiceProvider;
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var token = await db.Bots.FindAsync(Item.Username);
            token.Followed.Remove(Item.Channel);
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
                Item = FollowBot.Queue.First(x => x.State == ThreadState.Waiting && x.Date < TimeHelper.GetUnspecifiedUtc());
                Item.State = ThreadState.InProgress;
                Unlock();
                await Bot();
            }
        }
    }
}
