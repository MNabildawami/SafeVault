using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SafeVault.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}

public sealed record ErrorViewModel(string RequestId)
{
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
