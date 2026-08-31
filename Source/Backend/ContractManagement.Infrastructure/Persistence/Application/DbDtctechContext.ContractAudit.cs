using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private static long _syntheticRowVersionSeed = 10_000;

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RevokeCustomerAccessForCancelledContracts();
        PopulateContractAuditSnapshots();
        AssignSyntheticRowVersionsForInMemory();
        ValidateContractAuditEntries();
        ValidateContractTemplateAuditEntries();
        ValidateAuthorizationAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(
            acceptAllChangesOnSuccess: true,
            cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RevokeCustomerAccessForCancelledContracts();
        await PopulateContractAuditSnapshotsAsync(cancellationToken);
        AssignSyntheticRowVersionsForInMemory();
        ValidateContractAuditEntries();
        ValidateContractTemplateAuditEntries();
        ValidateAuthorizationAuditEntries();
        return await base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void PopulateContractAuditSnapshots() =>
        PopulateContractAuditSnapshotsAsync(CancellationToken.None, useAsync: false)
            .GetAwaiter()
            .GetResult();

    private Task PopulateContractAuditSnapshotsAsync(
        CancellationToken cancellationToken) =>
        PopulateContractAuditSnapshotsAsync(cancellationToken, useAsync: true);

    private async Task PopulateContractAuditSnapshotsAsync(
        CancellationToken cancellationToken,
        bool useAsync)
    {
        var audits = ChangeTracker.Entries<TblContractAudit>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();
        if (audits.Count == 0)
        {
            return;
        }

        var contractIds = audits.Select(audit => audit.ContractId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var contracts = ChangeTracker.Entries<TblContract>()
            .Where(entry => entry.State != EntityState.Deleted
                && contractIds.Contains(entry.Entity.ContractId))
            .Select(entry => entry.Entity)
            .ToDictionary(contract => contract.ContractId);
        var missingContractIds = contractIds.Except(contracts.Keys).ToList();
        if (missingContractIds.Count > 0)
        {
            var storedContracts = await LoadSnapshotRowsAsync(
                TblContracts.AsNoTracking()
                    .Where(contract => missingContractIds.Contains(contract.ContractId)),
                useAsync,
                cancellationToken);
            foreach (var contract in storedContracts)
            {
                contracts[contract.ContractId] = contract;
            }
        }

        var versionIds = audits.Where(audit => audit.VersionId.HasValue)
            .Select(audit => audit.VersionId!.Value)
            .Distinct()
            .ToList();
        var versions = ChangeTracker.Entries<TblContractVersion>()
            .Where(entry => entry.State != EntityState.Deleted
                && versionIds.Contains(entry.Entity.VersionId))
            .Select(entry => entry.Entity)
            .ToDictionary(version => version.VersionId);
        var missingVersionIds = versionIds.Except(versions.Keys).ToList();
        if (missingVersionIds.Count > 0)
        {
            var storedVersions = await LoadSnapshotRowsAsync(
                TblContractVersions.AsNoTracking()
                    .Where(version => missingVersionIds.Contains(version.VersionId)),
                useAsync,
                cancellationToken);
            foreach (var version in storedVersions)
            {
                versions[version.VersionId] = version;
            }
        }

        var employeeIds = audits.Where(audit => audit.ActorEmployeeId.HasValue)
            .Select(audit => audit.ActorEmployeeId!.Value)
            .Distinct()
            .ToList();
        var employees = ChangeTracker.Entries<TblEmployee>()
            .Where(entry => entry.State != EntityState.Deleted
                && employeeIds.Contains(entry.Entity.EmployeeId))
            .Select(entry => entry.Entity)
            .ToDictionary(employee => employee.EmployeeId);
        var missingEmployeeIds = employeeIds.Except(employees.Keys).ToList();
        if (missingEmployeeIds.Count > 0)
        {
            var storedEmployees = await LoadSnapshotRowsAsync(
                TblEmployees.AsNoTracking()
                    .Where(employee => missingEmployeeIds.Contains(employee.EmployeeId)),
                useAsync,
                cancellationToken);
            foreach (var employee in storedEmployees)
            {
                employees[employee.EmployeeId] = employee;
            }
        }

        var sessionIds = audits
            .Where(audit => audit.ActorCustomerAccessSessionId.HasValue)
            .Select(audit => audit.ActorCustomerAccessSessionId!.Value)
            .Distinct()
            .ToList();
        var sessions = ChangeTracker.Entries<TblContractCustomerAccessSession>()
            .Where(entry => entry.State != EntityState.Deleted
                && sessionIds.Contains(entry.Entity.CustomerAccessSessionId))
            .Select(entry => entry.Entity)
            .ToDictionary(session => session.CustomerAccessSessionId);
        var missingSessionIds = sessionIds.Except(sessions.Keys).ToList();
        if (missingSessionIds.Count > 0)
        {
            var storedSessions = await LoadSnapshotRowsAsync(
                TblContractCustomerAccessSessions.AsNoTracking()
                    .Where(session => missingSessionIds.Contains(
                        session.CustomerAccessSessionId)),
                useAsync,
                cancellationToken);
            foreach (var session in storedSessions)
            {
                sessions[session.CustomerAccessSessionId] = session;
            }
        }

        var customerIds = contracts.Values.Select(contract => contract.CustomerId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var customers = ChangeTracker.Entries<TblCustomer>()
            .Where(entry => entry.State != EntityState.Deleted
                && customerIds.Contains(entry.Entity.CustomerId))
            .Select(entry => entry.Entity)
            .ToDictionary(customer => customer.CustomerId);
        var missingCustomerIds = customerIds.Except(customers.Keys).ToList();
        if (missingCustomerIds.Count > 0)
        {
            var storedCustomers = await LoadSnapshotRowsAsync(
                TblCustomers.AsNoTracking()
                    .Where(customer => missingCustomerIds.Contains(customer.CustomerId)),
                useAsync,
                cancellationToken);
            foreach (var customer in storedCustomers)
            {
                customers[customer.CustomerId] = customer;
            }
        }

        var verificationPhoneIds = sessions.Values
            .Select(session => session.VerificationPhoneId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var verificationPhones = ChangeTracker
            .Entries<TblContractCustomerVerificationPhone>()
            .Where(entry => entry.State != EntityState.Deleted
                && verificationPhoneIds.Contains(entry.Entity.VerificationPhoneId))
            .Select(entry => entry.Entity)
            .ToDictionary(phone => phone.VerificationPhoneId);
        var missingPhoneIds = verificationPhoneIds
            .Except(verificationPhones.Keys)
            .ToList();
        if (missingPhoneIds.Count > 0)
        {
            var storedPhones = await LoadSnapshotRowsAsync(
                TblContractCustomerVerificationPhones.AsNoTracking()
                    .Where(phone => missingPhoneIds.Contains(phone.VerificationPhoneId)),
                useAsync,
                cancellationToken);
            foreach (var phone in storedPhones)
            {
                verificationPhones[phone.VerificationPhoneId] = phone;
            }
        }

        foreach (var audit in audits)
        {
            contracts.TryGetValue(audit.ContractId, out var contract);
            if (contract is not null)
            {
                audit.ContractCodeSnapshot ??= NormalizeAuditSnapshot(
                    contract.ContractCode,
                    50);
                audit.ContractNameSnapshot ??= NormalizeAuditSnapshot(
                    contract.ContractName,
                    1000);
            }

            if (audit.VersionId.HasValue
                && versions.TryGetValue(audit.VersionId.Value, out var version))
            {
                audit.VersionNoSnapshot ??= version.VersionNo;
            }

            if (audit.ActorEmployeeId.HasValue
                && employees.TryGetValue(audit.ActorEmployeeId.Value, out var employee))
            {
                audit.ActorDisplayNameSnapshot ??= NormalizeAuditSnapshot(
                    FirstAuditSnapshotValue(
                        employee.EmployeeFullName,
                        employee.EmployeeCode,
                        employee.EmployeeAccount),
                    1000);
            }

            if (!audit.ActorCustomerAccessSessionId.HasValue
                || !sessions.TryGetValue(
                    audit.ActorCustomerAccessSessionId.Value,
                    out var session))
            {
                continue;
            }

            if (contract is not null
                && customers.TryGetValue(contract.CustomerId, out var customer))
            {
                audit.ActorDisplayNameSnapshot ??= NormalizeAuditSnapshot(
                    FirstAuditSnapshotValue(
                        customer.CustomerFullName,
                        customer.CustomerCompany,
                        customer.CustomerRepresentativeName,
                        customer.CustomerCode),
                    1000);
            }

            if (verificationPhones.TryGetValue(
                    session.VerificationPhoneId,
                    out var phone))
            {
                audit.ActorMaskedPhoneSnapshot ??= MaskAuditPhone(
                    phone.PhoneNumberNormalized);
                audit.ActorPhoneSourceSnapshot ??= NormalizeAuditSnapshot(
                    phone.PhoneSource,
                    32);
            }
        }
    }

    private static async Task<List<T>> LoadSnapshotRowsAsync<T>(
        IQueryable<T> query,
        bool useAsync,
        CancellationToken cancellationToken) where T : class =>
        useAsync
            ? await query.ToListAsync(cancellationToken)
            : query.ToList();

    private static string? FirstAuditSnapshotValue(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? NormalizeAuditSnapshot(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? MaskAuditPhone(string? normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var visible = Math.Min(4, normalized.Length);
        return new string('*', normalized.Length - visible) + normalized[^visible..];
    }

    private void AssignSyntheticRowVersionsForInMemory()
    {
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries()
                     .Where(x => x.State == EntityState.Added
                         && x.Metadata.FindProperty("RowVersion") is not null))
        {
            var property = entry.Property("RowVersion");
            if (property.CurrentValue is not byte[] { Length: 8 })
            {
                property.CurrentValue = BitConverter.GetBytes(
                    Interlocked.Increment(ref _syntheticRowVersionSeed));
            }
        }

        foreach (var entry in ChangeTracker.Entries<TblEmployee>()
                     .Where(x => x.State == EntityState.Modified))
        {
            entry.Entity.RowVersion = BitConverter.GetBytes(
                Interlocked.Increment(ref _syntheticRowVersionSeed));
        }
    }

    private void RevokeCustomerAccessForCancelledContracts()
    {
        var cancelledContracts = ChangeTracker.Entries<TblContract>()
            .Where(entry => entry.State == EntityState.Modified
                && entry.Entity.Status == 6
                && entry.OriginalValues.GetValue<byte>(nameof(TblContract.Status)) != 6)
            .Select(entry => entry.Entity)
            .ToList();

        if (cancelledContracts.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var contract in cancelledContracts)
        {
            var actorEmployeeId = contract.UpdatedEmployeeId ?? contract.EmployeeId;
            var links = TblContractCustomerAccessLinks
                .Where(link => link.ContractId == contract.ContractId
                    && link.RevokedAt == null)
                .ToList();
            foreach (var link in links)
            {
                link.RevokedAt = now;
                link.RevokedByEmployeeId = actorEmployeeId;
                link.RevocationReason = "Contract cancelled";

                foreach (var challenge in TblContractCustomerOtpChallenges
                             .Where(challenge => challenge.LinkId
                                 == link.CustomerAccessLinkId
                                 && challenge.InvalidatedAt == null)
                             .ToList())
                {
                    challenge.InvalidatedAt = now;
                }

                foreach (var session in TblContractCustomerAccessSessions
                             .Where(session => session.LinkId
                                 == link.CustomerAccessLinkId
                                 && session.RevokedAt == null)
                             .ToList())
                {
                    session.RevokedAt = now;
                    session.RevocationReason = "Contract cancelled";
                }
            }

            contract.CurrentCustomerAccessLinkId = null;
        }
    }

    private void ValidateContractAuditEntries()
    {
        var changedEntries = ChangeTracker
            .Entries<TblContractAudit>()
            .Where(entry =>
                entry.State == EntityState.Modified
                || entry.State == EntityState.Deleted)
            .ToList();

        if (changedEntries.Count > 0)
        {
            throw new InvalidOperationException(
                "Contract audit là dữ liệu append-only và không được sửa hoặc xóa.");
        }

        foreach (var entry in ChangeTracker
                     .Entries<TblContractAudit>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            ValidateNewContractAudit(entry.Entity);
        }
    }

    private void ValidateAuthorizationAuditEntries()
    {
        if (ChangeTracker.Entries<TblAuthorizationAudit>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Authorization audit is append-only and cannot be updated or deleted.");
        }
    }

    private static void ValidateNewContractAudit(TblContractAudit audit)
    {
        if (audit.TenantId <= 0
            || audit.ContractId <= 0
            || audit.VersionId is <= 0)
        {
            throw new InvalidOperationException(
                "Contract audit phải tham chiếu Tenant, Contract và Version hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(audit.ActorType))
        {
            throw new InvalidOperationException(
                "Contract audit phải có ActorType.");
        }

        var hasSubjectType = audit.SubjectType is not null;
        var hasSubjectId = audit.SubjectId.HasValue;
        if (hasSubjectType != hasSubjectId
            || (hasSubjectType
                && (string.IsNullOrWhiteSpace(audit.SubjectType)
                    || audit.SubjectId is <= 0
                    || audit.SubjectType is not (
                        "Contract"
                        or "ContractVersion"
                        or "NegotiationComment"
                        or "CustomerAccessLink"
                        or "CustomerOtpChallenge"
                        or "CustomerAccessSession"
                        or "ApprovalRequest"))))
        {
            throw new InvalidOperationException(
                "Contract audit subject phải nhất quán và hợp lệ.");
        }

        var isEmployeeActor = string.Equals(
            audit.ActorType,
            "Employee",
            StringComparison.Ordinal);

        if (isEmployeeActor && audit.ActorEmployeeId is not > 0)
        {
            throw new InvalidOperationException(
                "Employee audit actor phải có ActorEmployeeId hợp lệ.");
        }

        if (!isEmployeeActor && audit.ActorEmployeeId.HasValue)
        {
            throw new InvalidOperationException(
                "Non-employee audit actor không được có ActorEmployeeId.");
        }

        var isCustomerActor = string.Equals(
            audit.ActorType,
            "Customer",
            StringComparison.Ordinal);
        var isSystemActor = string.Equals(
            audit.ActorType,
            "System",
            StringComparison.Ordinal);

        if (!isEmployeeActor && !isCustomerActor && !isSystemActor)
        {
            throw new InvalidOperationException(
                "Contract audit actor type is invalid.");
        }

        if ((isEmployeeActor && audit.ActorCustomerAccessSessionId.HasValue)
            || (isCustomerActor
                && audit.ActorCustomerAccessSessionId is not > 0)
            || (isSystemActor
                && audit.ActorCustomerAccessSessionId.HasValue))
        {
            throw new InvalidOperationException(
                "Contract audit actor is inconsistent.");
        }

        if (audit.PreviousResponsibleEmployeeId is <= 0
            || audit.NewResponsibleEmployeeId is <= 0)
        {
            throw new InvalidOperationException(
                "Responsible employee trong Contract audit phải hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(audit.ActionType)
            || string.IsNullOrWhiteSpace(audit.Result)
            || string.IsNullOrWhiteSpace(audit.CorrelationId))
        {
            throw new InvalidOperationException(
                "Contract audit phải có action, result và correlation.");
        }

        ValidateJsonObject(audit.PreviousValuesJson, "PreviousValuesJson");
        ValidateJsonObject(audit.NewValuesJson, "NewValuesJson");

        if (audit.FailureCode is not null
            && string.IsNullOrWhiteSpace(audit.FailureCode))
        {
            throw new InvalidOperationException(
                "Contract audit FailureCode không được rỗng.");
        }

        if (audit.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "Contract audit phải sử dụng timestamp UTC.");
        }
    }

    private static void ValidateJsonObject(string? json, string fieldName)
    {
        if (json is null)
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Contract audit {fieldName} must be a JSON object.");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Contract audit {fieldName} is invalid JSON.",
                exception);
        }
    }
}
