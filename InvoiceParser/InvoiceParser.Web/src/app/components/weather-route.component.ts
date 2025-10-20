import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ToastrService } from 'ngx-toastr';
import { WeatherRouteService, WeatherRouteResponse, WeatherPoint } from '../services/weather-route.service';

declare var google: any;

@Component({
  selector: 'app-weather-route',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatIconModule
  ],
  templateUrl: './weather-route.component.html',
  styleUrls: ['./weather-route.component.scss']
})
export class WeatherRouteComponent implements OnInit, AfterViewInit {
  origin = '';
  destination = '';
  startDate = '';
  today = new Date().toISOString().split('T')[0]; // Today's date in YYYY-MM-DD format
  loading = false;
  summary = '';
  route: any = null;
  weatherPoints: WeatherPoint[] = [];
  map: any;
  directionsService: any;
  directionsRenderer: any;

  constructor(
    private weatherRouteService: WeatherRouteService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    // Component initialization
  }

  ngAfterViewInit() {
    this.initializeMap();
  }

  initializeMap() {
    if (typeof google !== 'undefined') {
      this.map = new google.maps.Map(document.getElementById('map'), {
        zoom: 4,
        center: { lat: 39.8283, lng: -98.5795 }, // Center of USA
      });
      
      this.directionsService = new google.maps.DirectionsService();
      this.directionsRenderer = new google.maps.DirectionsRenderer();
      this.directionsRenderer.setMap(this.map);
    }
  }

  getForecast() {
    if (!this.origin || !this.destination) {
      this.toastr.error('Please enter both origin and destination');
      return;
    }

    this.loading = true;
    this.summary = '';
    this.weatherPoints = [];

    const startDateObj = this.startDate ? new Date(this.startDate) : undefined;
    this.weatherRouteService.getWeatherRoute(this.origin, this.destination, startDateObj)
      .subscribe({
        next: (response: WeatherRouteResponse) => {
          this.summary = response.summary;
          this.route = response.route;
          this.weatherPoints = response.weatherPoints;
          this.displayRoute();
          this.addWeatherMarkers();
          this.loading = false;
          this.toastr.success('Weather forecast generated successfully!');
        },
        error: (error) => {
          console.error('Error getting weather forecast:', error);
          this.loading = false;
          this.toastr.error('Failed to get weather forecast. Please try again.');
        }
      });
  }

  displayRoute() {
    if (this.directionsService && this.directionsRenderer) {
      this.directionsService.route({
        origin: this.origin,
        destination: this.destination,
        travelMode: google.maps.TravelMode.DRIVING
      }, (result: any, status: any) => {
        if (status === 'OK') {
          this.directionsRenderer.setDirections(result);
        }
      });
    }
  }

  addWeatherMarkers() {
    if (!this.map || !this.weatherPoints) return;

    this.weatherPoints.forEach((point, index) => {
      const marker = new google.maps.Marker({
        position: { lat: point.location.lat, lng: point.location.lng },
        map: this.map,
        title: `${point.locationName}: ${point.temperature}°C, ${point.description}`,
        icon: {
          url: `https://openweathermap.org/img/w/${point.icon}.png`,
          scaledSize: new google.maps.Size(40, 40)
        }
      });

      const infoWindow = new google.maps.InfoWindow({
        content: `
          <div>
            <h4>${point.locationName}</h4>
            <p><strong>Temperature:</strong> ${point.temperature}°C</p>
            <p><strong>Conditions:</strong> ${point.description}</p>
            <p><strong>Precipitation:</strong> ${point.precipitation}mm</p>
          </div>
        `
      });

      marker.addListener('click', () => {
        infoWindow.open(this.map, marker);
      });
    });
  }
}