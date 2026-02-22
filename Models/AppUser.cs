namespace GESTION_PANIER.Models
{
    using System.ComponentModel.DataAnnotations;

    namespace GESTION_PANIER.Models
    {
        public class AppUser
        {
            public int Id { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

            [Required]
            public string Role { get; set; } // Exemple : "Admin", "User", "Manager"
        }
    }

}
