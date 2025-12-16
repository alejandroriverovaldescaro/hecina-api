namespace HECINA.Api.Domain;

public class MedicalExpense
{
    public int Id { get; set; }
    public string? PatientName { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Description { get; set; }
    public List<MedicalExpenseDetail> Details { get; set; } = new();
}
