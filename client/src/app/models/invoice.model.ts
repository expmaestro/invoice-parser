export interface ParsedInvoice {
    vendorName?: string;
    vendorNameConfidence?: number;
    customerName?: string;
    customerNameConfidence?: number;
    items: InvoiceItem[];
    subTotal?: CurrencyField;
    totalTax?: CurrencyField;
    invoiceTotal?: CurrencyField;
}

export interface InvoiceItem {
    description?: string;
    descriptionConfidence?: number;
    amount?: CurrencyField;
}

export interface CurrencyField {
    currencySymbol?: string;
    amount: number;
    confidence?: number;
}
