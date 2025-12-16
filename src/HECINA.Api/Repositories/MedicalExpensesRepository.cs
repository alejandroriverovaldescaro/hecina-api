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

    public async Task<IEnumerable<PersonMedicalExpense>> GetExpensesForPersonAsync(string identificationNumber, string? skipToken, int top)
    {
        const string sql = @"
SELECT [groups].[PersonsIdentificationNumber]
    ,CONCAT(CAST([details].[EncounterDate] AS DATE), '-', TRIM([details].[ProviderCode]))
    ,[groups].[EncounterDate]
    ,[groups].[ProviderCode]
    ,[details].[ProviderName]
    ,[details].[CareType]
    ,[details].[ID]
    ,[details].[ServiceCode]
    ,[details].[ServiceName]
    ,[details].[Amount]
    ,[details].[MedicalReferenceNumber]
FROM [dbo].[V_MYSZV_EXPENSES] AS [details]
INNER JOIN (
    SELECT TOP (@Top)
        [EncounterDate]
        ,[ProviderCode]
        ,[PersonsIdentificationNumber]
    FROM [dbo].[V_MYSZV_EXPENSES]
    WHERE [PersonsIdentificationNumber] = @IdentificationNumber
      AND (@SkipToken IS NULL 
         OR ( [EncounterDate] <= CAST(LEFT(@SkipToken, 10) AS DATETIME) 
          AND NOT ([EncounterDate] = CAST(LEFT(@SkipToken, 10) AS DATETIME) 
                   AND [ProviderCode] <= SUBSTRING(@SkipToken, 12, 128))) )
    GROUP BY [PersonsIdentificationNumber], [EncounterDate], [ProviderCode]
    ORDER BY [EncounterDate] DESC, [ProviderCode]
) [groups] ON [groups].[PersonsIdentificationNumber] = [details].[PersonsIdentificationNumber]
           AND [groups].[EncounterDate] = [details].[EncounterDate]
           AND [groups].[ProviderCode] = [details].[ProviderCode]
ORDER BY [groups].[EncounterDate] DESC, [groups].[ProviderCode]";

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new
        {
            IdentificationNumber = identificationNumber,
            SkipToken = skipToken,
            Top = top
        };

        var expenses = await connection.QueryAsync<PersonMedicalExpense>(sql, parameters);
        return expenses;
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
