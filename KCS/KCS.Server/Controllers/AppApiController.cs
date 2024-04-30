using KCS.Server.BotsManager;
using KCS.Server.Controllers.Models;
using KCS.Server.Database;
using KCS.Server.Database.Models;
using KCS.Server.Filters;
using KCS.Server.Follow;
using KCS.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Action = KCS.Server.Follow.Action;
using ThreadState = KCS.Server.Follow.ThreadState;

namespace KCS.Server.Controllers;

[Route("api/app")]
[ApiController]
[TypeFilter(typeof(UserAuthorizationFilter))]
public class AppApiController(DatabaseContext db, HttpClient httpClient, Manager manager, FollowManager followManager)
    : ControllerBase
{
    [HttpGet]
    [Route("getUsername")]
    public async Task<ActionResult> GetUsername()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var username = await db.Users.Where(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == authToken))
            .Select(x => x.Username).FirstAsync();
        return Ok(new
        {
            status = "ok",
            username
        });
    }

    [HttpGet]
    [Route("getUser")]
    public async Task<ActionResult> GetUser()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);

        return Ok(new
        {
            username = user.Username,
            streamerInfo = user.Configuration.StreamerInfo,
            isAdmin = user.Admin,
            bindsTitles = user.Configuration.Binds.Select(x => x.Title)
        });
    }

    [HttpGet]
    [Route("getIsAdmin")]
    public async Task<ActionResult> GetIsAdmin()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var admin = await db.Users.Where(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == authToken))
            .Select(x => x.Admin).FirstAsync();
        return Ok(new
        {
            status = "ok",
            isAdmin = admin
        });
    }

    [HttpPut]
    [Route("updateStreamerUsername")]
    public async Task<ActionResult> UpdateStreamerUsername(string username)
    {
        StreamerInfoResponse? json = null;
        if (!UserValidators.ValidateStreamerUsername(username))
        {
            var data = new
            {
                status = "error",
                message = "Ошибка валидации данных."
            };
            return Ok(data);
        }

        var authToken = Guid.Parse(Request.Headers.Authorization!);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://kick.com/api/v1/channels/{username}");
        request.Headers.Add("Cookie", $"cf_clearance={CloudflareBackgroundSolverService.CfClearance}");
        try
        {
            var response = await httpClient.SendAsync(request);
            json = await response.Content.ReadFromJsonAsync<StreamerInfoResponse>();
            if (json.Value.Chatroom is null)
                return Ok(new
                {
                    status = "error",
                    message = "Такого стримера не существует"
                });
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Такого стримера не существует"
            });
        }


        var user = await db.GetUser(authToken);

        await manager.ChangeStreamerUsername(user.Id, new StreamerInfo
        {
            ChatroomId = json!.Value.Chatroom?.Id,
            Username = username
        });
        await followManager.RemoveAllFromQueue(x => x.Id == user.Id);
        user.Configuration.StreamerInfo.Username = username;
        user.Configuration.StreamerInfo.ChatroomId = json.Value.Chatroom?.Id;
        db.Entry(user.Configuration).Property(x => x.StreamerInfo).IsModified = true;
        await db.AddLog(user, $"Обновил ник стримера на {username}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("getBots")]
    public async Task<ActionResult> GetBots()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        var followedBots = await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerInfo.Username))
            .Select(x => x.Username).ToListAsync();
        var queueBots = await followManager.GetUserQueue(user.Id);
        var bots = user.Configuration.Tokens.Select(x => x.Username).Select(x => new
        {
            username = x,
            isConnected = manager.IsConnected(user.Id, x),
            isFollowed = followedBots.Contains(x),
            isQueue = queueBots.Contains(x),
            Tags = user.Configuration.Tokens.First(y => y.Username == x).Tags.Select(y => y.ToString().ToLower())
        });
        return Ok(bots);
    }

    [HttpGet]
    [Route("ping")]
    public ActionResult Ping()
    {
        return Ok(new
        {
            status = "ok",
            message = "pong"
        });
    }

    [HttpPost]
    [Route("connectBot")]
    public async Task<ActionResult> ConnectBot(ConnectBotModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerInfo.Username).FirstAsync())
            .Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        try
        {
            await manager.ConnectBot(id, model.BotUsername);
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Ошибка подключения."
            });
        }

        await db.AddLog(id, $"Подключил бота {model.BotUsername}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("disconnectBot")]
    public async Task<ActionResult> DisconnectBot(ConnectBotModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        try
        {
            await manager.DisconnectBot(id, model.BotUsername);
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Произошла неизвестная ошибка при отключении ботов."
            });
        }

        await db.AddLog(id, $"Отключил бота {model.BotUsername}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("connectAllBots")]
    public async Task<ActionResult> ConnectAllBots()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerInfo.Username).FirstAsync())
            .Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        try
        {
            await manager.ConnectAllBots(id);
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Произошла неизвестная ошибка при подключении ботов."
            });
        }

        await db.AddLog(id, "Подключил всех ботов.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("disconnectAllBots")]
    public async Task<ActionResult> DisconnectAllBots()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        try
        {
            await manager.DisconnectAllBots(id);
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Произошла неизвестная ошибка при подключении ботов."
            });
        }

        await db.AddLog(id, "Отключил всех ботов.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("sendMessage")]
    public async Task<ActionResult> SendMessage(SendMessageModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerInfo.Username).FirstAsync())
            .Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        if (!manager.IsConnected(id, model.BotName))
            return Ok(new
            {
                status = "error",
                message = "Бот не подключен."
            });

        switch (model.Message.Length)
        {
            case > 1000:
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение слишком длинное."
                });
            case 0:
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение не может быть пустым."
                });
        }

        if (await db.CheckMessageFilter(model.Message))
            return Ok(new
            {
                status = "error",
                message = "Сообщение содержит запрещенные слова."
            });

        try
        {
            await manager.Send(id, model);
            await db.AddLog(id, $"Отправил сообщение {model.Message}.", LogType.Chat);
            await db.SaveChangesAsync();
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Ошибка отправки сообщения."
            });
        }

        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("getSpamTemplates")]
    public async Task<ActionResult> GetSpamTemplates()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        return Ok(configuration.SpamTemplates.Select(x => new
        {
            title = x.Title,
            threads = x.Threads,
            delay = x.Delay,
            messages = x.Messages,
            mode = x.Mode.ToString().ToLower()
        }));
    }

    [HttpGet]
    [Route("addSpamTemplate")]
    public async Task<ActionResult> AddSpamTemplate(string title)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if (await manager.SpamStarted(id))
            return Ok(new
            {
                status = "error",
                message = "Нельзя изменить конфигурацию во время работы спама"
            });

        var configuration = await db.Configurations.FindAsync(id);

        if (configuration!.SpamTemplates.Any(x => x.Title == title))
            return Ok(new
            {
                status = "error",
                message = "Шаблон с таким названием уже существует."
            });

        configuration.SpamTemplates.Add(new SpamTemplate { Title = title });
        db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
        await db.AddLog(id, $"Добавил шаблон спама {title}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("updateSpamConfiguration")]
    public async Task<ActionResult> UpdateSpamConfiguration(SpamTemplateModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if (await manager.SpamStarted(id))
            return Ok(new
            {
                status = "error",
                message = "Нельзя изменить конфигурацию во время работы спама"
            });

        if (model.Delay is > 500 or < 1)
            return Ok(new
            {
                status = "error",
                message = "Задержка не может быть больше 500 и меньше 1 секунды."
            });

        if (model.Threads is > 50 or < 0)
            return Ok(new
            {
                status = "error",
                message = "Количество потоков не может быть больше 50."
            });

        var configuration = await db.Configurations.FindAsync(id);
        if (model.Title != model.OldTitle && configuration!.SpamTemplates.Any(x => x.Title == model.Title))
            return Ok(new
            {
                status = "error",
                message = "Шаблон с таким названием уже существует."
            });

        model.Messages = model.Messages.Select(x => x.Trim())
            .Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Length > 49)).ToArray();
        if (await db.CheckMessageFilter(model.Messages))
            return Ok(new
            {
                status = "error",
                message = "Сообщение содержит запрещенные слова."
            });

        var spamTemplate = configuration!.SpamTemplates.First(x => x.Title == model.OldTitle);
        if (model.Title != model.OldTitle)
        {
            spamTemplate.Title = model.Title;
            await db.AddLog(id, $"Переименовал шаблон спама {model.OldTitle} в {model.Title}.",
                LogType.Action);
        }

        spamTemplate.Threads = model.Threads;
        spamTemplate.Delay = model.Delay;
        spamTemplate.Messages = [.. model.Messages];
        spamTemplate.Mode = model.Mode;
        db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
        await db.AddLog(id, "Обновил конфигурацию спама.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("startSpam")]
    public async Task<ActionResult> StartSpam(string title)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        var template = configuration.SpamTemplates.FirstOrDefault(x => x.Title == title);
        if (template is null)
            return Ok(new
            {
                status = "error",
                message = "Шаблон не найден."
            });

        if (await manager.SpamStarted(configuration.Id))
            return Ok(new
            {
                status = "error",
                message = "Спам уже запущен."
            });

        if (Manager.Users[configuration.Id].Bots.Count < template.Threads)
            return Ok(new
            {
                status = "error",
                message =
                    $"Количество {(template.Mode == SpamMode.Random ? "потоков" : "ботов")} не может быть больше количества подключенных ботов."
            });

        await manager.StartSpam(configuration.Id, template.Threads, template.Delay, [.. template.Messages],
            template.Mode);
        await db.AddLog(configuration.Id, "Запустил спам.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("stopSpam")]
    public async Task<ActionResult> StopSpam()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if (await manager.SpamStarted(id)) await manager.StopSpam(id);

        await db.AddLog(id, "Остановил спам.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("spamIsStarted")]
    public async Task<ActionResult> SpamIsStarted()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        return Ok(new
        {
            status = "ok",
            isStarted = await manager.SpamStarted(id)
        });
    }

    [HttpDelete]
    [Route("deleteSpamTemplate")]
    public async Task<ActionResult> DeleteSpamTemplate(string title)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var id = await db.GetId(authToken);
        if (await manager.SpamStarted(id))
            return Ok(new
            {
                status = "error",
                message = "Нельзя изменить конфигурацию во время работы спама"
            });

        var configuration = await db.Configurations.FindAsync(id);
        var template = configuration!.SpamTemplates.FirstOrDefault(x => x.Title == title);
        if (template is null)
            return Ok(new
            {
                status = "error",
                message = "Шаблон не найден."
            });

        configuration.SpamTemplates.Remove(template);
        db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
        await db.AddLog(id, $"Удалил шаблон спама {title}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("getBinds")]
    public async Task<ActionResult> GetBinds()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        return Ok(configuration.Binds);
    }

    [HttpGet]
    [Route("addBind")]
    public async Task<ActionResult> AddBind(string bindName)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        if (configuration.Binds.Any(x => x.Title == bindName))
            return Ok(new
            {
                status = "error",
                message = "Бинд с таким именем уже существует."
            });

        bindName = bindName.Trim();
        if (bindName.Length < 1 || string.IsNullOrEmpty(bindName) || string.IsNullOrWhiteSpace(bindName))
            return Ok(new
            {
                status = "error",
                message = "Имя бинда не может быть пустым."
            });

        configuration.Binds.Add(new Bind { Title = bindName });
        db.Entry(configuration).Property(x => x.Binds).IsModified = true;
        await db.AddLog(configuration.Id, $"Добавил бинд {bindName}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("editBind")]
    public async Task<ActionResult> UpdateBind(EditBindModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        model.Name = model.Name.Trim();
        if (model.Name.Length < 1 || string.IsNullOrEmpty(model.Name) || string.IsNullOrWhiteSpace(model.Name))
            return Ok(new
            {
                status = "error",
                message = "Имя бинда не может быть пустым."
            });

        var bind = configuration.Binds.FirstOrDefault(x => x.Title == model.OldName);
        if (bind is null)
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });

        if (model.Name != model.OldName && configuration.Binds.Any(x => x.Title == model.Name))
            return Ok(new
            {
                status = "error",
                message = "Бинд с таким именем уже существует."
            });

        if (model.Name != model.OldName)
        {
            bind.Title = model.Name;
            await db.AddLog(configuration.Id, $"Переименовал бинд {model.OldName} в {model.Name}.",
                LogType.Action);
        }

        bind.Messages = [.. model.Messages];
        bind.HotKeys = model.HotKeys is null ? null : [.. model.HotKeys];
        db.Entry(configuration).Property(x => x.Binds).IsModified = true;
        await db.AddLog(configuration.Id, $"Обновил бинд {model.Name}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpDelete]
    [Route("deleteBind")]
    public async Task<ActionResult> DeleteBind(string bindName)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        if (!configuration.Binds.Any(x => x.Title == bindName))
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });

        configuration.Binds.RemoveAll(x => x.Title == bindName);
        db.Entry(configuration).Property(x => x.Binds).IsModified = true;
        await db.AddLog(configuration.Id, $"Удалил бинд {bindName}.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("sendBindMessage")]
    public async Task<ActionResult> SendBindMessage(SendBindMessageModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var configuration = await db.GetConfiguration(authToken);
        var bind = configuration.Binds.FirstOrDefault(x => x.Title == model.bindname);
        if (bind is null)
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });

        var messages = bind.Messages;
        if (configuration.StreamerInfo.Username.Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        if (!manager.IsConnected(configuration.Id, model.botname))
            return Ok(new
            {
                status = "error",
                message = "Бот не подключен."
            });

        var rnd = new Random();
        var message = messages[rnd.Next(0, messages.Count)];
        if (await db.CheckMessageFilter(message))
            return Ok(new
            {
                status = "error",
                message = "Сообщение содержит запрещенные слова."
            });

        try
        {
            await manager.Send(configuration.Id, message, model.botname);
        }
        catch
        {
            return Ok(new
            {
                status = "error",
                message = "Ошибка отправки сообщения."
            });
        }

        await db.AddLog(configuration.Id, $"Отправил сообщение {message} из бинда {model.bindname}.",
            LogType.Chat);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    //[HttpGet]
    //[Route("getFollowBots")]
    //public async Task<ActionResult> GetFollowBots()
    //{
    //    var authToken = Guid.Parse(Request.Headers.Authorization!);
    //    var configuration = await db.GetConfiguration(authToken);
    //    var followedUsernames = configuration.Tokens.Where(x =>
    //            db.Bots.Any(y =>
    //                y.Username == x.Username && y.Followed.Contains(configuration.StreamerInfo.Username)))
    //        .Select(x => x.Username);
    //    var inQueueTokens = manager.GetFollowQueue(configuration.Id).Select(x => x.Username);
    //    return Ok(configuration.Tokens.ToDictionary(x => x.Username, x =>
    //    {
    //        if (inQueueTokens.Contains(x.Username)) return "waiting";

    //        return followedUsernames.Contains(x.Username) ? "followed" : "not-followed";
    //    }));
    //}

    [HttpGet]
    [Route("followBot")]
    public async Task<ActionResult> FollowBot(string botName)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        if (user.Configuration.StreamerInfo.Username.Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botName);
        if (token is null)
            return Ok(new
            {
                status = "error",
                message = "Бот не найден."
            });

        if (await followManager.IsInQueue(botName, user.Id))
            return Ok(new
            {
                status = "error",
                message = "Бот уже в очереди."
            });

        if (!manager.IsConnected(user.Id, botName))
        {
            return Ok(new
            {
                status = "error",
                message = "Бот не подключен."
            });
        }

        await followManager.AddToQueue(new Item
        {
            Id = user.Id,
            Action = Action.Follow,
            Bot = Manager.Users[user.Id].Bots[botName],
            Date = TimeHelper.GetUnspecifiedUtc(),
            State = ThreadState.Waiting
        });
        await db.AddLog(user, $"Добавил бота {botName} в очередь на follow.", LogType.Action);


        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("unfollowBot")]
    public async Task<ActionResult> UnfollowBot(string botName)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        if (user.Configuration.StreamerInfo.Username.Length == 0)
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });

        var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botName);
        if (token is null)
            return Ok(new
            {
                status = "error",
                message = "Бот не найден."
            });

        if (await followManager.IsInQueue(botName, user.Id))
            return Ok(new
            {
                status = "error",
                message = "Бот уже в очереди."
            });

        if (!manager.IsConnected(user.Id, botName))
        {
            return Ok(new
            {
                status = "error",
                message = "Бот не подключен."
            });
        }

        await followManager.AddToQueue(new Item
        {
            Id = user.Id,
            Action = Action.Unfollow,
            Bot = Manager.Users[user.Id].Bots[botName],
            Date = TimeHelper.GetUnspecifiedUtc(),
            State = ThreadState.Waiting
        });
        await db.AddLog(user, $"Добавил бота {botName} в очередь на unfollow.", LogType.Action);

        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok",
        });
    }

    [HttpGet]
    [Route("followBotCancel")]
    public async Task<ActionResult> FollowBotCancel(string botName)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botName);
        if (token is null)
            return Ok(new
            {
                status = "error",
                message = "Бот не найден."
            });

        await followManager.RemoveFromQueue(botName, user.Id);
        await db.AddLog(user, $"Убрал из очереди бота {botName}.", LogType.Action);
        await db.SaveChangesAsync();

        return Ok(new
        {
            status = "ok",
            message = (await db.Bots.Where(x => x.Username == botName).Select(x => x.Followed).FirstAsync()).Any(
                x => x.Contains(user.Configuration.StreamerInfo.Username))
                ? "followed"
                : "not-followed"
        });
    }

    [HttpPost]
    [Route("followAllBots")]
    public async Task<ActionResult> FollowAllBots([FromBody] FollowAllBotsModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        if (user.Configuration.StreamerInfo.Username.Length == 0)
        {
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });
        }

        var tokens = user.Configuration.Tokens;
        var inQueueTokens = await followManager.IsInQueue(tokens.Select(x => x.Username), user.Id);
        var followedTokens = (await db.Bots
            .Where(x => tokens.Select(item => item.Username).Contains(x.Username) &&
                        x.Followed.Contains(user.Configuration.StreamerInfo.Username)).Select(x => x.Username)
            .ToListAsync());
        var num = 0;

        var items = tokens.Where(token =>
                !inQueueTokens.Contains(token.Username) && !followedTokens.Contains(token.Username) &&
                Manager.Users[user.Id].Bots.ContainsKey(token.Username))
            .Select(token =>
            {
                num++;
                return new Item
                {
                    Id = user.Id,
                    Action = Action.Follow,
                    Bot = Manager.Users[user.Id].Bots[token.Username],
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    State = ThreadState.Waiting
                };
            })
            .ToList();
        await followManager.AddToQueue(items);
        await db.AddLog(user, $"Добавил всех ботов в очередь на follow.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpPost]
    [Route("unfollowAllBots")]
    public async Task<ActionResult> UnfollowAllBots([FromBody] FollowAllBotsModel model)
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        if (user.Configuration.StreamerInfo.Username.Length == 0)
        {
            return Ok(new
            {
                status = "error",
                message = "Не указан ник стримера."
            });
        }

        var tokens = user.Configuration.Tokens;
        var inQueueTokens = await followManager.IsInQueue(tokens.Select(x => x.Username), user.Id);
        var followedTokens = (await db.Bots
            .Where(x => tokens.Select(item => item.Username).Contains(x.Username) &&
                        !x.Followed.Contains(user.Configuration.StreamerInfo.Username)).Select(x => x.Username)
            .ToListAsync());
        var num = 0;

        var items = tokens.Where(token =>
                !inQueueTokens.Contains(token.Username) && !followedTokens.Contains(token.Username) &&
                Manager.Users[user.Id].Bots.ContainsKey(token.Username))
            .Select(token =>
            {
                num++;
                return new Item
                {
                    Id = user.Id,
                    Action = Action.Unfollow,
                    Bot = Manager.Users[user.Id].Bots[token.Username],
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    State = ThreadState.Waiting
                };
            })
            .ToList();
        await followManager.AddToQueue(items);
        await db.AddLog(user, $"Добавил всех ботов в очередь на unfollow.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("followAllBotsCancel")]
    public async Task<ActionResult> FollowAllBotsCancel()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        await followManager.RemoveAllFromQueue(x => x.Id == user.Id);
        await db.AddLog(user, "Убрал всех ботов из очереди followbot.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("getAllTags")]
    public async Task<ActionResult> GetAllTags()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        var tags = await TokenCheck.GetAllTags(user.Configuration.Tokens, user.Configuration.StreamerInfo.Username,
            httpClient);
        foreach (var tag in tags) user.Configuration.Tokens.First(x => x.Username == tag.Key.Username).Tags = tag.Value;

        db.Entry(user.Configuration).Property(x => x.Tokens).IsModified = true;
        await db.AddLog(user, "Получил все теги.", LogType.Action);
        await db.SaveChangesAsync();
        return Ok(new
        {
            status = "ok"
        });
    }

    [HttpGet]
    [Route("checkIsPause")]
    public async Task<ActionResult> CheckIsPause()
    {
        var authToken = Guid.Parse(Request.Headers.Authorization!);
        var user = await db.GetUser(authToken);
        return Ok(new
        {
            status = "ok",
            isPause = user.Paused
        });
    }
}