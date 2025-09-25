export interface Address {
    street?: string;
    city?: string;
    state?: string;
    country?: string;
    postalCode?: string;
    fullAddress?: string;
}

export interface CompanyContact {
    name?: string;
    address?: Address;
    phone?: string;
    fax?: string;
    email?: string;
    website?: string;
    accountNumber?: string;
}

export interface ShippingInfo {
    accountNumber?: string;
    name?: string;
    address?: Address;
    phone?: string;
}

export interface ShipmentDetails {
    service?: string;
    shipmentDate?: string;
    poNumber?: string;
    billOfLading?: string;
    tariff?: string;
    paymentTerms?: string;
    totalPieces?: number;
    totalWeight?: number;
}

export interface ShipmentItem {
    pieces?: number;
    description?: string;
    weight?: number;
    class?: string;
    rate?: number;
    charge?: CurrencyField;
}

export interface ParsedInvoice {
    // Invoice Information
    service?: string;
    freightBillNo?: string;
    shipmentDate?: string;
    amountDue?: CurrencyField;
    paymentDueDate?: string;
    fedTaxId?: string;

    // Company Information
    remitTo?: CompanyContact;
    billTo?: CompanyContact;

    // Shipping Information
    shipper?: ShippingInfo;
    consignee?: ShippingInfo;

    // Shipment Details
    shipmentDetails?: ShipmentDetails;

    // Line Items
    items: ShipmentItem[];

    // Totals
    subTotal?: CurrencyField;
    totalTax?: CurrencyField;
    invoiceTotal?: CurrencyField;

    // Metadata Information
    usageMetadata?: UsageMetadata;
    modelVersion?: string;
}

export interface CurrencyField {
    currencySymbol?: string;
    amount: number;
}

export interface UsageMetadata {
  totalTokenCount: number;
  promptTokenCount: number;
  candidatesTokenCount: number;
  thoughtsTokenCount?: number;
  promptTokensDetails?: PromptTokenDetail[];
}

export interface PromptTokenDetail {
  modality: string;
  tokenCount: number;
}
