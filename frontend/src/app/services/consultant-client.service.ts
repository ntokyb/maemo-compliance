import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { TenantContextService } from './tenant-context.service';
import { TenantModulesService } from './tenant-modules.service';

export interface ConsultantClientDto {
  tenantId: string;
  tenantName: string;
  plan?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ConsultantClientService {
  private http = inject(HttpClient);
  private tenantContextService = inject(TenantContextService);
  private tenantModulesService = inject(TenantModulesService);
  
  private readonly STORAGE_KEY = 'maemo_active_client_id';
  private readonly API_URL = `${environment.apiBaseUrl}/api/consultants/me/clients`;
  
  private _clients = new BehaviorSubject<ConsultantClientDto[]>([]);
  private _activeClient = new BehaviorSubject<ConsultantClientDto | null>(null);
  private _loading = new BehaviorSubject<boolean>(false);

  /**
   * Observable of all available clients for the consultant.
   */
  get clients$(): Observable<ConsultantClientDto[]> {
    return this._clients.asObservable();
  }

  /**
   * Observable of the currently active client.
   */
  get activeClient$(): Observable<ConsultantClientDto | null> {
    return this._activeClient.asObservable();
  }

  /**
   * Observable indicating if clients are being loaded.
   */
  get loading$(): Observable<boolean> {
    return this._loading.asObservable();
  }

  /**
   * Gets the list of clients for the current consultant.
   */
  getClients(): Observable<ConsultantClientDto[]> {
    this._loading.next(true);
    
    return this.http.get<ConsultantClientDto[]>(this.API_URL).pipe(
      map(clients => clients.filter(c => c.isActive)), // Only return active clients
      tap(clients => {
        this._clients.next(clients);
        this._loading.next(false);
        
        // If no active client is set but clients are available, set the first one
        if (!this._activeClient.value && clients.length > 0) {
          const storedClientId = localStorage.getItem(this.STORAGE_KEY);
          const clientToSet = storedClientId 
            ? clients.find(c => c.tenantId === storedClientId) || clients[0]
            : clients[0];
          
          this.setActiveClient(clientToSet);
        }
      }),
      catchError(error => {
        console.error('Error loading consultant clients:', error);
        this._loading.next(false);
        this._clients.next([]);
        return of([]);
      })
    );
  }

  /**
   * Sets the active client and updates tenant context.
   */
  setActiveClient(client: ConsultantClientDto | null): void {
    if (!client) {
      this._activeClient.next(null);
      this.tenantContextService.clearTenantId();
      localStorage.removeItem(this.STORAGE_KEY);
      return;
    }

    this._activeClient.next(client);
    this.tenantContextService.setClientTenantId(client.tenantId);
    localStorage.setItem(this.STORAGE_KEY, client.tenantId);
    
    // Reload modules for the new tenant
    this.tenantModulesService.loadModules().subscribe({
      next: () => {
        console.log(`Switched to client: ${client.tenantName}`);
      },
      error: (err) => {
        console.error('Error loading modules for new client:', err);
      }
    });
  }

  /**
   * Gets the currently active client.
   */
  getActiveClient(): ConsultantClientDto | null {
    return this._activeClient.value;
  }

  /**
   * Initializes the service by loading clients and restoring active client.
   */
  initialize(): void {
    // Load clients
    this.getClients().subscribe();
    
    // Try to restore active client from storage
    const storedClientId = localStorage.getItem(this.STORAGE_KEY);
    if (storedClientId) {
      // Wait for clients to load, then set active client
      this.clients$.subscribe(clients => {
        if (clients.length > 0 && !this._activeClient.value) {
          const client = clients.find(c => c.tenantId === storedClientId);
          if (client) {
            this.setActiveClient(client);
          } else {
            // Stored client ID not found, use first available
            this.setActiveClient(clients[0]);
          }
        }
      });
    }
  }

  /**
   * Clears the active client.
   */
  clearActiveClient(): void {
    this.setActiveClient(null);
  }
}

