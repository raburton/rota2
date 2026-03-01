namespace Rota2.Models
{
    public class RotaAdmin
    {
        public int RotaId { get; set; }
        public Rota? Rota { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
