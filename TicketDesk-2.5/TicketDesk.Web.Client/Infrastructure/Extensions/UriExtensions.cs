using System;
using System.Linq;
using System.Web;

namespace TicketDesk.Web.Client.Infrastructure.Extensions {
    public static class UriExtensions {
        public static Uri AddQuery(this Uri uri, string name, string value) {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }

            var parameters = HttpUtility.ParseQueryString(uri.Query);
            parameters.Set(name, value);

            var queryString = string.Join("&", parameters.AllKeys.Select(key => string.Concat(key, "=", HttpUtility.UrlEncode(parameters[key]))));
            var builder = new UriBuilder(uri) { Query = queryString };
            return builder.Uri;
        }
    }
}