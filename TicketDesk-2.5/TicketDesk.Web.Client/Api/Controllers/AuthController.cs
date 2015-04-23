using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TicketDesk.Web.Client.Api.Dtos;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("auth")]
    [AllowAnonymous]
    public class AuthController : ApiController {
        private TicketDeskSignInManager SignInManager { get; set; }

        private IAuthenticationManager AuthenticationManager {
            get { return HttpContext.Current.GetOwinContext().Authentication; }
        }

        public AuthController(TicketDeskSignInManager signInManager) {
            SignInManager = signInManager;
        }

        // POST: /auth/login
        [HttpPost]
        [Route("login")]
        public async Task<IHttpActionResult> Login(LoginDto login) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var result = await SignInManager.PasswordSignInAsync(login.Username, login.Password, false, false);
            switch(result) {
                case SignInStatus.Success:
                    return Ok();
                case SignInStatus.LockedOut:
                    return BadRequest("LockedOut does not support in api-mode");
                case SignInStatus.RequiresVerification:
                    return BadRequest("RequiresVerification does not support in api-mode");
                // ReSharper disable once RedundantCaseLabel
                case SignInStatus.Failure:
                default:
                    return BadRequest("The username or password is incorrect");
            }
        }

        // POST: /auth/logout
        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout() {
            AuthenticationManager.SignOut();
            return Ok();
        }
    }
}
