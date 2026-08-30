import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe, NgIf } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '../../core/services/user.service';
import { Avatar } from 'primeng/avatar';

@Component({
  selector: 'app-employer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Avatar, AsyncPipe, NgIf],
  templateUrl: './employer-layout.html',
})
export class EmployerLayout {
  private oidcSecurityService = inject(OidcSecurityService);
  public userService = inject(UserService);

  logout() {
    this.oidcSecurityService.logoff().subscribe();
  }
}
