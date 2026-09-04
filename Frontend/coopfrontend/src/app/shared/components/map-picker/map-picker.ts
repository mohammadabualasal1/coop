import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';

import { GoogleMapsLoaderService } from '../../../core/services/google-maps-loader.service';

const AMMAN_CENTER: google.maps.LatLngLiteral = { lat: 31.9539, lng: 35.9106 };
const DEFAULT_ZOOM = 12;
const LOCATED_ZOOM = 15;
const UNABLE_TO_LOCATE_MESSAGE = 'تعذّر تحديد موقعك. اختر الموقع من الخريطة.';

@Component({
  selector: 'coop-map-picker',
  templateUrl: './map-picker.html',
  styleUrl: './map-picker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MapPickerComponent implements AfterViewInit, OnDestroy {
  private readonly loader = inject(GoogleMapsLoaderService);

  readonly latitude = input<number | null>(null);
  readonly longitude = input<number | null>(null);
  readonly height = input('320px');
  readonly readonly = input(false);

  readonly locationChange = output<{ latitude: number; longitude: number }>();

  private readonly mapContainer = viewChild<ElementRef<HTMLDivElement>>('mapContainer');
  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly loadFailed = signal(false);
  readonly geoErrorMessage = signal<string | null>(null);
  readonly locatingCurrent = signal(false);
  readonly currentLat = signal<number | null>(null);
  readonly currentLng = signal<number | null>(null);

  private map: google.maps.Map | null = null;
  private marker: google.maps.Marker | null = null;
  private autocomplete: google.maps.places.Autocomplete | null = null;
  private readonly listeners: google.maps.MapsEventListener[] = [];

  constructor() {
    effect(() => {
      const lat = this.latitude();
      const lng = this.longitude();

      if (lat == null || lng == null || !this.map || !this.marker) {
        return;
      }

      const position = { lat, lng };
      this.marker.setPosition(position);
      this.map.setCenter(position);
      this.currentLat.set(lat);
      this.currentLng.set(lng);
    });
  }

  ngAfterViewInit(): void {
    this.loader
      .load()
      .then(() => this.initMap())
      .catch(() => this.loadFailed.set(true));
  }

  ngOnDestroy(): void {
    this.listeners.forEach((listener) => listener.remove());
    this.listeners.length = 0;
    this.autocomplete = null;
    this.marker = null;
    this.map = null;
  }

  useCurrentLocation(): void {
    if (!navigator.geolocation) {
      this.geoErrorMessage.set(UNABLE_TO_LOCATE_MESSAGE);
      return;
    }

    this.geoErrorMessage.set(null);
    this.locatingCurrent.set(true);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.locatingCurrent.set(false);
        this.moveMarkerAndEmit(position.coords.latitude, position.coords.longitude);
        this.map?.setZoom(LOCATED_ZOOM);
      },
      () => {
        this.locatingCurrent.set(false);
        this.geoErrorMessage.set(UNABLE_TO_LOCATE_MESSAGE);
      }
    );
  }

  onFallbackCoordinateChange(field: 'latitude' | 'longitude', event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);

    if (Number.isNaN(value)) {
      return;
    }

    const lat = field === 'latitude' ? value : (this.currentLat() ?? AMMAN_CENTER.lat);
    const lng = field === 'longitude' ? value : (this.currentLng() ?? AMMAN_CENTER.lng);

    this.currentLat.set(lat);
    this.currentLng.set(lng);
    this.locationChange.emit({ latitude: lat, longitude: lng });
  }

  private initMap(): void {
    const container = this.mapContainer()?.nativeElement;

    if (!container) {
      this.loadFailed.set(true);
      return;
    }

    const lat = this.latitude();
    const lng = this.longitude();
    const hasPosition = lat != null && lng != null;
    const center = hasPosition ? { lat, lng } : AMMAN_CENTER;

    this.currentLat.set(center.lat);
    this.currentLng.set(center.lng);

    this.map = new google.maps.Map(container, {
      center,
      zoom: hasPosition ? LOCATED_ZOOM : DEFAULT_ZOOM,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: false
    });

    this.marker = new google.maps.Marker({
      position: center,
      map: this.map,
      draggable: !this.readonly()
    });

    if (!this.readonly()) {
      this.listeners.push(
        this.map.addListener('click', (event: google.maps.MapMouseEvent) => {
          if (event.latLng) {
            this.moveMarkerAndEmit(event.latLng.lat(), event.latLng.lng());
          }
        })
      );

      this.listeners.push(
        this.marker.addListener('dragend', () => {
          const position = this.marker?.getPosition();

          if (position) {
            this.moveMarkerAndEmit(position.lat(), position.lng());
          }
        })
      );

      this.setupAutocomplete();
    }

    setTimeout(() => {
      if (!this.map || !this.marker) {
        return;
      }

      google.maps.event.trigger(this.map, 'resize');
      this.map.setCenter(this.marker.getPosition() ?? center);
    });
  }

  private setupAutocomplete(): void {
    const input = this.searchInput()?.nativeElement;

    if (!input) {
      return;
    }

    this.autocomplete = new google.maps.places.Autocomplete(input, {
      componentRestrictions: { country: 'jo' },
      fields: ['geometry']
    });

    this.listeners.push(
      this.autocomplete.addListener('place_changed', () => {
        const location = this.autocomplete?.getPlace().geometry?.location;

        if (location) {
          this.moveMarkerAndEmit(location.lat(), location.lng());
          this.map?.setZoom(LOCATED_ZOOM);
        }
      })
    );
  }

  private moveMarkerAndEmit(lat: number, lng: number): void {
    const position = { lat, lng };
    this.marker?.setPosition(position);
    this.map?.setCenter(position);
    this.currentLat.set(lat);
    this.currentLng.set(lng);
    this.locationChange.emit({ latitude: lat, longitude: lng });
  }
}
