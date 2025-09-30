namespace InvoiceParser.Constants
{
    public static class PromptConstants
    {
        public const string InvoiceParsingPrompt = @"Analyze this logistics invoice image and extract the following information as structured JSON:

                INVOICE INFORMATION:
                - Service
                - Freight Bill No
                - Shipment Date
                - Amount Due
                - Payment Due Date
                - FED TAX ID

                REMIT TO INFORMATION:
                - Company Name
                - Address (full address as one string)
                - Phone / Fax
                - Email / Website
                - Account No

                BILL TO & PAYMENT DUE FROM:
                - Company Name
                - Address (full address as one string)
                - Phone
                - Account No

                SHIPPER INFORMATION:
                - Shipper Account #
                - Shipper Name
                - Shipper Address (full address as one string)
                - Shipper Phone

                CONSIGNEE INFORMATION:
                - Consignee Account #
                - Consignee Name
                - Consignee Address (full address as one string)
                - Consignee Phone

                SHIPMENT DETAILS:
                - P.O. Number
                - Bill of Lading No
                - Tariff
                - Payment Terms
                - Total Pieces
                - Total Weight

                LINE ITEMS (extract each piece/line item):
                - Number of pieces
                - Description
                - Weight (lbs)
                - Class
                - Rate
                - Charge

                TOTALS:
                - Subtotal
                - Tax amount
                - Total Amount

                Format as JSON with this exact structure:
                {
                    ""service"": ""string"",
                    ""freightBillNo"": ""string"",
                    ""shipmentDate"": ""string"",
                    ""amountDue"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""paymentDueDate"": ""string"",
                    ""fedTaxId"": ""string"",
                    ""remitTo"": {
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string"",
                        ""fax"": ""string"",
                        ""email"": ""string"",
                        ""website"": ""string"",
                        ""accountNumber"": ""string""
                    },
                    ""billTo"": {
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string"",
                        ""accountNumber"": ""string""
                    },
                    ""shipper"": {
                        ""accountNumber"": ""string"",
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string""
                    },
                    ""consignee"": {
                        ""accountNumber"": ""string"",
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string""
                    },
                    ""shipmentDetails"": {
                        ""service"": ""string"",
                        ""shipmentDate"": ""string"",
                        ""poNumber"": ""string"",
                        ""billOfLading"": ""string"",
                        ""tariff"": ""string"",
                        ""paymentTerms"": ""string"",
                        ""totalPieces"": 0,
                        ""totalWeight"": 0.00
                    },
                    ""items"": [
                        {
                            ""pieces"": 0,
                            ""description"": ""string"",
                            ""weight"": 0.00,
                            ""class"": ""string"",
                            ""rate"": 0.00,
                            ""charge"": { ""currencySymbol"": ""$"", ""amount"": 0.00 }
                        }
                    ],
                    ""subTotal"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""totalTax"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""invoiceTotal"": { ""currencySymbol"": ""$"", ""amount"": 0.00 }
                 }

Important:
- Return ONLY the JSON object, no additional text or explanation
- Use null for any missing values
- For currency amounts, always extract both the symbol and numeric value
- Ensure all extracted text is clean and properly formatted
- If multiple line items exist, include all of them in the lineItems array";
    }
}
