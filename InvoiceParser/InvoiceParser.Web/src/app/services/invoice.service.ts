import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ParsedInvoice } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  parseInvoice(file: File, parser: 'azure' | 'gemini' = 'azure'): Observable<ParsedInvoice> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ParsedInvoice>(`${this.baseUrl}/invoice/parse?parser=${parser}`, formData);
  }
}
