import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiLogService, ApiResponseLog, ApiResponseLogDetail } from '../services/api-log.service';
import { ToastrService } from 'ngx-toastr';
import { InvoiceDetailsComponent } from './invoice-details.component';

@Component({
  selector: 'app-api-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, InvoiceDetailsComponent],
  styleUrls: ['./api-logs.component.scss'],
  templateUrl: './api-logs.component.html'
})
export class ApiLogsComponent implements OnInit {
  logs: ApiResponseLog[] = [];
  isLoading = false;
  selectedProvider: 'all' | 'gemini' | 'azure' = 'all';
  selectedLog: ApiResponseLogDetail | null = null;
  showDetails = false;
  limit = 10;

  constructor(
    private apiLogService: ApiLogService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadLogs();
  }

  loadLogs() {
    this.isLoading = true;
    
    const request = this.selectedProvider === 'all' 
      ? this.apiLogService.getRecentLogs(this.limit)
      : this.apiLogService.getLogsByProvider(this.selectedProvider, this.limit);

    request.subscribe({
      next: (logs) => {
        this.logs = logs;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading logs:', error);
        this.toastr.error('Failed to load API logs', 'Error');
        this.isLoading = false;
      }
    });
  }

  onProviderChange() {
    this.loadLogs();
  }

  onLimitChange() {
    this.loadLogs();
  }

  viewDetails(log: ApiResponseLog) {
    this.apiLogService.getLogById(log.id).subscribe({
      next: (detailedLog) => {
        this.selectedLog = detailedLog;
        this.showDetails = true;
      },
      error: (error) => {
        console.error('Error loading log details:', error);
        this.toastr.error('Failed to load log details', 'Error');
      }
    });
  }

  closeDetails() {
    this.showDetails = false;
    this.selectedLog = null;
  }

  getImageUrl(logId: string): string {
    return this.apiLogService.getLogImage(logId);
  }

  formatTimestamp(timestamp: string): string {
    return new Date(timestamp).toLocaleString();
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return 'N/A';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + ' ' + sizes[i];
  }

  formatDuration(ms: number): string {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  }

  copyToClipboard(text: string, label: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.toastr.success(`${label} copied to clipboard`, 'Copied');
    }).catch(() => {
      this.toastr.error(`Failed to copy ${label}`, 'Error');
    });
  }

  downloadResponse() {
    if (!this.selectedLog) return;
    
    const blob = new Blob([this.selectedLog.responseContent], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `api-response-${this.selectedLog.requestId}.json`;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
