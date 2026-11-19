import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { SessionStore } from '../../core/session.store';
import {
  ServerErrors,
  controlError,
  fieldErrorsFrom,
  passwordValidators,
  profileValidators,
  toProblem,
} from '../../shared/forms';
import { AuthHero } from './auth-hero';

type Field = 'firstName' | 'lastName' | 'documentNumber' | 'email' | 'password';

/** Regla 1 (Create): registro en línea del estudiante. */
@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, AuthHero],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', profileValidators.firstName],
    lastName: ['', profileValidators.lastName],
    documentNumber: ['', profileValidators.documentNumber],
    email: ['', profileValidators.email],
    password: ['', passwordValidators],
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly serverErrors = signal<ServerErrors>({});

  private readonly patternMessages: Partial<Record<Field, string>> = {
    documentNumber: 'Solo dígitos, entre 5 y 20.',
    password: 'Debe incluir letras y números.',
  };

  constructor() {
    // Al corregir el formulario, los errores del servidor dejan de aplicar
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (Object.keys(this.serverErrors()).length) this.serverErrors.set({});
    });
  }

  protected fieldError(name: Field): string | null {
    return controlError(this.form.controls[name], {
      server: this.serverErrors()[name]?.[0],
      pattern: this.patternMessages[name],
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.api.register(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.session.start(response);
        this.router.navigateByUrl('/inscripcion');
      },
      error: (err) => {
        const problem = toProblem(err);
        const fieldErrors = fieldErrorsFrom(problem);
        this.serverErrors.set(fieldErrors);
        this.error.set(Object.keys(fieldErrors).length ? null : (problem.detail ?? problem.title));
        this.loading.set(false);
      },
    });
  }
}
