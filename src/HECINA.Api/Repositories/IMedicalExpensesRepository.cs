using HECINA.Api.Domain;

namespace HECINA.Api.Repositories;

public interface IMedicalExpensesRepository
{
    Task<IEnumerable<MedicalExpense>> GetExpensesByPersonAsync(string identificationNumber, string? skipToken, int top);
}
