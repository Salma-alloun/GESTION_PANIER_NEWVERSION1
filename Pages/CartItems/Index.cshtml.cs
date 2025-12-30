using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GESTION_PANIER.Data;
using GESTION_PANIER.Models;

namespace GESTION_PANIER.Pages.CartItems
{
    public class IndexModel : PageModel
    {
        private readonly GESTION_PANIER.Data.GESTION_PANIERContext _context;

        public IndexModel(GESTION_PANIER.Data.GESTION_PANIERContext context)
        {
            _context = context;
        }

        public IList<CartItem> CartItem { get;set; } = default!;

        public async Task OnGetAsync()
        {
            CartItem = await _context.CartItem
                .Include(c => c.Cart)
                .Include(c => c.Product).ToListAsync();
        }
    }
}
