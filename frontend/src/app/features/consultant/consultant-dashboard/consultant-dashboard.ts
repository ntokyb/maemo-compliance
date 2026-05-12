import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ConsultantService } from '../../../services/consultant.service';
import { ConsultantDashboardSummary, ConsultantClient } from '../../../models/consultant.model';

@Component({
  selector: 'app-consultant-dashboard',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './consultant-dashboard.html',
  styleUrl: './consultant-dashboard.scss',
})
export class ConsultantDashboardComponent implements OnInit {
  private consultantService = inject(ConsultantService);

  summary: ConsultantDashboardSummary | null = null;
  clients: ConsultantClient[] = [];
  selectedClientId: string | null = null;
  error: string | null = null;
  loading = false;

  ngOnInit(): void {
    this.loadClients();
    this.loadSummary();
  }

  loadSummary(): void {
    this.loading = true;
    this.error = null;

    this.consultantService.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load dashboard summary';
        this.loading = false;
        console.error('Error loading dashboard summary:', err);
      }
    });
  }

  loadClients(): void {
    this.consultantService.getClients().subscribe({
      next: (clients) => {
        this.clients = clients.filter(c => c.isActive);
      },
      error: (err) => {
        console.error('Error loading clients:', err);
      }
    });
  }

  onClientChange(clientId: string): void {
    this.selectedClientId = clientId;
    // TODO: Switch tenant context and reload summary
    // For now, just store the selection
  }
}

