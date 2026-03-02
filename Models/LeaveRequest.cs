using System.ComponentModel.DataAnnotations;

namespace Rota2.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Notes { get; set; }
    }
}
