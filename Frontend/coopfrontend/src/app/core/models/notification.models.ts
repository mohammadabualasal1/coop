export interface AppNotification {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
}
