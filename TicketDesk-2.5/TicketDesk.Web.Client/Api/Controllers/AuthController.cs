using System;
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
    public class AuthController : Controller {
        private TicketDeskUserManager UserManager { get; set; }
        private TicketDeskSignInManager SignInManager { get; set; }
        private IAuthenticationManager AuthenticationManager {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        public AuthController(TicketDeskUserManager userManager, TicketDeskSignInManager signInManager) {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login(string username, string password, string returnUrl) {
            if(string.IsNullOrEmpty(username)) {
                return RedirectFailedLogin(returnUrl, "username can not be empty");
            }
            if(string.IsNullOrEmpty(password)) {
                return RedirectFailedLogin(returnUrl, "password can not be empty");
            }
            if(string.IsNullOrEmpty(returnUrl)) {
                return RedirectFailedLogin(returnUrl, "returnUrl can not be empty");
            }

            var result = await SignInManager.PasswordSignInAsync(username, password, false, false);
            switch(result) {
                case SignInStatus.Success:
                    return RedirectSuccessLogin(returnUrl);
                case SignInStatus.LockedOut:
                    return RedirectFailedLogin(returnUrl, "LockedOut does not support in api-mode");
                case SignInStatus.RequiresVerification:
                    return RedirectFailedLogin(returnUrl, "RequiresVerification does not support in api-mode");
                // ReSharper disable once RedundantCaseLabel
                case SignInStatus.Failure:
                default:
                    return RedirectFailedLogin(returnUrl, "Invalid login attempt");
            }
        }

        //
        // POST: /auth/login/google
        [HttpPost]
        [Route("login/google")]
        public ActionResult LoginByGoogle(string returnUrl) {
            return ExternalLogin(returnUrl, "Google");
        }

        //
        // POST: /auth/login/external
        [HttpPost]
        [Route("login/external")]
        public ActionResult ExternalLogin(string returnUrl, string provider) {
            if(string.IsNullOrEmpty(returnUrl)) {
                return RedirectFailedLogin(null, "returnUrl can not be empty");
            }
            if(string.IsNullOrEmpty(provider)) {
                return RedirectFailedLogin(returnUrl, "provider can not be empty");
            }
            
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Auth", new {ReturnUrl = returnUrl}));
        }

        //
        // GET: /auth/login/external/callback
        [HttpGet]
        [Route("login/external/callback")]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl) {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if(loginInfo == null) {
                return RedirectFailedLogin(returnUrl, "Invalid login attempt");
            }

            var signInStatus = await SignInManager.ExternalSignInAsync(loginInfo, false);
            //var accessToken = loginInfo.ExternalIdentity.FindFirstValue(Startup.TokenClaimName);
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
                    return registrationResult ? RedirectSuccessLogin(returnUrl) : RedirectFailedLogin(returnUrl, "Invalid auto-register attempt");
            }
        }

        // TODO: test this method
        //
        // POST: /auth/logout
        [HttpPost]
        [Route("logout")]
        public void Logout() {
            AuthenticationManager.SignOut();
        }
        
        private async Task<bool> Register(ExternalLoginInfo info) {
            var user = new TicketDeskUser {
                UserName = info.Email,
                Email = info.Email,
                DisplayName = info.ExternalIdentity.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name") ?? info.DefaultUserName
            };
            var createResult = await UserManager.CreateAsync(user);
            var addToRoleResult = await UserManager.AddToRoleAsync(user.Id, "TdInternalUsers");
            if(createResult.Succeeded && addToRoleResult.Succeeded) {
                createResult = await UserManager.AddLoginAsync(user.Id, info.Login);
                if(createResult.Succeeded) {
                    await SignInManager.SignInAsync(user, false, false);
                    return true;
                }
            }
            return false;
        }

        private ActionResult RedirectSuccessLogin(string returnUrl) {
            return Redirect(returnUrl);
        }

        private ActionResult RedirectFailedLogin(string returnUrl, string message) {
            return Redirect(UpdateReturnUrl(returnUrl, "error", message));
        }

        private string UpdateReturnUrl(string returnUrl, string key, string value) {
            var uri = returnUrl == null || Url.IsLocalUrl(returnUrl) ? new Uri(Request.UrlReferrer, returnUrl) : new Uri(returnUrl, UriKind.Absolute);
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
