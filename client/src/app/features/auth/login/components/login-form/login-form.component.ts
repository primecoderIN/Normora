import { Component, output, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressSpinner } from 'primeng/progressspinner';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, ProgressSpinner],
  templateUrl: './login-form.component.html',
})
export class LoginFormComponent {
  isProcessing = input<boolean>(false);

  onLogin = output<void>();
  onGithubLogin = output<void>();
  onGoogleLogin = output<void>();



  submitLogin() {
    this.onLogin.emit();
  }
}
