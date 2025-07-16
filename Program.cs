using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
var wkHtmlToPdfPath = Path.Combine(
    builder.Environment.ContentRootPath,
    $"libwkhtmltox\\{architectureFolder}\\libwkhtmltox.dll"
);

// ตรวจสอบว่ามีไฟล์หรือไม่
if (File.Exists(wkHtmlToPdfPath))
{
    builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
    Console.WriteLine($"DinkToPdf initialized with: {wkHtmlToPdfPath}");
}
else
{
    Console.WriteLine($"wkhtmltopdf not found at: {wkHtmlToPdfPath}");
    Console.WriteLine("PDF generation will not be available");
    builder.Services.AddSingleton<IConverter>(provider => null);
}

// เพิ่มการกำหนด Hosting Model
builder
    .WebHost.UseIIS()
    .UseIISIntegration()
    .UseDefaultServiceProvider(options => options.ValidateScopes = false)
    .ConfigureKestrel(options => options.Limits.MaxRequestBodySize = int.MaxValue);

// เพิ่ม logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventLog();

// Add services to the container
builder
    .Services.AddControllersWithViews(options => options.Filters.Add(typeof(SessionCheckAttribute)))
    .AddRazorRuntimeCompilation();

builder
    .Services.AddMvc()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddDbContext<OracleDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection"))
);

builder.Services.AddDbContext<OracleHistoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HR_IPS"))
);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<
    IPS_TH.Services.IOracleHistoryService,
    IPS_TH.Services.OracleHistoryService
>();
builder.Services.AddScoped<EmployeeController>();

builder.Services.AddSingleton<
    Microsoft.AspNetCore.Mvc.ViewEngines.IViewEngine,
    Microsoft.AspNetCore.Mvc.ViewEngines.CompositeViewEngine
>();
builder.Services.AddSingleton<
    Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider,
    Microsoft.AspNetCore.Mvc.ViewFeatures.CookieTempDataProvider
>();

builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
    options.AuthenticationDisplayName = "Windows";
});

builder.Services.Configure<IISServerOptions>(options => options.AutomaticAuthentication = true);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

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

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

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
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // เปลี่ยนเป็น SameAsRequest
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = "/Authen/Login";
            options.LogoutPath = "/Authen/Logout";
            options.AccessDeniedPath = "/Authen/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        }
    );

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // ใช้ในโหมด Development เพื่อดูข้อผิดพลาด
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePages();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy(); 
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UsePathBase("/mgrips");

app.MapControllerRoute(name: "default", pattern: "{controller=Authen}/{action=Login}/{id?}");

app.Run();
