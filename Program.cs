using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserFactory.Data;
using UserFactory.Models;
using UserFactory.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = @"
### Initial Setup Instructions:

1. **Create Default Admin**  
   First, execute the endpoint:  
   🔹 `POST /api/users/init-default-admin`  
   This will create the default administrator account.

2. **Login as Admin**  
   Use the default credentials to authenticate:  
   🔹 Login: `Admin`  
   🔹 Password: `AdminPass123`

3. **Start Managing Users**  
   After authentication, you can access all admin endpoints."
    });
});

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

builder.Services.AddControllers();

builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<AccountService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/api/Account/login";
        options.LogoutPath = "/api/Account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    });

builder.Services.Configure<User>(builder.Configuration.GetSection("DefaultAdminUser"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty; // Это сделает Swagger UI доступным по корневому URL
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Добавляем перенаправление с корневого URL на Swagger
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.MapControllers();

app.Run();