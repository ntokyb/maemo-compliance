import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/tenants', label: 'Tenants', icon: 'people' },
    { path: '/workers', label: 'Workers', icon: 'settings' },
    { path: '/logs/business', label: 'Business Logs', icon: 'description' },
    { path: '/popia', label: 'POPIA Compliance', icon: 'security' },
    { path: '/evidence', label: 'Evidence Register', icon: 'folder' },
    { path: '/licenses', label: 'Licenses', icon: 'key', disabled: true },
    { path: '/modules', label: 'Modules', icon: 'extension', disabled: true }
  ];
}

