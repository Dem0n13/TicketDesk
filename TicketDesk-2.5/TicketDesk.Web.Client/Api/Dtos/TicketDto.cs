using System;
using System.ComponentModel.DataAnnotations;
using TicketDesk.Domain.Model;

namespace TicketDesk.Web.Client.Api.Dtos {
    public class TicketDto {
        public TicketDto() {
        }

        public TicketDto(Ticket ticket, Func<string, string> getUserDisplayName) {
            Id = ticket.TicketId;
            Type = ticket.TicketType;
            Category = ticket.Category;
            Title = ticket.Title;
            Details = ticket.Details;
            CreatedDate = ticket.CreatedDate;
            Owner = getUserDisplayName(ticket.Owner);
            Status = ticket.TicketStatus;
        }

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [Required]
        public string Details { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public string Owner { get; set; }

        [Required]
        public TicketStatus Status { get; set; }

        public void MergeTo(Ticket ticket) {
            ticket.TicketId = Id;
            ticket.TicketType = Type;
            ticket.Category = Category;
            ticket.Title = Title;
            ticket.Details = Details;
            ticket.TicketStatus = Status;
        }

        public Ticket ToTicket() {
            var ticket = new Ticket();
            MergeTo(ticket);
            return ticket;
        }
    }
}