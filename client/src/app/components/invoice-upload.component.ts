import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../services/invoice.service';
import { ParsedInvoice, CurrencyField, CompanyContact, ShipmentDetails, ShipmentItem } from '../models/invoice.model';

@Component({
  selector: 'app-invoice-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [`
    /* Container and Layout */
    .container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .content-wrapper {
      display: flex;
      gap: 20px;
      align-items: flex-start;
    }

    /* Upload Section */
    .upload-section {
      border: 2px dashed #ccc;
      padding: 20px;
      text-align: center;
      margin-bottom: 20px;
      border-radius: 4px;
    }

    .parser-selection {
      margin-bottom: 20px;
      display: flex;
      justify-content: center;
      gap: 20px;
    }

    .parser-selection label {
      cursor: pointer;
      padding: 10px 15px;
      border: 1px solid #ddd;
      border-radius: 4px;
      transition: all 0.3s ease;
    }

    .parser-selection label:hover {
      background-color: #f0f0f0;
    }

    .parser-selection input[type="radio"] {
      margin-right: 8px;
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

    /* Image Preview */
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

    /* Results Section */
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

    /* Section Styling */
    .section {
      margin: 1.5rem 0;
      padding: 1rem;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      background-color: #fff;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .section h4 {
      color: #333;
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid #f0f0f0;
    }

    /* Company Info */
    .company-info p {
      margin: 0.5rem 0;
    }

    .address {
      margin: 0.5rem 0;
      padding-left: 1rem;
      border-left: 3px solid #f0f0f0;
    }

    /* Line Items Table */
    .line-items table {
      width: 100%;
      border-collapse: collapse;
      margin: 1rem 0;
    }

    .line-items th, .line-items td {
      padding: 0.75rem;
      text-align: left;
      border: 1px solid #e0e0e0;
    }

    .line-items th {
      background-color: #f5f5f5;
      font-weight: 600;
    }

    .line-items tr:nth-child(even) {
      background-color: #fafafa;
    }

    /* Totals Section */
    .totals {
      margin-top: 2rem;
      padding-top: 1rem;
      border-top: 2px solid #e0e0e0;
    }

    /* Utilities */
  `],
  template: `
    <div class="container">
      <div class="upload-section">
        <h2>Upload Invoice</h2>
        <div class="parser-selection">
          <label>
            <input type="radio" name="parser" [value]="'azure'" [(ngModel)]="selectedParser">
            Azure Document Intelligence
          </label>
          <label>
            <input type="radio" name="parser" [value]="'gemini'" [(ngModel)]="selectedParser">
            Google Gemini
          </label>
        </div>
        <input type="file" 
               accept="image/*" 
               (change)="onFileSelected($event)"
               [class.drag-over]="isDragging"
               (dragover)="onDragOver($event)"
               (dragleave)="isDragging = false"
               (drop)="onDrop($event)">
        <div *ngIf="isLoading" class="loading">Processing with {{selectedParser}}...</div>
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
            <!-- Basic Invoice Information -->
            <div class="section">
              <h4>Invoice Information</h4>
              <p *ngIf="parsedInvoice.service">
                <strong>Service:</strong> 
                {{parsedInvoice.service}}
              </p>
              <p *ngIf="parsedInvoice.freightBillNo">
                <strong>Freight Bill No:</strong> 
                {{parsedInvoice.freightBillNo}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDate">
                <strong>Shipment Date:</strong> 
                {{parsedInvoice.shipmentDate}}
              </p>
              <p *ngIf="parsedInvoice.amountDue">
                <strong>Amount Due:</strong> 
                {{formatCurrency(parsedInvoice.amountDue)}}
              </p>
              <p *ngIf="parsedInvoice.paymentDueDate">
                <strong>Payment Due Date:</strong> 
                {{parsedInvoice.paymentDueDate}}
              </p>
              <p *ngIf="parsedInvoice.fedTaxId">
                <strong>FED TAX ID:</strong> 
                {{parsedInvoice.fedTaxId}}
              </p>
            </div>

            <!-- Remit To Information -->
            <div *ngIf="parsedInvoice.remitTo" class="section">
              <h4>Remit To Information</h4>
              <div class="company-info">
                <p *ngIf="parsedInvoice.remitTo.name">
                  <strong>Company:</strong> {{parsedInvoice.remitTo.name}}
                </p>
                <ng-container *ngIf="parsedInvoice.remitTo.address">
                  <div *ngIf="parsedInvoice.remitTo.address.fullAddress" class="address">
                    <p><strong>Address:</strong> {{parsedInvoice.remitTo.address.fullAddress}}</p>
                  </div>
                </ng-container>
                <p *ngIf="parsedInvoice.remitTo.phone">
                  <strong>Phone:</strong> {{parsedInvoice.remitTo.phone}}
                </p>
                <p *ngIf="parsedInvoice.remitTo.fax">
                  <strong>Fax:</strong> {{parsedInvoice.remitTo.fax}}
                </p>
                <p *ngIf="parsedInvoice.remitTo.email">
                  <strong>Email:</strong> {{parsedInvoice.remitTo.email}}
                </p>
                <p *ngIf="parsedInvoice.remitTo.website">
                  <strong>Website:</strong> {{parsedInvoice.remitTo.website}}
                </p>
                <p *ngIf="parsedInvoice.remitTo.accountNumber">
                  <strong>Account No:</strong> {{parsedInvoice.remitTo.accountNumber}}
                </p>
              </div>
            </div>

            <!-- Bill To Information -->
            <div *ngIf="parsedInvoice.billTo" class="section">
              <h4>Bill To Information</h4>
              <div class="company-info">
                <p *ngIf="parsedInvoice.billTo.name">
                  <strong>Company:</strong> {{parsedInvoice.billTo.name}}
                </p>
                <ng-container *ngIf="parsedInvoice.billTo.address">
                  <div *ngIf="parsedInvoice.billTo.address.fullAddress" class="address">
                    <p><strong>Address:</strong> {{parsedInvoice.billTo.address.fullAddress}}</p>
                  </div>
                </ng-container>
                <p *ngIf="parsedInvoice.billTo.phone">
                  <strong>Phone:</strong> {{parsedInvoice.billTo.phone}}
                </p>
                <p *ngIf="parsedInvoice.billTo.accountNumber">
                  <strong>Account No:</strong> {{parsedInvoice.billTo.accountNumber}}
                </p>
              </div>
            </div>

            <!-- Shipment Details -->
            <div *ngIf="parsedInvoice.shipmentDetails" class="section">
              <h4>Shipment Details</h4>
              <p *ngIf="parsedInvoice.shipmentDetails.service">
                <strong>Service:</strong> {{parsedInvoice.shipmentDetails.service}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.shipmentDate">
                <strong>Shipment Date:</strong> {{parsedInvoice.shipmentDetails.shipmentDate}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.poNumber">
                <strong>P.O. Number:</strong> {{parsedInvoice.shipmentDetails.poNumber}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.billOfLading">
                <strong>Bill of Lading:</strong> {{parsedInvoice.shipmentDetails.billOfLading}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.tariff">
                <strong>Tariff:</strong> {{parsedInvoice.shipmentDetails.tariff}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.paymentTerms">
                <strong>Payment Terms:</strong> {{parsedInvoice.shipmentDetails.paymentTerms}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.totalPieces !== undefined">
                <strong>Total Pieces:</strong> {{parsedInvoice.shipmentDetails.totalPieces}}
              </p>
              <p *ngIf="parsedInvoice.shipmentDetails.totalWeight">
                <strong>Total Weight:</strong> {{parsedInvoice.shipmentDetails.totalWeight}} lbs
              </p>
            </div>

            <!-- Shipper Information -->
            <div *ngIf="parsedInvoice.shipper" class="section">
              <h4>Shipper Information</h4>
              <div class="company-info">
                <p *ngIf="parsedInvoice.shipper.accountNumber">
                  <strong>Account No:</strong> {{parsedInvoice.shipper.accountNumber}}
                </p>
                <p *ngIf="parsedInvoice.shipper.name">
                  <strong>Name:</strong> {{parsedInvoice.shipper.name}}
                </p>
                <ng-container *ngIf="parsedInvoice.shipper.address">
                  <div *ngIf="parsedInvoice.shipper.address.fullAddress" class="address">
                    <p><strong>Address:</strong> {{parsedInvoice.shipper.address.fullAddress}}</p>
                  </div>
                </ng-container>
                <p *ngIf="parsedInvoice.shipper.phone">
                  <strong>Phone:</strong> {{parsedInvoice.shipper.phone}}
                </p>
              </div>
            </div>

            <!-- Consignee Information -->
            <div *ngIf="parsedInvoice.consignee" class="section">
              <h4>Consignee Information</h4>
              <div class="company-info">
                <p *ngIf="parsedInvoice.consignee.accountNumber">
                  <strong>Account No:</strong> {{parsedInvoice.consignee.accountNumber}}
                </p>
                <p *ngIf="parsedInvoice.consignee.name">
                  <strong>Name:</strong> {{parsedInvoice.consignee.name}}
                </p>
                <ng-container *ngIf="parsedInvoice.consignee.address">
                  <div *ngIf="parsedInvoice.consignee.address.fullAddress" class="address">
                    <p><strong>Address:</strong> {{parsedInvoice.consignee.address.fullAddress}}</p>
                  </div>
                </ng-container>
                <p *ngIf="parsedInvoice.consignee.phone">
                  <strong>Phone:</strong> {{parsedInvoice.consignee.phone}}
                </p>
              </div>
            </div>

            <!-- Line Items -->
            <div *ngIf="parsedInvoice.items?.length" class="section">
              <h4>Line Items</h4>
              <div class="line-items">
                <table>
                  <thead>
                    <tr>
                      <th>Pieces</th>
                      <th>Description</th>
                      <th>Weight (lbs)</th>
                      <th>Class</th>
                      <th>Rate</th>
                      <th>Charge</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of parsedInvoice.items">
                      <td>{{item.pieces}}</td>
                      <td>{{item.description}}</td>
                      <td>{{item.weight}}</td>
                      <td>{{item.class}}</td>
                      <td>{{item.rate}}</td>
                      <td>{{formatCurrency(item.charge)}}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Totals -->
            <div class="section totals">
              <p *ngIf="parsedInvoice.subTotal">
                <strong>Subtotal:</strong> {{formatCurrency(parsedInvoice.subTotal)}}
              </p>
              <p *ngIf="parsedInvoice.totalTax">
                <strong>Tax:</strong> {{formatCurrency(parsedInvoice.totalTax)}}
              </p>
              <p *ngIf="parsedInvoice.invoiceTotal">
                <strong>Total:</strong> {{formatCurrency(parsedInvoice.invoiceTotal)}}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InvoiceUploadComponent {
  isDragging = false;
  isLoading = false;
  parsedInvoice: ParsedInvoice | null = null;
  imagePreviewUrl: string | null = null;
  selectedParser: 'azure' | 'gemini' = 'gemini';

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
    this.invoiceService.parseInvoice(file, this.selectedParser).subscribe({
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
