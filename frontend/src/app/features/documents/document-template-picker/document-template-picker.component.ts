import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TemplateLibraryService, DocumentTemplateMeta } from '../../../services/template-library.service';

@Component({
  selector: 'app-document-template-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './document-template-picker.component.html',
  styleUrl: './document-template-picker.component.scss'
})
export class DocumentTemplatePickerComponent {
  private templateLibraryService = inject(TemplateLibraryService);

  @Output() templateSelected = new EventEmitter<DocumentTemplateMeta>();
  @Output() cancelled = new EventEmitter<void>();

  templates: DocumentTemplateMeta[] = [];
  filteredTemplates: DocumentTemplateMeta[] = [];
  selectedStandard: string | null = null;
  standards: string[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadTemplates();
    this.loadStandards();
  }

  loadTemplates(): void {
    this.loading = true;
    this.templateLibraryService.getDocumentTemplates(this.selectedStandard || undefined).subscribe({
      next: (templates) => {
        this.templates = templates;
        this.filteredTemplates = templates;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading templates:', err);
        this.loading = false;
      }
    });
  }

  loadStandards(): void {
    this.templateLibraryService.getStandards().subscribe({
      next: (standards) => {
        this.standards = standards;
      }
    });
  }

  onStandardFilterChange(): void {
    this.loadTemplates();
  }

  selectTemplate(template: DocumentTemplateMeta): void {
    this.templateSelected.emit(template);
  }

  cancel(): void {
    this.cancelled.emit();
  }
}

