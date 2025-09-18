using Azure;
using Azure.AI.DocumentIntelligence;
using InvoiceParser.Models;

namespace InvoiceParser.Services
{
    public interface IInvoiceParserService
    {
        Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream);
    }

    public class InvoiceParserService : IInvoiceParserService
    {
        private readonly IConfiguration _configuration;
        private readonly DocumentIntelligenceClient _client;

        public InvoiceParserService(IConfiguration configuration)
        {
            _configuration = configuration;
            string endpoint = _configuration["AzureDocumentIntelligence:Endpoint"] 
                ?? throw new ArgumentNullException("AzureDocumentIntelligence:Endpoint configuration is missing");
            string key = _configuration["AzureDocumentIntelligence:Key"]
                ?? throw new ArgumentNullException("AzureDocumentIntelligence:Key configuration is missing");
            
            var credential = new AzureKeyCredential(key);
            _client = new DocumentIntelligenceClient(new Uri(endpoint), credential);
        }

        public async Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream)
        {
            try
            {
                // Analyze the invoice
                var operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed, 
                    "prebuilt-invoice", 
                    BinaryData.FromStream(imageStream));

                var result = operation.Value;
                if (result.Documents.Count == 0)
                {
                    throw new Exception("No invoice data found in the image");
                }

                // Get the first document
                var document = result.Documents[0];

                // Create parsed invoice object
                var parsedInvoice = new ParsedInvoice();

                // Extract basic invoice information
                if (document.Fields.TryGetValue("InvoiceId", out DocumentField? invoiceIdField) &&
                    invoiceIdField.FieldType == DocumentFieldType.String)
                {
                    parsedInvoice.FreightBillNo = invoiceIdField.ValueString;
                }

                if (document.Fields.TryGetValue("InvoiceDate", out DocumentField? invoiceDateField) &&
                    invoiceDateField.FieldType == DocumentFieldType.Date)
                {
                    parsedInvoice.ShipmentDate = invoiceDateField.ValueDate?.ToString("yyyy-MM-dd");
                }

                if (document.Fields.TryGetValue("DueDate", out DocumentField? dueDateField) &&
                    dueDateField.FieldType == DocumentFieldType.Date)
                {
                    parsedInvoice.PaymentDueDate = dueDateField.ValueDate?.ToString("yyyy-MM-dd");
                }

                // Extract vendor information (map to RemitTo)
                if (document.Fields.TryGetValue("VendorName", out DocumentField? vendorNameField) &&
                    vendorNameField.FieldType == DocumentFieldType.String)
                {
                    parsedInvoice.RemitTo = new CompanyContact
                    {
                        Name = vendorNameField.ValueString
                    };
                }

                if (document.Fields.TryGetValue("VendorAddress", out DocumentField? vendorAddressField) &&
                    vendorAddressField.FieldType == DocumentFieldType.String)
                {
                    if (parsedInvoice.RemitTo == null)
                        parsedInvoice.RemitTo = new CompanyContact();
                        
                    parsedInvoice.RemitTo.Address = new Address
                    {
                        FullAddress = vendorAddressField.ValueString
                    };
                }

                // Extract customer information (map to BillTo)
                if (document.Fields.TryGetValue("CustomerName", out DocumentField? customerNameField) &&
                    customerNameField.FieldType == DocumentFieldType.String)
                {
                    parsedInvoice.BillTo = new CompanyContact
                    {
                        Name = customerNameField.ValueString
                    };
                }

                if (document.Fields.TryGetValue("CustomerAddress", out DocumentField? customerAddressField) &&
                    customerAddressField.FieldType == DocumentFieldType.String)
                {
                    if (parsedInvoice.BillTo == null)
                        parsedInvoice.BillTo = new CompanyContact();
                        
                    parsedInvoice.BillTo.Address = new Address
                    {
                        FullAddress = customerAddressField.ValueString
                    };
                }

                // Extract items
                if (document.Fields.TryGetValue("Items", out DocumentField? itemsField) &&
                    itemsField.FieldType == DocumentFieldType.List)
                {
                    foreach (DocumentField itemField in itemsField.ValueList)
                    {
                        if (itemField.FieldType == DocumentFieldType.Dictionary)
                        {
                            var item = new ShipmentItem();
                            var itemFields = itemField.ValueDictionary;

                            if (itemFields.TryGetValue("Description", out DocumentField? descriptionField) &&
                                descriptionField.FieldType == DocumentFieldType.String)
                            {
                                item.Description = descriptionField.ValueString;
                            }

                            if (itemFields.TryGetValue("Quantity", out DocumentField? quantityField) &&
                                quantityField.FieldType == DocumentFieldType.Double)
                            {
                                item.Pieces = (int?)quantityField.ValueDouble;
                            }

                            if (itemFields.TryGetValue("Amount", out DocumentField? amountField) &&
                                amountField.FieldType == DocumentFieldType.Currency)
                            {
                                item.Charge = new CurrencyField
                                {
                                    Amount = (decimal)amountField.ValueCurrency.Amount,
                                    CurrencySymbol = amountField.ValueCurrency.CurrencySymbol
                                };
                            }

                            parsedInvoice.Items.Add(item);
                        }
                    }
                }

                // Extract currency fields
                if (document.Fields.TryGetValue("SubTotal", out DocumentField? subTotalField) &&
                    subTotalField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.SubTotal = new CurrencyField
                    {
                        Amount = (decimal)subTotalField.ValueCurrency.Amount,
                        CurrencySymbol = subTotalField.ValueCurrency.CurrencySymbol
                    };
                }

                if (document.Fields.TryGetValue("TotalTax", out DocumentField? totalTaxField) &&
                    totalTaxField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.TotalTax = new CurrencyField
                    {
                        Amount = (decimal)totalTaxField.ValueCurrency.Amount,
                        CurrencySymbol = totalTaxField.ValueCurrency.CurrencySymbol
                    };
                }

                if (document.Fields.TryGetValue("InvoiceTotal", out DocumentField? invoiceTotalField) &&
                    invoiceTotalField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.InvoiceTotal = new CurrencyField
                    {
                        Amount = (decimal)invoiceTotalField.ValueCurrency.Amount,
                        CurrencySymbol = invoiceTotalField.ValueCurrency.CurrencySymbol
                    };
                }

                if (document.Fields.TryGetValue("AmountDue", out DocumentField? amountDueField) &&
                    amountDueField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.AmountDue = new CurrencyField
                    {
                        Amount = (decimal)amountDueField.ValueCurrency.Amount,
                        CurrencySymbol = amountDueField.ValueCurrency.CurrencySymbol
                    };
                }

                return parsedInvoice;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing invoice: {ex.Message}", ex);
            }
        }
    }

    public static class DocumentHelper
    {
        public static Dictionary<string, string> ExtractFields(AnalyzeResult result)
        {
            var map = new Dictionary<string, string>();

            if (result?.Documents == null || result.Documents.Count == 0)
                return map;

            var fields = result.Documents[0].Fields;

            foreach (var field in fields)
            {
                var key = field.Key;
                var content = field.Value?.Content;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    map[key] = content;
                }
            }

            return map;
        }
    }
}
