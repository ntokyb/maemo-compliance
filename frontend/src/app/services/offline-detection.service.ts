import { Injectable } from '@angular/core';
import { BehaviorSubject, fromEvent, merge, of } from 'rxjs';
import { map } from 'rxjs/operators';

/**
 * Service for detecting online/offline status.
 */
@Injectable({
  providedIn: 'root'
})
export class OfflineDetectionService {
  private onlineStatusSubject = new BehaviorSubject<boolean>(navigator.onLine);
  public onlineStatus$ = this.onlineStatusSubject.asObservable();

  constructor() {
    // Listen to online/offline events
    merge(
      fromEvent(window, 'online').pipe(map(() => true)),
      fromEvent(window, 'offline').pipe(map(() => false)),
      of(navigator.onLine)
    ).subscribe(isOnline => {
      this.onlineStatusSubject.next(isOnline);
    });
  }

  /**
   * Check if currently online.
   */
  isOnline(): boolean {
    return navigator.onLine;
  }

  /**
   * Get current online status.
   */
  getOnlineStatus(): boolean {
    return this.onlineStatusSubject.value;
  }
}

