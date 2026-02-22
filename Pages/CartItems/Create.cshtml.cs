using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using GESTION_PANIER.Data;
using GESTION_PANIER.Models;

namespace GESTION_PANIER.Pages.CartItems
{
    public class CreateModel : PageModel
    {
        private readonly GESTION_PANIER.Data.GESTION_PANIERContext _context;

        public CreateModel(GESTION_PANIER.Data.GESTION_PANIERContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["CartId"] = new SelectList(_context.Cart, "Id", "Id");
        ViewData["ProductId"] = new SelectList(_context.Product, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public CartItem CartItem { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.CartItem.Add(CartItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
