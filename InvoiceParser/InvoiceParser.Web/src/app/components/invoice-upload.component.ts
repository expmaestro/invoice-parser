import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { InvoiceService } from '../services/invoice.service';
import { ParsedInvoice } from '../models/invoice.model';
import { InvoiceDetailsComponent } from './invoice-details.component';

@Component({
  selector: 'app-invoice-upload',
  standalone: true,
  imports: [CommonModule, FormsModule, InvoiceDetailsComponent],
  styleUrls: ['./invoice-upload.component.scss'],
  templateUrl: './invoice-upload.component.html'
})
export class InvoiceUploadComponent {
  isDragging = false;
  isLoading = false;
  parsedInvoice: ParsedInvoice | null = null;
  imagePreviewUrl: string | null = null;
  selectedParser: 'azure' | 'gemini' = 'gemini';

  constructor(
    private invoiceService: InvoiceService,
    private toastr: ToastrService
  ) {}

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
      this.toastr.error('Please upload an image file', 'Invalid File Type');
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
        this.toastr.success('Invoice parsed successfully!', 'Success');
      },
      error: (error) => {
        console.error('Error parsing invoice:', error);
        const errorMessage = error.error?.message || error.message || 'Unknown error occurred';
        this.toastr.error(`Error parsing invoice: ${errorMessage}`, 'Parsing Failed');
        this.isLoading = false;
      }
    });
  }
}
