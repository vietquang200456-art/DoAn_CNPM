using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using PharmaCheck.Data;
using PharmaCheck.Services;
using System.Globalization;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký HttpClient Factory (Bắt buộc phải có để DrugAiService gọi sang Python) 🌟
builder.Services.AddHttpClient();

// 2. Thay đổi từ AddSingleton sang AddScoped để khớp với vòng đời HttpClient và Controller 🌟
builder.Services.AddScoped<IDrugAiService, DrugAiService>();

// CÚ PHÁP ĐÚNG CHUẨN DÀNH RIÊNG CHO EPPLUS 8+ ⭐
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Viet Quang");

// Đăng ký các dịch vụ hệ thống
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddTransient<IEmailService, EmailService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "PharmaCheckAuth";
    });

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 🔥 ĐÃ XÓA ĐOẠN CODE GỌI aiService.TrainModelFromDb() Ở ĐÂY ĐỂ TRÁNH SẬP WEB 🔥

// Thiết lập Culture toàn cục cho VN (Hiển thị dd/MM/yyyy)
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 3. Cấu hình HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Cấu hình Middleware theo đúng thứ tự
app.UseSession(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();