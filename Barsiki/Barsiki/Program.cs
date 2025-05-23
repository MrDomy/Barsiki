using Microsoft.EntityFrameworkCore;
using Barsiki.Models;
using Barsiki.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<Barsiki.Services.SensorSimulator>();
builder.Services.AddControllersWithViews();
// Подключение контекста БД для датчика света
builder.Services.AddDbContext<LightSensorContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LightSensorConnection")));

// Регистрация сервиса датчика света
builder.Services.AddHostedService<LightSensorService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
