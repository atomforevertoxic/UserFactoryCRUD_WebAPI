using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserFactory.Data;
using UserFactory.Models;
using UserFactory.Services;

var builder = WebApplication.CreateBuilder(args);

// ��������� Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "Default admin| Login: Admin, Password: AdminPass123\nCreate admin by api before login like administrator"
    });
});

// ���� ������
builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

// ��������� ��������� API ������������
builder.Services.AddControllers();

// ���� ��� ������������� ����� MVC-����������� (��������, ��� Views), ����������������:
// builder.Services.AddControllersWithViews();

// �������
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<AccountService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHttpContextAccessor();

// ��������������
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

builder.Services.AddAuthorization();

// ������������ ��������������
builder.Services.Configure<User>(builder.Configuration.GetSection("DefaultAdminUser"));

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger(); // ���������� swagger.json
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger"; // URL ��� UI: /swagger
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ��� API-������������
app.MapControllers();

app.Run();