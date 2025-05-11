using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using UserFactory.Data;
using UserFactory.Models;
using UserFactory.Services;

var builder = WebApplication.CreateBuilder(args);

// Swagger initialization
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
});

// InMemory registration
builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

builder.Services.AddControllersWithViews();

builder.Services.AddTransient<UserService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var configuration = builder.Configuration;
builder.Services.Configure<User>(configuration.GetSection("DefaultAdminUser"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Swagger launch
app.UseSwagger(); 

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger"; 
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
