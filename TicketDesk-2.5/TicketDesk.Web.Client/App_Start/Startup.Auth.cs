﻿// TicketDesk - Attribution notice
// Contributor(s):
//
//      Stephen Redd (stephen@reddnet.net, http://www.reddnet.net)
//
// This file is distributed under the terms of the Microsoft Public 
// License (Ms-PL). See http://opensource.org/licenses/MS-PL
// for the complete terms of use. 
//
// For any distribution that contains code from this file, this notice of 
// attribution must remain intact, and a copy of the license must be 
// provided to the recipient.

using System;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Newtonsoft.Json.Linq;
using Owin;
using SimpleInjector;
using TicketDesk.Web.Identity;
using TicketDesk.Web.Identity.Migrations;
using TicketDesk.Web.Identity.Model;

namespace TicketDesk.Web.Client
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app, Container container)
        {
            //app.createperowincontext stuff is now done by simpleinjector instead.
            //  It is worth noting that CreatePerOwinContext has one fatal down-side... it WILL 
            //  create an instance for each owin context, even if nothing ever attempts to get
            //  that instance... in TD's case, this causes things like the user manager to try
            //  to instantiate, even if we're in first-run-setup mode and don't have a database
            //  available yet. 

                //app.CreatePerOwinContext(TicketDeskIdentityContext.Create);
                //app.CreatePerOwinContext<TicketDeskUserManager>(TicketDeskUserManager.Create);
                //app.CreatePerOwinContext<TicketDeskRoleManager>(TicketDeskRoleManager.Create);
                //app.CreatePerOwinContext<TicketDeskSignInManager>(TicketDeskSignInManager.Create);
                //app.CreatePerOwinContext(container.GetInstance<TicketDeskUserManager>);
            
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/account/login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<TicketDeskUserManager, TicketDeskUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });       
     
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});

            ConfigureGoogleAuth(app);

            if (DatabaseConfig.IsFirstRunDemoRefreshEnabled())
            {
                DemoIdentityDataManager.SetupDemoIdentityData(container.GetInstance<TdIdentityContext>());
            }

        }

        public const string TokenClaimName = "access_token";

        private void ConfigureGoogleAuth(IAppBuilder app) {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\oauth-google.json");
            var configJson = File.ReadAllText(configPath);
            var config = JObject.Parse(configJson);
            var clientId = config["web"]["client_id"].ToString();
            var clientSecret = config["web"]["client_secret"].ToString();

            var options = new GoogleOAuth2AuthenticationOptions {
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
            app.UseGoogleAuthentication(options);
        }
    }
}