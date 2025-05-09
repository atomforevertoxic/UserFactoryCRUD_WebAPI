using Microsoft.EntityFrameworkCore;
using System;
using UserFactory.Data;

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

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
