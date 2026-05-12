import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConsultantService } from '../../../services/consultant.service';
import { AuditTemplate, AuditQuestion, AuditRun, AuditAnswer, ConsultantClient, StartAuditRunRequest, SubmitAuditAnswerRequest } from '../../../models/consultant.model';
import { TenantContextService } from '../../../services/tenant-context.service';

@Component({
  selector: 'app-audit-run',
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-run.html',
  styleUrl: './audit-run.scss',
})
export class AuditRunComponent implements OnInit {
  private consultantService = inject(ConsultantService);
  private tenantContextService = inject(TenantContextService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  templates: AuditTemplate[] = [];
  clients: ConsultantClient[] = [];
  questions: AuditQuestion[] = [];
  answers: AuditAnswer[] = [];
  
  selectedClientId: string | null = null;
  selectedTemplateId: string | null = null;
  currentRun: AuditRun | null = null;
  
  showStartRun = false;
  startRunRequest: StartAuditRunRequest = {
    auditTemplateId: '',
    auditorUserId: undefined
  };
  
  loading = false;
  saving = false;
  error: string | null = null;
  isCompleting = false;

  get isCompleted(): boolean {
    return !!this.currentRun?.completedAt;
  }

  ngOnInit(): void {
    this.loadTemplates();
    this.loadClients();
    
    // Check if we have a run ID in route
    const runId = this.route.snapshot.params['runId'];
    if (runId) {
      this.loadRun(runId);
    }
  }

  loadTemplates(): void {
    this.consultantService.getAuditTemplates().subscribe({
      next: (templates) => {
        this.templates = templates;
      },
      error: (err) => {
        console.error('Error loading templates:', err);
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

  async startRun(): Promise<void> {
    if (!this.selectedClientId || !this.startRunRequest.auditTemplateId) {
      this.error = 'Please select a client and template';
      return;
    }

    // Switch tenant context
    this.tenantContextService.setTenantId(this.selectedClientId);

    this.saving = true;
    this.error = null;

    try {
      const result = await firstValueFrom(this.consultantService.startAuditRun(this.startRunRequest));
      if (result) {
        this.showStartRun = false;
        await this.loadRun(result.id);
        this.router.navigate(['/consultant/audit-run', result.id]);
      }
    } catch (err: any) {
      this.error = err.message || 'Failed to start audit run';
      console.error('Error starting audit run:', err);
    } finally {
      this.saving = false;
    }
  }

  async loadRun(runId: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const run = await firstValueFrom(this.consultantService.getAuditRun(runId));
      this.currentRun = run;
      this.tenantContextService.setTenantId(run.tenantId);
      this.selectedTemplateId = run.auditTemplateId;

      const [questions, answers] = await Promise.all([
        firstValueFrom(this.consultantService.getAuditQuestions(run.auditTemplateId)),
        firstValueFrom(this.consultantService.getAuditAnswers(runId)),
      ]);
      this.questions = questions ?? [];
      this.answers = answers ?? [];
    } catch (err: any) {
      this.error = err.message || 'Failed to load audit run';
      console.error('Error loading audit run:', err);
    } finally {
      this.loading = false;
    }
  }

  async completeAudit(): Promise<void> {
    if (!this.currentRun || this.isCompleted) {
      return;
    }
    this.isCompleting = true;
    this.error = null;
    try {
      const run = await firstValueFrom(this.consultantService.completeAuditRun(this.currentRun.id));
      this.currentRun = run;
    } catch (err: any) {
      this.error = err.error?.error ?? err.message ?? 'Failed to complete audit run';
      console.error('Error completing audit run:', err);
    } finally {
      this.isCompleting = false;
    }
  }

  getAnswerForQuestion(questionId: string): AuditAnswer | undefined {
    return this.answers.find(a => a.auditQuestionId === questionId);
  }

  async submitAnswer(question: AuditQuestion): Promise<void> {
    if (!this.currentRun) {
      this.error = 'No active audit run';
      return;
    }
    if (this.isCompleted) {
      return;
    }

    const existingAnswer = this.getAnswerForQuestion(question.id);
    const score = existingAnswer?.score ?? 0;
    
    if (score < 0 || score > question.maxScore) {
      this.error = `Score must be between 0 and ${question.maxScore}`;
      return;
    }

    const request: SubmitAuditAnswerRequest = {
      auditQuestionId: question.id,
      score: score,
      comment: existingAnswer?.comment,
      evidenceFileUrl: existingAnswer?.evidenceFileUrl
    };

    try {
      await firstValueFrom(this.consultantService.submitAuditAnswer(this.currentRun.id, request));
      // Reload answers
      await this.loadRun(this.currentRun.id);
    } catch (err: any) {
      this.error = err.message || 'Failed to submit answer';
      console.error('Error submitting answer:', err);
    }
  }

  async uploadEvidence(questionId: string, event: Event): Promise<void> {
    if (!this.currentRun) {
      this.error = 'No active audit run';
      return;
    }
    if (this.isCompleted) {
      return;
    }

    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    
    try {
      await firstValueFrom(
        this.consultantService.uploadAuditEvidence(this.currentRun.id, questionId, file)
      );
      await this.loadRun(this.currentRun.id);
    } catch (err: any) {
      this.error = err.message || 'Failed to upload evidence';
      console.error('Error uploading evidence:', err);
    }
  }

  updateAnswerScore(questionId: string, score: number): void {
    if (this.isCompleted) {
      return;
    }
    const answer = this.getAnswerForQuestion(questionId);
    if (answer) {
      answer.score = score;
    }
  }

  updateAnswerComment(questionId: string, comment: string): void {
    if (this.isCompleted) {
      return;
    }
    const answer = this.getAnswerForQuestion(questionId);
    if (answer) {
      answer.comment = comment;
    }
  }
}

