using ContractManagement.Common.Enums;
using ContractManagement.Domains.DTOs.Requests.Contract;
using ContractManagement.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.DTOs.Responses.File;
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
        private readonly IContractAuditWriter _contractAuditWriter;

        public ContractAttachmentService(
            DbDtctechContext dbContext,
            IFileStorageService fileStorageService,
            IContractResourceAuthorizationService contractAuthorization,
            IContractAuditWriter contractAuditWriter)
        {
            _dbContext = dbContext;
            _fileStorageService = fileStorageService;
            _contractAuthorization = contractAuthorization;
            _contractAuditWriter = contractAuditWriter;
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

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                FileStorageResponse? uploadedFile = null;
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync();
                try
                {
                    // Backend tự gán objectType = Contract, objectId = contractId.
                    uploadedFile = await _fileStorageService.UploadAsync(
                        request.File,
                        "Contract",
                        contractId,
                        uploadedBy);

                    var now = DateTime.UtcNow;
                    var attachment = new TblContractAttachment
                    {
                        ContractId = contractId,
                        ContractFileName = uploadedFile.FileName,
                        ContractFilePath = uploadedFile.FilePath,
                        DocumentType = request.DocumentType,
                        UploadDate = now,
                        UploadEmployeeId = uploadedBy
                    };

                    _dbContext.TblContractAttachments.Add(attachment);
                    await _dbContext.SaveChangesAsync();

                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contractId,
                            null,
                            uploadedBy,
                            ContractAuditActionTypes.ContractAttachmentUploaded,
                            ContractAuditResults.Succeeded,
                            now,
                            SubjectType:
                                ContractAuditSubjectTypes.Contract,
                            SubjectId: contractId,
                            NewValues: ContractAuditValues.Create(
                                ("AttachmentId", attachment.AttachmentId),
                                ("FileName", attachment.ContractFileName),
                                ("DocumentType", attachment.DocumentType),
                                ("UploadDate", attachment.UploadDate)))
                    ]);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return MapToResponse(attachment);
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch
                    {
                        // Giữ lại exception gốc để execution strategy quyết định retry.
                    }

                    _dbContext.ChangeTracker.Clear();
                    if (uploadedFile is not null)
                    {
                        await _fileStorageService.DeleteUploadedArtifactAsync(
                            uploadedFile);
                    }

                    throw;
                }
            });
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

        public async Task<(Stream Stream, string FileName)?> DownloadAsync(
            int contractId,
            int attachmentId,
            int employeeId)
        {
            await _contractAuthorization.EnsureCanReadAsync(contractId, employeeId);

            var attachment = await _dbContext.TblContractAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.ContractId == contractId
                    && candidate.AttachmentId == attachmentId);
            if (attachment is null)
            {
                return null;
            }

            var fileId = await _dbContext.TblFileStorages
                .AsNoTracking()
                .Where(file => file.FilePath == attachment.ContractFilePath)
                .Select(file => (int?)file.FileId)
                .FirstOrDefaultAsync();
            return fileId.HasValue
                ? await _fileStorageService.DownloadAsync(fileId.Value)
                : null;
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

            var now = DateTime.UtcNow;
            _dbContext.TblContractAttachments.Remove(attachment);
            _contractAuditWriter.StageEmployeeAudits(
            [
                new EmployeeContractAuditWriteRequest(
                    contractId,
                    null,
                    employeeId,
                    ContractAuditActionTypes.ContractAttachmentDeleted,
                    ContractAuditResults.Succeeded,
                    now,
                    SubjectType:
                        ContractAuditSubjectTypes.Contract,
                    SubjectId: contractId,
                    PreviousValues: ContractAuditValues.Create(
                        ("AttachmentId", attachment.AttachmentId),
                        ("FileName", attachment.ContractFileName),
                        ("DocumentType", attachment.DocumentType),
                        ("UploadDate", attachment.UploadDate)))
            ]);

            if (fileStorage != null)
            {
                await _fileStorageService.DeleteAsync(fileStorage.FileId);
            }

            // FileStorageService có thể đã SaveChanges; lệnh này xử lý trường
            // hợp file metadata không tồn tại và giữ ý nghĩa rõ ràng cho service.
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
