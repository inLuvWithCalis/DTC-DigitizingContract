namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Điều khoản snapshot của version hiện hành.
    /// </summary>
    public class ContractTermDetailResponse
    {
        public int TermId { get; set; }

        public int? SourceTemplateTermId { get; set; }

        public string TermCode { get; set; } = string.Empty;

        public string TermTitle { get; set; } = string.Empty;

        public string? TermTitleEn { get; set; }

        public string? TermContent { get; set; }

        public string? TermContentEn { get; set; }

        public bool IsNegotiable { get; set; }

        public int DisplayOrder { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}