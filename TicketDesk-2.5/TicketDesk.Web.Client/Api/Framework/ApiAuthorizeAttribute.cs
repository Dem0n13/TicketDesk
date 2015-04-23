using System.Web.Http;
using System.Web.Http.Controllers;

namespace TicketDesk.Web.Client.Api.Framework {
    public class ApiAuthorizeAttribute : AuthorizeAttribute {
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext) {
            base.HandleUnauthorizedRequest(actionContext);
            actionContext.Response.Headers.Add(Auth.SuppressRedirectHeader, true.ToString());
        }
    }
}