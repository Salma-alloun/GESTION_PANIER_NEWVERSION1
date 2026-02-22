using GESTION_PANIER.Data;
using GESTION_PANIER.Models;
using GESTION_PANIER.Models.GESTION_PANIER.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GESTION_PANIER.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;

        public RegisterModel(GESTION_PANIERContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string Role { get; set; } // Admin, User, Manager
        [TempData]
        public string ErrorMessage { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            if (_context.AppUsers.Any(u => u.Email == Email))
            {
                ModelState.AddModelError("", "Cet email est déjà utilisé.");
                return Page();
            }

            var user = new AppUser
            {
                Email = Email,
                Password = Password,
                Role = Role
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            // Créer le cookie après l'inscription
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToPage("/Products/PublicIndex");
        }
    }
}
