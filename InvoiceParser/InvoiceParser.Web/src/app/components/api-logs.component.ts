import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { ApiLogService } from '../services/api-log.service';
import { ApiResponseLog, ApiResponseLogDetail } from '../models';
import { ToastrService } from 'ngx-toastr';
import { InvoiceDetailsComponent } from './invoice-details.component';
import { DeleteConfirmationDialogComponent, DeleteConfirmationData } from './delete-confirmation-dialog.component';
import { InvoiceComparisonComponent } from './invoice-comparison.component';

@Component({
  selector: 'app-api-logs',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule,
    MatTooltipModule,
    MatCheckboxModule,
    MatCardModule,
    MatTabsModule,
    InvoiceDetailsComponent,
    InvoiceComparisonComponent
  ],
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
  
  // Comparison functionality
  selectedLogsForComparison: string[] = [];
  showComparison = false;
  comparisonLogs: ApiResponseLogDetail[] = [];
  isLoadingComparison = false;

  constructor(
    private apiLogService: ApiLogService,
    private toastr: ToastrService,
    private dialog: MatDialog
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

  formatTimestamp(timestamp: string | undefined): string {
    if (!timestamp) return 'N/A';
    return new Date(timestamp).toLocaleString();
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return 'N/A';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + ' ' + sizes[i];
  }

  formatDuration(ms: number | undefined): string {
    if (ms === undefined || ms === null) return 'N/A';
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

  confirmDeleteLog(logId: string) {
    const dialogRef = this.dialog.open(DeleteConfirmationDialogComponent, {
      width: '400px',
      data: { type: 'single' } as DeleteConfirmationData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteSingleLog(logId);
      }
    });
  }

  confirmDeleteAll() {
    const dialogRef = this.dialog.open(DeleteConfirmationDialogComponent, {
      width: '400px',
      data: { type: 'all', count: this.logs.length } as DeleteConfirmationData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteAllLogs();
      }
    });
  }

  deleteSingleLog(logId: string) {
    this.apiLogService.deleteLog(logId).subscribe({
      next: (response) => {
        this.toastr.success('Log deleted successfully', 'Success');
        this.loadLogs();
        if (this.selectedLog && this.selectedLog.id === logId) {
          this.closeDetails();
        }
      },
      error: (error) => {
        console.error('Error deleting log:', error);
        this.toastr.error('Failed to delete log', 'Error');
      }
    });
  }

  deleteAllLogs() {
    this.apiLogService.deleteAllLogs().subscribe({
      next: (response) => {
        this.toastr.success(`${response.deletedCount} logs deleted successfully`, 'Success');
        this.loadLogs();
        this.closeDetails();
      },
      error: (error) => {
        console.error('Error deleting all logs:', error);
        this.toastr.error('Failed to delete all logs', 'Error');
      }
    });
  }

  // Comparison functionality methods
  toggleLogSelection(logId: string) {
    const index = this.selectedLogsForComparison.indexOf(logId);
    
    if (index > -1) {
      // Remove from selection
      this.selectedLogsForComparison.splice(index, 1);
    } else {
      // Add to selection (max 2 logs)
      if (this.selectedLogsForComparison.length < 2) {
        this.selectedLogsForComparison.push(logId);
      } else {
        this.toastr.warning('You can only compare 2 logs at a time', 'Selection Limit');
        return;
      }
    }

    // If we have exactly 2 logs selected, automatically start comparison
    if (this.selectedLogsForComparison.length === 2) {
      this.startComparison();
    } else {
      this.closeComparison();
    }
  }

  isLogSelected(logId: string): boolean {
    return this.selectedLogsForComparison.includes(logId);
  }

  startComparison() {
    if (this.selectedLogsForComparison.length !== 2) {
      this.toastr.error('Please select exactly 2 logs to compare', 'Comparison Error');
      return;
    }

    this.isLoadingComparison = true;
    this.comparisonLogs = [];

    // Load detailed data for both selected logs
    const log1Promise = this.apiLogService.getLogById(this.selectedLogsForComparison[0]).toPromise();
    const log2Promise = this.apiLogService.getLogById(this.selectedLogsForComparison[1]).toPromise();

    Promise.all([log1Promise, log2Promise]).then((results) => {
      this.comparisonLogs = results.filter(log => log !== undefined) as ApiResponseLogDetail[];
      this.showComparison = true;
      this.isLoadingComparison = false;
    }).catch((error) => {
      console.error('Error loading comparison logs:', error);
      this.toastr.error('Failed to load logs for comparison', 'Error');
      this.isLoadingComparison = false;
    });
  }

  closeComparison() {
    this.showComparison = false;
    this.comparisonLogs = [];
  }

  clearSelection() {
    this.selectedLogsForComparison = [];
    this.closeComparison();
  }
}
