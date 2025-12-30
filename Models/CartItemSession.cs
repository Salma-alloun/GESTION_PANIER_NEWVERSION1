using projetwebtestmigration.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class CartItemSession
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool SavedForLater { get; set; } = false;

    // Propriété temporaire pour l'affichage
    [NotMapped]
    public Product? Product { get; set; }
}
