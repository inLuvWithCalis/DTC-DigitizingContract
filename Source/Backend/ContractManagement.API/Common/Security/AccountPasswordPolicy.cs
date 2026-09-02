using Microsoft.AspNetCore.Identity;

namespace ContractManagement.API.Common.Security;

public static class AccountPasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 100;

    public static void EnsureValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < MinimumLength
            || password.Length > MaximumLength)
        {
            throw new RbacOperationException(
                StatusCodes.Status400BadRequest,
                AuthorizationErrorCodes.PasswordPolicyViolation,
                $"Mật khẩu phải có từ {MinimumLength} đến {MaximumLength} ký tự.");
        }
    }

    public static PasswordVerificationResult VerifyCurrentPassword<TUser>(
        IPasswordHasher<TUser> passwordHasher,
        TUser user,
        string? currentHash,
        string? currentPassword)
        where TUser : class
    {
        if (string.IsNullOrEmpty(currentHash)
            || string.IsNullOrEmpty(currentPassword))
        {
            throw CurrentPasswordIncorrect();
        }

        var result = passwordHasher.VerifyHashedPassword(
            user,
            currentHash,
            currentPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            throw CurrentPasswordIncorrect();
        }

        return result;
    }

    public static void EnsureNotReused<TUser>(
        IPasswordHasher<TUser> passwordHasher,
        TUser user,
        string? currentHash,
        string newPassword)
        where TUser : class
    {
        EnsureValid(newPassword);
        if (!string.IsNullOrEmpty(currentHash)
            && passwordHasher.VerifyHashedPassword(
                user,
                currentHash,
                newPassword) != PasswordVerificationResult.Failed)
        {
            throw new RbacOperationException(
                StatusCodes.Status400BadRequest,
                AuthorizationErrorCodes.PasswordReuseNotAllowed,
                "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }
    }

    private static RbacOperationException CurrentPasswordIncorrect() =>
        new(
            StatusCodes.Status400BadRequest,
            AuthorizationErrorCodes.CurrentPasswordIncorrect,
            "Mật khẩu hiện tại không chính xác.");
}

public static class AccountSessionKeys
{
    public const string EmployeeId = "EmployeeId";
    public const string EmployeeName = "EmployeeName";
    public const string EmployeeSessionVersion = "EmployeeSessionVersion";
    public const string SystemAdminId = "SystemAdminId";
    public const string SystemAdminName = "SystemAdminName";
    public const string SystemAdminSessionVersion = "SystemAdminSessionVersion";
}
