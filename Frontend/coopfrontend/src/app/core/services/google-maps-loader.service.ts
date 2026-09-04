import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

const SCRIPT_ID = 'coop-google-maps-script';

@Injectable({ providedIn: 'root' })
export class GoogleMapsLoaderService {
  private loadPromise: Promise<void> | null = null;

  load(): Promise<void> {
    if (typeof google !== 'undefined' && google.maps) {
      return Promise.resolve();
    }

    if (this.loadPromise) {
      return this.loadPromise;
    }

    this.loadPromise = new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.id = SCRIPT_ID;
      script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.mapsApiKey}&libraries=places&language=ar&region=JO`;
      script.async = true;

      script.onload = () => resolve();
      script.onerror = () => {
        script.remove();
        this.loadPromise = null;
        reject(new Error('تعذّر تحميل خرائط Google'));
      };

      document.head.appendChild(script);
    });

    return this.loadPromise;
  }
}
