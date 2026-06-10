using Microsoft.AspNetCore.Mvc;

namespace NewsAPI.Controllers;

public class NewsController : Controller
{
    public IActionResult Index() => View("~/Views/Social/Index.cshtml");
}
