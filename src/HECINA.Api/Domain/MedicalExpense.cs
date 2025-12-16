namespace HECINA.Api.Domain;

public class MedicalExpense
{
    public string Id { get; set; } = string.Empty; // ExpenseId: CONCAT(CAST([EncounterDate] AS DATE), '-', TRIM([ProviderCode]))
    public string PersonsIdentificationNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } // EncounterDate
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string CareType { get; set; } = string.Empty;
    public List<MedicalExpenseDetail> Details { get; set; } = new();
}
