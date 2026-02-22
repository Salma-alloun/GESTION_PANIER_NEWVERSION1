using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using GESTION_PANIER.Data;
using Microsoft.EntityFrameworkCore;

namespace GESTION_PANIER.Pages
{
    public class LoginModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;

        public LoginModel(GESTION_PANIERContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string Role { get; set; }  // Admin, User, etc.

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // Vérifier si l'utilisateur existe dans AppUsers
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u =>
                    u.Email == Email &&
                    u.Password == Password &&
                    u.Role == Role);

            if (user == null)
            {
                ModelState.AddModelError("", "Email, mot de passe ou rôle incorrect.");
                return Page();
            }

            // ? Créer les claims pour cookie + rôle
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)  // Important: ClaimTypes.Role
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,    // User.Identity.Name
                ClaimTypes.Role     // Autorisation basée sur les rôles
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,       // cookie persistant
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                }
            );

            // ?? Redirection selon le rôle
            if (user.Role == "Admin")
            {
                return RedirectToPage("/Products/Index"); // Page admin
            }
            else
            {
                return RedirectToPage("/Products/PublicIndex"); // Page publique pour les autres
            }
        }
    }
}
