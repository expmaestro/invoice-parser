namespace WebApplication1.Models
{
    public class ShippingInfo
    {
        public string? Name { get; set; }
        public double? NameConfidence { get; set; }
        public Address? Address { get; set; }
        public double? AddressConfidence { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }

    public class ParsedInvoice
    {
        public string? VendorName { get; set; }
        public double? VendorNameConfidence { get; set; }
        public string? CustomerName { get; set; }
        public double? CustomerNameConfidence { get; set; }
        public ShippingInfo? Shipper { get; set; }
        public ShippingInfo? Consignee { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
        public CurrencyField? SubTotal { get; set; }
        public CurrencyField? TotalTax { get; set; }
        public CurrencyField? InvoiceTotal { get; set; }
    }

    public class InvoiceItem
    {
        public string? Description { get; set; }
        public double? DescriptionConfidence { get; set; }
        public CurrencyField? Amount { get; set; }
    }

    public class CurrencyField
    {
        public string? CurrencySymbol { get; set; }
        public double Amount { get; set; }
        public double? Confidence { get; set; }
    }
}
