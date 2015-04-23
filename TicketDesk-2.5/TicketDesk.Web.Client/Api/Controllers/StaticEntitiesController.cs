using System;
using System.Linq;
using System.Web.Http;
using TicketDesk.Domain;
using TicketDesk.Domain.Model;
using TicketDesk.Web.Client.Api.Framework;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("api")]
    [ApiAuthorize(Roles = "TdInternalUsers")]
    public class StaticEntitiesController : ApiController {
        private static readonly object Statuses = Enum.GetValues(typeof(TicketStatus))
            .Cast<TicketStatus>()
            .Select(status => new {Value = status, Title = status.GetDescription()})
            .ToArray();
        private static readonly ApplicationSelectListSetting DefaultListSettings = new ApplicationSelectListSetting();

        // GET: api/statuses
        [HttpGet]
        [Route("statuses")]
        public IHttpActionResult GetStatuses() {
            return Ok(Statuses);
        }

        // GET: api/categories
        [HttpGet]
        [Route("categories")]
        public IHttpActionResult GetCategories() {
            return Ok(DefaultListSettings.CategoryList);
        }

        // GET: api/priorities
        [HttpGet]
        [Route("priorities")]
        public IHttpActionResult GetPriorities() {
            return Ok(DefaultListSettings.PriorityList);
        }

        // GET: api/types
        [HttpGet]
        [Route("types")]
        public IHttpActionResult GetTypes() {
            return Ok(DefaultListSettings.TicketTypesList);
        }
    }
}