namespace GESTION_PANIER.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!; // lien avec IdentityUser
        // Ajouter la navigation vers les items
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
