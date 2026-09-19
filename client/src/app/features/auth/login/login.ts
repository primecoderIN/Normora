import { Component, inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { LoginFormComponent } from './components/login-form/login-form.component';
import { LoginPromoComponent } from './components/login-promo/login-promo.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [LoginFormComponent, LoginPromoComponent],
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);

  get isProcessingLogin(): boolean {
    return window.location.search.includes('code=') || window.location.search.includes('state=');
  }

  login() {
    this.authService.login();
  }

  loginWithGithub() {
    window.location.href = '/bff/login?provider=github';
  }

  loginWithGoogle() {
    window.location.href = '/bff/login?provider=google';
  }
}

