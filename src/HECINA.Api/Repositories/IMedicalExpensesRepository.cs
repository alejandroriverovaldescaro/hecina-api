using HECINA.Api.Domain;

namespace HECINA.Api.Repositories;

public interface IMedicalExpensesRepository
{
    Task<IEnumerable<MedicalExpense>> GetAllAsync();
    Task<MedicalExpense?> GetByIdAsync(int id);
}
