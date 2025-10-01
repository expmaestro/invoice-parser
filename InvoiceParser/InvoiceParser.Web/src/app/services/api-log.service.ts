import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponseLog, ApiResponseLogDetail } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiLogService {
  private baseUrl = environment.apiUrl;

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

  deleteLog(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/apilogs/${id}`);
  }

  deleteAllLogs(): Observable<any> {
    return this.http.delete(`${this.baseUrl}/apilogs/all`);
  }
}
