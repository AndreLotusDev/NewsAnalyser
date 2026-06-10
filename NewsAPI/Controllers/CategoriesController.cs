using Microsoft.AspNetCore.Mvc;
using NewsAPI.Repositories;

namespace NewsAPI.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryRepository _repo;

    public CategoriesController(CategoryRepository repo) => _repo = repo;

    [HttpGet]
    public IActionResult GetAll() => Ok(_repo.GetAll());
}
