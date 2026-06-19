using ContractManagement.Data;
using ContractManagement.Domains.Quotation.Interfaces;
using ContractManagement.Domains.Quotation.Mappings;
using ContractManagement.Domains.Quotation.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext with connection string from configuration
builder.Services.AddDbContextPool<DbDtctechContext>(option => option.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddControllers();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
