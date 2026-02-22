using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace projetwebtestmigration.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation : une catégorie contient plusieurs produits
    public ICollection<Product> Products { get; set; } = new List<Product>();
}