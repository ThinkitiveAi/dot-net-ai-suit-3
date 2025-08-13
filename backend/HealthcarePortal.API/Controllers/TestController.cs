using Microsoft.AspNetCore.Mvc;

namespace HealthcarePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Healthcare Portal API is running!",
                timestamp = DateTime.UtcNow,
                swagger = "Visit /swagger for API documentation"
            });
        }

        [HttpGet("swagger-test")]
        public IActionResult SwaggerTest()
        {
            return Ok(new
            {
                message = "If you can see this, the API is accessible from your network",
                swagger_url = "/swagger/index.html",
                api_docs = "/swagger/v1/swagger.json"
            });
        }
    }
}
