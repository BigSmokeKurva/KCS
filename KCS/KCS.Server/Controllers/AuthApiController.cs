using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KCS.Server.Controllers.Models;
using KCS.Server.Database;
using KCS.Server.Server.Controllers.Models;

namespace KCS.Server.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthApiController(ILogger<AuthApiController> logger, DatabaseContext db) : ControllerBase
    {
        private readonly ILogger<AuthApiController> _logger = logger;
        private readonly DatabaseContext db = db;

        [HttpPost]
        [Route("signup")]
        public async Task<ActionResult> Registration([FromBody] RegistrationModel model)
        {
            var loginValid = UserValidators.ValidateLogin(model.Login);
            var passwordValid = UserValidators.ValidatePassword(model.Password);
            var inviteCodeValid = await db.InviteCodes.AnyAsync(x => x.Code == model.InviteCode && x.Status == Database.Models.InviteCodeStatus.Active);
            if (!(loginValid && passwordValid && inviteCodeValid))
            {
                var data = new ResponseModel
                {
                    status = "error",
                    errors = new List<ErrorResponseModel>()
                };
                if (!loginValid)
                {
                    data.errors.Add(new ErrorResponseModel
                    {
                        type = "login",
                        message = "Логин должен содержать только латинские буквы, цифры, символы _ и -."
                    });
                }
                if (!passwordValid)
                {
                    data.errors.Add(new ErrorResponseModel
                    {
                        type = "password",
                        message = "Пароль должен содержать только буквы латинского алфавита (заглавные и строчные), цифры и символы !@#$%^&*()_-."
                    });
                }
                if (!inviteCodeValid)
                {
                    data.errors.Add(new ErrorResponseModel
                    {
                        type = "inviteCode",
                        message = "Неверный инвайт код."
                    });
                }
                return Ok(data);
            }

            if (await db.Users.AnyAsync(x => x.Username == model.Login))
            {
                var data = new ResponseModel
                {
                    status = "error",
                    errors = new List<ErrorResponseModel>()
                };
                data.errors.Add(new ErrorResponseModel
                {
                    type = "login",
                    message = "Пользователь с таким логином уже существует."
                });
                return Ok(data);
            }
            var user = await db.Users.AddAsync(new()
            {
                Username = model.Login,
                Password = model.Password,
                Sessions = [
                   new()
                   {
                       Expires = TimeHelper.GetUnspecifiedUtc().AddDays(30),
                   }
               ],
                Configuration = new(),
                Logs = [
                   new()
                   {
                       Time = TimeHelper.GetUnspecifiedUtc(),
                       Message = "Зарегистрировался.",
                       Type = Database.Models.LogType.Action
                   },
                   new()
                   {
                          Time = TimeHelper.GetUnspecifiedUtc(),
                          Message = $"Использовал инвайт-код: {model.InviteCode}",
                          Type = Database.Models.LogType.Action
                     }
               ]
            });
            try
            {
                await db.SaveChangesAsync();
                var code = await db.InviteCodes.FirstAsync(x => x.Code == model.InviteCode);
                code.Status = Database.Models.InviteCodeStatus.Used;
                code.UserId = user.Entity.Id;
                code.ActivationDate = TimeHelper.GetUnspecifiedUtc();
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var data = new ResponseModel
                {
                    status = "error",
                    errors = []
                };
                data.errors.Add(new ErrorResponseModel
                {
                    type = "notification",
                    message = "Произошла ошибка при регистрации. Попробуйте позже."
                });
                return Ok(data);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,

                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = TimeHelper.GetUnspecifiedUtc().AddDays(30)
            };
            Response.Cookies.Append("auth_token", user.Entity.Sessions.First().AuthToken.ToString(), cookieOptions);
            // редирект на главную страницу
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("signin")]
        public async Task<ActionResult> Authorization([FromBody] AuthorizationModel model)
        {
            var loginValid = UserValidators.ValidateLogin(model.Login);
            var passwordValid = UserValidators.ValidatePassword(model.Password);
            if (!(loginValid && passwordValid))
            {
                var data = new ResponseModel
                {
                    status = "error",
                    errors = new List<ErrorResponseModel>()
                };
                if (!loginValid)
                {
                    data.errors.Add(new ErrorResponseModel
                    {
                        type = "login",
                        message = "Логин должен содержать только латинские буквы, цифры, символы _ и -."
                    });
                }
                if (!passwordValid)
                {
                    data.errors.Add(new ErrorResponseModel
                    {
                        type = "password",
                        message = "Пароль должен содержать только буквы латинского алфавита (заглавные и строчные), цифры и символы !@#$%^&*()_-."
                    });
                }
                return Ok(data);
            }

            // проверка на существование пользователя
            var user = await db.Users.FirstOrDefaultAsync(x => x.Username == model.Login && x.Password == model.Password);
            if (user is null)
            {
                var data = new ResponseModel
                {
                    status = "error",
                    errors = new List<ErrorResponseModel>()
                };
                data.errors.Add(new ErrorResponseModel
                {
                    type = "notification",
                    message = "Пользователь с таким логином и паролем не найден."
                });
                return Ok(data);
            }
            // получаем токен сессии
            var auth_token = await db.Sessions.AddAsync(new()
            {
                Id = user.Id,
                Expires = TimeHelper.GetUnspecifiedUtc().AddDays(30)
            });
            await db.AddLog(user, "Авторизовался.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = TimeHelper.GetUnspecifiedUtc().AddDays(30),
                Path = "/"
            };
            Response.Cookies.Append("auth_token", auth_token.Entity.AuthToken.ToString(), cookieOptions);
            // редирект на главную страницу
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpDelete]
        [Route("logout")]
        public async Task<ActionResult> Unauthorization()
        {

            var auth_token = Guid.Parse(Request.Cookies["auth_token"]);

            if (!await db.Sessions.AnyAsync(x => x.AuthToken == auth_token))
                return Ok(new { status = "ok" });
            db.Sessions.Remove(await db.Sessions.FindAsync(auth_token));
            await db.AddLog(await db.Users.FirstAsync(x => x.Sessions.Any(y => y.AuthToken == auth_token && x.Id == y.Id)), "Вышел из аккаунта.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            // Удаляем все cookie на сайте
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            // Выполняем редирект на страницу "/"
            return Ok(new { status = "ok" });
        }

        [HttpGet]
        [Route("checkAuth")]
        public async Task<ActionResult> CheckAuth()
        {
            var auth_token = Request.Cookies["auth_token"];
            if (!Guid.TryParse(auth_token, out Guid auth_token_uid) || !await db.Sessions.AnyAsync(x => x.AuthToken == auth_token_uid))
            {
                return Ok(new
                {
                    auth = false
                });
            }
            return Ok(new
            {
                auth = true
            });
        }
    }
}
