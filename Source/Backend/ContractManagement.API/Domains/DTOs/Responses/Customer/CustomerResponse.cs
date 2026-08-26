namespace ContractManagement.API.Domains.DTOs.Responses.Customer
{
    /// <summary>
    /// Response trả về thông tin khách hàng.
    /// Không trả CustomerPassword ra ngoài API.
    /// </summary>
    public sealed class CustomerLookupResponse
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        public string? CustomerFullName { get; set; }

        public string? CustomerCompany { get; set; }

        public string? CustomerTaxCode { get; set; }

        public string? CustomerMobile { get; set; }

        public string? CustomerPhone { get; set; }

        public byte? Status { get; set; }
    }

    public class CustomerResponse
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        public string? CustomerFullName { get; set; }

        public string? CustomerCompany { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerMobile { get; set; }

        public string? CustomerPhone { get; set; }

        public string? CustomerFaxNumber { get; set; }

        public string? CustomerTaxCode { get; set; }

        public string? CustomerRepresentativeName { get; set; }

        public string? CustomerRepresentativeTitle { get; set; }

        public string? CustomerBankAccountNumber { get; set; }

        public string? CustomerBankName { get; set; }

        public string? CustomerAddress { get; set; }

        public string? CustomerCity { get; set; }

        public string? CustomerCountry { get; set; }

        public string? CustomerWebsite { get; set; }

        public string? CustomerNotes { get; set; }

        public byte? Status { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public int TotalContracts { get; set; }
    }
}
