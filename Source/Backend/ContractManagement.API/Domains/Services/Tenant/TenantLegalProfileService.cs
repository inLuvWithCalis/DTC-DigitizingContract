using ContractManagement.API.Domains.DTOs.Requests.LegalProfiles;
using ContractManagement.API.Domains.DTOs.Responses.LegalProfiles;
using ContractManagement.API.Domains.Interfaces.LegalProfiles;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.LegalProfiles;

public sealed class TenantLegalProfileService : ITenantLegalProfileService
{
    private const int SingletonProfileId = 1;
    private readonly DbDtctechContext _dbContext;

    public TenantLegalProfileService(DbDtctechContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantLegalProfileResponse?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TblTenantLegalProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TenantLegalProfileId == SingletonProfileId,
                cancellationToken);

        return profile is null ? null : Map(profile);
    }

    public async Task<TenantLegalProfileResponse> UpsertAsync(
        UpsertTenantLegalProfileRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (employeeId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được nhân viên đăng nhập.");
        }

        var profile = await _dbContext.TblTenantLegalProfiles
            .SingleOrDefaultAsync(
                x => x.TenantLegalProfileId == SingletonProfileId,
                cancellationToken);
        var now = DateTime.UtcNow;

        if (profile is null)
        {
            if (!string.IsNullOrWhiteSpace(request.RowVersion))
            {
                throw new DbUpdateConcurrencyException(
                    "Hồ sơ pháp lý đã thay đổi. Vui lòng tải lại dữ liệu.");
            }

            profile = new TblTenantLegalProfile
            {
                TenantLegalProfileId = SingletonProfileId,
                CreatedByEmployeeId = employeeId,
                CreatedAt = now,
                UpdatedByEmployeeId = employeeId,
                UpdatedAt = now
            };
            _dbContext.TblTenantLegalProfiles.Add(profile);
        }
        else
        {
            var expectedRowVersion = DecodeRequiredRowVersion(request.RowVersion);
            if (!profile.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
            {
                throw new DbUpdateConcurrencyException(
                    "Hồ sơ pháp lý đã được cập nhật. Vui lòng tải lại dữ liệu.");
            }

            _dbContext.Entry(profile)
                .Property(x => x.RowVersion)
                .OriginalValue = expectedRowVersion;
            profile.UpdatedByEmployeeId = employeeId;
            profile.UpdatedAt = now;
        }

        profile.LegalEntityName = NormalizeRequired(
            request.LegalEntityName,
            "Tên pháp nhân");
        profile.TaxCode = NormalizeRequired(request.TaxCode, "Mã số thuế");
        profile.Address = NormalizeRequired(request.Address, "Địa chỉ");
        profile.RepresentativeName = NormalizeRequired(
            request.RepresentativeName,
            "Người đại diện");
        profile.RepresentativeTitle = NormalizeRequired(
            request.RepresentativeTitle,
            "Chức danh người đại diện");
        profile.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        profile.FaxNumber = NormalizeOptional(request.FaxNumber);
        profile.BankAccountNumber = NormalizeOptional(request.BankAccountNumber);
        profile.BankName = NormalizeOptional(request.BankName);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new DbUpdateConcurrencyException(
                "Hồ sơ pháp lý đã được cập nhật. Vui lòng tải lại dữ liệu.",
                exception);
        }

        return Map(profile);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{fieldName} không được để trống.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static byte[] DecodeRequiredRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new DbUpdateConcurrencyException(
                "RowVersion là bắt buộc khi cập nhật hồ sơ pháp lý.");
        }

        try
        {
            var value = Convert.FromBase64String(rowVersion);
            if (value.Length != 8)
            {
                throw new FormatException();
            }

            return value;
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("RowVersion không hợp lệ.", exception);
        }
    }

    private static TenantLegalProfileResponse Map(TblTenantLegalProfile profile)
    {
        return new TenantLegalProfileResponse
        {
            TenantLegalProfileId = profile.TenantLegalProfileId,
            LegalEntityName = profile.LegalEntityName,
            TaxCode = profile.TaxCode,
            Address = profile.Address,
            RepresentativeName = profile.RepresentativeName,
            RepresentativeTitle = profile.RepresentativeTitle,
            PhoneNumber = profile.PhoneNumber,
            FaxNumber = profile.FaxNumber,
            BankAccountNumber = profile.BankAccountNumber,
            BankName = profile.BankName,
            CreatedByEmployeeId = profile.CreatedByEmployeeId,
            CreatedAt = profile.CreatedAt,
            UpdatedByEmployeeId = profile.UpdatedByEmployeeId,
            UpdatedAt = profile.UpdatedAt,
            RowVersion = Convert.ToBase64String(profile.RowVersion)
        };
    }
}
