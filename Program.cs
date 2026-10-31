using System.Text;
using EcoEast.API.Data;
using EcoEast.API.Models;
using EcoEast.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONTROLLERS & ROUTING
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// ==========================================
// 2. SWAGGER CONFIGURATION WITH JWT
// ==========================================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcoEast.API",
        Version = "v1",
        Description = "EcoEast Recycling Management API"
    });


// JWT Bearer security definition
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter your JWT token below."
});

    // Apply JWT authentication to API endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});


});

// ==========================================
// 3. DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<EcoEastDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("EcoEast")
)
);

// ==========================================
// 4. ASP.NET CORE IDENTITY
// ==========================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;


options.User.RequireUniqueEmail = true;


})
.AddEntityFrameworkStores<EcoEastDbContext>()
.AddDefaultTokenProviders();

// ==========================================
// 5. JWT AUTHENTICATION
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "EcoEast_Secure_JWT_Secret_Key_For_Local_Development_2026_32Bytes!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    JwtBearerDefaults.AuthenticationScheme;


options.DefaultChallengeScheme =
    JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,


        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EcoEast.API",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EcoEast.Web",

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        ClockSkew = TimeSpan.FromSeconds(30)
    };


});

// ==========================================
// 6. AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// 7. APPLICATION SERVICES
// ==========================================

builder.Services.AddScoped<JwtTokenService>();

// ==========================================
// 8. CORS CONFIGURATION
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin();
    });
});

// ==========================================
// 9. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 10. HTTP PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();


app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "EcoEast.API v1"
    );

    options.RoutePrefix = "swagger";
});

}

app.UseHttpsRedirection();

// Serve wwwroot/index.html at the application root.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

// Authentication must come before authorization
app.UseAuthentication();

app.UseAuthorization();

// Map API controllers
app.MapControllers();
// ==========================================
// TEMPORARY ADMIN SEEDER
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EcoEastDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // 1. Ensure Admin role exists
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 2. Check if admin user already exists
    if (await userManager.FindByNameAsync("admin_thabo") == null)
    {
        // Create Person record
        var person = new Person
        {
            FIRST_NAME = "Thabo",
            LAST_NAME = "Molefe",
            USERNAME = "admin_thabo",
            EMAIL = "thabo.admin@ecoeast.co.za",
            PHONE = "0723456789",
            ROLE = "Admin"
        };
        db.People.Add(person);
        await db.SaveChangesAsync();

        // Create Identity User
        var user = new ApplicationUser
        {
            UserName = "admin_thabo",
            Email = "thabo.admin@ecoeast.co.za",
            PhoneNumber = "0723456789",
            PersonId = person.PersonID,
            EmailConfirmed = true
        };

        // This automatically hashes the password correctly!
        var result = await userManager.CreateAsync(user, "AdminPass2026!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
// ==========================================
// 11. START APPLICATION
// ==========================================

app.Run();
