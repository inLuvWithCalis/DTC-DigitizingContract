using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.API.Domains.Interfaces.Customer;
using ContractManagement.API.Domains.Interfaces.CustomerInteraction;
using ContractManagement.API.Domains.Interfaces.Department;
using ContractManagement.API.Domains.Services.Catalog;
using ContractManagement.API.Domains.Services.Customer;
using ContractManagement.API.Domains.Services.CustomerInteraction;
using ContractManagement.API.Domains.Services.Department;
using ContractManagement.API.Domains.Services.Employee;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Interfaces.Quotation;
using ContractManagement.Domains.Mappings.Quotation;
using ContractManagement.Domains.Services.Catalog;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Domains.Services.File;
using ContractManagement.Domains.Services.Quotation;
using ContractManagement.Infrastructure.DatabaseScripts.SeedData;
using ContractManagement.Infrastructure.MultiTenancy.DI;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Persistence.Central.Entities;
using ContractManagement.Middleware;
using ContractManagement.Middleware.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using ContractManagement.API.Helpers;

var builder = WebApplication.CreateBuilder(args);

#region 1. MVC Controllers

/*
 * Đăng ký Controller cho Web API.
 */
builder.Services.AddControllers();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
#endregion

#region 2. Swagger / OpenAPI

/*
 * Dùng để hiển thị Swagger UI và tài liệu API.
 */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*
 * AddOpenApi() không bắt buộc nếu bạn đã dùng SwaggerGen.
 * Có thể bỏ để tránh trùng chức năng.
 */
// builder.Services.AddOpenApi();

#endregion

#region 3. Multi-tenancy Infrastructure

/*
 * Đăng ký toàn bộ thành phần Infrastructure:
 *
 * - CentralDbContext
 * - DbDtctechContext dùng connection string động
 * - CurrentTenant
 * - TenantResolver
 * - TenantProvisioningService
 * - TenantDatabaseInitializer
 *
 * Không đăng ký DbDtctechContext thêm lần nữa trong Program.cs.
 */
builder.Services.AddContractManagementInfrastructure(
    builder.Configuration);

#endregion

#region 4. Session

/*
 * Session hiện được lưu trong bộ nhớ của server.
 *
 * Phù hợp khi phát triển hoặc chỉ chạy một server.
 * Khi scale nhiều server, nên dùng Redis hoặc SQL Server Session.
 */
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    /*
     * Session hết hạn sau 30 phút không hoạt động.
     */
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    /*
     * JavaScript phía frontend không đọc được session cookie.
     * Giúp hạn chế nguy cơ đánh cắp cookie bằng XSS.
     */
    options.Cookie.HttpOnly = true;

    /*
     * Cho phép cookie session hoạt động
     * dù người dùng chưa đồng ý cookie không thiết yếu.
     */
    options.Cookie.IsEssential = true;

    options.Cookie.Name = "ContractManagement.Session";

    /*
     * Cho phép cookie được gửi trong các request thông thường
     * nhưng hạn chế một số request cross-site.
     */
    options.Cookie.SameSite = SameSiteMode.Lax;

    /*
     * Local HTTP vẫn chạy được.
     * Khi request dùng HTTPS thì cookie cũng dùng Secure.
     */
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;
});

#endregion

#region 5. CORS

/*
 * Cho phép React frontend gọi API và gửi session cookie.
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000",
                "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

#endregion

#region 6. Authentication helpers

/*
 * Service dùng để hash và kiểm tra mật khẩu nhân viên.
 */
builder.Services.AddScoped<
    IPasswordHasher<TblEmployee>,
    PasswordHasher<TblEmployee>>();

/*
 * Service dùng để hash và kiểm tra mật khẩu SystemAdmin.
 */
builder.Services.AddScoped<
    IPasswordHasher<SystemAdmin>,
    PasswordHasher<SystemAdmin>>();
#endregion

#region 7. Business services

/*
 * Đăng ký các service nghiệp vụ.
 */
builder.Services.AddScoped<
    IQuotationService,
    QuotationService>();

builder.Services.AddScoped<
    IFileStorageService,
    FileStorageService>();

builder.Services.AddScoped<
    IContractAttachmentService,
    ContractAttachmentService>();

builder.Services.AddScoped<
    IDepartmentService,
    DepartmentService>();

builder.Services.AddScoped<
    IEmployeeService,
    EmployeeService>();

builder.Services.AddScoped<
    ICustomerService,
    CustomerService>();

builder.Services.AddScoped<
    ICustomerInteractionService,
    CustomerInteractionService>();

builder.Services.AddScoped<
    ICategoryService,
    CategoryService>();

builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<
    IServiceTypeService,
    ServiceTypeService>();

builder.Services.AddScoped<
    IServiceService,
    ServiceService>();

#endregion

#region 8. AutoMapper

/*
 * Đăng ký profile ánh xạ dữ liệu báo giá.
 */
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<QuotationMappingProfile>();
});

#endregion

var app = builder.Build();

#region 8.5 Seed central Data

using (var scope = app.Services.CreateScope())
{
    var centralSeedData =
        scope.ServiceProvider
            .GetRequiredService<ICentralSeedData>();

    await centralSeedData.InitializeAsync();
}

#endregion

#region 9. Development tools

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#endregion

#region 10. HTTP middleware pipeline

/*
 * Chuyển HTTP sang HTTPS.
 */
app.UseHttpsRedirection();

/*
 * Bắt lỗi toàn cục và trả về response thống nhất.
 */
app.UseMiddleware<ExceptionHandlingMiddleware>();

/*
 * Xác định endpoint hiện tại.
 *
 * Phải đứng trước TenantResolutionMiddleware để middleware
 * đọc được metadata như AllowWithoutTenantAttribute.
 */
app.UseRouting();

/*
 * CORS phải chạy trước Controller.
 */
app.UseCors("CorsPolicy");

/*
 * Session phải chạy trước TenantResolutionMiddleware,
 * vì middleware có thể lấy TenantCode từ session.
 */
app.UseSession();

/*
 * Khi triển khai authentication chuẩn của ASP.NET Core,
 * thêm middleware này trước TenantResolutionMiddleware:
 *
 * app.UseAuthentication();
 */

/*
 * Xác định tenant hiện tại:
 *
 * 1. Đọc TenantCode từ session, claim hoặc header.
 * 2. Tra cứu trong Central Database.
 * 3. Lưu tenant vào CurrentTenant.
 * 4. DbDtctechContext dùng connection string của tenant đó.
 */
app.UseMiddleware<TenantResolutionMiddleware>();

/*
 * Kiểm tra quyền truy cập.
 */
app.UseAuthorization();

/*
 * Ánh xạ endpoint Controller.
 */
app.MapControllers();

/*
 * Chuyển các request không khớp Controller
 * sang Next.js.
 */
app.MapReverseProxy();

FrontendLauncher.Start();
#endregion

/*
 * Không chạy SeedData cũ tại startup ở thời điểm này.
 *
 * Lý do:
 * SeedData dùng DbDtctechContext, nhưng lúc startup chưa có
 * request và chưa xác định được tenant.
 *
 * Seed dữ liệu tenant nên được thực hiện:
 * - Ngay sau khi tạo database tenant; hoặc
 * - Bằng một TenantDatabaseInitializer riêng.
 */

app.Run();