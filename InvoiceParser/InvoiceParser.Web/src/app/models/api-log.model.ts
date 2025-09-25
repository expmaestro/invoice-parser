import { ParsedInvoice, UsageMetadata, PromptTokenDetail } from './invoice.model';

export interface ApiResponseLog {
  id: string;
  requestId: string;
  timestamp: string;
  apiProvider: string;
  modelVersion?: string;
  processingTimeMs: number;
  success: boolean;
  errorMessage?: string;
  fileName?: string;
  fileSize?: number;
  imageMimeType?: string;
  usageMetadata?: UsageMetadata;
}

export interface ApiResponseLogDetail extends ApiResponseLog {
  requestPayload?: string;
  responseContent: string;
  imageBase64?: string;
  parsedInvoice?: ParsedInvoice;
}
