import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppNotification } from '../models/notification.models';

export interface UnreadCountResponse {
  unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/notifications`;

  private readonly _unreadCount = signal(0);
  readonly unreadCount = this._unreadCount.asReadonly();

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http
      .get<UnreadCountResponse>(`${this.baseUrl}/unread-count`)
      .pipe(tap((res) => this._unreadCount.set(res.unreadCount)));
  }

  getAll(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(this.baseUrl);
  }

  markRead(id: string): Observable<void> {
    return this.http
      .put<void>(`${this.baseUrl}/${id}/read`, null)
      .pipe(tap(() => this._unreadCount.update((count) => Math.max(0, count - 1))));
  }

  markAllRead(): Observable<void> {
    return this.http
      .put<void>(`${this.baseUrl}/read-all`, null)
      .pipe(tap(() => this._unreadCount.set(0)));
  }

  remove(id: string, wasUnread: boolean): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      tap(() => {
        if (wasUnread) {
          this._unreadCount.update((count) => Math.max(0, count - 1));
        }
      })
    );
  }
}
