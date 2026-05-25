namespace ARMStored.Models;

public class ReportItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "📊";
}

public class StockReportRow
{
    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal TotalValue { get; set; }
}

public class TurnoverReportRow
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long IncomingQty { get; set; }
    public long OutgoingQty { get; set; }
    public long WriteOffQty { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal Turnover { get; set; }
}

public class OperationReportRow
{
    public DateTime OperationDate { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
