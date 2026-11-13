using System.Text;
using Core.Entities.Identity;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Identity;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<EcommerceContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddDbContext<ApplicationIdentityDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("IdentityConnection"));
});
builder.Services.AddSingleton<IConnectionMultiplexer>(c =>
{
    try
    {
        var redisConnection = builder.Configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnection))
        {
            redisConnection = "localhost:6379";
        }
        var config = ConfigurationOptions.Parse(redisConnection, true);
        return ConnectionMultiplexer.Connect(config);
    }
    catch (Exception ex)
    {
        var logger = c.GetRequiredService<ILogger<Program>>();
        logger.LogWarning($"Redis connection failed: {ex.Message}. Redis features will be unavailable.");
        return null;
    }
});
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBasketRepository, RedisBasketRepository>();
builder.Services.AddScoped<ITokenGenerationService, TokenGenerationService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", builder =>
    {
        builder.WithOrigins("http://localhost:4200") // Replace with the actual Angular app URL.
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Configure authentication and authorization middleware
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = tokenSettings.Issuer,
        ValidAudience = tokenSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Key))
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularApp");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<EcommerceContext>();
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<EcommerceContextSeed>();

    logger.LogInformation("Applying migrations...");
    await context.Database.MigrateAsync();

    logger.LogInformation("Seeding product data...");
    var ecommerceContextSeed = new EcommerceContextSeed(logger);
    await ecommerceContextSeed.SeedDataAsync(context);

    logger.LogInformation("Seeding identity data...");
    await ApplicationIdentityDbContextSeed.SeedAsync(services);

    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogError(ex, "An error occurred during database initialization");
}
app.Run();