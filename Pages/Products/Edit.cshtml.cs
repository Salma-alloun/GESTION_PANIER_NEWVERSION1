using GESTION_PANIER.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Pour IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projetwebtestmigration.Models;
using System;
using System.IO; // Pour FileStream
using System.Linq;
using System.Threading.Tasks;

namespace GESTION_PANIER.Pages.Products
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;

        public EditModel(GESTION_PANIERContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

        // Propriété pour gérer l'image uploadée
        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Product
    .Include(p => p.Category)
    .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            Product = product; // maintenant Product n’est plus null


            ViewData["CategoryId"] = new SelectList(_context.Set<Category>(), "Id", "Name", Product.CategoryId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Set<Category>(), "Id", "Name", Product.CategoryId);
                return Page();
            }

            // Si l'utilisateur a uploadé une nouvelle image
            if (ImageFile != null)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine("wwwroot/images/products", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                Product.ImagePath = "/images/products/" + fileName;
            }

            _context.Attach(Product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Product.Any(e => e.Id == Product.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToPage("./Index");
        }
    }
}
