using ContractManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Data
{
    public class SeedData
    {
        private readonly DbDtctechContext _dtctechContext;
        private readonly IPasswordHasher<TblEmployee> _passwordHasher;

        public SeedData(DbDtctechContext dtctechContext, IPasswordHasher<TblEmployee> passwordHasher)
        {
            _dtctechContext = dtctechContext;
            _passwordHasher = passwordHasher;
        }

        public async Task InitializeAsync()
        {
            // 1. Get the current database context and check if the admin account already exists
            var accountExists = await _dtctechContext.TblEmployees.AnyAsync(e => e.EmployeeAccount == "admin");

            if (accountExists)
            {
                return;

            }

            // 2. Create a new admin employee account`
            var adminEmployee = new TblEmployee
            {
                EmployeeAccount = "admin",
                EmployeeFullName = "Administrator",
                EmployeeEmail = "admin@example.com"
            };

            adminEmployee.EmployeePassword = _passwordHasher.HashPassword(adminEmployee, "123456");

            // 3. Add the new admin employee to the database context
            _dtctechContext.TblEmployees.Add(adminEmployee);
            await _dtctechContext.SaveChangesAsync();
        }
    }
}
