using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Newtonsoft.Json.Linq;

namespace TicketDesk.Web.Client.Api.Framework {
    public static class Auth {
        public const string AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
        public const string CookieName = CookieAuthenticationDefaults.CookiePrefix + AuthenticationType;
        public const string TokenClaimName = "access_token";

        public static GoogleOAuth2AuthenticationOptions ReadGoogleOAuthOptions(string configPath) {
            var configJson = File.ReadAllText(configPath);
            var config = JObject.Parse(configJson);
            var clientId = config["web"]["client_id"].ToString();
            var clientSecret = config["web"]["client_secret"].ToString();
            return new GoogleOAuth2AuthenticationOptions {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Provider = new GoogleOAuth2AuthenticationProvider {
                    OnAuthenticated = context => {
                        var accessToken = context.AccessToken;
                        context.Identity.AddClaim(new Claim(TokenClaimName, accessToken));
                        return Task.FromResult(0);
                    }
                }
            };
        }
    }
}