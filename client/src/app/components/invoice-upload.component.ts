import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InvoiceService } from '../services/invoice.service';
import { ParsedInvoice, CurrencyField } from '../models/invoice.model';

@Component({
  selector: 'app-invoice-upload',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <div class="upload-section">
        <h2>Upload Invoice</h2>
        <input type="file" 
               accept="image/*" 
               (change)="onFileSelected($event)"
               [class.drag-over]="isDragging"
               (dragover)="onDragOver($event)"
               (dragleave)="isDragging = false"
               (drop)="onDrop($event)">
        <div *ngIf="isLoading" class="loading">Processing...</div>
      </div>

      <div class="content-wrapper" *ngIf="imagePreviewUrl || parsedInvoice">
        <div class="image-preview" *ngIf="imagePreviewUrl">
          <div class="image-container">
            <img [src]="imagePreviewUrl" alt="Invoice Preview" class="preview-image">
          </div>
        </div>
        
        <div *ngIf="parsedInvoice" class="results-section">
          <h3>Parsed Invoice Details</h3>
        <div class="invoice-details">
          <p *ngIf="parsedInvoice.vendorName">
            <strong>Vendor Name:</strong> 
            {{parsedInvoice.vendorName}}
            <small *ngIf="parsedInvoice.vendorNameConfidence" class="confidence">
              ({{(parsedInvoice.vendorNameConfidence * 100).toFixed(1)}}% confidence)
            </small>
          </p>
          <p *ngIf="parsedInvoice.customerName">
            <strong>Customer Name:</strong> 
            {{parsedInvoice.customerName}}
            <small *ngIf="parsedInvoice.customerNameConfidence" class="confidence">
              ({{(parsedInvoice.customerNameConfidence * 100).toFixed(1)}}% confidence)
            </small>
          </p>
          <p *ngIf="parsedInvoice.subTotal">
            <strong>Subtotal:</strong> 
            {{formatCurrency(parsedInvoice.subTotal)}}
          </p>
          <p *ngIf="parsedInvoice.totalTax">
            <strong>Tax:</strong> 
            {{formatCurrency(parsedInvoice.totalTax)}}
          </p>
          <p *ngIf="parsedInvoice.invoiceTotal">
            <strong>Total:</strong> 
            {{formatCurrency(parsedInvoice.invoiceTotal)}}
          </p>
        </div>

        <div class="line-items" *ngIf="parsedInvoice.items?.length">
          <h4>Line Items</h4>
          <table>
            <thead>
              <tr>
                <th>Description</th>
                <th>Amount</th>
                <th>Confidence</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of parsedInvoice.items">
                <td>
                  {{item.description}}
                  <small *ngIf="item.descriptionConfidence" class="confidence">
                    ({{(item.descriptionConfidence * 100).toFixed(1)}}%)
                  </small>
                </td>
                <td>{{formatCurrency(item.amount)}}</td>
                <td>
                  <small *ngIf="item.amount?.confidence !== undefined" class="confidence">
                    {{(item.amount?.confidence! * 100).toFixed(1)}}%
                  </small>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        </div>
      </div>
      </div>
  `,
  styles: [`
    .container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .upload-section {
      border: 2px dashed #ccc;
      padding: 20px;
      text-align: center;
      margin-bottom: 20px;
      border-radius: 4px;
    }

    input[type="file"] {
      width: 100%;
      height: 100px;
      cursor: pointer;
      border: none;
    }

    .drag-over {
      background-color: #e1e1e1;
      border-color: #999;
    }

    .loading {
      margin-top: 10px;
      color: #666;
    }

    .content-wrapper {
      display: flex;
      gap: 20px;
      align-items: flex-start;
    }

    .image-preview {
      flex: 1;
      max-width: 50%;
      position: sticky;
      top: 20px;
    }

    .image-container {
      border: 1px solid #ddd;
      padding: 10px;
      border-radius: 4px;
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .preview-image {
      width: 100%;
      height: auto;
      max-height: 800px;
      object-fit: contain;
      border-radius: 2px;
    }

    .results-section {
      flex: 1;
      background: #f9f9f9;
      padding: 20px;
      border-radius: 4px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .invoice-details {
      margin-bottom: 20px;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
      background: white;
    }

    th, td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    th {
      background-color: #f2f2f2;
    }

    .confidence {
      color: #666;
      font-style: italic;
      margin-left: 5px;
    }
  `]
})
export class InvoiceUploadComponent {
  isDragging = false;
  isLoading = false;
  parsedInvoice: ParsedInvoice | null = null;
  imagePreviewUrl: string | null = null;

  constructor(private invoiceService: InvoiceService) {}

  formatCurrency(field?: CurrencyField): string {
    if (!field) return '';
    return `${field.currencySymbol || '$'}${field.amount.toFixed(2)}`;
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.processFile(file);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
    
    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.processFile(files[0]);
    }
  }

  private processFile(file: File) {
    if (!file.type.startsWith('image/')) {
      alert('Please upload an image file');
      return;
    }

    // Create image preview
    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.imagePreviewUrl = e.target.result;
    };
    reader.readAsDataURL(file);

    this.isLoading = true;
    this.invoiceService.parseInvoice(file).subscribe({
      next: (result) => {
        this.parsedInvoice = result;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error parsing invoice:', error);
        alert('Error parsing invoice. Please try again.');
        this.isLoading = false;
      }
    });
  }
}
