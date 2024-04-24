using KCS.Server.BotsManager;

namespace KCS.Server.Follow
{
    public class FollowManager
    {
        private readonly List<Item> _queue = [];
        private readonly List<Task> _tasks = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public FollowManager(int threads)
        {
            for (var i = 0; i < threads; i++)
            {
                _tasks.Add(new FollowThread(_queue, _semaphore).Polling());
            }
        }

        public async Task AddToQueue(Item item)
        {
            await _semaphore.WaitAsync();
            _queue.Add(item);
            _semaphore.Release();
        }

        public async Task<IEnumerable<string>> GetUserQueue(int id)
        {
            await _semaphore.WaitAsync();
            var result = _queue.Where(x => x.Id == id).Select(x => x.Bot.Username);
            _semaphore.Release();
            return result;
        }

        public async Task RemoveFromQueue(string botName, int id)
        {
            await _semaphore.WaitAsync();
            _queue.Remove(_queue.First(x => x.Id == id && x.Bot.Username == botName));
            _semaphore.Release();
        }

        public async Task RemoveAllFromQueue(Predicate<Item> predicate)
        {
            await _semaphore.WaitAsync();
            _queue.RemoveAll(predicate);
            _semaphore.Release();
        }

        public async Task<IEnumerable<string>> IsInQueue(IEnumerable<string> botNames, int id)
        {
            await _semaphore.WaitAsync();
            var result = _queue.Where(x => botNames.Contains(x.Bot.Username) && x.Id == id).Select(x => x.Bot.Username);
            _semaphore.Release();
            return result;
        }

        public async Task<bool> IsInQueue(string botName, int id)
        {
            await _semaphore.WaitAsync();
            var result = _queue.Any(x => x.Bot.Username == botName && x.Id == id);
            _semaphore.Release();
            return result;
        }

        public async Task AddToQueue(IEnumerable<Item> items)
        {
            await _semaphore.WaitAsync();
            _queue.AddRange(items);
            _semaphore.Release();
        }
    }

    public class Item
    {
        // User
        public int Id;

        // Bot
        public Bot Bot = null!;

        // Action
        public Action Action;

        // State
        public ThreadState State = ThreadState.Waiting;
        public DateTime Date;
    }

    public enum ThreadState
    {
        Followed,
        Unfollowed,
        InProgress,
        Waiting
    }

    public enum Action
    {
        Follow,
        Unfollow
    }
}