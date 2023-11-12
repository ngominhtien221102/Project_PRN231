using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Client.Manager.Models;
using Microsoft.Extensions.Logging;
using Service.MangaOnline.Models;
using Service.MangaOnline.ResponseModels;

namespace Client.Manager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient client = null;
    private string ServiceMangaUrl = "";
    
    public HomeController(ILogger<HomeController> logger)
    {
        client = new HttpClient();
        var contentType = new MediaTypeWithQualityHeaderValue("application/json");
        client.DefaultRequestHeaders.Accept.Add(contentType);
        ServiceMangaUrl = "http://localhost:5098/";
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await client.GetAsync(ServiceMangaUrl+"Manga/HomeManga");
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions()
                { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<HomeResponse>(responseBody, option);
            ViewBag.TopMonthManga = list;
        }
        else
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}