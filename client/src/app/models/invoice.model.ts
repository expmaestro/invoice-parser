export interface Address {
    street?: string;
    city?: string;
    state?: string;
    country?: string;
    postalCode?: string;
}

export interface ShippingInfo {
    name?: string;
    nameConfidence?: number;
    address?: Address;
    addressConfidence?: number;
}

export interface ParsedInvoice {
    vendorName?: string;
    vendorNameConfidence?: number;
    customerName?: string;
    customerNameConfidence?: number;
    shipper?: ShippingInfo;
    consignee?: ShippingInfo;
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
