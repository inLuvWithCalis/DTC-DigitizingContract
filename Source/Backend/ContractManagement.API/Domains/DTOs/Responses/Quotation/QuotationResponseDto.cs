namespace ContractManagement.Domains.DTOs.Responses.Quotation
{
    // This DTO is used to return quotation along with its detail items in a structured format.
    public class QuotationResponseDto
    {
        public int QuotationId { get; set; }
        public string QuotationNo { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime QuotationDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string QuatationStatus { get; set; } = string.Empty;
        public List<ItemResponse> Items { get; set; } = new List<ItemResponse>();

        public class ItemResponse
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
