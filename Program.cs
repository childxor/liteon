using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using DinkToPdf;
using DinkToPdf.Contracts;
using IPS_TH.Controllers.Employee;
using IPS_TH.Data;
using IPS_TH.Extensions;
using IPS_TH.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Initialize SessionExtensions
IPS_TH.Extensions.SessionExtensions.Initialize(builder.Configuration);

// กำหนด path สำหรับ wkhtmltopdf
var architectureFolder = (IntPtr.Size == 8) ? "64 bit" : "32 bit";
var wkHtmlToPdfPath = Path.Combine(builder.Environment.ContentRootPath, $"libwkhtmltox\\{architectureFolder}\\libwkhtmltox.dll");

// ตรวจสอบว่ามีไฟล์หรือไม่
if (File.Exists(wkHtmlToPdfPath))
{
    // ลงทะเบียน DinkToPdf
    builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
    Console.WriteLine($"DinkToPdf initialized with: {wkHtmlToPdfPath}");
}
else
{
    Console.WriteLine($"wkhtmltopdf not found at: {wkHtmlToPdfPath}");
    Console.WriteLine("PDF generation will not be available");
    
    // ลงทะเบียน null converter
    builder.Services.AddSingleton<IConverter>(provider => null);
}

// เพิ่มการกำหนด Hosting Model
builder
    .WebHost.UseIIS()
    .UseIISIntegration()
    .UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false;
    })
    .ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = int.MaxValue;
    });

// เพิ่ม logging ก่อนสร้าง app
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventLog();

// Add services to the container.
builder
    .Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(typeof(SessionCheckAttribute));
    })
    .AddRazorRuntimeCompilation();

// เพิ่มการรองรับ Razor Views แบบเก่า
builder
    .Services.AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// เพิ่มบริการ DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// เพิ่มบริการ Oracle DbContext (สำหรับเชื่อมต่อ Oracle Database)
builder.Services.AddDbContext<OracleDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection"))
);

// เพิ่มบริการ Oracle History DbContext (สำหรับเก็บ History ในฐานข้อมูล HR_IPS)
builder.Services.AddDbContext<OracleHistoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HR_IPS"))
);

// เพิ่มบริการ Oracle History Service สำหรับจัดการ History ของการทำงานกับ Oracle
builder.Services.AddHttpContextAccessor(); // จำเป็นสำหรับ OracleHistoryService
builder.Services.AddScoped<IPS_TH.Services.IOracleHistoryService, IPS_TH.Services.OracleHistoryService>();

// เพิ่มการลงทะเบียน EmployeeController
builder.Services.AddScoped<EmployeeController>();

// เพิ่มการลงทะเบียน ViewEngine และ TempDataProvider
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine, Microsoft.AspNetCore.Mvc.ViewEngines.CompositeViewEngine>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider, Microsoft.AspNetCore.Mvc.ViewFeatures.CookieTempDataProvider>();

// เพิ่มการกำหนดค่า IIS Integration
builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
    options.AuthenticationDisplayName = "Windows";
});

// เพิ่มการตั้งค่า Windows Authentication
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = true;
});

// เพิ่ม Forward Headers Middleware
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// เพิ่มบริการ session และ cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".IPS_TH.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// เพิ่มการกำหนดค่า Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// กำหนดค่า Authentication แบบเดียว
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name = ".IPS_TH.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = "/Authen/Login";
            options.LogoutPath = "/Authen/Logout";
            options.AccessDeniedPath = "/Authen/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        }
    );

// เพิ่ม CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePages();

// เพิ่ม Forward Headers Middleware ก่อน middleware อื่นๆ
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

// เพิ่ม Cookie Policy Middleware
app.UseCookiePolicy();

// ย้าย Session มาก่อน Routing
app.UseSession();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Authen}/{action=Login}/{id?}");

app.Run();
