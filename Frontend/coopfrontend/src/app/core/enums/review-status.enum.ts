import { BadgeTone } from './badge-tone';

export enum ReviewStatus {
  Visible = 0,
  Hidden = 1
}

export const ReviewStatusLabels: Record<ReviewStatus, string> = {
  [ReviewStatus.Visible]: 'ظاهر',
  [ReviewStatus.Hidden]: 'مخفي'
};

export const ReviewStatusTones: Record<ReviewStatus, BadgeTone> = {
  [ReviewStatus.Visible]: 'success',
  [ReviewStatus.Hidden]: 'neutral'
};
