using Boekhouding.Api.Authorization;
using Boekhouding.Api.Middleware;
using Boekhouding.Api.Swagger;
using Boekhouding.Application;
using Boekhouding.Infrastructure;
using Boekhouding.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Override connection string from environment variable (Railway support)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"📊 DATABASE_URL env var: {(string.IsNullOrEmpty(databaseUrl) ? "EMPTY/NULL" : $"Length={databaseUrl.Length}")}");
Console.WriteLine($"📊 DATABASE_URL raw value: [{databaseUrl}]"); // Show actual value with brackets
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("🔧 Overriding connection string from DATABASE_URL environment variable");
    builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
}
else
{
    Console.WriteLine("⚠️  DATABASE_URL environment variable is empty or not set!");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Boekhouding API",
        Version = "v1",
        Description = "ASP.NET Core API voor boekhoud applicatie met JWT authenticatie en multi-tenancy"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

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

    // Add X-Tenant-Id header parameter
    options.OperationFilter<TenantHeaderOperationFilter>();

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopmentPurposesOnly123456789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Boekhouding.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Boekhouding.Client";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization with policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireAdminRole, policy =>
        policy.RequireRole(Roles.Admin));
    
    options.AddPolicy(Policies.RequireAccountantRole, policy =>
        policy.RequireRole(Roles.Accountant));
    
    options.AddPolicy(Policies.RequireViewerRole, policy =>
        policy.RequireRole(Roles.Viewer, Roles.Accountant, Roles.Admin));
    
    options.AddPolicy(Policies.RequireAccountantOrAdmin, policy =>
        policy.RequireRole(Roles.Accountant, Roles.Admin));
    
    options.AddPolicy(Policies.RequireAdminOrOwner, policy =>
        policy.RequireRole(Roles.Accountant, Roles.Admin));
});

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure CORS - Strict configuration for production
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "https://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithExposedHeaders("X-Tenant-Id")
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains(); // Voor production subdomains
    });
});

var app = builder.Build();

// Run database migrations and seeding automatically
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        await DbSeeder.SeedAsync(services);
        
        Console.WriteLine("✅ Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Database setup warning: {ex.Message}");
        // Don't throw - seeding might fail if data already exists
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// Add security middleware BEFORE authentication
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<SecurityValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Add tenant middleware AFTER authentication/authorization
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
