using System;

namespace Rota2.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public int RotaId { get; set; }
        public Rota? Rota { get; set; }
    }
}
