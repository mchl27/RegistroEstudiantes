import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { SessionStore } from '../../core/session.store';
import {
  ServerErrors,
  controlError,
  fieldErrorsFrom,
  profileValidators,
  toProblem,
} from '../../shared/forms';

type Field = 'firstName' | 'lastName' | 'documentNumber' | 'email';

/** Regla 1: Read, Update y Delete del propio registro. */
@Component({
  selector: 'app-profile-page',
  imports: [ReactiveFormsModule],
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', profileValidators.firstName],
    lastName: ['', profileValidators.lastName],
    documentNumber: ['', profileValidators.documentNumber],
    email: ['', profileValidators.email],
  });

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly deleting = signal(false);
  protected readonly confirmDelete = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly serverErrors = signal<ServerErrors>({});

  constructor() {
    this.api.getProfile().subscribe({
      next: (profile) => {
        this.form.reset({
          firstName: profile.firstName,
          lastName: profile.lastName,
          documentNumber: profile.documentNumber,
          email: profile.email,
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No se pudo cargar tu perfil.');
        this.loading.set(false);
      },
    });

    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.success.set(null);
      if (Object.keys(this.serverErrors()).length) this.serverErrors.set({});
    });
  }

  protected fieldError(name: Field): string | null {
    return controlError(this.form.controls[name], {
      server: this.serverErrors()[name]?.[0],
      pattern: name === 'documentNumber' ? 'Solo dígitos, entre 5 y 20.' : undefined,
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set(null);

    this.api.updateProfile(this.form.getRawValue()).subscribe({
      next: (profile) => {
        this.session.updateStudent(profile);
        this.form.reset(this.form.getRawValue());
        this.success.set('Tus datos se actualizaron correctamente.');
        this.saving.set(false);
      },
      error: (err) => {
        const problem = toProblem(err);
        const fieldErrors = fieldErrorsFrom(problem);
        this.serverErrors.set(fieldErrors);
        this.error.set(Object.keys(fieldErrors).length ? null : (problem.detail ?? problem.title));
        this.saving.set(false);
      },
    });
  }

  protected deleteAccount(): void {
    this.deleting.set(true);
    this.api.deleteAccount().subscribe({
      next: () => {
        this.session.clear();
        this.router.navigate(['/login'], { queryParams: { deleted: 1 } });
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No se pudo eliminar la cuenta.');
        this.deleting.set(false);
        this.confirmDelete.set(false);
      },
    });
  }
}
