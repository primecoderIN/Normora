import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DsTooltipDirective } from '@design-system';
import { ConversationService, SharedConversationDetailDto } from '@core/services/conversation.service';
import { MarkdownSanitizerService } from '@core/services/markdown-sanitizer.service';
import { ChunkPreviewDrawerComponent } from '@shared/components/chunk-preview-drawer/chunk-preview-drawer.component';
import { ApiMessages } from '@core/constants/api-messages';
import { UserService } from '@core/services/user.service';
import { AppRoutes } from '@core/constants/app-routes';

/**
 * Component responsible for fetching, displaying, and navigating a shared read-only conversation snapshot.
 * Adheres to SRP by delegating markdown rendering to MarkdownSanitizerService and
 * citation previews to ChunkPreviewDrawerComponent.
 */
@Component({
  selector: 'app-shared-conversation',
  standalone: true,
  imports: [CommonModule, RouterModule, DsTooltipDirective, DatePipe, ChunkPreviewDrawerComponent],
  templateUrl: './shared-conversation.html',
})
export class SharedConversationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private conversationService = inject(ConversationService);
  private userService = inject(UserService);
  public sanitizer = inject(MarkdownSanitizerService);

  @ViewChild('chunkDrawer') private chunkDrawer!: ChunkPreviewDrawerComponent;

  conversation = signal<SharedConversationDetailDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  copiedId = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.errorMessage.set(ApiMessages.InvalidShareToken);
      this.isLoading.set(false);
      return;
    }

    this.loadSharedConversation(token);
  }

  loadSharedConversation(token: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.conversationService.getSharedConversation(token).subscribe({
      next: (data) => {
        this.conversation.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(ApiMessages.SharedConversationNotFound);
        this.isLoading.set(false);
      }
    });
  }

  goToConversations(): void {
    const slug = this.userService.activeTenant()?.tenantSlug || this.route.snapshot.paramMap.get('slug');
    if (slug) {
      const isEmployer = window.location.pathname.includes('/employer/');
      const target = isEmployer 
        ? AppRoutes.WorkspaceEmployerDashboard(slug).replace('/dashboard', '/conversations') 
        : AppRoutes.WorkspaceEmployeeConversations(slug);
      this.router.navigateByUrl(target);
      return;
    }
    const match = window.location.pathname.match(/\/app\/workspaces\/[^/]+/);
    const workspacePath = match ? match[0] : '';
    this.router.navigateByUrl(`${workspacePath}/employee/conversations`);
  }

  copyToClipboard(text: string, id: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedId.set(id);
      setTimeout(() => this.copiedId.set(null), 2000);
    });
  }

  openPreview(chunkId: string): void {
    this.chunkDrawer.open(chunkId);
  }
}
