using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestingController : ControllerBase
{
    private readonly IAService _aService;
    private readonly IBService _bService;

    public TestingController(IAService aService, IBService bService)
    {
        _aService = aService;
        _bService = bService;
    }

    [HttpGet]
    public async Task<IActionResult> TreeTesting()
    {
        _aService.ACall1().Wait();
        _bService.BCall1();

        Task.WaitAll(_aService.ACall2(), _bService.BCall2());

        return Ok("Done");
    }
}