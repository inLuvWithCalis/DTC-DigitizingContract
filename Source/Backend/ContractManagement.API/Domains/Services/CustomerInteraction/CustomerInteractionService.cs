using ContractManagement.API.Domains.DTOs.Requests.CustomerInteraction;
using ContractManagement.API.Domains.DTOs.Responses.CustomerInteraction;
using ContractManagement.API.Domains.Interfaces.CustomerInteraction;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.CustomerInteraction
{
    public class CustomerInteractionService : ICustomerInteractionService
    {
        private readonly DbDtctechContext _dbContext;

        public CustomerInteractionService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CustomerInteractionResponse> CreateAsync(
            int customerId,
            CreateCustomerInteractionRequest request,
            int employeeId)
        {
            // Vì DB không dùng FK, service tự check khách hàng tồn tại.
            var customerExists = await _dbContext.TblCustomers
                .AnyAsync(x => x.CustomerId == customerId);

            if (!customerExists)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            ValidateInteractionType(request.InteractionType);

            var interaction = new TblCustomerInteraction
            {
                CustomerId = customerId,
                EmployeeId = employeeId,
                InteractionDate = DateTime.Now,
                InteractionType = request.InteractionType.Trim(),
                InteractionSubject = request.InteractionSubject?.Trim(),
                Content = request.Content?.Trim(),
                NextFollowUpDate = request.NextFollowUpDate
            };

            _dbContext.TblCustomerInteractions.Add(interaction);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(interaction.InteractionId);
        }

        public async Task<List<CustomerInteractionResponse>> GetByCustomerAsync(
            int customerId)
        {
            var customerExists = await _dbContext.TblCustomers
                .AnyAsync(x => x.CustomerId == customerId);

            if (!customerExists)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            var interactions = await _dbContext.TblCustomerInteractions
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.InteractionDate)
                .ToListAsync();

            return await MapListAsync(interactions);
        }

        public async Task UpdateAsync(
            int customerId,
            int interactionId,
            UpdateCustomerInteractionRequest request)
        {
            ValidateInteractionType(request.InteractionType);

            var interaction = await _dbContext.TblCustomerInteractions
                .FirstOrDefaultAsync(x =>
                    x.InteractionId == interactionId &&
                    x.CustomerId == customerId);

            if (interaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lịch sử tương tác.");
            }

            interaction.InteractionType = request.InteractionType.Trim();
            interaction.InteractionSubject = request.InteractionSubject?.Trim();
            interaction.Content = request.Content?.Trim();
            interaction.NextFollowUpDate = request.NextFollowUpDate;

            await _dbContext.SaveChangesAsync();
        }

        private async Task<CustomerInteractionResponse> GetByIdAsync(int interactionId)
        {
            var interaction = await _dbContext.TblCustomerInteractions
                .AsNoTracking()
                .FirstAsync(x => x.InteractionId == interactionId);

            var employeeName = await _dbContext.TblEmployees
                .AsNoTracking()
                .Where(x => x.EmployeeId == interaction.EmployeeId)
                .Select(x => x.EmployeeFullName)
                .FirstOrDefaultAsync();

            return MapToResponse(interaction, employeeName);
        }

        private async Task<List<CustomerInteractionResponse>> MapListAsync(
            List<TblCustomerInteraction> interactions)
        {
            var employeeIds = interactions
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToList();

            var employees = await _dbContext.TblEmployees
                .AsNoTracking()
                .Where(x => employeeIds.Contains(x.EmployeeId))
                .ToDictionaryAsync(
                    x => x.EmployeeId,
                    x => x.EmployeeFullName);

            return interactions
                .Select(x => MapToResponse(
                    x,
                    employees.TryGetValue(x.EmployeeId, out var name)
                        ? name
                        : null))
                .ToList();
        }

        private static CustomerInteractionResponse MapToResponse(
            TblCustomerInteraction interaction,
            string? employeeName)
        {
            return new CustomerInteractionResponse
            {
                InteractionId = interaction.InteractionId,
                CustomerId = interaction.CustomerId,
                EmployeeId = interaction.EmployeeId,
                EmployeeName = employeeName,
                InteractionDate = interaction.InteractionDate,
                InteractionType = interaction.InteractionType,
                InteractionSubject = interaction.InteractionSubject,
                Content = interaction.Content,
                NextFollowUpDate = interaction.NextFollowUpDate
            };
        }

        private static void ValidateInteractionType(string interactionType)
        {
            var allowedTypes = new[] { "Call", "Email", "Meeting", "Zalo" };

            if (!allowedTypes.Contains(interactionType))
            {
                throw new ArgumentException(
                    "InteractionType không hợp lệ. Chỉ nhận: Call, Email, Meeting, Zalo.");
            }
        }
    }
}