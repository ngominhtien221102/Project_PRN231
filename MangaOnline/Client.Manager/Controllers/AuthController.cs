using ClientWebMVC.Extentions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.MangaOnline.Commons;
using Service.MangaOnline.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebAPI.RequestModels;

namespace Client.Manager.Controllers;

public class AuthController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient client = null;
    private string ServiceMangaUrl = "";
    [BindProperty] public String gmailUserUpdate { get; set; }
    [BindProperty] public String hashForm { get; set; }
    [BindProperty] public String fromForm { get; set; }
    [BindProperty] public String toForm { get; set; }
    [BindProperty] public String valueForm { get; set; }

    public AuthController(ILogger<HomeController> logger)
    {
        client = new HttpClient();
        var contentType = new MediaTypeWithQualityHeaderValue("application/json");
        client.DefaultRequestHeaders.Accept.Add(contentType);
        ServiceMangaUrl = "http://localhost:5098/";
        _logger = logger;
    }

    [HttpGet]
    public IActionResult AuthLogin(String? noti) 
    {
        if (noti is null)
        {
            return View("AuthLogin");
        }

        ViewData["notice"] = "abc";
        return View("AuthLogin");

    }
    [HttpGet]
    public IActionResult AuthChangePassword()
    {
        return View("AuthChangePassword");
    }
    [HttpGet]
    public IActionResult AuthRegistration()
    {
        return View("AuthRegistration");
    }
    [HttpGet]
    public IActionResult AuthVerifyEmail()
    {
        return View("AuthVerifyEmail");
    }

    [HttpGet]
    public IActionResult HistoryAccount()
    {
        return View("HistoryAccount");
    }

    [HttpGet]
    public IActionResult UpdateAccount(String? noti)
    {
        if (noti is null)    
        {
            return View("UpdateAccount");
        }

        ViewData["notice"] = noti;
        return View("UpdateAccount");
    }

    [HttpGet]
    public IActionResult GetResultBanking(string? vnp_TxnRef, int? vnp_Amount)
    {
        var userId = HttpContext.Request.Cookies["user_id"];

        ResultBanking result = new ResultBanking
        {
            userId = userId,
            vnp_Amount = (int)vnp_Amount,
            vnp_TxnRef = vnp_TxnRef
        }; 
        HttpClient client = new HttpClient();
        var contentType = new MediaTypeWithQualityHeaderValue("application/json");
        client.DefaultRequestHeaders.Accept.Add(contentType);

        string url = "http://localhost:5098/Auth/UpdateAccountVip";
        string data = System.Text.Json.JsonSerializer.Serialize(result);
        var httpContent = new StringContent(data, Encoding.UTF8, "application/json");
        HttpResponseMessage response = client.PostAsync(url, httpContent).GetAwaiter().GetResult();
        if(response.IsSuccessStatusCode)
        {
             return Redirect("/Auth/AuthLogin");
        }    
        return View("HistoryAccount");
    }




    [HttpPost]
    public async Task<IActionResult> PostUpdateAccountAsync(string userId, string selectedPackage)
    {

        try
        {
            var config = new ConfigurationBuilder().AddJsonFile("vnpayConfig.json").Build();
            var vnp_Returnurl = config["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            var vnp_Url = config["vnp_Url"];//URL thanh toan cua VNPAY 
            var vnp_TmnCode = config["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            var vnp_HashSecret = config["vnp_HashSecret"];
            var amount = 0;
            if(selectedPackage == "1 tuần")
            {
                amount = 50000;
            }
            else
            {
                amount = 100000;
            }

            //Get payment input
            var order = new OrderInfo
            {
                OrderId = DateTime.Now.Ticks, // Giả lập mã giao dịch hệ thống merchant gửi sang VNPAY
                Status = "0", //0: Trạng thái thanh toán "chờ thanh toán" hoặc "Pending" khởi tạo giao dịch chưa có IPN
                CreatedDate = DateTime.Now,
                Amount = amount,
            };

            //Build URL for VNPAY
            ClientWebMVC.Extentions.VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (order.Amount * 100).ToString());
            vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            vnpay.AddRequestData("vnp_CreateDate", order.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "123456789");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày
            var paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            var tran = new History()
            {
                Id = Guid.NewGuid(),
                User = userId,
                From = DateTime.Now.ToString("dd/MM/yyyy"), // bđ gói
                To = DateTime.Now.ToString("dd/MM/yyyy"), // kt gói
                Value = order.OrderId.ToString(),
                Date = DateTime.Now, // ngày tạo,
                Hash = ((int)3).ToString()
            };
            HttpClient client = new HttpClient();
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Add(contentType);

            string url = "http://localhost:5098/Auth/CreateHistory";
            string data = System.Text.Json.JsonSerializer.Serialize(tran);
            var httpContent = new StringContent(data, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(url, httpContent).GetAwaiter().GetResult();
            if(response.IsSuccessStatusCode)
            {
                return Redirect(paymentUrl);
            }
            return RedirectToPage("/Error"); 
        }
        catch
        {
            return RedirectToPage("/Error");
        }
    }
}