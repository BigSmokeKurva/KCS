using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using KCS.Server.BotsManager;
using KCS.Server.Controllers.Models;
using KCS.Server.Database;
using KCS.Server.Database.Models;
using KCS.Server.Filters;
using KCS.Server.Follow;

namespace KCS.Server.Controllers
{

    [Route("api/admin")]
    [ApiController]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public class AdminPanelApiController(DatabaseContext db) : ControllerBase
    {
        private readonly DatabaseContext db = db;
        [HttpGet]
        [Route("getusers")]
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
        [Route("getuserinfo")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == id);

            var userInfo = new
            {
                user.Username,
                user.Id,
                Admin = user.Admin,
                Password = user.Password,
                InviteCode = await db.InviteCodes.AsNoTracking().Where(x => x.UserId == id).Select(x => x.Code).FirstOrDefaultAsync(),
                TokensCount = await db.Configurations.AsNoTracking().Where(x => x.Id == id)
                        .Select(x => x.Tokens.Count()).FirstAsync(),
                Paused = user.Paused,
                LogsTime = (await db.Logs.AsNoTracking().Where(x => x.Id == id).ToListAsync())
                        .Select(l => TimeHelper.ToMoscow(l.Time).Date)
                        .Distinct()
                        .OrderByDescending(x => x)
                        .Select(x => x.ToString("dd.MM.yyyy"))
            };
            return Ok(userInfo);
        }

        [HttpGet]
        [Route("getlogs")]
        public async Task<ActionResult> GetLogs(int id, string time, LogType type)
        {

            var timeParsed = DateTime.ParseExact(time, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
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
        [Route("edituser")]
        public async Task<ActionResult> EditUser([FromBody] EditUserModel model)
        {
            return model.Property switch
            {
                ChangeType.Username => await ChangeUsername(model.Id, model.Value.GetString()),
                ChangeType.Password => await ChangePassword(model.Id, model.Value.GetString()),
                ChangeType.Admin => await ChangeAdmin(model.Id, model.Value.GetBoolean()),
                ChangeType.Tokens => await ChangeTokens(model.Id, model.Value),
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
            user.Username = username;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangePassword(int id, string password)
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
            user.Password = password;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeAdmin(int id, bool value)
        {
            var user = await db.Users.FindAsync(id);
            user.Admin = value;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangePaused(int id, bool value)
        {
            var user = await db.Users.FindAsync(id);
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            user.Paused = value;
            if (value)
            {
                await Manager.StopSpam(id, db);
                await Manager.DisconnectAllBots(id, db);
                await FollowBot.RemoveAllFromQueue(x => x.Id == id);
            }
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeTokens(int id, JsonElement tokens)
        {
            // Format: {username}:token:proxy_type:proxy_host:proxy_port:proxy_username:proxy_password
            var __tokens = tokens.EnumerateArray().Select(x => (x.GetString()).Split(':')).Distinct().Where(x => x.Length == 6 || x.Length == 7)/*.ToDictionary(x => x[0], x => x[1..])*/;
            Dictionary<string, string[]> _tokens = new();
            foreach (var token in __tokens)
            {
                if (token[0].Length != 30)
                {
                    _tokens.TryAdd(token[1], token[2..]);
                    continue;
                }
                _tokens.TryAdd(token[0], token[1..]);
            }
            var tokensChecked = await TokenCheck.Check(_tokens.Keys);
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            await FollowBot.RemoveAllFromQueue(x => x.Id == id);
            var user = await db.Users.FindAsync(id);
            user.Configuration.Tokens = tokensChecked.Keys.Select(x => new TokenItem
            {
                Proxy = new KCS.Server.Database.Models.Proxy
                {
                    Type = _tokens[x][0],
                    Host = _tokens[x][1],
                    Port = _tokens[x][2],
                    Credentials = new Proxy.UnSafeCredentials(_tokens[x][3], _tokens[x][4])
                },
                Token = x,
                Username = tokensChecked[x]
            }).ToList();
            await db.Bots.AddRangeAsync(tokensChecked.Select(x => new BotInfo
            {
                Username = x.Value
            }).Where(x => !db.Bots.Any(y => x.Username == y.Username)));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                message = tokensChecked.Count
            });
        }

        [HttpGet]
        [Route("gettokens")]
        public async Task<ActionResult> GetTokens(int id, bool usernames)
        {
            IEnumerable<string> tokens;
            var _tokens = await db.Configurations
                .Where(x => x.Id == id)
                .Select(x => x.Tokens)
                .FirstAsync();

            if (usernames)
            {
                var maxUsernameLength = _tokens.Any() ? _tokens.Max(x => x.Username.Length) : 0;
                tokens = _tokens.Select(x => $"{x.Username.PadRight(maxUsernameLength)}:{x.Token}:{x.Proxy.Type}:{x.Proxy.Host}:{x.Proxy.Port}:{x.Proxy.Credentials.Value.Username}:{x.Proxy.Credentials.Value.Password}");
            }
            else
            {
                tokens = _tokens.Select(x => $"{x.Token}:{x.Proxy.Type}:{x.Proxy.Host}:{x.Proxy.Port}:{x.Proxy.Credentials.Value.Username}:{x.Proxy.Credentials.Value.Password}");
            }
            return Ok(tokens);
        }

        [HttpDelete]
        [Route("deleteuser")]
        public async Task<ActionResult> DeleteUser(int id)
        {

            await Manager.Remove(id, db);
            await db.Users.Where(x => x.Id == id).ExecuteDeleteAsync();
            await db.InviteCodes.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("uploadfilter")]
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
        [Route("getfilter")]
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
                    username = x.UserId != null ? db.Users.Where(user => user.Id == x.UserId).Select(user => user.Username).FirstOrDefault() : null,
                    expires = (DateTime?)(x.Expires == null ? null : TimeHelper.ToMoscow(x.Expires.Value)),
                    activationdate = (DateTime?)(x.ActivationDate == null ? null : TimeHelper.ToMoscow(x.ActivationDate.Value)),
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
                    username = code.UserId != null ? db.Users.Where(user => user.Id == code.UserId).Select(user => user.Username).FirstOrDefault() : null,
                    expires = (DateTime?)(code.Expires == null ? null : TimeHelper.ToMoscow(code.Expires.Value)),
                    activationdate = (DateTime?)(code.ActivationDate == null ? null : TimeHelper.ToMoscow(code.ActivationDate.Value)),
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
