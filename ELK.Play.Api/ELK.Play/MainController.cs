using Microsoft.AspNetCore.Mvc;

namespace ELK.Play;

[ApiController]
[Route("[controller]")]
public class MainController : ControllerBase
{
    private readonly ILogger<MainController> _logger;

    public MainController(ILogger<MainController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<string> Get()
    {
        _logger.LogInformation($"{nameof(MainController)} - Get - {DateTime.UtcNow}");

        return Enumerable.Empty<string>();
    }
}