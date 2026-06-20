using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using PharmaCheck.Data;
using PharmaCheck.Services;
using System.Globalization;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
// Đăng ký dạng Singleton để dùng chung bộ nhớ mô hình AI cho toàn server, tối ưu tốc độ
builder.Services.AddSingleton<IDrugAiService, DrugAiService>();
// CÚ PHÁP ĐÚNG CHUẨN DÀNH RIÊNG CHO EPPLUS 8+ ⭐
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Viet Quang");
// 1. Cấu hình Dịch vụ (Services)
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
// Gọi AI tự động quét DB để học dữ liệu khi vừa start web
using (var scope = app.Services.CreateScope())
{
    var aiService = scope.ServiceProvider.GetRequiredService<IDrugAiService>();
    aiService.TrainModelFromDb();
}
// 2. Thiết lập Culture toàn cục cho VN (Hiển thị dd/MM/yyyy)
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