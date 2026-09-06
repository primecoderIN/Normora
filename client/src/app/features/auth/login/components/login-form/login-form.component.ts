import { Component, output, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Checkbox } from 'primeng/checkbox';
import { ProgressSpinner } from 'primeng/progressspinner';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, FormsModule, InputText, Checkbox, ProgressSpinner],
  templateUrl: './login-form.component.html',
})
export class LoginFormComponent {
  isProcessing = input<boolean>(false);

  onLogin = output<void>();
  onGithubLogin = output<void>();
  onGoogleLogin = output<void>();

  email = '';
  password = '';
  keepMeSignedIn = false;

  submitLogin() {
    this.onLogin.emit();
  }
}
