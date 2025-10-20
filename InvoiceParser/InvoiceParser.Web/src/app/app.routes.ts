import { Routes } from '@angular/router';
import { InvoiceUploadComponent } from './components/invoice-upload.component';
import { ApiLogsComponent } from './components/api-logs.component';
import { WeatherRouteComponent } from './components/weather-route.component';

export const routes: Routes = [
    { path: '', component: InvoiceUploadComponent },
    { path: 'logs', component: ApiLogsComponent },
    { path: 'weather-route', component: WeatherRouteComponent },
];
