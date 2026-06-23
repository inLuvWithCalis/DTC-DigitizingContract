using ContractManagement.Data;
using ContractManagement.Domains.Interfaces.Quotation;
using ContractManagement.Domains.Mappings.Quotation;
using ContractManagement.Domains.Services.Quotation;
using ContractManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


// Add DbContext with connection string from configuration
builder.Services.AddDbContextPool<DbDtctechContext>(option => option.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// Save session state in memory
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.Name = "ContractManagement.Session";

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Hasing and check password
builder.Services.AddScoped<IPasswordHasher<TblEmployee>, PasswordHasher<TblEmployee>>();

// Resgister SeedData for seeding initial data.
builder.Services.AddScoped<SeedData>();

// Configure CORS to allow requests from the React client
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add services for DI
builder.Services.AddScoped<IQuotationService, QuotationService>();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AutoMapper configuration
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<QuotationMappingProfile>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("CorsPolicy");

app.UseSession();

app.UseAuthorization();

app.MapControllers();

// Seed initial data if in development environment
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var seedData = services.GetRequiredService<SeedData>();
        await seedData.InitializeAsync();
    }
}

app.Run();
