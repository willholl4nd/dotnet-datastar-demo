using Microsoft.EntityFrameworkCore;

using StarFederation.Datastar.DependencyInjection;

using dotnet_html_sortable_table.Data;
using dotnet_html_sortable_table.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<SqliteContext>(options => 
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddDatastar();

builder.Services.AddSingleton<SessionQueueStore>();
builder.Services.AddSession(options => 
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.IdleTimeout = TimeSpan.FromMinutes(30); // customize if needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Important for GDPR compliance
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
