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

    public async Task<IEnumerable<MedicalExpense>> GetExpensesByPersonAsync(
        string identificationNumber, 
        string? skipToken, 
        int top)
    {
        const string sql = @"
            SELECT
                 [groups].[PersonsIdentificationNumber]
                ,CONCAT(CAST([details].[EncounterDate] AS DATE), '-', TRIM([details].[ProviderCode])) AS ExpenseId
                ,[groups].[EncounterDate]
                ,[groups].[ProviderCode]
                ,[details].[ProviderName]
                ,[details].[CareType]
                ,[details].[ID] AS DetailId
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
                WHERE 
                    [PersonsIdentificationNumber] = @IdentificationNumber
                    AND (@SkipToken IS NULL 
                        OR ( [EncounterDate] <= CAST(LEFT(@SkipToken, 10) AS DATETIME) 
                            AND NOT ([EncounterDate] = CAST(LEFT(@SkipToken, 10) AS DATETIME) AND [ProviderCode] <= SUBSTRING(@SkipToken, 12, 128))) )
                GROUP BY [PersonsIdentificationNumber], [EncounterDate], [ProviderCode]
                ORDER BY [EncounterDate] DESC, [ProviderCode]
            ) [groups] ON 
                [groups].[PersonsIdentificationNumber] = [details].[PersonsIdentificationNumber] 
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

        var results = await connection.QueryAsync<dynamic>(sql, parameters);

        var grouped = results.GroupBy(r => (string)r.ExpenseId).Select(g =>
        {
            var first = g.First();
            var expense = new MedicalExpense
            {
                Id = first.ExpenseId,
                PersonsIdentificationNumber = first.PersonsIdentificationNumber,
                ExpenseDate = first.EncounterDate,
                ProviderCode = first.ProviderCode,
                ProviderName = first.ProviderName,
                CareType = first.CareType,
                Details = g.Select(d => new MedicalExpenseDetail
                {
                    Id = d.DetailId,
                    ServiceCode = d.ServiceCode,
                    ServiceName = d.ServiceName,
                    Amount = d.Amount,
                    MedicalReferenceNumber = d.MedicalReferenceNumber
                }).ToList()
            };
            return expense;
        });
        return grouped;
    }

    public async Task<IEnumerable<MedicalExpense>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                CONCAT(CAST([EncounterDate] AS DATE), '-', TRIM([ProviderCode])) AS ExpenseId,
                [PersonsIdentificationNumber],
                [EncounterDate],
                [ProviderCode],
                [ProviderName],
                [CareType],
                [ID] AS DetailId,
                [ServiceCode],
                [ServiceName],
                [Amount],
                [MedicalReferenceNumber]
            FROM [dbo].[V_MYSZV_EXPENSES]
            ORDER BY [EncounterDate] DESC, [ProviderCode]";

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<dynamic>(sql);

        var grouped = results.GroupBy(r => (string)r.ExpenseId).Select(g =>
        {
            var first = g.First();
            var expense = new MedicalExpense
            {
                Id = first.ExpenseId,
                PersonsIdentificationNumber = first.PersonsIdentificationNumber,
                ExpenseDate = first.EncounterDate,
                ProviderCode = first.ProviderCode,
                ProviderName = first.ProviderName,
                CareType = first.CareType,
                Details = g.Select(d => new MedicalExpenseDetail
                {
                    Id = d.DetailId,
                    ServiceCode = d.ServiceCode,
                    ServiceName = d.ServiceName,
                    Amount = d.Amount,
                    MedicalReferenceNumber = d.MedicalReferenceNumber
                }).ToList()
            };
            return expense;
        });
        return grouped;
    }

    public async Task<MedicalExpense?> GetByIdAsync(string expenseId)
    {
        const string sql = @"
            SELECT 
                CONCAT(CAST([EncounterDate] AS DATE), '-', TRIM([ProviderCode])) AS ExpenseId,
                [PersonsIdentificationNumber],
                [EncounterDate],
                [ProviderCode],
                [ProviderName],
                [CareType],
                [ID] AS DetailId,
                [ServiceCode],
                [ServiceName],
                [Amount],
                [MedicalReferenceNumber]
            FROM [dbo].[V_MYSZV_EXPENSES]
            WHERE CONCAT(CAST([EncounterDate] AS DATE), '-', TRIM([ProviderCode])) = @ExpenseId";

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<dynamic>(sql, new { ExpenseId = expenseId });
        var group = results.GroupBy(r => (string)r.ExpenseId).FirstOrDefault();
        if (group == null) return null;
        var first = group.First();
        var expense = new MedicalExpense
        {
            Id = first.ExpenseId,
            PersonsIdentificationNumber = first.PersonsIdentificationNumber,
            ExpenseDate = first.EncounterDate,
            ProviderCode = first.ProviderCode,
            ProviderName = first.ProviderName,
            CareType = first.CareType,
            Details = group.Select(d => new MedicalExpenseDetail
            {
                Id = d.DetailId,
                ServiceCode = d.ServiceCode,
                ServiceName = d.ServiceName,
                Amount = d.Amount,
                MedicalReferenceNumber = d.MedicalReferenceNumber
            }).ToList()
        };
        return expense;
    }
}
