using System.Text.RegularExpressions;
using KCS.Server.Database.Models;

namespace KCS.Server.Follow;

public class FollowBot
{
    public static readonly List<Item?> Queue = [];
    private static readonly List<Task> tasks = [];
    public static int Threads;
    private static readonly HttpClient httpClient = new();
    private static readonly Regex channelIdRegex = new("\"id\":\"(.*?)\",");
    internal static readonly SemaphoreSlim semaphore = new(1, 1);

    internal static void StartPolling()
    {
        for (var i = 0; i < Threads; i++) tasks.Add(new BrowserThread().Polling());
    }

    public static async Task AddToQueue(Item? item)
    {
        await semaphore.WaitAsync();
        Queue.Add(item);
        semaphore.Release();
    }

    public static async Task RemoveFromQueue(string username, int id)
    {
        await semaphore.WaitAsync();
        try
        {
            Queue.Remove(Queue.First(x => x.Id == id && x.Username == username));
        }
        catch
        {
        }

        semaphore.Release();
    }

    public static async Task RemoveAllFromQueue(Predicate<Item?> predicate)
    {
        await semaphore.WaitAsync();
        Queue.RemoveAll(predicate);
        semaphore.Release();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokens">Token1</param>
    /// <param name="id"></param>
    /// <returns>Список токенов в очереди</returns>
    public static async Task<IEnumerable<string>> IsInQueue(IEnumerable<string> tokens, int id)
    {
        await semaphore.WaitAsync();
        var result = Queue.Where(x => tokens.Contains(x.Token1) && x.Id == id).Select(x => x.Token1);
        semaphore.Release();
        return result;
    }

    public static async Task<IEnumerable<string>> IsInQueue(IEnumerable<string> tokens, int id, bool username)
    {
        await semaphore.WaitAsync();
        var result = Queue.Where(x => tokens.Contains(x.Token1) && x.Id == id).Select(x => x.Username);
        semaphore.Release();
        return result;
    }

    public static async Task<bool> IsInQueue(string token, int id)
    {
        await semaphore.WaitAsync();
        var result = Queue.Any(x => x.Token1 == token && x.Id == id);
        semaphore.Release();
        return result;
    }

    public static async Task AddToQueue(IEnumerable<Item?> items)
    {
        await semaphore.WaitAsync();
        Queue.AddRange(items);
        semaphore.Release();
    }

    public static async Task RemoveFromQueue(IEnumerable<string> items, int id)
    {
        await semaphore.WaitAsync();
        Queue.RemoveAll(x => items.Contains(x.Token1) && x.Id == id);
        semaphore.Release();
    }

    public static async Task<string> GetChannelId(string channel)
    {
        var message = new HttpRequestMessage
        {
            RequestUri = new Uri("https://gql.twitch.tv/gql"),
            Method = HttpMethod.Post,
            Content = new StringContent("{\"operationName\": \"ChannelShell\", \"variables\": {\"login\": \"" +
                                        channel +
                                        "\"}, \"extensions\": {\"persistedQuery\": {\"version\": 1, \"sha256Hash\": \"580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe\"}}}")
        };
        message.Headers.Add("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
        var r = await httpClient.SendAsync(message);
        return channelIdRegex.Match(await r.Content.ReadAsStringAsync()).Groups[1].Value;
    }
}

public class Item
{
    // Action
    public Actions Action;
    public string StreamerUsername;

    public DateTime Date;

    // User
    public int Id;

    public Proxy Proxy;

    // State
    public ThreadState State = ThreadState.Waiting;

    public string Token1;
    public string Token2;
    public string Token3;
    public string Token4;

    // Bot
    public string Username;
}

public enum ThreadState
{
    Followed,
    Unfollowed,
    Error,
    InProgress,
    Waiting
}

public enum Actions
{
    Follow,
    Unfollow
}