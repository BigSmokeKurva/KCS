using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using KCS.Server.BotsManager;
using KCS.Server.Controllers.Models;
using KCS.Server.Database;
using KCS.Server.Database.Models;
using KCS.Server.Filters;
using KCS.Server.Follow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KCS.Server.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public class AdminPanelApiController(DatabaseContext db, Manager manager, HttpClient client) : ControllerBase
    {
        private static readonly string[] ProxyTypes = ["http", "socks5"];

        [HttpGet]
        [Route("getUsers")]
        public async Task<ActionResult> GetUsers()
        {
            var users = await db.Users
                .Where(x => x.Username != "root")
                .Select(x => new
                {
                    x.Id,
                    x.Username,
                    x.Admin
                }).ToListAsync();
            return Ok(users);
        }

        [HttpGet]
        [Route("getUserInfo")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == id);

            var userInfo = new
            {
                user.Username,
                user.Id,
                user.Admin,
                user.Password,
                InviteCode = await db.InviteCodes.AsNoTracking().Where(x => x.UserId == id).Select(x => x.Code)
                    .FirstOrDefaultAsync(),
                TokensCount = await db.Configurations.AsNoTracking().Where(x => x.Id == id)
                    .Select(x => x.Tokens.Count()).FirstAsync(),
                user.Paused,
                LogsTime = (await db.Logs.AsNoTracking().Where(x => x.Id == id).ToListAsync())
                    .Select(l => TimeHelper.ToMoscow(l.Time).Date)
                    .Distinct()
                    .OrderByDescending(x => x)
                    .Select(x => x.ToString("dd.MM.yyyy"))
            };
            return Ok(userInfo);
        }

        [HttpGet]
        [Route("getLogs")]
        public async Task<ActionResult> GetLogs(int id, string time, LogType type)
        {
            var timeParsed = DateTime.ParseExact(time, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            //var time = 
            var logs = (await db.Logs
                    .Where(x => x.Id == id && x.Type == type).ToListAsync())
                .Where(x => TimeHelper.ToMoscow(x.Time).Date == timeParsed)
                .Select(x => new
                {
                    x.Message,
                    Time = TimeHelper.ToMoscow(x.Time)
                })
                .OrderByDescending(x => x.Time);
            return Ok(logs);
        }

        [HttpPost]
        [Route("editUser")]
        public async Task<ActionResult> EditUser([FromBody] EditUserModel model)
        {
            return model.Property switch
            {
                ChangeType.Username => await ChangeUsername(model.Id, model.Value.GetString()!),
                ChangeType.Password => await ChangePassword(model.Id, model.Value.GetString()),
                ChangeType.Admin => await ChangeAdmin(model.Id, model.Value.GetBoolean()),
                ChangeType.Tokens => await ChangeTokensAsync(model.Id, model.Value),
                ChangeType.Paused => await ChangePaused(model.Id, model.Value.GetBoolean()),
                _ => Ok(new
                {
                    status = "error",
                    message = "Неизвестное свойство."
                }),
            };
        }

        private async Task<ActionResult> ChangeUsername(int id, string username)
        {
            if (!UserValidators.ValidateLogin(username))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                });
            }

            if (await db.Users.AnyAsync(x => x.Username == username))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Пользователь с таким логином уже существует."
                });
            }

            var user = await db.Users.FindAsync(id);
            user!.Username = username;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        private async Task<ActionResult> ChangePassword(int id, string? password)
        {
            if (!UserValidators.ValidatePassword(password))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                });
            }

            var user = await db.Users.FindAsync(id);
            user!.Password = password;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        private async Task<ActionResult> ChangeAdmin(int id, bool value)
        {
            var user = await db.Users.FindAsync(id);
            user!.Admin = value;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        private async Task<ActionResult> ChangePaused(int id, bool value)
        {
            var user = await db.Users.FindAsync(id);
            await manager.StopSpam(id);
            await manager.DisconnectAllBots(id);
            user!.Paused = value;
            if (value)
            {
                await manager.StopSpam(id);
                await manager.DisconnectAllBots(id);
                await FollowBot.RemoveAllFromQueue(x => x!.Id == id);
            }

            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        private async Task<ActionResult> ChangeTokensAsync(int id, JsonElement tokensElement)
        {
            // Format {username}:proxy_type:proxy_host:proxy_port:proxy_username:proxy_password:token1:token2:token3:token4
            List<TokenItem> tokens = [];
            foreach (var token in tokensElement.EnumerateArray().Select(token => token.GetString()?.Split(':')))
            {
                switch (token!.Length)
                {
                    // С ником
                    case 10 when !ProxyTypes.Contains(token[1]):
                        continue;
                    case 10:
                        tokens.Add(new TokenItem
                        {
                            Username = string.Empty,
                            Proxy = new Proxy
                            {
                                Type = token[1],
                                Host = token[2],
                                Port = token[3],
                                Credentials = new Proxy.UnSafeCredentials(token[4], token[5])
                            },
                            Token1 = token[6],
                            Token2 = token[7],
                            Token3 = token[8],
                            Token4 = token[9],
                        });
                        break;
                    // Без ника
                    case 9 when !ProxyTypes.Contains(token[0]):
                        continue;
                    case 9:
                        tokens.Add(new TokenItem
                        {
                            Username = string.Empty,
                            Proxy = new Proxy
                            {
                                Type = token[0],
                                Host = token[1],
                                Port = token[2],
                                Credentials = new Proxy.UnSafeCredentials(token[3], token[4])
                            },
                            Token1 = token[5],
                            Token2 = token[6],
                            Token3 = token[7],
                            Token4 = token[8],
                        });
                        break;
                }
            }

            var checkedTokens = await TokenCheck.Check(tokens.Select(x => (x.Token1, x.Token2, x.Token3)), client);
            List<TokenItem> tokensResult = [];
            foreach (var checkedToken in checkedTokens)
            {
                if (checkedToken.Value == string.Empty)
                    continue;
                var token = tokens.First(x => x.Token1 == checkedToken.Key);
                token.Username = checkedToken.Value;
                tokensResult.Add(token);
            }

            var configuration = await db.Configurations.FindAsync(id);
            configuration!.Tokens = tokensResult;
            await db.Bots.AddRangeAsync(tokens.Select(x => new BotInfo
            {
                Username = x.Username
            }).Where(x => !db.Bots.Any(y => x.Username == y.Username)));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                message = tokensResult.Count
            });
        }

        [HttpGet]
        [Route("getTokens")]
        public async Task<ActionResult> GetTokens(int id, bool usernames)
        {
            IEnumerable<string> tokensResult;
            var tokens = await db.Configurations
                .Where(x => x.Id == id)
                .Select(x => x.Tokens)
                .FirstAsync();

            if (usernames)
            {
                var maxUsernameLength = tokens.Count > 0 ? tokens.Max(x => x.Username.Length) : 0;
                tokensResult = tokens.Select(x =>
                    $"{x.Username.PadRight(maxUsernameLength)}:{x.Proxy.Type}:{x.Proxy.Host}:{x.Proxy.Port}:{x.Proxy.Credentials!.Value.Username}:{x.Proxy.Credentials.Value.Password}:{x.Token1}:{x.Token2}:{x.Token3}:{x.Token4}");
            }
            else
            {
                tokensResult = tokens.Select(x =>
                    $"{x.Proxy.Type}:{x.Proxy.Host}:{x.Proxy.Port}:{x.Proxy.Credentials!.Value.Username}:{x.Proxy.Credentials.Value.Password}:{x.Token1}:{x.Token2}:{x.Token3}:{x.Token4}");
            }

            return Ok(tokensResult);
        }

        [HttpDelete]
        [Route("deleteUser")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            await manager.Remove(id);
            await db.Users.Where(x => x.Id == id).ExecuteDeleteAsync();
            await db.InviteCodes.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("uploadFilter")]
        public async Task<ActionResult> UploadFilter([FromBody] List<string> words)
        {
            var set = words.ToImmutableHashSet();
            db.FilterWords.RemoveRange(db.FilterWords);
            await db.FilterWords.AddRangeAsync(set.Select(x => new FilterWord
            {
                Word = x
            }));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getFilter")]
        public async Task<ActionResult> GetFilter()
        {
            var words = await db.FilterWords
                .Select(x => x.Word)
                .ToListAsync();
            return Ok(words);
        }

        [HttpGet]
        [Route("getInviteCodes")]
        public async Task<ActionResult> GetInviteCodes()
        {
            var codes = await db.InviteCodes
                .Select(x => new
                {
                    x.Code,
                    status = x.Status.ToString(),
                    username = x.UserId != null
                        ? db.Users.Where(user => user.Id == x.UserId).Select(user => user.Username).FirstOrDefault()
                        : null,
                    expires = (DateTime?)(x.Expires == null ? null : TimeHelper.ToMoscow(x.Expires.Value)),
                    activationdate =
                        (DateTime?)(x.ActivationDate == null ? null : TimeHelper.ToMoscow(x.ActivationDate.Value)),
                    mode = x.Mode.ToString(),
                })
                .ToListAsync();
            codes.Reverse();
            return Ok(codes);
        }

        [HttpPost]
        [Route("createInviteCode")]
        public async Task<ActionResult> CreateInviteCode([FromBody] CreateInviteCodeModel model)
        {
            if (await db.InviteCodes.AnyAsync(x => x.Code == model.Code))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Код уже существует."
                });
            }

            DateTime? expires = model.Hours == null ? null : TimeHelper.GetUnspecifiedUtc().AddHours(model.Hours.Value);
            if (expires is null && model.Mode == "Time")
            {
                return Ok(new
                {
                    status = "error",
                    message = "Не указан срок жизни."
                });
            }

            var code = new InviteCode
            {
                Code = model.Code,
                Mode = model.Mode == "Time" ? InviteCodeMode.Time : InviteCodeMode.Unlimited,
                Expires = expires,
                Status = InviteCodeStatus.Active
            };
            await db.InviteCodes.AddAsync(code);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                code = new
                {
                    code.Code,
                    status = code.Status.ToString(),
                    username = code.UserId != null
                        ? db.Users.Where(user => user.Id == code.UserId).Select(user => user.Username).FirstOrDefault()
                        : null,
                    expires = (DateTime?)(code.Expires == null ? null : TimeHelper.ToMoscow(code.Expires.Value)),
                    activationdate = (DateTime?)(code.ActivationDate == null
                        ? null
                        : TimeHelper.ToMoscow(code.ActivationDate.Value)),
                    mode = code.Mode.ToString(),
                }
            });
        }

        [HttpDelete]
        [Route("deleteInviteCode")]
        public async Task<ActionResult> DeleteInviteCode(string code)
        {
            await db.InviteCodes.Where(x => x.Code == code).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}