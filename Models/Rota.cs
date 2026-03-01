using System.Collections.Generic;

namespace Rota2.Models
{
    public class Rota
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        public ICollection<RotaDoctor> RotaDoctors { get; set; } = new List<RotaDoctor>();
        public ICollection<RotaAdmin> RotaAdmins { get; set; } = new List<RotaAdmin>();
    }
}
