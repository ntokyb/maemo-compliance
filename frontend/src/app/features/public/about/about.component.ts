import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss'
})
export class AboutComponent {}
