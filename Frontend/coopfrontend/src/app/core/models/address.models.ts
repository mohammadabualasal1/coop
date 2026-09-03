export interface Address {
  id: string;
  label: string;
  contactName: string;
  contactPhone: string;
  city: string;
  area: string;
  street: string;
  building: string | null;
  floor: string | null;
  additionalDirections: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}

export interface AddressRequest {
  label: string;
  contactName: string;
  contactPhone: string;
  city: string;
  area: string;
  street: string;
  building: string | null;
  floor: string | null;
  additionalDirections: string | null;
  latitude: number;
  longitude: number;
}
