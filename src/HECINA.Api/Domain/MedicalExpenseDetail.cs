namespace HECINA.Api.Domain;

public class MedicalExpenseDetail
{
    public Guid Id { get; set; } // DetailId: [ID]
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MedicalReferenceNumber { get; set; } = string.Empty;
}
