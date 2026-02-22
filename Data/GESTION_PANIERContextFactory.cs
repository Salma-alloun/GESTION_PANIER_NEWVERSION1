using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GESTION_PANIER.Data
{
    public class GESTION_PANIERContextFactory : IDesignTimeDbContextFactory<GESTION_PANIERContext>
    {
        public GESTION_PANIERContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GESTION_PANIERContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GESTION_PANIERDB;Trusted_Connection=True;");

            return new GESTION_PANIERContext(optionsBuilder.Options);
        }
    }
}
