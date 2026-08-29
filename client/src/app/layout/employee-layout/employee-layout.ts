import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Button } from 'primeng/button';

@Component({
  imports: [RouterOutlet, Button],
  selector: 'app-employee-layout',
  styleUrl: './employee-layout.css',
  templateUrl: './employee-layout.html',
})
export class EmployeeLayout {
  private oidcSecurityService = inject(OidcSecurityService);

  logout() {
    this.oidcSecurityService.logoff().subscribe();
  }
}
