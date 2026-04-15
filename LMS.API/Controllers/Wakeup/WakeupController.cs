using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Wakeup;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class WakeupController : ControllerBase
{
    [HttpGet]
    public IActionResult Awake()
    {
        return Ok("API is awake!");
    }
}