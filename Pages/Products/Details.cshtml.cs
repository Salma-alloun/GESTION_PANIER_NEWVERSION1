using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GESTION_PANIER.Data;
using projetwebtestmigration.Models;

namespace GESTION_PANIER.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;

        public DetailsModel(GESTION_PANIERContext context)
        {
            _context = context;
        }

        // Produit à afficher (nullable pour gérer le null de FirstOrDefaultAsync)
        public Product? Product { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Charger le produit avec sa catégorie
            Product = await _context.Product
                .Include(p => p.Category) // inclure la catégorie pour accéder à Category.Name
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Product == null)
            {
                return NotFound();
            }

            // Image par défaut si ImagePath est vide
            if (string.IsNullOrEmpty(Product.ImagePath))
            {
                Product.ImagePath = "/images/products/default.jpeg";
            }

            return Page();
        }
    }
}
