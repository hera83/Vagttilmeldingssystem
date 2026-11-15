using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.ViewModels.Home;

namespace web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorHomeViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult Status(int id) => View(new StatusHomeViewModel { StatusCode = id });
}
