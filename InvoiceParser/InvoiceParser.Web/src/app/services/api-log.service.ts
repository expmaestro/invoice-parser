import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ParsedInvoice } from '../models/invoice.model';

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

@Injectable({
  providedIn: 'root'
})
export class ApiLogService {
  private baseUrl = 'https://localhost:7132/api';

  constructor(private http: HttpClient) {}

  getRecentLogs(limit: number = 20): Observable<ApiResponseLog[]> {
    return this.http.get<ApiResponseLog[]>(`${this.baseUrl}/apilogs/recent?limit=${limit}`);
  }

  getLogsByProvider(provider: string, limit: number = 20): Observable<ApiResponseLog[]> {
    return this.http.get<ApiResponseLog[]>(`${this.baseUrl}/apilogs/provider/${provider}?limit=${limit}`);
  }

  getLogById(id: string): Observable<ApiResponseLogDetail> {
    return this.http.get<ApiResponseLogDetail>(`${this.baseUrl}/apilogs/${id}`);
  }

  getLogImage(id: string): string {
    return `${this.baseUrl}/apilogs/${id}/image`;
  }
}
