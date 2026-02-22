using GESTION_PANIER.Data;
using GESTION_PANIER.Models.Session;
using GESTION_PANIER.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using projetwebtestmigration.Models;
using Microsoft.Extensions.Caching.Memory;




namespace GESTION_PANIER.Pages.Products
{
    public class PublicIndexModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;
        private readonly CartSessionService _cartService;
        private readonly IMemoryCache _cache;

        public PublicIndexModel(
            GESTION_PANIERContext context,
            CartSessionService cartService,
            IMemoryCache cache)
        {
            _context = context;
            _cartService = cartService;
            _cache = cache;
        }


        public IList<Product> Product { get; set; } = new List<Product>();
        public int CartCount { get; set; }

        public async Task OnGetAsync(string? search)
        {
            string cacheKey = string.IsNullOrWhiteSpace(search)
                ? "products_all"
                : $"products_search_{search.ToLower()}";

            if (!_cache.TryGetValue(cacheKey, out List<Product> products))
            {
                Console.WriteLine("? Produits chargés depuis la BASE DE DONNÉES");
                IQueryable<Product> query = _context.Product
                    .Include(p => p.Category);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search));

                    // ?? Incrémenter SearchCount
                    var searchedProducts = await _context.Product
                        .Where(p => p.Name.Contains(search))
                        .ToListAsync();

                    foreach (var p in searchedProducts)
                    {
                        p.SearchCount++;
                    }

                    await _context.SaveChangesAsync();
                }
                


                products = await query.ToListAsync();

                // ?? Mise en cache (10 minutes)
                _cache.Set(
                    cacheKey,
                    products,
                    TimeSpan.FromMinutes(10)
                );
            }
            else
            {
                // ?? DANS LE CACHE
                Console.WriteLine("? Produits chargés depuis le CACHE (Redis / MemoryCache)");
            }

            Product = products;

            var cart = _cartService.GetCart();
            CartCount = cart.Items
                .Where(i => !i.SavedForLater)
                .Sum(i => i.Quantity);
        }


        public IActionResult OnPostAdd(int productId)
        {
            _cartService.AddToCart(productId);

            // Redirection vers le panier
            return RedirectToPage("/Carts/Index");
        }
    }
}
