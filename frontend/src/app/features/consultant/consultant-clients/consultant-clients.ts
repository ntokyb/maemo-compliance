import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConsultantService } from '../../../services/consultant.service';
import { ConsultantClient } from '../../../models/consultant.model';

@Component({
  selector: 'app-consultant-clients',
  imports: [CommonModule],
  templateUrl: './consultant-clients.html',
  styleUrl: './consultant-clients.scss',
})
export class ConsultantClientsComponent implements OnInit {
  private consultantService = inject(ConsultantService);

  clients: ConsultantClient[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.loading = true;
    this.error = null;

    this.consultantService.getClients().subscribe({
      next: (clients) => {
        this.clients = clients;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load clients';
        this.loading = false;
        console.error('Error loading clients:', err);
      }
    });
  }
}

