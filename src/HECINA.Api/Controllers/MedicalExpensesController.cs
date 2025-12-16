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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var expenses = await _repository.GetAllAsync();
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical expenses");
            return StatusCode(500, "An error occurred while retrieving medical expenses");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
            {
                return NotFound();
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical expense {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the medical expense");
        }
    }

    [HttpGet("person/{identificationNumber}")]
    public async Task<IActionResult> GetExpensesForPerson(
        string identificationNumber,
        [FromQuery] string? skipToken = null,
        [FromQuery] int top = 5)
    {
        try
        {
            var expenses = await _repository.GetExpensesForPersonAsync(identificationNumber, skipToken, top);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical expenses for person {IdentificationNumber}", identificationNumber);
            return StatusCode(500, "An error occurred while retrieving medical expenses for the person");
        }
    }
}
