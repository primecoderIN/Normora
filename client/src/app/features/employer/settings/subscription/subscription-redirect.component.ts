import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '@core/services/user.service';

@Component({
  selector: 'app-subscription-redirect',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 p-6">
      <div class="text-center space-y-4 max-w-sm">
        <div class="w-12 h-12 mx-auto rounded-2xl bg-primary-500/10 text-primary-600 dark:text-primary-400 flex items-center justify-center">
          <i class="pi pi-spin pi-spinner text-2xl"></i>
        </div>
        <h2 class="text-lg font-bold text-slate-900 dark:text-white">Redirecting to your workspace...</h2>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Finalizing payment return and routing you to your workspace subscription settings.
        </p>
      </div>
    </div>
  `
})
export class SubscriptionRedirectComponent implements OnInit {
  private userService = inject(UserService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  ngOnInit(): void {
    // Fetch current user memberships to determine the active workspace slug
    this.userService.getMe().subscribe({
      next: (res) => {
        const memberships = res.data?.memberships || [];
        // Prioritize admin role workspace, fallback to first membership
        const activeTenant = memberships.find(m => m.role === 'admin') || memberships[0];
        const slug = activeTenant?.tenantSlug;

        // Preserve all query parameters (status=success, session_id=cs_test_..., etc.)
        const queryParams = this.route.snapshot.queryParams;

        if (slug) {
          // Route seamlessly to tenant-scoped subscription settings with Stripe session parameters
          this.router.navigate(['/app/workspaces', slug, 'employer', 'settings', 'subscription'], {
            queryParams,
            replaceUrl: true
          });
        } else {
          // If user has no active tenant memberships, direct to workspace onboarding
          this.router.navigate(['/onboarding'], { replaceUrl: true });
        }
      },
      error: () => {
        // Fallback to landing root if unauthenticated
        this.router.navigate(['/'], { replaceUrl: true });
      }
    });
  }
}
