using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblCustomer
{
    public int CustomerId { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerAccount { get; set; }

    public string? CustomerPassword { get; set; }

    public string? CustomerFullName { get; set; }

    public string? CustomerCompany { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerMobile { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerFaxNumber { get; set; }

    public string? CustomerTaxCode { get; set; }

    /// <summary>
    /// Người đại diện được dùng trong nội dung pháp lý của hợp đồng.
    /// Không suy diễn trường này từ người liên hệ sau khi version đã submit.
    /// </summary>
    public string? CustomerRepresentativeName { get; set; }

    public string? CustomerRepresentativeTitle { get; set; }

    public string? CustomerBankAccountNumber { get; set; }

    public string? CustomerBankName { get; set; }

    public string? CustomerCity { get; set; }

    public string? CustomerZipCode { get; set; }

    public string? CustomerCountry { get; set; }

    public int? UserCreated { get; set; }

    public int? UserModified { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public byte? Status { get; set; }

    public int? CustomerPoints { get; set; }

    public int? CustomerCardId { get; set; }

    public short? CustomerRegion { get; set; }

    public string? CustomerComments { get; set; }

    public string? CustomerImageIcon { get; set; }

    public DateTime? CustomerBirthday { get; set; }

    public string? CustomerWebsite { get; set; }

    public int? CustomerSourceId { get; set; }

    public int? CustomerParentId { get; set; }

    public string? CustomerNotes { get; set; }

    public int? CustomerCareerId { get; set; }
}
