using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Catalog
{
    /// <summary>
    /// Service xử lý nghiệp vụ danh mục sản phẩm.
    /// Lưu ý:
    /// - DbDtctechContext đã trỏ đúng tenant DB hiện tại.
    /// - DB không dùng FK nên phải tự check logic trong service.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly DbDtctechContext _dbContext;

        public CategoryService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CategoryResponse>> GetAllAsync()
        {
            var categories = await _dbContext.TblCategories
                .AsNoTracking()
                .OrderBy(x => x.CategoryOrder)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();

            return await MapListAsync(categories);
        }

        public async Task<CategoryResponse> GetByIdAsync(byte id)
        {
            var category = await _dbContext.TblCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh mục.");
            }

            var productCount = await _dbContext.TblProducts
                .AsNoTracking()
                .CountAsync(x => x.CategoryId == id);

            return MapToResponse(category, productCount);
        }

        public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
        {
            // Check trùng tên danh mục trong cùng tenant DB.
            var name = request.CategoryName.Trim();

            var nameExists = await _dbContext.TblCategories
                .AnyAsync(x => x.CategoryName == name);

            if (nameExists)
            {
                throw new InvalidOperationException("Tên danh mục đã tồn tại.");
            }

            // Nếu có danh mục cha thì phải check tồn tại.
            await ValidateParentCategoryAsync(request.CategoryParentId);

            var category = new TblCategory
            {
                CategoryName = name,
                CategoryShortDesc = request.CategoryShortDesc?.Trim(),
                CategoryOrder = request.CategoryOrder,
                CategoryParentId = request.CategoryParentId,
                LangId = request.LangId,
                Image = request.Image?.Trim()
            };

            _dbContext.TblCategories.Add(category);
            await _dbContext.SaveChangesAsync();

            return MapToResponse(category, 0);
        }

        public async Task UpdateAsync(byte id, UpdateCategoryRequest request)
        {
            var category = await _dbContext.TblCategories
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh mục.");
            }

            // Không cho danh mục tự chọn chính nó làm cha.
            if (request.CategoryParentId == id)
            {
                throw new InvalidOperationException("Danh mục cha không hợp lệ.");
            }

            await ValidateParentCategoryAsync(request.CategoryParentId);

            category.CategoryName = request.CategoryName.Trim();
            category.CategoryShortDesc = request.CategoryShortDesc?.Trim();
            category.CategoryOrder = request.CategoryOrder;
            category.CategoryParentId = request.CategoryParentId;
            category.LangId = request.LangId;
            category.Image = request.Image?.Trim();

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(byte id)
        {
            var category = await _dbContext.TblCategories
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh mục.");
            }

            // Vì DB không có FK, service tự check Product đang dùng Category này.
            var hasProducts = await _dbContext.TblProducts
                .AnyAsync(x => x.CategoryId == id);

            if (hasProducts)
            {
                throw new InvalidOperationException(
                    "Không thể xóa danh mục vì đang có sản phẩm sử dụng.");
            }

            _dbContext.TblCategories.Remove(category);
            await _dbContext.SaveChangesAsync();
        }

        private async Task ValidateParentCategoryAsync(byte? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var exists = await _dbContext.TblCategories
                .AnyAsync(x => x.CategoryId == parentId.Value);

            if (!exists)
            {
                throw new KeyNotFoundException("Danh mục cha không tồn tại.");
            }
        }

        private async Task<List<CategoryResponse>> MapListAsync(
            List<TblCategory> categories)
        {
            var categoryIds = categories
                .Select(x => (int)x.CategoryId)
                .ToList();

            var productCounts = await _dbContext.TblProducts
                .AsNoTracking()
                .Where(x => x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value))
                .GroupBy(x => x.CategoryId!.Value)
                .ToDictionaryAsync(x => x.Key, x => x.Count());

            return categories
                .Select(x => MapToResponse(
                    x,
                    productCounts.TryGetValue(x.CategoryId, out var count)
                        ? count
                        : 0))
                .ToList();
        }

        private static CategoryResponse MapToResponse(
            TblCategory category,
            int productCount)
        {
            return new CategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                CategoryShortDesc = category.CategoryShortDesc,
                CategoryOrder = category.CategoryOrder,
                CategoryParentId = category.CategoryParentId,
                LangId = category.LangId,
                Image = category.Image,
                ProductCount = productCount
            };
        }
    }
}