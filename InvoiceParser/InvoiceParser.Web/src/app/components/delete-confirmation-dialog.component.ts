import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface DeleteConfirmationData {
  type: 'single' | 'all';
  count?: number;
}

@Component({
  selector: 'app-delete-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="delete-dialog">
      <h2 mat-dialog-title>
        <mat-icon class="warning-icon">warning</mat-icon>
        Confirm Delete
      </h2>
      
      <mat-dialog-content>
        <p *ngIf="data.type === 'single'">
          Are you sure you want to delete this API log? This action cannot be undone.
        </p>
        <p *ngIf="data.type === 'all'">
          Are you sure you want to delete ALL <strong>{{ data.count }}</strong> API logs? This action cannot be undone.
        </p>
      </mat-dialog-content>
      
      <mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">Cancel</button>
        <button mat-raised-button color="warn" (click)="onConfirm()">
          {{ data.type === 'all' ? 'Delete All' : 'Delete' }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .delete-dialog {
      min-width: 400px;
    }
    
    .warning-icon {
      color: #ff9800;
      margin-right: 8px;
      vertical-align: middle;
    }
    
    h2 {
      display: flex;
      align-items: center;
      margin: 0;
    }
    
    mat-dialog-content {
      margin: 20px 0;
    }
    
    mat-dialog-content p {
      margin: 0;
      line-height: 1.5;
    }
    
    mat-dialog-actions {
      gap: 8px;
    }
  `]
})
export class DeleteConfirmationDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<DeleteConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DeleteConfirmationData
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
