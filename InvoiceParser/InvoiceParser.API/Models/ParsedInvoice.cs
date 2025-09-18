namespace InvoiceParser.Models
{
    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? FullAddress { get; set; }
    }

    public class CompanyContact
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? AccountNumber { get; set; }
    }

    public class ShippingInfo
    {
        public string? AccountNumber { get; set; }
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public string? Phone { get; set; }
    }

    public class ShipmentDetails
    {
        public string? Service { get; set; }
        public string? ShipmentDate { get; set; }
        public string? PoNumber { get; set; }
        public string? BillOfLading { get; set; }
        public string? Tariff { get; set; }
        public string? PaymentTerms { get; set; }
        public int? TotalPieces { get; set; }
        public decimal? TotalWeight { get; set; }
    }

    public class ShipmentItem
    {
        public int? Pieces { get; set; }
        public string? Description { get; set; }
        public decimal? Weight { get; set; }
        public string? Class { get; set; }
        public decimal? Rate { get; set; }
        public CurrencyField? Charge { get; set; }
    }

    public class ParsedInvoice
    {
        // Invoice Information
        public string? Service { get; set; }
        public string? FreightBillNo { get; set; }
        public string? ShipmentDate { get; set; }
        public CurrencyField? AmountDue { get; set; }
        public string? PaymentDueDate { get; set; }
        public string? FedTaxId { get; set; }

        // Company Information
        public CompanyContact? RemitTo { get; set; }
        public CompanyContact? BillTo { get; set; }

        // Shipping Information
        public ShippingInfo? Shipper { get; set; }
        public ShippingInfo? Consignee { get; set; }

        // Shipment Details
        public ShipmentDetails? ShipmentDetails { get; set; }

        // Line Items
        public List<ShipmentItem> Items { get; set; } = new();

        // Totals
        public CurrencyField? SubTotal { get; set; }
        public CurrencyField? TotalTax { get; set; }
        public CurrencyField? InvoiceTotal { get; set; }

        // Metadata Information
        public UsageMetadata? UsageMetadata { get; set; }
        public string? ModelVersion { get; set; }
    }

    public class CurrencyField
    {
        public string? CurrencySymbol { get; set; }
        public decimal Amount { get; set; }
    }

    public class UsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
        public List<PromptTokenDetail>? PromptTokensDetails { get; set; }
        public int? ThoughtsTokenCount { get; set; }
    }

    public class PromptTokenDetail
    {
        public string? Modality { get; set; }
        public int TokenCount { get; set; }
    }
}
