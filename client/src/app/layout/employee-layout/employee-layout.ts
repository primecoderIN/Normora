import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '@core/services/user.service';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, FormsModule],
  selector: 'app-employee-layout',
  styleUrl: './employee-layout.css',
  templateUrl: './employee-layout.html',
})
export class EmployeeLayout {
  private oidcSecurityService = inject(OidcSecurityService);
  public userService = inject(UserService);
  private router = inject(Router);

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

  changeWorkspace(tenantId: string) {
    this.userService.activeTenantId.set(tenantId);
    
    // Check if the new workspace is admin
    const newWorkspace = this.userService.currentUser()?.memberships.find(m => m.tenantId === tenantId);
    if (newWorkspace?.role === 'admin') {
      window.location.href = '/employer/dashboard';
    } else {
      window.location.reload();
    }
  }

  logout() {
    this.oidcSecurityService.getIdToken().subscribe((idToken) => {
      this.oidcSecurityService.logoff('', { customParams: { id_token_hint: idToken } }).subscribe();
    });
  }
}
