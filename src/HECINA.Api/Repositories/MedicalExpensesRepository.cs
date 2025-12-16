using System.Data;
using Dapper;
using HECINA.Api.Domain;
using HECINA.Api.Infrastructure;

namespace HECINA.Api.Repositories;

public class MedicalExpensesRepository : IMedicalExpensesRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MedicalExpensesRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<MedicalExpense>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                e.Id, e.PatientName, e.ExpenseDate, e.TotalAmount, e.Description,
                d.Id, d.MedicalExpenseId, d.ItemDescription, d.Amount, d.Quantity
            FROM MedicalExpenses e
            LEFT JOIN MedicalExpenseDetails d ON e.Id = d.MedicalExpenseId
            ORDER BY e.Id";

        using var connection = _connectionFactory.CreateConnection();
        var expenseDictionary = await MapExpensesWithDetails(connection, sql, null);
        return expenseDictionary.Values;
    }

    public async Task<MedicalExpense?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                e.Id, e.PatientName, e.ExpenseDate, e.TotalAmount, e.Description,
                d.Id, d.MedicalExpenseId, d.ItemDescription, d.Amount, d.Quantity
            FROM MedicalExpenses e
            LEFT JOIN MedicalExpenseDetails d ON e.Id = d.MedicalExpenseId
            WHERE e.Id = @Id
            ORDER BY e.Id";

        using var connection = _connectionFactory.CreateConnection();
        var expenseDictionary = await MapExpensesWithDetails(connection, sql, new { Id = id });
        return expenseDictionary.Values.FirstOrDefault();
    }

    private async Task<Dictionary<int, MedicalExpense>> MapExpensesWithDetails(
        IDbConnection connection, 
        string sql, 
        object? parameters)
    {
        var expenseDictionary = new Dictionary<int, MedicalExpense>();
        
        await connection.QueryAsync<MedicalExpense, MedicalExpenseDetail, MedicalExpense>(
            sql,
            (expense, detail) =>
            {
                if (!expenseDictionary.TryGetValue(expense.Id, out var expenseEntry))
                {
                    expenseEntry = expense;
                    expenseEntry.Details = new List<MedicalExpenseDetail>();
                    expenseDictionary.Add(expense.Id, expenseEntry);
                }

                if (detail != null)
                {
                    expenseEntry.Details.Add(detail);
                }

                return expenseEntry;
            },
            parameters,
            splitOn: "Id"
        );

        return expenseDictionary;
    }
}
