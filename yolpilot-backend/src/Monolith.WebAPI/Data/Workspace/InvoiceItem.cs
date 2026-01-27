namespace Monolith.WebAPI.Data.Workspace;

public class InvoiceItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Total => Amount * Quantity;
}