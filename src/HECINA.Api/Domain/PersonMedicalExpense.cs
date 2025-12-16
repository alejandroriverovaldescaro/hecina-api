namespace HECINA.Api.Domain;

public class PersonMedicalExpense
{
    public string? PersonsIdentificationNumber { get; set; }
    public string? NextPageToken { get; set; }
    public DateTime EncounterDate { get; set; }
    public string? ProviderCode { get; set; }
    public string? ProviderName { get; set; }
    public string? CareType { get; set; }
    public int ID { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public decimal Amount { get; set; }
    public string? MedicalReferenceNumber { get; set; }
}
