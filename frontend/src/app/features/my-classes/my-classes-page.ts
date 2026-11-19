import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { MyEnrollment } from '../../core/models';
import { toProblem } from '../../shared/forms';
import { initials } from '../../shared/text';

/** Regla 9: mis clases con SOLO el nombre de los compañeros. */
@Component({
  selector: 'app-my-classes-page',
  imports: [RouterLink],
  templateUrl: './my-classes-page.html',
})
export class MyClassesPage {
  private readonly api = inject(ApiService);

  protected readonly initials = initials;
  protected readonly enrollment = signal<MyEnrollment | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.api.getMyEnrollment().subscribe({
      next: (data) => {
        this.enrollment.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No se pudieron cargar tus clases.');
        this.loading.set(false);
      },
    });
  }
}
