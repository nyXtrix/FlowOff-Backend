using System.Text;
using LMS.API.Middleware;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Application.Features.Auth.Services;
using LMS.Infrastructure.Persistense.DbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Auth.Validators;
using LMS.Infrastructure.Services.Email;
using LMS.Infrastructure.Services.Redis;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.Services;
using LMS.API.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://flowoff.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddValidatorsFromAssembly(typeof(RegisterCompanyValidator).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Leave Management System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

    // Add X-Tenant-Subdomain header to Swagger UI
    c.OperationFilter<TenantHeaderFilter>();
});

builder.Services.AddScoped<IWorkflowEngine, WorkflowEngineService>();

// Fetch connection string from Environment Variables
var defaultConnectionString = builder.Configuration.GetConnectionString("Default");

// Critical Startup Validation: Fail immediately if missing
if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException("CRITICAL: Database connection string 'ConnectionStrings__Default' is missing from environment variables.");
}

// 3. Central DB Setup
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(defaultConnectionString);
});
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 4. Register Infrastructure Services
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// 5. Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_secret_key_that_is_at_least_32_chars_long";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["AuthToken"];

                // Fallback: Check standard Authorization header (needed for Swagger)
                if (string.IsNullOrEmpty(context.Token))
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                try
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                    var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

                    if (token != null && await cache.GetAsync<string>($"blacklisted_{token.RawData}") != null)
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
                catch (Exception ex)
                {
                    // Fail-safe: if Redis is down, we allow authentication to proceed (revocation check skipped)
                    Console.WriteLine($"⚠️ REDIS_ERROR: {ex.Message}");
                }
            }
        };
    });


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();


if (args.Contains("db-migrate"))
{
    Console.WriteLine("Applying database migrations...");
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    Console.WriteLine("Database updated successfully!");
    return;
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class TenantHeaderFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>();

        operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
        {
            Name = "X-Tenant-Subdomain",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Required = false,
            Schema = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
            Description = "Subdomain of the tenant (needed for localhost testing)"
        });
    }
}
