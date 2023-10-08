using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Service.MangaOnline.ResponseModels;

namespace Client.Manager.Controllers;

public class PublicController : Controller
{
    private readonly HttpClient client = null;
    private string ServiceMangaUrl = "";

    public IActionResult Search()
        {
            return View();
        }

    public PublicController()
    {
        client = new HttpClient();
        var contentType = new MediaTypeWithQualityHeaderValue("application/json");
        client.DefaultRequestHeaders.Accept.Add(contentType);
        ServiceMangaUrl = "http://localhost:5098/";
    }
    public IActionResult DetailChapter(Guid id)
    {
        return View("DetailChapter");
    }
    
    public async Task<IActionResult> DetailManga(Guid id)
    {
        HttpResponseMessage response = await client.GetAsync(ServiceMangaUrl + "manga/GetManga?id="+ id);
        string responseBody = await response.Content.ReadAsStringAsync();
        var option = new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true };
        if (response.IsSuccessStatusCode)
        {
            var dataMangaResponse = JsonSerializer.Deserialize<DataMangaResponse>(responseBody, option);
            ViewData["manga"] = dataMangaResponse!.data;
            return View("DetailManga");
        }

        return View("Error");
    }
}
