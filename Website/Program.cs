using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Website.Data;
using Website.Models;
using Website.Services;
using Website.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── DATABASE ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

// ─── IDENTITY ─────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ─── JWT ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ─── DI ───────────────────────────────────────────────────────
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<VnpayService>();
builder.Services.AddHttpContextAccessor();

// ─── CONTROLLERS + SWAGGER ────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Website API", Version = "v1" });
});

builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024);

// ─── PIPELINE ─────────────────────────────────────────────────
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Tạo các Role nếu chưa tồn tại
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));

        // 2. Tạo tài khoản test admin cố định
        var testAdminEmail = "testadmin_image@shopvn.com";
        var testAdminUser = await userManager.FindByEmailAsync(testAdminEmail);
        if (testAdminUser != null)
        {
            await userManager.DeleteAsync(testAdminUser);
        }
        testAdminUser = new ApplicationUser
        {
            UserName = testAdminEmail,
            Email = testAdminEmail,
            FullName = "Test Admin Image",
            EmailConfirmed = true
        };
        var createAdminResult = await userManager.CreateAsync(testAdminUser, "Password123!");
        if (createAdminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(testAdminUser, "Admin");
            Console.WriteLine($"[Seeder] Created testadmin_image@shopvn.com with Admin role.");
        }
        else
        {
            Console.WriteLine($"[Seeder] Failed to create testadmin_image: {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
        }

        // 3. Cập nhật quyền cho các tài khoản hiện có
        var users = await userManager.Users.ToListAsync();
        if (users.Any())
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (!admins.Any())
            {
                var firstUser = users.First();
                await userManager.AddToRoleAsync(firstUser, "Admin");
                Console.WriteLine($"[Seeder] Assigned 'Admin' role to the first user: {firstUser.Email}");
            }

            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                if (!roles.Any())
                {
                    await userManager.AddToRoleAsync(u, "User");
                    Console.WriteLine($"[Seeder] Assigned 'User' role to user: {u.Email}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seeding Error] {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Website API v1");
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serve index.html for root path
app.MapFallbackToFile("index.html");

app.Run();