using System.Text.Json;
using GESTION_PANIER.Models.Session;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Web; // pour HttpUtility.UrlEncode (ou Uri.EscapeDataString si .NET Core)
namespace GESTION_PANIER.Services
{
    public class CartSessionService
    {
        private const string CartCookieKey = "CART_COOKIE";
        private readonly IHttpContextAccessor _http; 

        public CartSessionService(IHttpContextAccessor http)
        {
            _http = http;
        }

        // Récupérer le panier
        public CartSession GetCart()
        {
            var request = _http.HttpContext!.Request;

            // Identifier le panier par utilisateur authentifié ou anonyme
            string key = GetCookieKey();

            if (request.Cookies.TryGetValue(key, out string? json))
            {
                try
                {
                    return JsonSerializer.Deserialize<CartSession>(json) ?? new CartSession();
                }
                catch
                {
                    return new CartSession();
                }
            }

            return new CartSession();
        }

        // Sauvegarder le panier
        public void SaveCart(CartSession cart)
        {
            var response = _http.HttpContext!.Response;
            string key = GetCookieKey();
            var json = JsonSerializer.Serialize(cart);

            response.Cookies.Append(
                key,
                json,
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30) // ✅ CRUCIAL
                });
        }


        // Supprimer le panier
        public void Clear()
        {
            string key = GetCookieKey();
            _http.HttpContext!.Response.Cookies.Delete(key);
        }

        // Ajouter un produit
        public void AddToCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && !i.SavedForLater);
            if (item != null)
                item.Quantity++;
            else
                cart.Items.Add(new CartItemSession { ProductId = productId, Quantity = 1 });

            SaveCart(cart);
        }

        public void IncreaseQuantity(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && !i.SavedForLater);
            if (item != null) item.Quantity++;
            SaveCart(cart);
        }

        public void DecreaseQuantity(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && !i.SavedForLater);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Items.Remove(item);
            }
            SaveCart(cart);
        }

        public void Delete(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
                cart.Items.Remove(item);
            SaveCart(cart);
        }

        public void SaveForLater(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null) item.SavedForLater = true;
            SaveCart(cart);
        }

        public void MoveBackToCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null) item.SavedForLater = false;
            SaveCart(cart);
        }

        // ===== Helper pour gérer panier séparé par utilisateur =====
        private string GetCookieKey()
        {
            var user = _http.HttpContext!.User;
            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                // Encode le nom d'utilisateur pour le rendre compatible avec les cookies
                var encodedUser = Uri.EscapeDataString(user.Identity.Name!);
                return $"{CartCookieKey}_{encodedUser}";
            }
            // Utilisateur anonyme
            return CartCookieKey;
        }
    }
}
