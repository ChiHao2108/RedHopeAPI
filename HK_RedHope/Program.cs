using HK_RedHope.Data;
using HK_RedHope.Models;
using HK_RedHope.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Add Controllers + Swagger
// -----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<ProfileResetService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HK_RedHope API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token!",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[]{}
        }
    });

    c.MapType<HK_RedHope.DTOs.RegisterDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["email"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
            ["password"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
            ["fullName"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
            ["phoneNumber"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
        }
    });

    c.MapType<HK_RedHope.DTOs.CreateAdminDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["email"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
            ["password"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
            ["fullName"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("string") },
        }
    });
});


// -----------------------------
// DbContext + SQL Server
// -----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -----------------------------
// Identity
// -----------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// -----------------------------
// JWT Authentication
// -----------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// -----------------------------
// Authorization
// -----------------------------
builder.Services.AddAuthorization();

// -----------------------------
// Role Seeder (User, Admin)
// -----------------------------
builder.Services.AddScoped<RoleSeeder>();

var app = builder.Build();

// -----------------------------
// Seed roles & admin (chạy 1 lần khi start)
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await seeder.SeedRolesAndAdminAsync();
}

// -----------------------------
// Middleware Pipeline
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HK_RedHope API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();  
app.UseAuthorization();

app.MapControllers();

app.Run();
