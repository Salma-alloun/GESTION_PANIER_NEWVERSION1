using GESTION_PANIER.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Pour IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using projetwebtestmigration.Models;
using System;
using System.Collections.Generic;
using System.IO; // Pour FileStream
using System.Linq;
using System.Threading.Tasks;

namespace GESTION_PANIER.Pages.Products
{

    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;

        public CreateModel(GESTION_PANIERContext context)
        {
            _context = context;
        }

        // Affiche la page
        public IActionResult OnGet()
        {
            Product = new Product(); // <- Initialisation pour éviter le null
            ViewData["CategoryId"] = new SelectList(_context.Set<Category>(), "Id", "Name");
            return Page();
        }


        [BindProperty]
        public Product Product { get; set; } = default!;

        // Pour l'image uploadée
        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        // Gestion du POST
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Set<Category>(), "Id", "Name");
                return Page();
            }

            // Si une image est uploadée
            if (ImageFile != null)
            {
                // Nom du fichier
                var fileName = Path.GetFileName(ImageFile.FileName);

                // Dossier où stocker les images (wwwroot/images/products)
                var filePath = Path.Combine("wwwroot/images/products", fileName);

                // Sauvegarde du fichier sur le serveur
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Stocke le chemin relatif dans la propriété Product.ImagePath
                Product.ImagePath = "/images/products/" + fileName;
            }

            _context.Product.Add(Product);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
