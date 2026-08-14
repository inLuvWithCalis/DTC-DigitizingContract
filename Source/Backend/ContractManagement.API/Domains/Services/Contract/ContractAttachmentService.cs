using ContractManagement.Common.Enums;
using ContractManagement.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract
{
    /// <summary>
    /// Service xử lý file đính kèm hợp đồng.
    /// 
    /// Ghi chú:
    /// - File vật lý và metadata chung vẫn lưu qua FileStorageService.
    /// - Metadata riêng của hợp đồng lưu thêm vào tbl_ContractAttachment.
    /// </summary>
    public class ContractAttachmentService : IContractAttachmentService
    {
        private readonly DbDtctechContext _dbContext;
        private readonly IFileStorageService _fileStorageService;
        private readonly IContractResourceAuthorizationService _contractAuthorization;

        public ContractAttachmentService(
            DbDtctechContext dbContext,
            IFileStorageService fileStorageService,
            IContractResourceAuthorizationService contractAuthorization)
        {
            _dbContext = dbContext;
            _fileStorageService = fileStorageService;
            _contractAuthorization = contractAuthorization;
        }

        public async Task<ContractAttachmentResponse> UploadAsync(
            int contractId,
            UploadContractAttachmentRequest request,
            int uploadedBy)
        {
            await _contractAuthorization.EnsureCanWriteAsync(
                contractId,
                uploadedBy);

            // 2. Check DocumentType có hợp lệ theo enum không.
            if (!Enum.IsDefined(typeof(DocumentType), request.DocumentType))
            {
                throw new ArgumentException("DocumentType không hợp lệ.");
            }

            // 3. Upload file vào FileStorage generic.
            // Backend tự gán objectType = Contract, objectId = contractId.
            var uploadedFile = await _fileStorageService.UploadAsync(
                request.File,
                "Contract",
                contractId,
                uploadedBy);

            // 4. Lưu metadata riêng cho hợp đồng.
            var attachment = new TblContractAttachment
            {
                ContractId = contractId,
                ContractFileName = uploadedFile.FileName,
                ContractFilePath = uploadedFile.FilePath,
                DocumentType = request.DocumentType,
                UploadDate = DateTime.Now,
                UploadEmployeeId = uploadedBy
            };

            _dbContext.TblContractAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            return MapToResponse(attachment);
        }

        public async Task<List<ContractAttachmentResponse>> GetByContractAsync(
            int contractId,
            int employeeId)
        {
            await _contractAuthorization.EnsureCanReadAsync(
                contractId,
                employeeId);

            var attachments = await _dbContext.TblContractAttachments
                .AsNoTracking()
                .Where(x => x.ContractId == contractId)
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            return attachments.Select(MapToResponse).ToList();
        }

        public async Task DeleteAsync(
            int contractId,
            int attachmentId,
            int employeeId)
        {
            await _contractAuthorization.EnsureCanWriteAsync(
                contractId,
                employeeId);

            var attachment = await _dbContext.TblContractAttachments
                .FirstOrDefaultAsync(x =>
                    x.AttachmentId == attachmentId &&
                    x.ContractId == contractId);

            if (attachment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy file đính kèm.");
            }

            // Nếu file này cũng có record trong tbl_FileStorage,
            // xóa qua FileStorageService để xóa cả file vật lý.
            var fileStorage = await _dbContext.TblFileStorages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FilePath == attachment.ContractFilePath);

            if (fileStorage != null)
            {
                await _fileStorageService.DeleteAsync(fileStorage.FileId);
            }

            // Xóa metadata riêng trong tbl_ContractAttachment.
            _dbContext.TblContractAttachments.Remove(attachment);
            await _dbContext.SaveChangesAsync();
        }

        private static ContractAttachmentResponse MapToResponse(
            TblContractAttachment attachment)
        {
            return new ContractAttachmentResponse
            {
                AttachmentId = attachment.AttachmentId,
                ContractId = attachment.ContractId,
                ContractFileName = attachment.ContractFileName,
                ContractFilePath = attachment.ContractFilePath,
                DocumentType = attachment.DocumentType,
                DocumentTypeName = Enum.IsDefined(typeof(DocumentType), attachment.DocumentType)
                    ? ((DocumentType)attachment.DocumentType).ToString()
                    : "Unknown",
                UploadDate = attachment.UploadDate,
                UploadEmployeeId = attachment.UploadEmployeeId
            };
        }
    }
}
