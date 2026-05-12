import { Injectable } from '@angular/core';
import { Observable, from } from 'rxjs';

/**
 * Service for managing IndexedDB operations.
 * Provides a simple interface for storing and retrieving data offline.
 */
@Injectable({
  providedIn: 'root'
})
export class IndexedDbService {
  private dbName = 'maemo-offline-db';
  private dbVersion = 1;
  private db: IDBDatabase | null = null;

  /**
   * Initialize the IndexedDB database.
   */
  async init(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.dbVersion);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => {
        this.db = request.result;
        resolve();
      };

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;

        // Create object stores if they don't exist
        if (!db.objectStoreNames.contains('pendingNcrs')) {
          const ncrStore = db.createObjectStore('pendingNcrs', { keyPath: 'id', autoIncrement: true });
          ncrStore.createIndex('createdAt', 'createdAt', { unique: false });
        }
      };
    });
  }

  /**
   * Get all items from a store.
   */
  getAll<T>(storeName: string): Observable<T[]> {
    return from(
      new Promise<T[]>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readonly');
        const store = transaction.objectStore(storeName);
        const request = store.getAll();

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result || []);
      })
    );
  }

  /**
   * Add an item to a store.
   */
  add<T>(storeName: string, item: T): Observable<IDBValidKey> {
    return from(
      new Promise<IDBValidKey>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readwrite');
        const store = transaction.objectStore(storeName);
        const request = store.add(item);

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
      })
    );
  }

  /**
   * Update an item in a store.
   */
  update<T>(storeName: string, item: T & { id: IDBValidKey }): Observable<IDBValidKey> {
    return from(
      new Promise<IDBValidKey>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readwrite');
        const store = transaction.objectStore(storeName);
        const request = store.put(item);

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
      })
    );
  }

  /**
   * Delete an item from a store.
   */
  delete(storeName: string, id: IDBValidKey): Observable<void> {
    return from(
      new Promise<void>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readwrite');
        const store = transaction.objectStore(storeName);
        const request = store.delete(id);

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
      })
    );
  }

  /**
   * Get a single item by ID.
   */
  getById<T>(storeName: string, id: IDBValidKey): Observable<T | undefined> {
    return from(
      new Promise<T | undefined>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readonly');
        const store = transaction.objectStore(storeName);
        const request = store.get(id);

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
      })
    );
  }

  /**
   * Clear all items from a store.
   */
  clear(storeName: string): Observable<void> {
    return from(
      new Promise<void>((resolve, reject) => {
        if (!this.db) {
          reject(new Error('Database not initialized'));
          return;
        }

        const transaction = this.db.transaction([storeName], 'readwrite');
        const store = transaction.objectStore(storeName);
        const request = store.clear();

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
      })
    );
  }
}

