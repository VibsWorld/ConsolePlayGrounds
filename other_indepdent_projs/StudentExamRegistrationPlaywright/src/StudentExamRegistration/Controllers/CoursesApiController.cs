using Microsoft.AspNetCore.Mvc;
namespace StudentExamRegistration.Controllers;
[ApiController]
[Route("api/courses")]
public class CoursesApiController:ControllerBase{
[HttpGet] public IActionResult Get()=>Ok(new[]{"Math","Physics","Chemistry"});
}