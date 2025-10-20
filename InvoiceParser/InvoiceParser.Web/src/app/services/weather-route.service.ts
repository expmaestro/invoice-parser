import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WeatherRouteService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getWeatherRoute(origin: string, destination: string, startDate?: Date): Observable<WeatherRouteResponse> {
    return this.http.post<WeatherRouteResponse>(`${this.baseUrl}/weather-route/forecast`, { 
      origin, 
      destination,
      startDate: startDate?.toISOString()
    });
  }
}

export interface WeatherRouteResponse {
  summary: string;
  route: RouteInfo;
  weatherPoints: WeatherPoint[];
}

export interface RouteInfo {
  distance: string;
  duration: string;
  path: LatLng[];
}

export interface WeatherPoint {
  location: LatLng;
  locationName: string;
  temperature: number;
  description: string;
  precipitation: number;
  icon: string;
}

export interface LatLng {
  lat: number;
  lng: number;
}
