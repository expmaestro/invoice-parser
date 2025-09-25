import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ParsedInvoice, CurrencyField } from '../models';

@Component({
  selector: 'app-invoice-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './invoice-details.component.html',
  styleUrls: ['./invoice-details.component.scss']
})
export class InvoiceDetailsComponent {
  @Input() parsedInvoice: ParsedInvoice | null = null;
  @Input() jsonContent: string | null = null;

  get displayInvoice(): ParsedInvoice | null {
    if (this.parsedInvoice) {
      return this.parsedInvoice;
    }
    
    if (this.jsonContent) {
      try {
        const parsedInvoice = JSON.parse(this.jsonContent) as ParsedInvoice;
        return parsedInvoice;
      } catch (error) {
        console.error('Error parsing JSON content:', error);
        console.error('JSON content was:', this.jsonContent);
        return null;
      }
    }
    
    return null;
  }

  formatCurrency(field?: CurrencyField): string {
    if (!field) return 'N/A';
    return `${field.currencySymbol || '$'}${field.amount.toFixed(2)}`;
  }
}
