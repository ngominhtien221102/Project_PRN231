using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Service.MangaOnline.Extensions;
using Service.MangaOnline.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//Session

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MangaOnlineV1DevContext>();
builder.Services.AddSingleton<IJwtTokenHandler, JwtTokenHandler>();
builder.Services.AddSingleton<IExtensionManga, ExtensionManga>();
builder.Services.AddSingleton<IMapping, Mapping>();
builder.Services.AddScoped<MangaOnlineV1DevContext>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
builder.Services.AddControllers().AddOData(otp => otp.Select().Filter().Count().OrderBy().Expand().SetMaxTop(100).AddRouteComponents("odata", GetEdmModel()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
}
app.UseCors(builder => builder
    .WithOrigins("http://localhost:5030") 
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseSession();

app.Run();



static IEdmModel GetEdmModel()
{

    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Manga>("Manga");
    return builder.GetEdmModel();
}
