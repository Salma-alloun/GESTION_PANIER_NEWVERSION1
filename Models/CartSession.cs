namespace GESTION_PANIER.Models.Session
{
    public class CartSession
    {
        public List<CartItemSession> Items { get; set; } = new List<CartItemSession>();

    }
}
