using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace c1Soft_b4bProje.Models;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    public int FirmaId { get; set; }

    [Required]
    [StringLength(50)]
    public string ProductCode { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string ProductName { get; set; } = "";

    [StringLength(100)]
    public string? Brand { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Firma? Firma { get; set; }
}
