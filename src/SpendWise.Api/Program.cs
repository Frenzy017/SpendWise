using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SpendWise.Api.Infrastructure;
using SpendWise.Data;
using SpendWise.Data.Entities;
using SpendWise.Data.Seeding;
using SpendWise.Services;
using SpendWise.Services.Options;

var builder = WebApplication.CreateBuilder(args);

// ---- Data + business services ----
builder.Services.AddDbContext<SpendWiseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SpendWise")));
builder.Services.AddSpendWiseServices();

// ---- JWT authentication (FR-AUTH-03, FR-AUTH-05, NFR-05) ----
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Missing 'Jwt' configuration.");
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters (256 bits).");
builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names exactly as issued ("sub", "role") instead of mapping them to long URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
    });

// Every endpoint requires a logged-in user unless it is marked [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---- Errors as ProblemDetails (NFR-06) ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

// ---- Swagger with a JWT "Authorize" button (NFR-12) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpendWise API",
        Version = "v1",
        Description = "Monthly expense tracker. Log in via /api/auth/login, then click Authorize and paste the token."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token from /api/auth/login (without the 'Bearer ' prefix)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

// ---- Create / upgrade the database and add demo data on startup (NFR-11) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpendWiseDbContext>();
    db.Database.Migrate();

    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var today = DateOnly.FromDateTime(scope.ServiceProvider.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        await DbSeeder.SeedAsync(db, hasher.HashPassword, today);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages(); // gives empty 401/403/404 responses a ProblemDetails body

app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "SpendWise API");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
