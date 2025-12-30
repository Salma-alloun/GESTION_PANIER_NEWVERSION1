using projetwebtestmigration.Models;

namespace GESTION_PANIER.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Champ pour SaveForLater
        public bool SavedForLater { get; set; } = false;

        // Navigation vers le panier
        public int CartId { get; set; }
        public Cart Cart { get; set; } = default!;

        // Navigation vers le produit
        public Product Product { get; set; } = default!;
    }
}
