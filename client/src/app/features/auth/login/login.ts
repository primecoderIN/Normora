import { Component, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { LoginFormComponent } from './components/login-form/login-form.component';
import { LoginPromoComponent } from './components/login-promo/login-promo.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [LoginFormComponent, LoginPromoComponent],
  templateUrl: './login.html',
})
export class Login {
  private oidcSecurityService = inject(OidcSecurityService);

  get isProcessingLogin(): boolean {
    return window.location.search.includes('code=') || window.location.search.includes('state=');
  }

  login() {
    this.oidcSecurityService.authorize();
  }

  loginWithGithub() {
    try {
      this.oidcSecurityService.authorize(undefined, {
        customParams: {
          kc_idp_hint: 'github',
        },
      });
    } catch (e) {
      console.error('Error during authorize call:', e);
    }
  }

  loginWithGoogle() {
    try {
      this.oidcSecurityService.authorize(undefined, {
        customParams: {
          kc_idp_hint: 'google',
        },
      });
    } catch (e) {
      console.error('Error during authorize call:', e);
    }
  }
}
