using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using KCS.Server.BotsManager;
using KCS.Server.Controllers.Models;
using KCS.Server.Database;
using KCS.Server.Database.Models;
using KCS.Server.Filters;
using KCS.Server.Follow;

namespace KCS.Server.Controllers
{
    [Route("api/app")]
    [ApiController]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppApiController(DatabaseContext db, HttpClient httpClient) : ControllerBase
    {
        private static readonly Random rnd = new();
        private readonly DatabaseContext db = db;
        private readonly HttpClient httpClient = httpClient;

        [HttpGet]
        [Route("getUsername")]
        public async Task<ActionResult> GetUsername()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var username = await db.Users.Where(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == auth_token)).Select(x => x.Username).FirstAsync();
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);

            return Ok(new
            {
                username = user.Username,
                streamerUsername = user.Configuration.StreamerUsername,
                isAdmin = user.Admin,
                bindsTitles = user.Configuration.Binds.Select(x => x.Title)
            });
        }

        [HttpGet]
        [Route("getIsAdmin")]
        public async Task<ActionResult> GetIsAdmin()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var admin = await db.Users.Where(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == auth_token)).Select(x => x.Admin).FirstAsync();
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
            if (!UserValidators.ValidateStreamerUsername(username))
            {
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            var auth_token = Guid.Parse(Request.Headers.Authorization);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql");
            var requestData = new
            {
                operationName = "UseLive",
                variables = new
                {
                    channelLogin = username
                },
                extensions = new
                {
                    persistedQuery = new
                    {
                        version = 1,
                        sha256Hash = "639d5f11bfb8bf3053b424d9ef650d04c4ebb7d94711d644afb08fe9a0fad5d9"
                    }
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            request.Headers.Add("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            try
            {
                var response = await httpClient.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK || text.Contains("\"user\":null"))
                {
                    throw new Exception();
                }
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Такого стримера не существует"
                });
            }


            var user = await db.GetUser(auth_token);
            user.Configuration.StreamerUsername = username;

            try
            {
                await Manager.ChangeStreamerUsername(user.Id, username);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Неизвестная ошибка"
                });
            }
            await FollowBot.RemoveAllFromQueue(x => x.Id == user.Id);
            await db.AddLog(user, $"Обновил ник стримера на {username}.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            FollowBot.Queue.RemoveAll(x => x.Id == user.Id);
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getBots")]
        public async Task<ActionResult> GetBots()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var followedBots = await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerUsername)).Select(x => x.Username).ToListAsync();
            var queueBots = (await FollowBot.IsInQueue(user.Configuration.Tokens.Select(x => x.Token), user.Id)).Select(x => user.Configuration.Tokens.First(y => y.Token == x).Username);
            var bots = user.Configuration.Tokens.Select(x => x.Username).Select(x => new
            {
                username = x,
                isConnected = Manager.IsConnected(user.Id, x),
                isFollowed = followedBots.Contains(x),
                isQueue = queueBots.Contains(x),
                Tags = user.Configuration.Tokens.First(y => y.Username == x).Tags.Select(y => y.ToString().ToLower())
            });
            return Ok(bots);
        }

        [HttpGet]
        [Route("ping")]
        public async Task<ActionResult> Ping()
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync()).Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            try
            {
                await Manager.ConnectBot(id, model.BotUsername, db);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка подключения."
                });
            }
            await db.AddLog(id, $"Подключил бота {model.BotUsername}.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.DisconnectBot(id, model.BotUsername, db);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Произошла неизвестная ошибка при отключении ботов."
                });
            }
            await db.AddLog(id, $"Отключил бота {model.BotUsername}.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync()).Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            try
            {
                await Manager.ConnectAllBots(id, db);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Произошла неизвестная ошибка при подключении ботов."
                });
            }
            await db.AddLog(id, $"Подключил всех ботов.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.DisconnectAllBots(id, db);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await db.AddLog(id, $"Отключил всех ботов.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if ((await db.Configurations.Where(x => x.Id == id).Select(x => x.StreamerUsername).FirstAsync()).Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            if (!Manager.IsConnected(id, model.BotName))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не подключен."
                });
            }
            if (model.Message.Length > 1000)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение слишком длинное."
                });
            }
            if (model.Message.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение не может быть пустым."
                });
            }
            if (await db.CheckMessageFilter(model.Message))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            try
            {
                var r = await Manager.Send(id, model.BotName, model.Message, model.ReplyTo, db);
                if (r)
                {
                    await db.AddLog(id, $"Отправил сообщение {model.Message}.", Database.Models.LogType.Chat);
                    await db.SaveChangesAsync();
                    return Ok(new
                    {
                        status = "ok"
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "Ошибка отправки сообщения."
                    });
                }
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка отправки сообщения."
                });
            }
        }

        [HttpGet]
        [Route("getSpamTemplates")]
        public async Task<ActionResult> GetSpamTemplates()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Нельзя изменить конфигурацию во время работы спама"
                });
            }
            var configuration = await db.Configurations.FindAsync(id);

            if (configuration.SpamTemplates.Any(x => x.Title == title))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Шаблон с таким названием уже существует."
                });
            }
            configuration.SpamTemplates.Add(new SpamTemplate() { Title = title });
            db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
            await db.AddLog(id, $"Добавил шаблон спама {title}.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Нельзя изменить конфигурацию во время работы спама"
                });
            }
            if (model.Delay > 500 || model.Delay < 1)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Задержка не может быть больше 500 и меньше 1 секунды."
                });
            }
            if (model.Threads > 50 || model.Threads < 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Количество потоков не может быть больше 50."
                });
            }
            var configuration = await db.Configurations.FindAsync(id);
            if (model.Title != model.OldTitle && configuration.SpamTemplates.Any(x => x.Title == model.Title))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Шаблон с таким названием уже существует."
                });
            }
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Length > 49)).ToArray();
            if (await db.CheckMessageFilter(model.Messages))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }

            var spamTemplate = configuration.SpamTemplates.First(x => x.Title == model.OldTitle);
            if (model.Title != model.OldTitle)
            {
                spamTemplate.Title = model.Title;
                await db.AddLog(id, $"Переименовал шаблон спама {model.OldTitle} в {model.Title}.", Database.Models.LogType.Action);
            }
            spamTemplate.Threads = model.Threads;
            spamTemplate.Delay = model.Delay;
            spamTemplate.Messages = model.Messages.ToList();
            spamTemplate.Mode = model.Mode;
            db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
            await db.AddLog(id, $"Обновил конфигурацию спама.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            var template = configuration.SpamTemplates.FirstOrDefault(x => x.Title == title);
            if (template is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Шаблон не найден."
                });
            }
            if (await Manager.SpamStarted(configuration.Id, db))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Спам уже запущен."
                });
            }
            if (Manager.users[configuration.Id].bots.Count < template.Threads)
            {
                return Ok(new
                {
                    status = "error",
                    message = $"Количество {(template.Mode == SpamMode.Random ? "потоков" : "ботов")} не может быть больше количества подключенных ботов."
                });
            }
            await Manager.StartSpam(configuration.Id, template.Threads, template.Delay, [.. template.Messages], template.Mode, db);
            await db.AddLog(configuration.Id, $"Запустил спам.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                await Manager.StopSpam(id, db);
            }
            await db.AddLog(id, $"Остановил спам.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            return Ok(new
            {
                status = "ok",
                isStarted = await Manager.SpamStarted(id, db)
            });
        }

        [HttpDelete]
        [Route("deleteSpamTemplate")]
        public async Task<ActionResult> DeleteSpamTemplate(string title)
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Нельзя изменить конфигурацию во время работы спама"
                });
            }
            var configuration = await db.Configurations.FindAsync(id);
            var template = configuration.SpamTemplates.FirstOrDefault(x => x.Title == title);
            if (template is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Шаблон не найден."
                });
            }
            configuration.SpamTemplates.Remove(template);
            db.Entry(configuration).Property(x => x.SpamTemplates).IsModified = true;
            await db.AddLog(id, $"Удалил шаблон спама {title}.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            return Ok(configuration.Binds);
        }

        [HttpGet]
        [Route("addBind")]
        public async Task<ActionResult> AddBind(string bindname)
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            if (configuration.Binds.Any(x => x.Title == bindname))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд с таким именем уже существует."
                });
            }
            bindname = bindname.Trim();
            if (bindname.Length < 1 || string.IsNullOrEmpty(bindname) || string.IsNullOrWhiteSpace(bindname))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Имя бинда не может быть пустым."
                });
            }
            configuration.Binds.Add(new Bind() { Title = bindname });
            db.Entry(configuration).Property(x => x.Binds).IsModified = true;
            await db.AddLog(configuration.Id, $"Добавил бинд {bindname}.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            model.Name = model.Name.Trim();
            if (model.Name.Length < 1 || string.IsNullOrEmpty(model.Name) || string.IsNullOrWhiteSpace(model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Имя бинда не может быть пустым."
                });
            }
            var bind = configuration.Binds.FirstOrDefault(x => x.Title == model.OldName);
            if (bind is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд не найден."
                });
            }
            if (model.Name != model.OldName && configuration.Binds.Any(x => x.Title == model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд с таким именем уже существует."
                });
            }
            if (model.Name != model.OldName)
            {
                bind.Title = model.Name;
                await db.AddLog(configuration.Id, $"Переименовал бинд {model.OldName} в {model.Name}.", Database.Models.LogType.Action);
            }
            bind.Messages = model.Messages.ToList();
            bind.HotKeys = model.HotKeys is null ? null : model.HotKeys.ToList();
            db.Entry(configuration).Property(x => x.Binds).IsModified = true;
            await db.AddLog(configuration.Id, $"Обновил бинд {model.Name}.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpDelete]
        [Route("deleteBind")]
        public async Task<ActionResult> DeleteBind(string bindname)
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            if (!configuration.Binds.Any(x => x.Title == bindname))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд не найден."
                });
            }
            configuration.Binds.RemoveAll(x => x.Title == bindname);
            db.Entry(configuration).Property(x => x.Binds).IsModified = true;
            await db.AddLog(configuration.Id, $"Удалил бинд {bindname}.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            var bind = configuration.Binds.FirstOrDefault(x => x.Title == model.bindname);
            if (bind is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд не найден."
                });
            }
            var messages = bind.Messages;
            if (configuration.StreamerUsername.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            if (!Manager.IsConnected(configuration.Id, model.botname))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не подключен."
                });
            }
            var message = messages[rnd.Next(0, messages.Count)];
            if (await db.CheckMessageFilter(message))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            var r = await Manager.Send(configuration.Id, model.botname, message, null, db);
            if (!r)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка отправки сообщения."
                });
            }
            await db.AddLog(configuration.Id, $"Отправил сообщение {message} из бинда {model.bindname}.", Database.Models.LogType.Chat);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getFollowBots")]
        public async Task<ActionResult> GetFollowBots()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            var followedUsernames = configuration.Tokens.Where(x => db.Bots.Any(y => y.Username == x.Username && y.Followed.Contains(configuration.StreamerUsername))).Select(x => x.Username);
            var inQueueTokens = await FollowBot.IsInQueue(configuration.Tokens.Select(x => x.Token), configuration.Id);
            return Ok(configuration.Tokens.ToDictionary(x => x.Username, x =>
            {
                if (inQueueTokens.Contains(x.Token))
                {
                    return "waiting";
                }
                if (followedUsernames.Contains(x.Username))
                {
                    return "followed";
                }
                return "not-followed";
            }));
        }

        [HttpGet]
        [Route("followBot")]
        public async Task<ActionResult> FollowBot_(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            if (user.Configuration.StreamerUsername.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            if (await FollowBot.IsInQueue(token.Token, user.Id))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот уже в очереди."
                });
            }

            var item = new Item
            {
                Id = user.Id,
                Username = token.Username,
                Token = token.Token,
                TargetId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername),
                Channel = user.Configuration.StreamerUsername,
                Action = Actions.Follow,
                Date = TimeHelper.GetUnspecifiedUtc(),
                Proxy = token.Proxy
            };
            await FollowBot.AddToQueue(item);
            await db.AddLog(user, $"Добавил бота {botname} в очередь на follow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("unfollowBot")]
        public async Task<ActionResult> UnfollowBot(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            if (user.Configuration.StreamerUsername.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            if (await FollowBot.IsInQueue(token.Token, user.Id))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот уже в очереди."
                });
            }
            var item = new Item
            {
                Id = user.Id,
                Username = token.Username,
                Token = token.Token,
                TargetId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername),
                Channel = user.Configuration.StreamerUsername,
                Action = Actions.Unfollow,
                Date = TimeHelper.GetUnspecifiedUtc(),
                Proxy = token.Proxy
            };
            await FollowBot.AddToQueue(item);
            await db.AddLog(user, $"Добавил бота {botname} в очередь на unfollow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("followBotCancel")]
        public async Task<ActionResult> FollowBotCancel(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            await FollowBot.RemoveFromQueue(token.Username, user.Id);
            await db.AddLog(user, $"Убрал из очереди бота {botname}.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();

            return Ok(new
            {
                status = "ok",
                message = (await db.Bots.Where(x => x.Username == botname).Select(x => x.Followed).FirstAsync()).Any(x => x.Contains(user.Configuration.StreamerUsername)) ?
                "followed" : "not-followed"
            });
        }

        [HttpPost]
        [Route("followAllBots")]
        public async Task<ActionResult> FollowAllBots([FromBody] FollowAllBotsModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            if (user.Configuration.StreamerUsername.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            var tokens = user.Configuration.Tokens.Select(x => x.Token);
            var bots = user.Configuration.Tokens.Select(x => x.Username);
            var channelId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername);
            var inQueueTokens = await FollowBot.IsInQueue(tokens, user.Id);
            var followedTokens = (await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerUsername) && bots.Contains(x.Username)).Select(x => x.Username).ToListAsync())
                .Select(x => user.Configuration.Tokens.First(y => x == y.Username).Token);
            var items = new List<Item>();
            var num = 1;
            foreach (var token in tokens)
            {
                if (inQueueTokens.Contains(token) || followedTokens.Contains(token))
                {
                    continue;
                }
                items.Add(new Item
                {
                    Id = user.Id,
                    Username = user.Configuration.Tokens.First(x => x.Token == token).Username,
                    Token = token,
                    TargetId = channelId,
                    Channel = user.Configuration.StreamerUsername,
                    Action = Actions.Follow,
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    Proxy = user.Configuration.Tokens.First(y => y.Token == token).Proxy
                });
                num++;
            }
            await FollowBot.AddToQueue(items);
            await db.AddLog(user, $"Добавил всех ботов в очередь на follow.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            if (user.Configuration.StreamerUsername.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан ник стримера."
                });
            }
            var tokens = user.Configuration.Tokens.Select(x => x.Token);
            var bots = user.Configuration.Tokens.Select(x => x.Username);
            var channelId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername);
            var inQueueTokens = await FollowBot.IsInQueue(tokens, user.Id);
            var followedTokens = (await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerUsername) && bots.Contains(x.Username)).Select(x => x.Username).ToListAsync())
                .Select(x => user.Configuration.Tokens.First(y => x == y.Username).Token);
            var items = new List<Item>();
            var num = 1;
            foreach (var token in tokens)
            {
                if (inQueueTokens.Contains(token) || !followedTokens.Contains(token))
                {
                    continue;
                }
                items.Add(new Item
                {
                    Id = user.Id,
                    Username = user.Configuration.Tokens.First(x => x.Token == token).Username,
                    Token = token,
                    TargetId = channelId,
                    Channel = user.Configuration.StreamerUsername,
                    Action = Actions.Unfollow,
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    Proxy = user.Configuration.Tokens.First(y => y.Token == token).Proxy
                });
                num++;
            }
            await FollowBot.AddToQueue(items);
            await db.AddLog(user, $"Добавил всех ботов в очередь на unfollow.", Database.Models.LogType.Action);
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            await FollowBot.RemoveAllFromQueue(x => x.Id == user.Id);
            await db.AddLog(user, $"Убрал всех ботов из очереди followbot.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
            });
        }

        [HttpGet]
        [Route("getAllTags")]
        public async Task<ActionResult> GetAllTags()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var tags = await TokenCheck.GetAllTags(user.Configuration.Tokens, user.Configuration.StreamerUsername);
            foreach (var tag in tags)
            {
                user.Configuration.Tokens.First(x => x.Username == tag.Key.Username).Tags = tag.Value;
            }
            db.Entry(user.Configuration).Property(x => x.Tokens).IsModified = true;
            await db.AddLog(user, $"Получил все теги.", Database.Models.LogType.Action);
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
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            return Ok(new
            {
                status = "ok",
                isPause = user.Paused
            });
        }
    }
}
