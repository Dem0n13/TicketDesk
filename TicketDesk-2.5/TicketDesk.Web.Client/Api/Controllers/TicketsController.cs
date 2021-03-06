﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using TicketDesk.Domain;
using TicketDesk.Web.Client.Api.Dtos;
using TicketDesk.Web.Client.Api.Framework;

namespace TicketDesk.Web.Client.Api.Controllers {
    [RoutePrefix("api/tickets")]
    [ApiAuthorize(Roles = "TdInternalUsers")]
    public class TicketsController : ApiController {
        private readonly TdDomainContext _domainContext;
        private readonly Func<string, string> _getUserDisplayName;

        public TicketsController(TdDomainContext domainContext) {
            _domainContext = domainContext;
            _getUserDisplayName = domainContext.SecurityProvider.GetUserDisplayName;
        }

        // GET: api/tickets
        [HttpGet]
        [Route]
        [EnableQuery]
        public IQueryable<TicketDto> GetAll() {
            return _domainContext.Tickets.ToArray()
                .Select(ticket => new TicketDto(ticket, _getUserDisplayName))
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
            return Ok(new TicketDto(ticket, _getUserDisplayName));
        }

        // POST: api/tickets
        [HttpPost]
        [Route]
        public async Task<IHttpActionResult> Create(TicketDto dto) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var ticket = dto.ToTicket();
            _domainContext.Tickets.Add(ticket);
            await _domainContext.SaveChangesAsync();
            return Ok(new TicketDto(ticket, _getUserDisplayName));
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
            return Ok(new TicketDto(ticket, _getUserDisplayName));
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
