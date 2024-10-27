using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("[controller]")]
public class TestingController : ControllerBase
{
    private readonly IPersonService _personService;

    public TestingController(IPersonService personService)
    {
        _personService = personService;
    }

    [HttpGet("person/{personName}")]
    public async Task<IActionResult> GetPerson([FromRoute] string personName)
    {
        Person? result = await _personService.FindPersonByName(personName);

        if (result == null)
            return NotFound("Person was not found with that name");

        return Ok(result);
    }
}