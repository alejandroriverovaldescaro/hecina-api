namespace HECINA.Api.Domain;

public class MedicalExpenseDetail
{
    public int Id { get; set; }
    public int MedicalExpenseId { get; set; }
    public string? ItemDescription { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}
