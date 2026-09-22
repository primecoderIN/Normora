import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '@core/services/user.service';
import { InvitationService } from '@core/services/invitation.service';
import { TenantService } from '@core/services/tenant.service';
import { ThemeService } from '@core/services/theme.service';
import { TenantBrandingService } from '@core/services/tenant-branding.service';
import { signal } from '@angular/core';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, SelectModule, FormsModule, CommonModule],
  selector: 'app-employee-layout',

  templateUrl: './employee-layout.html',
})
export class EmployeeLayout {
  private authService = inject(AuthService);
  public userService = inject(UserService);
  public tenantService = inject(TenantService);
  public themeService = inject(ThemeService);
  public brandingService = inject(TenantBrandingService);
  private invitationService = inject(InvitationService);
  
  isAccepting = signal(false);

  // Retrieve the currently active workspace, falling back to their first available workspace if none is explicitly selected
  get activeWorkspace() {
    const user = this.userService.currentUser();
    if (!user) return null;
    const activeId = this.userService.activeTenantId();
    return user.memberships.find(m => m.tenantId === activeId) || user.memberships[0];
  }

  get otherWorkspaces() {
    const user = this.userService.currentUser();
    if (!user) return [];
    const activeId = this.activeWorkspace?.tenantId;
    return user.memberships.filter(m => m.tenantId !== activeId);
  }

  get allWorkspaces() {
    return this.userService.currentUser()?.memberships || [];
  }


  // Switch the user's active workspace and enforce role-based redirection if they are an admin in the new workspace
  changeWorkspace(tenantId: string) {
    this.userService.activeTenantId.set(tenantId);
    
    // Check if the new workspace is admin
    const newWorkspace = this.userService.currentUser()?.memberships.find(m => m.tenantId === tenantId);
    if (newWorkspace) {
      if (newWorkspace.role === 'admin') {
        window.location.href = `/app/workspaces/${newWorkspace.tenantSlug}/employer/dashboard`;
      } else {
        window.location.href = `/app/workspaces/${newWorkspace.tenantSlug}/employee/conversations`;
      }
    } else {
      window.location.reload();
    }
  }

  logout() {
    this.authService.logout();
  }

  acceptInvite(token: string) {
    if (this.isAccepting()) return;
    this.isAccepting.set(true);
    this.invitationService.acceptInvitation(token).subscribe({
      next: () => window.location.reload(),
      error: (err: any) => {
        alert(err.error?.message || 'Failed to accept invitation');
        this.isAccepting.set(false);
      }
    });
  }
}
