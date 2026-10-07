using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Dtos;

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? Brand { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class ProductKaydetRequest
{
    [Required(ErrorMessage = "Ürün kodu zorunludur.")]
    [StringLength(50)]
    public string ProductCode { get; set; } = "";

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [StringLength(150)]
    public string ProductName { get; set; } = "";

    [StringLength(100)]
    public string? Brand { get; set; }

    [Range(0, 9999999, ErrorMessage = "Fiyat 0 veya daha büyük olmalı.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stok 0 veya daha büyük olmalı.")]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}
