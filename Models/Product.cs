using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetwebtestmigration.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(0, 10000)]
    public int Quantity { get; set; }
    // ✅ NOUVEL ATTRIBUT
    [Required(ErrorMessage = "La description est obligatoire")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    // ✅ Clé étrangère vers Category
    [Display(Name = "Catégorie")]
    public int CategoryId { get; set; }

    // ✅ Propriété de navigation (JOINTURE)
    public Category? Category { get; set; }

    // ✅ Image du produit
    [Display(Name = "Image")]
    public string? ImagePath { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date de sortie")]
    public DateTime ReleaseDate { get; set; }
    // Ajouter cette propriété pour compter les recherches
    public int SearchCount { get; set; } = 0;
}
