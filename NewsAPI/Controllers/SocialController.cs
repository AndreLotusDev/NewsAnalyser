using Hangfire;
using Microsoft.AspNetCore.Mvc;
using NewsAPI.Jobs;
using NewsAPI.Models;
using NewsAPI.Services;

namespace NewsAPI.Controllers;

[ApiController]
[Route("api/social")]
public class SocialController : ControllerBase
{
    private readonly SocialService _service;
    private readonly IBackgroundJobClient _jobs;
    private readonly IConfiguration _config;

    public SocialController(SocialService service, IBackgroundJobClient jobs, IConfiguration config)
    {
        _service = service;
        _jobs = jobs;
        _config = config;
    }

    [HttpGet("posts")]
    public IActionResult GetPosts(
        [FromQuery] List<string>? accounts,
        [FromQuery] List<string>? tags,
        [FromQuery] List<string>? categories,
        [FromQuery] string? after,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "page must be ≥ 1 and pageSize must be between 1 and 100." });

        return Ok(_service.GetPosts(accounts, tags, categories, after, page, pageSize));
    }

    [HttpGet("health")]
    public IActionResult GetHealth() => Ok(_service.GetHealth());

    [HttpPost("accounts")]
    public IActionResult ManageAccount([FromBody] AccountActionRequest request)
    {
        var (success, error, accounts) = _service.ManageAccount(request.Handle, request.Action);
        return success ? Ok(accounts) : BadRequest(new { error });
    }

    [HttpPost("jobs/trigger")]
    public IActionResult TriggerFetch()
    {
        if (_config.GetValue<bool>("Features:SandboxMode"))
        {
            _jobs.Enqueue<FetchPostsJob>(j => j.RunSandboxAsync());
            return Ok(new { message = "SandboxMode: AI flow enqueued for a random account." });
        }

        _jobs.Enqueue<FetchPostsJob>(j => j.RunAsync());
        return Ok(new { message = "FetchPostsJob enqueued." });
    }
}
