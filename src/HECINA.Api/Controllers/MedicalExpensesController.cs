using HECINA.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HECINA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
 
[Authorize(AuthenticationSchemes = "Basic,Bearer")]
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

    [HttpGet("{identificationNumber}")]
    public async Task<IActionResult> GetByIdentificationNumber(string identificationNumber, [FromQuery] string? skipToken, [FromQuery] int top = 10)
    {
        try
        {
            var expenses = await _repository.GetExpensesByPersonAsync(identificationNumber, skipToken, top);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical expenses for identification number {IdentificationNumber}", identificationNumber);
            return StatusCode(500, "An error occurred while retrieving medical expenses");
        }
    }
}
