using KCS.Server.Database;

namespace KCS.Server.Follow
{
    public class FollowThread(ICollection<Item> queue, SemaphoreSlim semaphore, HttpClient client)
    {
        private Item? _item;

        private async Task SaveToDatabase()
        {
            await using var scope = ServiceProviderAccessor.ServiceProvider.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var botInfo = await db.Bots.FindAsync(_item!.Bot.Username);
            switch (_item.State)
            {
                case ThreadState.Followed:
                    botInfo!.Followed.Add(_item.Bot.StreamerInfo.Username);
                    break;
                case ThreadState.Unfollowed:
                    botInfo!.Followed.Remove(_item.Bot.StreamerInfo.Username);
                    break;
            }

            db.Entry(botInfo!).Property(x => x.Followed).IsModified = true;
            await db.SaveChangesAsync();
        }

        internal async Task Polling()
        {
            while (true)
            {
                await semaphore.WaitAsync();
                _item = queue.FirstOrDefault(
                    x => x!.State == ThreadState.Waiting && x.Date < TimeHelper.GetUnspecifiedUtc(), null);
                if (_item is null)
                {
                    semaphore.Release();
                    await Task.Delay(1000);
                    continue;
                }

                _item.State = ThreadState.InProgress;
                semaphore.Release();
                bool response;
                switch (_item.Action)
                {
                    case Action.Follow:
                        response = await _item.Bot.Follow();
                        if (response)
                        {
                            _item.State = ThreadState.Followed;
                            await SaveToDatabase();
                        }

                        await semaphore.WaitAsync();
                        queue.Remove(_item);
                        break;
                    case Action.Unfollow:
                        response = await _item.Bot.UnFollow();
                        if (response)
                        {
                            _item.State = ThreadState.Unfollowed;
                            await SaveToDatabase();
                        }

                        await semaphore.WaitAsync();
                        queue.Remove(_item);
                        break;
                }

                semaphore.Release();
            }
        }
    }
}