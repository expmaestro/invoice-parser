import { Routes } from '@angular/router';
import { InvoiceUploadComponent } from './components/invoice-upload.component';
import { ApiLogsComponent } from './components/api-logs.component';

export const routes: Routes = [
    { path: '', component: InvoiceUploadComponent },
    { path: 'logs', component: ApiLogsComponent },
];
