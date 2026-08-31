import { BadgeTone } from './badge-tone';

export enum StockReservationStatus {
  Active = 0,
  Confirmed = 1,
  Released = 2,
  Expired = 3
}

export const StockReservationStatusLabels: Record<StockReservationStatus, string> = {
  [StockReservationStatus.Active]: 'محجوز',
  [StockReservationStatus.Confirmed]: 'مؤكد',
  [StockReservationStatus.Released]: 'تم التحرير',
  [StockReservationStatus.Expired]: 'منتهي'
};

export const StockReservationStatusTones: Record<StockReservationStatus, BadgeTone> = {
  [StockReservationStatus.Active]: 'warning',
  [StockReservationStatus.Confirmed]: 'success',
  [StockReservationStatus.Released]: 'neutral',
  [StockReservationStatus.Expired]: 'neutral'
};
