using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TicketDesk.Web.Client.Api.Framework;
using TicketDesk.Web.Identity.Model;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("auth")]
    [AllowAnonymous]
    public class ExternalAuthController : Controller {
        private TicketDeskUserManager UserManager { get; set; }
        private TicketDeskSignInManager SignInManager { get; set; }
        private IAuthenticationManager AuthenticationManager {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        public ExternalAuthController(TicketDeskUserManager userManager, TicketDeskSignInManager signInManager) {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        // POST: /auth/login/google
        [HttpGet]
        [Route("login/google")]
        public ActionResult LoginByGoogle(string returnUrl) {
            return ExternalLogin(returnUrl, "Google");
        }

        // POST: /auth/login/external
        [HttpGet]
        [Route("login/external")]
        public ActionResult ExternalLogin(string returnUrl, string provider) {
            if(string.IsNullOrEmpty(returnUrl)) {
                return Error("returnUrl can not be empty");
            }
            if(string.IsNullOrEmpty(provider)) {
                return Error("provider can not be empty");
            }
            
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "ExternalAuth", new {ReturnUrl = returnUrl}));
        }

        // GET: /auth/login/external/callback
        [HttpGet]
        [Route("login/external/callback")]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl) {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if(loginInfo == null) {
                return RedirectFailedLogin(returnUrl, "Invalid login attempt");
            }

            var signInStatus = await SignInManager.ExternalSignInAsync(loginInfo, false);
            //var accessToken = loginInfo.ExternalIdentity.FindFirstValue(Auth.TokenClaimName);
            switch(signInStatus) {
                case SignInStatus.Success:
                    return RedirectSuccessLogin(returnUrl);
                case SignInStatus.LockedOut:
                    return RedirectFailedLogin(returnUrl, "LockedOut does not support in api-mode");
                case SignInStatus.RequiresVerification:
                    return RedirectFailedLogin(returnUrl, "RequiresVerification does not support in api-mode");
                // ReSharper disable once RedundantCaseLabel
                case SignInStatus.Failure:
                default:
                    var registrationResult = await Register(loginInfo);
                    return registrationResult
                        ? RedirectSuccessLogin(returnUrl)
                        : RedirectFailedLogin(returnUrl, "Invalid auto-register attempt");
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult SuccessLoginCallback(string returnUrl) {
            var cookie = Request.Cookies[Auth.CookieName];
            return cookie != null
                ? Redirect(UpdateReturnUrl(returnUrl, "cookie", cookie.Value))
                : RedirectFailedLogin(returnUrl, "Internal login error");
        }

        private async Task<bool> Register(ExternalLoginInfo info) {
            var user = new TicketDeskUser {
                UserName = info.Email,
                Email = info.Email,
                DisplayName = info.ExternalIdentity.FindFirstValue(ClaimTypes.Name) ?? info.DefaultUserName
            };
            var createResult = await UserManager.CreateAsync(user);
            if(createResult.Succeeded) {
                var addToRoleResult = await UserManager.AddToRoleAsync(user.Id, "TdInternalUsers");
                var addLoginResult = await UserManager.AddLoginAsync(user.Id, info.Login);
                if(addToRoleResult.Succeeded && addLoginResult.Succeeded) {
                    await SignInManager.SignInAsync(user, false, false);
                    return true;
                }
            }
            return false;
        }

        private ActionResult Error(string message) {
            Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Json(message, JsonRequestBehavior.AllowGet);
        }

        private ActionResult RedirectSuccessLogin(string returnUrl) {
            return RedirectToAction("SuccessLoginCallback", new {returnUrl});
        }

        private ActionResult RedirectFailedLogin(string returnUrl, string message) {
            return Redirect(UpdateReturnUrl(returnUrl, "error", message));
        }

        private string UpdateReturnUrl(string returnUrl, string key, string value) {
            var uri = returnUrl == null || Url.IsLocalUrl(returnUrl) ? new Uri(Request.Url, returnUrl) : new Uri(returnUrl, UriKind.Absolute);
            var newUri = uri.AddQuery(key, value);
            return newUri.ToString();
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        internal class ChallengeResult : HttpUnauthorizedResult {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null) {
            }

            public ChallengeResult(string provider, string redirectUri, string userId) {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context) {
                var properties = new AuthenticationProperties {RedirectUri = RedirectUri};
                if(UserId != null) {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}
