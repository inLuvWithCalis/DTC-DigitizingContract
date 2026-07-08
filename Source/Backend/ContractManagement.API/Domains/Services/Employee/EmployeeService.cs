using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Employee
{
    /// <summary>
    /// Service xử lý nghiệp vụ nhân viên.
    /// Lưu ý:
    /// - Không dùng DefaultConnection.
    /// - DbDtctechContext đã tự trỏ đúng tenant DB hiện tại.
    /// - Không trả password ra ngoài API.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly DbDtctechContext _dbContext;
        private readonly IPasswordHasher<TblEmployee> _passwordHasher;

        public EmployeeService(
            DbDtctechContext dbContext,
            IPasswordHasher<TblEmployee> passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public async Task<PagedResult<EmployeeResponse>> GetListAsync(
            int page,
            int pageSize,
            string? keyword,
            byte? status,
            DateTime? dateCreated)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _dbContext.TblEmployees.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    (x.EmployeeFullName != null && x.EmployeeFullName.Contains(keyword)) ||
                    (x.EmployeeAccount != null && x.EmployeeAccount.Contains(keyword)) ||
                    (x.EmployeeEmail != null && x.EmployeeEmail.Contains(keyword)));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (dateCreated.HasValue)
            {
                var date = dateCreated.Value.Date;
                query = query.Where(x => x.DateCreated >= date && x.DateCreated < date.AddDays(1));
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .OrderByDescending(x => x.EmployeeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var departmentNames = await GetDepartmentNamesAsync(employees);

            return new PagedResult<EmployeeResponse>
            {
                Items = employees
                    .Select(x => MapToResponse(
                        x,
                        x.DepartmentId.HasValue &&
                        departmentNames.TryGetValue(x.DepartmentId.Value, out var name)
                            ? name
                            : null))
                    .ToList(),

                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<EmployeeResponse> GetByIdAsync(int id)
        {
            var employee = await _dbContext.TblEmployees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            }

            string? departmentName = null;

            if (employee.DepartmentId.HasValue)
            {
                departmentName = await _dbContext.TblDepartments
                    .AsNoTracking()
                    .Where(x => x.DepartmentId == employee.DepartmentId.Value)
                    .Select(x => x.DepartmentName)
                    .FirstOrDefaultAsync();
            }

            return MapToResponse(employee, departmentName);
        }

        public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
        {
            // 1. Check account không được trùng trong tenant DB hiện tại.
            var account = request.EmployeeAccount.Trim();

            var accountExists = await _dbContext.TblEmployees
                .AnyAsync(x => x.EmployeeAccount == account);

            if (accountExists)
            {
                throw new InvalidOperationException("Tài khoản nhân viên đã tồn tại.");
            }

            // 2. Check phòng ban nếu có truyền DepartmentId.
            await ValidateDepartmentAsync(request.DepartmentId);

            // 3. Check loại nhân viên.
            ValidateEmployeeType(request.EmployeeType);

            // 4. Tạo entity nhân viên.
            var employee = new TblEmployee
            {
                EmployeeCode = request.EmployeeCode?.Trim(),
                EmployeeAccount = account,
                EmployeeFullName = request.EmployeeFullName.Trim(),
                EmployeeMobile = request.EmployeeMobile?.Trim(),
                EmployeeEmail = request.EmployeeEmail?.Trim(),
                DepartmentId = request.DepartmentId,
                EmployeeType = request.EmployeeType,

                // Quy ước: 1 = Active, 0 = Inactive.
                Status = 1,

                DateCreated = DateTime.Now
            };

            // 5. Hash password bằng PasswordHasher giống auth/seed hiện tại.
            employee.EmployeePassword =
                _passwordHasher.HashPassword(employee, request.EmployeePassword);

            _dbContext.TblEmployees.Add(employee);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(employee.EmployeeId);
        }

        public async Task UpdateAsync(int id, UpdateEmployeeRequest request)
        {
            var employee = await _dbContext.TblEmployees
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            }

            await ValidateDepartmentAsync(request.DepartmentId);
            ValidateEmployeeType(request.EmployeeType);

            employee.EmployeeCode = request.EmployeeCode?.Trim();
            employee.EmployeeFullName = request.EmployeeFullName.Trim();
            employee.EmployeeMobile = request.EmployeeMobile?.Trim();
            employee.EmployeeEmail = request.EmployeeEmail?.Trim();
            employee.DepartmentId = request.DepartmentId;
            employee.EmployeeType = request.EmployeeType;
            employee.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(int id, ChangePasswordRequest request)
        {
            var employee = await _dbContext.TblEmployees
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            }

            employee.EmployeePassword =
                _passwordHasher.HashPassword(employee, request.NewPassword);

            employee.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task SetStatusAsync(int id, byte status)
        {
            if (status is not 0 and not 1)
            {
                throw new ArgumentException("Trạng thái nhân viên không hợp lệ. Chỉ nhận 0 hoặc 1.");
            }

            var employee = await _dbContext.TblEmployees
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (employee == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân viên.");
            }

            employee.Status = status;
            employee.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        private async Task ValidateDepartmentAsync(int? departmentId)
        {
            if (!departmentId.HasValue)
            {
                return;
            }

            var exists = await _dbContext.TblDepartments
                .AnyAsync(x => x.DepartmentId == departmentId.Value);

            if (!exists)
            {
                throw new KeyNotFoundException("Phòng ban không tồn tại.");
            }
        }

        private static void ValidateEmployeeType(byte? employeeType)
        {
            if (!employeeType.HasValue)
            {
                return;
            }

            if (!Enum.IsDefined(typeof(EmployeeType), employeeType.Value))
            {
                throw new ArgumentException("Loại nhân viên không hợp lệ.");
            }
        }

        private async Task<Dictionary<int, string>> GetDepartmentNamesAsync(
            List<TblEmployee> employees)
        {
            var departmentIds = employees
                .Where(x => x.DepartmentId.HasValue)
                .Select(x => x.DepartmentId!.Value)
                .Distinct()
                .ToList();

            if (departmentIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return await _dbContext.TblDepartments
                .AsNoTracking()
                .Where(x => departmentIds.Contains(x.DepartmentId))
                .ToDictionaryAsync(
                    x => (int)x.DepartmentId,
                    x => x.DepartmentName);
        }

        private static EmployeeResponse MapToResponse(
            TblEmployee employee,
            string? departmentName)
        {
            return new EmployeeResponse
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                EmployeeAccount = employee.EmployeeAccount,
                EmployeeFullName = employee.EmployeeFullName,
                EmployeeMobile = employee.EmployeeMobile,
                EmployeeEmail = employee.EmployeeEmail,
                DepartmentId = employee.DepartmentId,
                DepartmentName = departmentName,
                EmployeeType = employee.EmployeeType,
                EmployeeTypeName =
                    employee.EmployeeType.HasValue &&
                    Enum.IsDefined(typeof(EmployeeType), employee.EmployeeType.Value)
                        ? ((EmployeeType)employee.EmployeeType.Value).ToString()
                        : string.Empty,
                Status = employee.Status,
                DateCreated = employee.DateCreated,
                DateModified = employee.DateModified
            };
        }
    }
}