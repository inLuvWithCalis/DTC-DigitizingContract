using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Employee;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using ContractManagement.Infrastructure.Security;
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
        private readonly ICurrentTenant _currentTenant;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeService(
            DbDtctechContext dbContext,
            IPasswordHasher<TblEmployee> passwordHasher,
            ICurrentTenant currentTenant,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _currentTenant = currentTenant;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagedResult<EmployeeResponse>> GetListAsync(EmployeeFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query = _dbContext.TblEmployees.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.EmployeeFullName != null && x.EmployeeFullName.Contains(keyword)) ||
                    (x.EmployeeAccount != null && x.EmployeeAccount.Contains(keyword)) ||
                    (x.EmployeeEmail != null && x.EmployeeEmail.Contains(keyword)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            query = ApplyDateFilter(query, filter.FromDate, filter.ToDate);

            var totalCount = await query.CountAsync();

            var employees = await query
                .OrderByDescending(x => x.EmployeeId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
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
                Page = filter.Page,
                PageSize = filter.PageSize
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

        public async Task<List<EmployeeDirectoryResponse>> GetDirectoryAsync()
        {
            var rows = await (
                    from employee in _dbContext.TblEmployees.AsNoTracking()
                    join department in _dbContext.TblDepartments.AsNoTracking()
                        on employee.DepartmentId equals (int?)department.DepartmentId
                        into departments
                    from department in departments.DefaultIfEmpty()
                    where employee.Status == 1
                    orderby employee.EmployeeFullName, employee.EmployeeId
                    select new
                    {
                        employee.EmployeeId,
                        employee.EmployeeCode,
                        employee.EmployeeFullName,
                        employee.EmployeeMobile,
                        employee.DepartmentId,
                        DepartmentName = department == null ? null : department.DepartmentName,
                        employee.EmployeeType,
                        employee.Status
                    })
                .ToListAsync();

            return rows
                .Where(row => row.EmployeeType.HasValue
                    && Enum.IsDefined(typeof(EmployeeType), row.EmployeeType.Value))
                .Select(row => new EmployeeDirectoryResponse
                {
                    EmployeeId = row.EmployeeId,
                    EmployeeCode = row.EmployeeCode,
                    EmployeeFullName = row.EmployeeFullName,
                    EmployeeMobile = row.EmployeeMobile,
                    DepartmentId = row.DepartmentId,
                    DepartmentName = row.DepartmentName,
                    EmployeeType = row.EmployeeType!.Value,
                    EmployeeTypeName = ((EmployeeType)row.EmployeeType.Value).ToString(),
                    Status = row.Status!.Value
                })
                .ToList();
        }

        public async Task<PagedResult<EmployeeDirectoryResponse>>
            SearchDirectoryAsync(
                EmployeeDirectoryFilterRequest filter,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
            if (pageSize > 100)
            {
                throw new ArgumentException("PageSize không được vượt quá 100.");
            }

            var query =
                from employee in _dbContext.TblEmployees.AsNoTracking()
                join department in _dbContext.TblDepartments.AsNoTracking()
                    on employee.DepartmentId equals (int?)department.DepartmentId
                    into departments
                from department in departments.DefaultIfEmpty()
                where employee.Status == 1
                    && employee.EmployeeType >= (byte)EmployeeType.Sale
                    && employee.EmployeeType <= (byte)EmployeeType.Manager
                select new
                {
                    employee.EmployeeId,
                    employee.EmployeeCode,
                    employee.EmployeeFullName,
                    employee.EmployeeMobile,
                    employee.DepartmentId,
                    DepartmentName = department == null
                        ? null
                        : department.DepartmentName,
                    employee.EmployeeType,
                    employee.Status
                };

            var keyword = filter.Keyword?.Trim();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(item =>
                    (item.EmployeeFullName != null
                        && item.EmployeeFullName.Contains(keyword))
                    || (item.EmployeeCode != null
                        && item.EmployeeCode.Contains(keyword))
                    || (item.DepartmentName != null
                        && item.DepartmentName.Contains(keyword)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var offset = ((long)page - 1) * pageSize;
            if (offset > int.MaxValue)
            {
                throw new ArgumentException(
                    "Requested employee page is outside the supported range.");
            }
            var rows = await query
                .OrderBy(item => item.EmployeeFullName)
                .ThenBy(item => item.EmployeeId)
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<EmployeeDirectoryResponse>
            {
                Items = rows.Select(row => new EmployeeDirectoryResponse
                {
                    EmployeeId = row.EmployeeId,
                    EmployeeCode = row.EmployeeCode,
                    EmployeeFullName = row.EmployeeFullName,
                    EmployeeMobile = row.EmployeeMobile,
                    DepartmentId = row.DepartmentId,
                    DepartmentName = row.DepartmentName,
                    EmployeeType = row.EmployeeType!.Value,
                    EmployeeTypeName = ((EmployeeType)row.EmployeeType.Value).ToString(),
                    Status = row.Status!.Value
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public Task<EmployeeResponse> CreateManagedEmployeeAsync(
            int managerEmployeeId,
            CreateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ExecuteManagedMutationAsync(async () =>
            {
                await LoadActiveManagerAsync(managerEmployeeId, cancellationToken);
                EnsureManagerCannotAssignManagerRole(request.EmployeeType);
                await ValidateDepartmentAsync(request.DepartmentId);

                var account = NormalizeRequired(request.EmployeeAccount, nameof(request.EmployeeAccount));
                if (await _dbContext.TblEmployees.AnyAsync(
                        employee => employee.EmployeeAccount == account,
                        cancellationToken))
                {
                    throw new InvalidOperationException("Tài khoản nhân viên đã tồn tại.");
                }

                var employee = new TblEmployee
                {
                    EmployeeCode = NormalizeOptional(request.EmployeeCode),
                    EmployeeAccount = account,
                    EmployeeFullName = NormalizeRequired(
                        request.EmployeeFullName,
                        nameof(request.EmployeeFullName)),
                    EmployeeMobile = NormalizeOptional(request.EmployeeMobile),
                    EmployeeEmail = NormalizeOptional(request.EmployeeEmail),
                    DepartmentId = request.DepartmentId,
                    EmployeeType = request.EmployeeType,
                    Status = 1,
                    DateCreated = DateTime.UtcNow
                };
                employee.EmployeePassword = _passwordHasher.HashPassword(
                    employee,
                    request.EmployeePassword);

                _dbContext.TblEmployees.Add(employee);
                await _dbContext.SaveChangesAsync(cancellationToken);

                StageTenantAudit(
                    managerEmployeeId,
                    AuthorizationAuditActionTypes.EmployeeCreated,
                    employee,
                    null,
                    null);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return await GetByIdAsync(employee.EmployeeId);
            }, cancellationToken);
        }

        public Task UpdateManagedEmployeeAsync(
            int managerEmployeeId,
            int employeeId,
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ExecuteManagedMutationAsync(async () =>
            {
                await LoadActiveManagerAsync(managerEmployeeId, cancellationToken);
                var employee = await GetTrackedEmployeeAsync(employeeId, cancellationToken);
                EnsureManagerCanMutateTarget(employee, request.EmployeeType);
                await ValidateDepartmentAsync(request.DepartmentId);
                SetExpectedRowVersion(employee, request.RowVersion);

                var previousEmployeeType = employee.EmployeeType;
                employee.EmployeeCode = NormalizeOptional(request.EmployeeCode);
                employee.EmployeeFullName = NormalizeRequired(
                    request.EmployeeFullName,
                    nameof(request.EmployeeFullName));
                employee.EmployeeMobile = NormalizeOptional(request.EmployeeMobile);
                employee.EmployeeEmail = NormalizeOptional(request.EmployeeEmail);
                employee.DepartmentId = request.DepartmentId;
                employee.EmployeeType = request.EmployeeType;
                employee.DateModified = DateTime.UtcNow;

                if (previousEmployeeType != employee.EmployeeType)
                {
                    StageTenantAudit(
                        managerEmployeeId,
                        AuthorizationAuditActionTypes.EmployeeRoleChanged,
                        employee,
                        previousEmployeeType,
                        employee.Status);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);
        }

        public Task ResetManagedEmployeePasswordAsync(
            int managerEmployeeId,
            int employeeId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ExecuteManagedMutationAsync(async () =>
            {
                await LoadActiveManagerAsync(managerEmployeeId, cancellationToken);
                var employee = await GetTrackedEmployeeAsync(employeeId, cancellationToken);
                EnsureManagerCanMutateTarget(employee, employee.EmployeeType);
                SetExpectedRowVersion(employee, request.RowVersion);

                employee.EmployeePassword = _passwordHasher.HashPassword(
                    employee,
                    request.NewPassword);
                employee.DateModified = DateTime.UtcNow;
                StageTenantAudit(
                    managerEmployeeId,
                    AuthorizationAuditActionTypes.EmployeePasswordReset,
                    employee,
                    employee.EmployeeType,
                    employee.Status);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);
        }

        public Task SetManagedEmployeeStatusAsync(
            int managerEmployeeId,
            int employeeId,
            SetEmployeeStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ExecuteManagedMutationAsync(async () =>
            {
                if (request.Status is not 0 and not 1)
                {
                    throw new ArgumentException(
                        "Trạng thái nhân viên không hợp lệ. Chỉ nhận 0 hoặc 1.");
                }

                await LoadActiveManagerAsync(managerEmployeeId, cancellationToken);
                var employee = await GetTrackedEmployeeAsync(employeeId, cancellationToken);
                EnsureManagerCanMutateTarget(employee, employee.EmployeeType);
                SetExpectedRowVersion(employee, request.RowVersion);

                var previousStatus = employee.Status;
                employee.Status = request.Status;
                employee.DateModified = DateTime.UtcNow;

                if (previousStatus != employee.Status)
                {
                    StageTenantAudit(
                        managerEmployeeId,
                        AuthorizationAuditActionTypes.EmployeeStatusChanged,
                        employee,
                        employee.EmployeeType,
                        previousStatus);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);
        }

        private async Task<TblEmployee> LoadActiveManagerAsync(
            int managerEmployeeId,
            CancellationToken cancellationToken)
        {
            var actor = await _dbContext.TblEmployees
                .FirstOrDefaultAsync(
                    employee => employee.EmployeeId == managerEmployeeId,
                    cancellationToken);

            if (actor is null || actor.Status != 1)
            {
                throw new RbacOperationException(
                    StatusCodes.Status401Unauthorized,
                    AuthorizationErrorCodes.AuthenticationRequired,
                    "Employee session is no longer valid.");
            }

            if (actor.EmployeeType != (byte)EmployeeType.Manager)
            {
                throw new RbacOperationException(
                    StatusCodes.Status403Forbidden,
                    AuthorizationErrorCodes.PermissionDenied,
                    "Only an active Manager can manage employees.");
            }

            return actor;
        }

        private async Task<TblEmployee> GetTrackedEmployeeAsync(
            int employeeId,
            CancellationToken cancellationToken)
        {
            var employee = await _dbContext.TblEmployees
                .FirstOrDefaultAsync(
                    candidate => candidate.EmployeeId == employeeId,
                    cancellationToken);

            return employee ?? throw new RbacOperationException(
                StatusCodes.Status404NotFound,
                AuthorizationErrorCodes.ResourceNotFound,
                "Employee was not found.");
        }

        private static void EnsureManagerCannotAssignManagerRole(
            byte employeeType)
        {
            if (!Enum.IsDefined(typeof(EmployeeType), employeeType))
            {
                throw new ArgumentException("Loại nhân viên không hợp lệ.");
            }

            if (employeeType == (byte)EmployeeType.Manager)
            {
                throw new RbacOperationException(
                    StatusCodes.Status403Forbidden,
                    AuthorizationErrorCodes.PermissionDenied,
                    "Only System Admin can assign the Manager role.");
            }
        }

        private static void EnsureManagerCanMutateTarget(
            TblEmployee target,
            byte? requestedEmployeeType)
        {
            if (target.EmployeeType == (byte)EmployeeType.Manager
                || requestedEmployeeType == (byte)EmployeeType.Manager)
            {
                throw new RbacOperationException(
                    StatusCodes.Status403Forbidden,
                    AuthorizationErrorCodes.PermissionDenied,
                    "Manager cannot modify an existing Manager or assign the Manager role.");
            }
        }

        private void SetExpectedRowVersion(
            TblEmployee employee,
            string rowVersion)
        {
            var expected = DecodeRowVersion(rowVersion);
            if (employee.RowVersion is not { Length: 8 }
                || !employee.RowVersion.AsSpan().SequenceEqual(expected))
            {
                throw new RbacOperationException(
                    StatusCodes.Status409Conflict,
                    AuthorizationErrorCodes.StaleRowVersion,
                    "Employee has been updated by another request.");
            }

            _dbContext.Entry(employee)
                .Property(candidate => candidate.RowVersion)
                .OriginalValue = expected;
        }

        private void StageTenantAudit(
            int actorEmployeeId,
            string action,
            TblEmployee target,
            byte? previousEmployeeType,
            byte? previousStatus)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            _dbContext.TblAuthorizationAudits.Add(
                AuthorizationAuditRecordFactory.CreateTenant(
                    _currentTenant.GetRequiredTenant().TenantId,
                    actorEmployeeId,
                    "Employee",
                    action,
                    AuthorizationAuditResultTypes.Success,
                    "Employee",
                    target.EmployeeId.ToString(),
                    previousEmployeeType,
                    target.EmployeeType,
                    previousStatus,
                    target.Status,
                    null,
                    DateTime.UtcNow,
                    httpContext?.Connection.RemoteIpAddress?.ToString(),
                    httpContext?.Request.Headers.UserAgent.ToString(),
                    httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N")));
        }

        private async Task<T> ExecuteManagedMutationAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken)
        {
            if (!_dbContext.Database.IsRelational())
            {
                return await operation();
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                try
                {
                    var result = await operation();
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                    throw;
                }
            });
        }

        private static byte[] DecodeRowVersion(string rowVersion)
        {
            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                throw new ArgumentException("RowVersion không được để trống.");
            }

            try
            {
                var bytes = Convert.FromBase64String(rowVersion);
                if (bytes.Length != 8)
                {
                    throw new ArgumentException("RowVersion không hợp lệ.");
                }

                return bytes;
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(
                    "RowVersion phải là Base64 hợp lệ.",
                    exception);
            }
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{parameterName} không được để trống.",
                    parameterName);
            }

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

        private IQueryable<TblEmployee> ApplyDateFilter(IQueryable<TblEmployee> query, DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                return query;
            }

            var from = fromDate?.Date ?? DateTime.MinValue;
            var to = toDate?.Date.AddDays(1) ?? DateTime.MaxValue;

            return query.Where(x => x.DateCreated >= from && x.DateCreated < to);
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
                DateModified = employee.DateModified,
                RowVersion = employee.RowVersion is { Length: > 0 }
                    ? Convert.ToBase64String(employee.RowVersion)
                    : string.Empty
            };
        }
    }
}
