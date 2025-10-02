import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ApiResponseLogDetail } from '../models';

@Component({
  selector: 'app-invoice-comparison',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <div class="comparison-container" *ngIf="log1 && log2">
      <mat-card class="comparison-card">
        <mat-card-header>
          <mat-card-title>
            <mat-icon>compare_arrows</mat-icon>
            Parsed Invoice Data Comparison
          </mat-card-title>
          <mat-card-subtitle>
            Comparing {{ log1.apiProvider }} ({{ formatDate(log1.timestamp) }}) 
            vs {{ log2.apiProvider }} ({{ formatDate(log2.timestamp) }})
          </mat-card-subtitle>
        </mat-card-header>
        
        <mat-card-content>
          <div class="comparison-header">
            <div class="file-header left">
              <mat-icon>account_circle</mat-icon>
              {{ log1.apiProvider }} Result
              <span class="timestamp">{{ formatDate(log1.timestamp) }}</span>
            </div>
            <div class="file-header right">
              <mat-icon>account_circle</mat-icon>
              {{ log2.apiProvider }} Result
              <span class="timestamp">{{ formatDate(log2.timestamp) }}</span>
            </div>
          </div>

          <div class="diff-viewer">
            <div class="json-column left">
              <div class="line-numbers">
                <div class="line-number" *ngFor="let line of getJsonLines(log1); let i = index">
                  {{ i + 1 }}
                </div>
              </div>
              <div class="json-content">
                <div class="json-line" 
                     *ngFor="let line of getJsonLines(log1); let i = index"
                     [class.different]="isLineDifferent(i)"
                     [class.same]="!isLineDifferent(i)">
                  <span class="line-content">{{ line }}</span>
                </div>
              </div>
            </div>

            <div class="json-column right">
              <div class="line-numbers">
                <div class="line-number" *ngFor="let line of getJsonLines(log2); let i = index">
                  {{ i + 1 }}
                </div>
              </div>
              <div class="json-content">
                <div class="json-line" 
                     *ngFor="let line of getJsonLines(log2); let i = index"
                     [class.different]="isLineDifferent(i)"
                     [class.same]="!isLineDifferent(i)">
                  <span class="line-content">{{ line }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="comparison-stats">
            <div class="stat-item">
              <mat-icon class="same-icon">check_circle</mat-icon>
              <span>{{ getSameLineCount() }} identical lines</span>
            </div>
            <div class="stat-item">
              <mat-icon class="diff-icon">error</mat-icon>
              <span>{{ getDifferentLineCount() }} different lines</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .comparison-container {
      width: 100%;
      margin: 20px 0;
    }

    .comparison-card {
      box-shadow: 0 4px 8px rgba(0,0,0,0.1);
      border: 1px solid #d1d5db;
    }

    .comparison-card mat-card-title {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #1976d2;
      font-size: 16px;
    }

    .comparison-header {
      display: grid;
      grid-template-columns: 1fr 1fr;
      border: 1px solid #d1d5db;
      border-radius: 6px 6px 0 0;
      overflow: hidden;
      margin-bottom: 0;
    }

    .file-header {
      background: #f6f8fa;
      border-bottom: 1px solid #d1d5db;
      padding: 8px 16px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
      font-size: 14px;
      font-weight: 600;
      color: #24292f;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .file-header.left {
      border-right: 1px solid #d1d5db;
    }

    .file-header mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .timestamp {
      font-weight: 400;
      color: #656d76;
      margin-left: auto;
      font-size: 12px;
    }

    .diff-viewer {
      display: grid;
      grid-template-columns: 1fr 1fr;
      border: 1px solid #d1d5db;
      border-top: none;
      font-family: 'SFMono-Regular', 'Consolas', 'Liberation Mono', 'Menlo', monospace;
      font-size: 12px;
      line-height: 20px;
      background: #ffffff;
    }

    .json-column {
      display: flex;
      border-right: 1px solid #d1d5db;
      min-height: 400px;
      max-height: 600px;
      overflow: auto;
    }

    .json-column.right {
      border-right: none;
    }

    .line-numbers {
      background: #f6f8fa;
      border-right: 1px solid #d1d5db;
      min-width: 50px;
      text-align: right;
      color: #656d76;
      user-select: none;
    }

    .line-number {
      padding: 0 8px;
      line-height: 20px;
      font-size: 12px;
      border-bottom: 1px solid transparent;
    }

    .json-content {
      flex: 1;
      background: #ffffff;
    }

    .json-line {
      padding: 0 8px;
      line-height: 20px;
      white-space: pre;
      font-family: 'SFMono-Regular', 'Consolas', 'Liberation Mono', 'Menlo', monospace;
      border-left: 3px solid transparent;
    }

    .json-line.same {
      background: #ffffff;
    }

    .json-line.different {
      background: #fff5f5;
      border-left-color: #fd7b7b;
    }

    .json-line.different .line-content {
      background: rgba(248, 81, 73, 0.1);
    }

    .line-content {
      display: block;
      width: 100%;
      word-break: break-all;
    }

    .comparison-stats {
      display: flex;
      gap: 24px;
      margin-top: 16px;
      padding: 12px 16px;
      background: #f6f8fa;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 14px;
    }

    .stat-item {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .same-icon {
      color: #1a7f37;
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .diff-icon {
      color: #cf222e;
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    /* Scrollbar styling for webkit browsers */
    .json-column::-webkit-scrollbar {
      width: 12px;
      height: 12px;
    }

    .json-column::-webkit-scrollbar-track {
      background: #f6f8fa;
    }

    .json-column::-webkit-scrollbar-thumb {
      background: #d1d5db;
      border-radius: 6px;
    }

    .json-column::-webkit-scrollbar-thumb:hover {
      background: #b7bcc3;
    }

    @media (max-width: 768px) {
      .comparison-header,
      .diff-viewer {
        grid-template-columns: 1fr;
      }

      .file-header.left {
        border-right: none;
        border-bottom: 1px solid #d1d5db;
      }

      .json-column {
        border-right: none;
        border-bottom: 1px solid #d1d5db;
      }

      .json-column.right {
        border-bottom: none;
      }

      .comparison-stats {
        flex-direction: column;
        gap: 8px;
      }
    }
  `]
})
export class InvoiceComparisonComponent implements OnChanges {
  @Input() log1!: ApiResponseLogDetail;
  @Input() log2!: ApiResponseLogDetail;

  jsonLines1: string[] = [];
  jsonLines2: string[] = [];
  lineDifferences: boolean[] = [];

  ngOnChanges(changes: SimpleChanges) {
    if ((changes['log1'] || changes['log2']) && this.log1 && this.log2) {
      this.generateJsonComparison();
    }
  }

  private generateJsonComparison() {
    // Get formatted JSON for both logs
    const json1 = this.getFormattedJson(this.log1);
    const json2 = this.getFormattedJson(this.log2);

    // Split into lines
    this.jsonLines1 = json1.split('\n');
    this.jsonLines2 = json2.split('\n');

    // Make sure both arrays have the same length for comparison
    const maxLength = Math.max(this.jsonLines1.length, this.jsonLines2.length);
    
    while (this.jsonLines1.length < maxLength) {
      this.jsonLines1.push('');
    }
    while (this.jsonLines2.length < maxLength) {
      this.jsonLines2.push('');
    }

    // Calculate line differences
    this.lineDifferences = [];
    for (let i = 0; i < maxLength; i++) {
      this.lineDifferences[i] = this.jsonLines1[i] !== this.jsonLines2[i];
    }
  }

  getJsonLines(log: ApiResponseLogDetail): string[] {
    if (log === this.log1) return this.jsonLines1;
    if (log === this.log2) return this.jsonLines2;
    return [];
  }

  isLineDifferent(lineIndex: number): boolean {
    return this.lineDifferences[lineIndex] || false;
  }

  getSameLineCount(): number {
    return this.lineDifferences.filter(diff => !diff).length;
  }

  getDifferentLineCount(): number {
    return this.lineDifferences.filter(diff => diff).length;
  }

  formatDate(timestamp: string): string {
    return new Date(timestamp).toLocaleString();
  }

  getFormattedJson(log: ApiResponseLogDetail): string {
    return JSON.stringify(log.parsedInvoice, null, 2);
  }
}
