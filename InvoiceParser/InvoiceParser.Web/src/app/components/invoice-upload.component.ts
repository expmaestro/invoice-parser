import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../services/invoice.service';
import { ParsedInvoice, CurrencyField, CompanyContact, ShipmentDetails, ShipmentItem } from '../models/invoice.model';

@Component({
  selector: 'app-invoice-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['./invoice-upload.component.scss'],
  templateUrl: './invoice-upload.component.html'
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
