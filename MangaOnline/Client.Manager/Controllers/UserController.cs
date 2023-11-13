using Client.Manager.Controllers;
using Microsoft.AspNetCore.Mvc;
using Service.MangaOnline.ResponseModels;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientWebMVC.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient client = null;
        private string UserUrl = "";

        public UserController(ILogger<HomeController> logger)
        {
            client = new HttpClient();
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Add(contentType);
            UserUrl = "http://localhost:5098/api/";
            _logger = logger;
        }

        public async Task<IActionResult> ListUser(string? isActive, string? role, int index)
        {
            isActive = string.IsNullOrEmpty(isActive) ? "Tất cả" : isActive;
            role = string.IsNullOrEmpty(role) ? "Tất cả" : role;
            HttpResponseMessage response =
                await client.GetAsync(
                    UserUrl
                    + $"User/GetAll?isActive={isActive}&role={role}&index={index}");
            string responseBody = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions()
            { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<DataListUserResponse>(responseBody, option);
            ViewData["ListUser"] = responseData!.data;
            ViewData["isActive"] = isActive;
            ViewData["role"] = role;
            ViewData["index"] = index == 0 ? 1 : index;
            ViewData["LastPage"] = responseData.lastPage;
            return View("/Views/Manager/ListUserManager.cshtml");
        }
    }
}

