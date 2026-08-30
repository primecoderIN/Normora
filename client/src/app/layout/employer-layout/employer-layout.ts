import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe, NgIf } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '../../core/services/user.service';


@Component({
  selector: 'app-employer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, NgIf],
  templateUrl: './employer-layout.html',
})
export class EmployerLayout {
  private oidcSecurityService = inject(OidcSecurityService);
  public userService = inject(UserService);

  logout() {
    this.oidcSecurityService.getIdToken().subscribe((idToken) => {
      this.oidcSecurityService.logoff('', { customParams: { id_token_hint: idToken } }).subscribe();
    });
  }
}
