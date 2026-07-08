using ContractManagement.API.Domains.DTOs.Requests.Department;
using ContractManagement.API.Domains.DTOs.Responses.Department;
using ContractManagement.API.Domains.Interfaces.Department;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Department
{
    public class DepartmentService : IDepartmentService
    {
        private readonly DbDtctechContext _dbContext;

        public DepartmentService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<DepartmentResponse>> GetAllAsync()
        {
            var departments = await _dbContext.TblDepartments
                .AsNoTracking()
                .OrderBy(x => x.DepartmentName)
                .ToListAsync();

            return departments.Select(MapToResponse).ToList();
        }

        public async Task<DepartmentResponse> GetByIdAsync(short id)
        {
            var department = await _dbContext.TblDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DepartmentId == id);

            if (department == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng ban.");
            }

            return MapToResponse(department);
        }

        public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
        {
            // Check trùng mã phòng ban trong tenant DB hiện tại
            var codeExists = await _dbContext.TblDepartments
                .AnyAsync(x => x.DepartmentCode == request.DepartmentCode);

            if (codeExists)
            {
                throw new InvalidOperationException("Mã phòng ban đã tồn tại.");
            }

            var department = new TblDepartment
            {
                DepartmentCode = request.DepartmentCode.Trim(),
                DepartmentName = request.DepartmentName.Trim(),
                LangId = request.LangId,
                ModifiedDate = DateTime.Now,

                // Entity đang tên là Stutus, không phải Status.
                // Quy ước: 1 = Active.
                Stutus = 1
            };

            _dbContext.TblDepartments.Add(department);
            await _dbContext.SaveChangesAsync();

            return MapToResponse(department);
        }

        public async Task UpdateAsync(short id, UpdateDepartmentRequest request)
        {
            var department = await _dbContext.TblDepartments
                .FirstOrDefaultAsync(x => x.DepartmentId == id);

            if (department == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng ban.");
            }

            department.DepartmentName = request.DepartmentName.Trim();
            department.LangId = request.LangId;
            department.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task SetStatusAsync(short id, byte status)
        {
            if (status is not 0 and not 1)
            {
                throw new ArgumentException("Trạng thái phòng ban không hợp lệ. Chỉ nhận 0 hoặc 1.");
            }

            var department = await _dbContext.TblDepartments
                .FirstOrDefaultAsync(x => x.DepartmentId == id);

            if (department == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng ban.");
            }

            department.Stutus = status;
            department.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        private static DepartmentResponse MapToResponse(TblDepartment department)
        {
            return new DepartmentResponse
            {
                DepartmentId = department.DepartmentId,
                DepartmentCode = department.DepartmentCode,
                DepartmentName = department.DepartmentName,
                ModifiedDate = department.ModifiedDate,
                Status = department.Stutus,
                LangId = department.LangId
            };
        }
    }
}
