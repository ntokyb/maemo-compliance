import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ConsultantService } from '../../../services/consultant.service';
import { AuditTemplate, AuditQuestion, CreateAuditTemplateRequest, AddAuditQuestionRequest } from '../../../models/consultant.model';

@Component({
  selector: 'app-audit-templates',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './audit-templates.html',
  styleUrl: './audit-templates.scss',
})
export class AuditTemplatesComponent implements OnInit {
  private consultantService = inject(ConsultantService);

  templates: AuditTemplate[] = [];
  selectedTemplate: AuditTemplate | null = null;
  questions: AuditQuestion[] = [];
  
  showCreateTemplate = false;
  showAddQuestion = false;
  
  newTemplate: CreateAuditTemplateRequest = {
    name: '',
    description: ''
  };
  
  newQuestion: AddAuditQuestionRequest = {
    category: '',
    questionText: '',
    maxScore: 5
  };
  
  loading = false;
  saving = false;
  error: string | null = null;

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.loading = true;
    this.error = null;

    this.consultantService.getAuditTemplates().subscribe({
      next: (templates) => {
        this.templates = templates;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load audit templates';
        this.loading = false;
        console.error('Error loading templates:', err);
      }
    });
  }

  selectTemplate(template: AuditTemplate): void {
    this.selectedTemplate = template;
    this.showAddQuestion = false;
    this.loadQuestions(template.id);
  }

  loadQuestions(templateId: string): void {
    this.consultantService.getAuditQuestions(templateId).subscribe({
      next: (questions) => {
        this.questions = questions;
      },
      error: (err) => {
        console.error('Error loading questions:', err);
      }
    });
  }

  async createTemplate(): Promise<void> {
    if (!this.newTemplate.name.trim()) {
      this.error = 'Template name is required';
      return;
    }

    this.saving = true;
    this.error = null;

    try {
      const result = await this.consultantService.createAuditTemplate(this.newTemplate).toPromise();
      if (result) {
        this.showCreateTemplate = false;
        this.newTemplate = { name: '', description: '' };
        this.loadTemplates();
      }
    } catch (err: any) {
      this.error = err.message || 'Failed to create template';
      console.error('Error creating template:', err);
    } finally {
      this.saving = false;
    }
  }

  async addQuestion(): Promise<void> {
    if (!this.selectedTemplate) {
      this.error = 'Please select a template first';
      return;
    }

    if (!this.newQuestion.category.trim() || !this.newQuestion.questionText.trim()) {
      this.error = 'Category and question text are required';
      return;
    }

    this.saving = true;
    this.error = null;

    try {
      const result = await this.consultantService.addAuditQuestion(
        this.selectedTemplate.id,
        this.newQuestion
      ).toPromise();
      
      if (result) {
        this.showAddQuestion = false;
        this.newQuestion = { category: '', questionText: '', maxScore: 5 };
        this.loadQuestions(this.selectedTemplate.id);
      }
    } catch (err: any) {
      this.error = err.message || 'Failed to add question';
      console.error('Error adding question:', err);
    } finally {
      this.saving = false;
    }
  }

  cancelCreateTemplate(): void {
    this.showCreateTemplate = false;
    this.newTemplate = { name: '', description: '' };
  }

  cancelAddQuestion(): void {
    this.showAddQuestion = false;
    this.newQuestion = { category: '', questionText: '', maxScore: 5 };
  }
}

