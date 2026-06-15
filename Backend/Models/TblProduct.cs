using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblProduct
{
    public int ProductId { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public int? CategoryId { get; set; }

    public string? ProductShortDesc { get; set; }

    public string? ProductDetails { get; set; }

    public string? ProductFeatures { get; set; }

    public string? ProductDeployment { get; set; }

    public string? ProductBenefit { get; set; }

    public string? ProductSmallImage { get; set; }

    public string? ProductLargeImage { get; set; }

    public double? ProductPrice { get; set; }

    public byte? LangId { get; set; }

    public byte? Status { get; set; }

    public string? Others { get; set; }

    public int? ViewTotal { get; set; }

    public string? Customers { get; set; }

    public string? ProductOverall { get; set; }

    public string? ProductIconClass { get; set; }

    public string? ProductSlogan { get; set; }

    public byte? DisplaySlide { get; set; }

    public bool? GetTrialVersion { get; set; }

    public int? ProductOrder { get; set; }

    public string? ProductShortName { get; set; }

    public string? MetaKeyword { get; set; }

    public string? MetaDescription { get; set; }

    public string? ProductTags { get; set; }

    public string? Rewrite { get; set; }

    public string? TitleBrowser { get; set; }

    public string? ProductDocument { get; set; }

    public DateTime? ProductCreatedDate { get; set; }

    public int? GoogleClick { get; set; }
}
