namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Hành động duyệt/ký trong workflow.
    /// </summary>
    public enum ApprovalAction : byte
    {
        Approved = 0,
        Rejected = 1,
        Returned = 2
    }
}