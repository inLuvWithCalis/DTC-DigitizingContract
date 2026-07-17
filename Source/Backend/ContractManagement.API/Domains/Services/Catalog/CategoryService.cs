using ContractManagement.API.Common.Responses;
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

        public async Task<PagedResult<CategoryResponse>> GetListAsync(
            CategoryFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query = _dbContext.TblCategories
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.CategoryName != null && x.CategoryName.Contains(keyword)) ||
                    (x.CategoryShortDesc != null && x.CategoryShortDesc.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var categories = await query
                .OrderBy(x => x.CategoryOrder)
                .ThenBy(x => x.CategoryName)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var mapped = await MapListAsync(categories);

            return new PagedResult<CategoryResponse>
            {
                Items = mapped,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<PagedResult<CategoryResponse>> GetParentsAsync(CategoryFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var allCategories = await _dbContext.TblCategories
                .AsNoTracking()
                .OrderBy(x => x.CategoryOrder)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();

            var childrenByParent = allCategories
                .Where(x => x.CategoryParentId.HasValue)
                .GroupBy(x => x.CategoryParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var parentCategories = allCategories
                .Where(x => !x.CategoryParentId.HasValue)
                .ToList();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                parentCategories = parentCategories.Where(x =>
                    (x.CategoryName != null && x.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (x.CategoryShortDesc != null && x.CategoryShortDesc.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (childrenByParent.ContainsKey(x.CategoryId) && childrenByParent[x.CategoryId].Any(c =>
                        (c.CategoryName != null && c.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.CategoryShortDesc != null && c.CategoryShortDesc.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ))
                ).ToList();
            }

            var totalCount = parentCategories.Count;

            var pagedParents = parentCategories
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var relevantIds = pagedParents.Select(p => (int)p.CategoryId).ToList();
            
            foreach (var parent in pagedParents)
            {
                if (childrenByParent.TryGetValue(parent.CategoryId, out var children))
                {
                    relevantIds.AddRange(children.Select(c => (int)c.CategoryId));
                }
            }
            
            relevantIds = relevantIds.Distinct().ToList();

            var productCounts = await _dbContext.TblProducts
                .AsNoTracking()
                .Where(x => x.CategoryId.HasValue && relevantIds.Contains(x.CategoryId.Value))
                .GroupBy(x => x.CategoryId!.Value)
                .ToDictionaryAsync(x => x.Key, x => x.Count());

            var items = pagedParents.Select(parent =>
            {
                var resp = MapToResponse(
                    parent,
                    productCounts.TryGetValue(parent.CategoryId, out var pc) ? pc : 0);

                if (childrenByParent.TryGetValue(parent.CategoryId, out var children))
                {
                    resp.Items = children
                        .Select(child => MapToResponse(
                            child,
                            productCounts.TryGetValue(child.CategoryId, out var cc) ? cc : 0))
                        .ToList();
                }
                else
                {
                    resp.Items = new List<CategoryResponse>();  
                }

                return resp;
            }).ToList();

            return new PagedResult<CategoryResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
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

            var idsToDelete = new List<byte> { id };
            var currentLevelIds = new List<byte> { id };

            while (true)
            {
                var nextLevelIds = await _dbContext.TblCategories
                    .Where(x => x.CategoryParentId.HasValue && currentLevelIds.Contains(x.CategoryParentId.Value))
                    .Select(x => x.CategoryId)
                    .ToListAsync();

                if (!nextLevelIds.Any())
                    break;

                idsToDelete.AddRange(nextLevelIds);
                currentLevelIds = nextLevelIds;
            }

            var intIdsToDelete = idsToDelete.Select(x => (int)x).ToList();
            var hasProducts = await _dbContext.TblProducts
                .AnyAsync(x => x.CategoryId.HasValue && intIdsToDelete.Contains(x.CategoryId.Value));

            if (hasProducts)
            {
                throw new InvalidOperationException(
                    "Không thể xóa vì danh mục này (hoặc danh mục con của nó) đang chứa sản phẩm.");
            }

            var categoriesToDelete = await _dbContext.TblCategories
                .Where(x => idsToDelete.Contains(x.CategoryId))
                .ToListAsync();

            _dbContext.TblCategories.RemoveRange(categoriesToDelete);
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