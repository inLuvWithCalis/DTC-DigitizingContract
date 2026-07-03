using ContractManagement.Data;
using ContractManagement.Domains.DTOs.Requests;
using ContractManagement.Domains.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DbDtctechContext _dbContext;
        public CustomerService (DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<CustomerResponseDto>> GetAllCustomerAsync()
        {
            
            var customer = await _dbContext.TblCustomers.Select(c => new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                CustomerFullName = c.CustomerFullName,
                CustomerCode = c.CustomerCode,
                CustomerEmail = c.CustomerEmail,
                CustomerMobile = c.CustomerMobile
            }).AsNoTracking().ToListAsync(); 
            return customer;
        }
    }
}
