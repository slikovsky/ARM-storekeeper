namespace ARMStored.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; } = 10;
    public string Unit { get; set; } = "шт";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public string Status => Quantity <= MinQuantity ? "⚠️ Низкий запас" : "✅ В наличии";
    public string StatusColor => Quantity <= MinQuantity ? "#FFFF6B6B" : "#FF51CF66";
}