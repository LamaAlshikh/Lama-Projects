using Acadify.Models.Db;
using Acadify.Services;
using Acadify.Services.AcademicCalendar;
using Acadify.Services.AcademicCalendar.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// HttpClient support
builder.Services.AddHttpClient();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database
var connectionString =
    builder.Configuration.GetConnectionString("AcadifyDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AcadifyDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql =>
        {
            sql.CommandTimeout(120);
            sql.EnableRetryOnFailure();
        }));

// Project services
builder.Services.AddHttpClient<AiAcademicAgentService>();
builder.Services.AddScoped<ITranscriptParserService, TranscriptParserService>();
builder.Services.AddScoped<IRecommendationEngineService, RecommendationEngineService>();
builder.Services.AddScoped<ITranscriptAiParserService, TranscriptAiParserService>();

// Academic calendar service
builder.Services.AddScoped<IAcademicCalendarAiExtractor, AcademicCalendarAiExtractor>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=Welcome}/{id?}");

app.Run();