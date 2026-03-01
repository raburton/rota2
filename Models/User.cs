using System.ComponentModel.DataAnnotations;

namespace Rota2.Models
{
    public enum UserRole
    {
        None = 0,
        Registrar = 1,
        Consultant = 2
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.None;
        public decimal Wte { get; set; } = 1.0m;
        public bool Active { get; set; } = true;
        public bool IsGlobalAdmin { get; set; } = false;
    }
}
