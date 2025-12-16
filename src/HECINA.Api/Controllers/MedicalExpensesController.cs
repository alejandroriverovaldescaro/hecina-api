using HECINA.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HECINA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalExpensesController : ControllerBase
{
    private readonly IMedicalExpensesRepository _repository;
    private readonly ILogger<MedicalExpensesController> _logger;

    public MedicalExpensesController(
        IMedicalExpensesRepository repository,
        ILogger<MedicalExpensesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("by-person")]
    public async Task<IActionResult> GetByPerson([FromQuery] string identificationNumber, [FromQuery] string? skipToken, [FromQuery] int top = 10)
    {
        try
        {
            var expenses = await _repository.GetExpensesByPersonAsync(identificationNumber, skipToken, top);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical expenses by person");
            return StatusCode(500, "An error occurred while retrieving medical expenses by person");
        }
    }
}
