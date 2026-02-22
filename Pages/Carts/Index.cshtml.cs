using GESTION_PANIER.Data;
using GESTION_PANIER.Models.Session;
using GESTION_PANIER.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using projetwebtestmigration.Models;

namespace GESTION_PANIER.Pages.Carts
{
    public class IndexModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;
        private readonly CartSessionService _cartService;

        public IndexModel(GESTION_PANIERContext context, CartSessionService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public CartSession Cart { get; set; } = new CartSession();
        public List<Product> SimilarProducts { get; set; } = new List<Product>();
        
        public IList<CartItemSession> CartItems { get; set; } = new List<CartItemSession>();



        public async Task<IActionResult> OnGetAsync()
        {
            Cart = _cartService.GetCart();

            // Recharger les produits pour affichage
            foreach (var item in Cart.Items)
            {
                item.Product = await _context.Product.FindAsync(item.ProductId);
            }

            return Page();
        }


        public IActionResult OnPostAdd(int productId)
        {
            _cartService.AddToCart(productId);
            return RedirectToPage();
        }

        public IActionResult OnPostIncrease(int productId)
        {
            _cartService.IncreaseQuantity(productId);
            return RedirectToPage();
        }

        public IActionResult OnPostDecrease(int productId)
        {
            _cartService.DecreaseQuantity(productId);
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int productId)
        {
            _cartService.Delete(productId);
            return RedirectToPage();
        }

        public IActionResult OnPostSaveForLater(int productId)
        {
            _cartService.SaveForLater(productId);
            return RedirectToPage();
        }

        public IActionResult OnPostAddBackToCart(int productId)
        {
            _cartService.MoveBackToCart(productId);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompareAsync(int productId)
        {
            // 1️⃣ Recharger le panier
            Cart = _cartService.GetCart();

            // 2️⃣ Recharger les produits du panier
            foreach (var item in Cart.Items)
            {
                item.Product = await _context.Product.FindAsync(item.ProductId);
            }

            // 3️⃣ Produit cliqué
            var product = await _context.Product.FindAsync(productId);
            if (product == null)
                return Page();

            // 4️⃣ Produits similaires
            SimilarProducts = await _context.Product
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            // 5️⃣ RESTER SUR LA MÊME PAGE
            return Page();
        }

    }
}
