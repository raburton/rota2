using System;

namespace Rota2.Models
{
    public class ShiftAssignment
    {
        public int Id { get; set; }
        public int RotaId { get; set; }
        public Rota? Rota { get; set; }
        public int ShiftId { get; set; }
        public Shift? Shift { get; set; }
        // Date portion only (time is determined by Shift)
        public DateTime Date { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
