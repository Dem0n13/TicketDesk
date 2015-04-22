using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using TicketDesk.Domain;
using TicketDesk.Web.Client.Api.Dtos;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("api/tickets")]
    [Authorize(Roles = "TdInternalUsers")]
    public class TicketsController : ApiController {
        private readonly TdDomainContext _domainContext;

        public TicketsController(TdDomainContext domainContext) {
            _domainContext = domainContext;
        }

        // GET: api/tickets
        [HttpGet]
        [EnableQuery]
        public IQueryable<TicketDto> GetAll() {
            return _domainContext.Tickets.ToArray()
                .Select(ticket => new TicketDto(ticket))
                .AsQueryable();
        }

        // GET: api/tickets/5
        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> Get(int id) {
            var ticket = await _domainContext.Tickets.FindAsync(id);
            if(ticket == null) {
                return NotFound();
            }
            return Ok(new TicketDto(ticket));
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<IHttpActionResult> Create(TicketDto dto) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var ticket = dto.ToTicket();
            _domainContext.Tickets.Add(ticket);
            await _domainContext.SaveChangesAsync();
            return Ok(new TicketDto(ticket));
        }

        // PUT: api/tickets/5
        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> Update(int id, TicketDto dto) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            if(id != dto.Id) {
                return BadRequest();
            }
            var ticket = await _domainContext.Tickets.FindAsync(id);
            if(ticket == null) {
                return NotFound();
            }

            dto.MergeTo(ticket);
            await _domainContext.SaveChangesAsync();
            return Ok(new TicketDto(ticket));
        }

        // DELETE: api/tickets/5
        [HttpDelete]
        [Route("{id}")]
        public async Task<IHttpActionResult> Delete(int id) {
            var ticket = await _domainContext.Tickets.FindAsync(id);
            if(ticket == null) {
                return NotFound();
            }

            _domainContext.Tickets.Remove(ticket);
            await _domainContext.SaveChangesAsync();
            return Ok();
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                _domainContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}