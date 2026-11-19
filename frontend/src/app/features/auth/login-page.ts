import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { SessionStore } from '../../core/session.store';
import { controlError, toProblem } from '../../shared/forms';
import { AuthHero } from './auth-hero';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, AuthHero],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly params = inject(ActivatedRoute).snapshot.queryParamMap;

  /** Estudiantes creados por DemoDataSeeder en la API (solo entorno de desarrollo). */
  protected readonly demoPassword = 'Demo1234';
  protected readonly demoAccounts = [
    { name: 'Ana García', email: 'ana.garcia@registro.test' },
    { name: 'Luis Martínez', email: 'luis.martinez@registro.test' },
    { name: 'Sofía Rodríguez', email: 'sofia.rodriguez@registro.test' },
    { name: 'Mateo López', email: 'mateo.lopez@registro.test' },
  ];

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = this.params.has('expired')
    ? 'Tu sesión expiró. Ingresa de nuevo.'
    : this.params.has('deleted')
      ? 'Tu cuenta fue eliminada. Puedes registrarte de nuevo cuando quieras.'
      : null;

  protected fieldError(name: 'email' | 'password'): string | null {
    return controlError(this.form.controls[name]);
  }

  protected useDemo(email: string): void {
    this.form.setValue({ email, password: this.demoPassword });
    this.error.set(null);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.api.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.session.start(response);
        this.router.navigateByUrl('/inscripcion');
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No fue posible iniciar sesión.');
        this.loading.set(false);
      },
    });
  }
}
