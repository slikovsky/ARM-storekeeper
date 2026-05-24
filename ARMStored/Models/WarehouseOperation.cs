namespace ARMStored.Models;

public class WarehouseOperation
{
    public int Id { get; set; }
    public OperationType Type { get; set; }
    public string TypeName => Type switch
    {
        OperationType.Incoming => "📥 Приход",
        OperationType.Outgoing => "📤 Расход",
        OperationType.Transfer => "🔄 Перемещение",
        OperationType.WriteOff => "❌ Списание",
        _ => "Неизвестно"
    };
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public string? Reason { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime OperationDate { get; set; }
    public string? Notes { get; set; }
}