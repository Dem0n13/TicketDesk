using System;
using System.Linq;
using System.Web.Http;
using TicketDesk.Domain;
using TicketDesk.Domain.Model;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("api/statuses")]
    [Authorize(Roles = "TdInternalUsers")]
    public class StatusesController : ApiController {
        private static readonly object Statuses = Enum.GetValues(typeof(TicketStatus))
            .Cast<TicketStatus>()
            .Select(status => new {Value = status, Title = status.GetDescription()})
            .ToArray();

        [HttpGet]
        public IHttpActionResult GetAll() {
            return Ok(Statuses);
        }
    }
}