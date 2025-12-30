using GESTION_PANIER.Models;
using GESTION_PANIER.Models.GESTION_PANIER.Models;
using Microsoft.EntityFrameworkCore;
using projetwebtestmigration.Models; // importer les modèles
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GESTION_PANIER.Data
{
    public class GESTION_PANIERContext : DbContext
    {
        public GESTION_PANIERContext(DbContextOptions<GESTION_PANIERContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Product { get; set; } = default!;
        public DbSet<Category> Category { get; set; } = default!;
        public DbSet<GESTION_PANIER.Models.Cart> Cart { get; set; } = default!;
        public DbSet<GESTION_PANIER.Models.CartItem> CartItem { get; set; } = default!;
        public DbSet<AppUser> AppUsers { get; set; }
    }
}
