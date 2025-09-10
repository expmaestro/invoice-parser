using Azure;
using Azure.AI.DocumentIntelligence;
using WebApplication1.Models;

namespace WebApplication1.Services
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

                // Extract vendor name
                if (document.Fields.TryGetValue("VendorName", out DocumentField? vendorNameField) &&
                    vendorNameField.FieldType == DocumentFieldType.String)
                {
                    parsedInvoice.VendorName = vendorNameField.ValueString;
                    parsedInvoice.VendorNameConfidence = vendorNameField.Confidence;
                }

                // Extract customer name
                if (document.Fields.TryGetValue("CustomerName", out DocumentField? customerNameField) &&
                    customerNameField.FieldType == DocumentFieldType.String)
                {
                    parsedInvoice.CustomerName = customerNameField.ValueString;
                    parsedInvoice.CustomerNameConfidence = customerNameField.Confidence;
                }

                // Extract items
                if (document.Fields.TryGetValue("Items", out DocumentField? itemsField) &&
                    itemsField.FieldType == DocumentFieldType.List)
                {
                    foreach (DocumentField itemField in itemsField.ValueList)
                    {
                        if (itemField.FieldType == DocumentFieldType.Dictionary)
                        {
                            var item = new InvoiceItem();
                            var itemFields = itemField.ValueDictionary;

                            if (itemFields.TryGetValue("Description", out DocumentField? descriptionField) &&
                                descriptionField.FieldType == DocumentFieldType.String)
                            {
                                item.Description = descriptionField.ValueString;
                                item.DescriptionConfidence = descriptionField.Confidence;
                            }

                            if (itemFields.TryGetValue("Amount", out DocumentField? amountField) &&
                                amountField.FieldType == DocumentFieldType.Currency)
                            {
                                item.Amount = new CurrencyField
                                {
                                    Amount = amountField.ValueCurrency.Amount,
                                    CurrencySymbol = amountField.ValueCurrency.CurrencySymbol,
                                    Confidence = amountField.Confidence
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
                        Amount = subTotalField.ValueCurrency.Amount,
                        CurrencySymbol = subTotalField.ValueCurrency.CurrencySymbol,
                        Confidence = subTotalField.Confidence
                    };
                }

                if (document.Fields.TryGetValue("TotalTax", out DocumentField? totalTaxField) &&
                    totalTaxField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.TotalTax = new CurrencyField
                    {
                        Amount = totalTaxField.ValueCurrency.Amount,
                        CurrencySymbol = totalTaxField.ValueCurrency.CurrencySymbol,
                        Confidence = totalTaxField.Confidence
                    };
                }

                if (document.Fields.TryGetValue("InvoiceTotal", out DocumentField? invoiceTotalField) &&
                    invoiceTotalField.FieldType == DocumentFieldType.Currency)
                {
                    parsedInvoice.InvoiceTotal = new CurrencyField
                    {
                        Amount = invoiceTotalField.ValueCurrency.Amount,
                        CurrencySymbol = invoiceTotalField.ValueCurrency.CurrencySymbol,
                        Confidence = invoiceTotalField.Confidence
                    };
                }

                return parsedInvoice;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing invoice: {ex.Message}", ex);
            }
            //    }
            //}

            return null;

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
