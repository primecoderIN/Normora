import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';

@Component({
  selector: 'app-employer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, AvatarModule],
  templateUrl: './employer-layout.html',
})
export class EmployerLayout {
  private oidcSecurityService = inject(OidcSecurityService);

  logout() {
    this.oidcSecurityService.logoff().subscribe();
  }
}
