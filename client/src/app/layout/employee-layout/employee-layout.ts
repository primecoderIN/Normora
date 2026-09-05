import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '../../core/services/user.service';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-employee-layout',
  styleUrl: './employee-layout.css',
  templateUrl: './employee-layout.html',
})
export class EmployeeLayout {
  private oidcSecurityService = inject(OidcSecurityService);
  public userService = inject(UserService);

  logout() {
    this.oidcSecurityService.getIdToken().subscribe((idToken) => {
      this.oidcSecurityService.logoff('', { customParams: { id_token_hint: idToken } }).subscribe();
    });
  }
}
