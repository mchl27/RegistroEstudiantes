import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, switchMap, timer } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { PublicStudent } from '../../core/models';
import { SessionStore } from '../../core/session.store';
import { toProblem } from '../../shared/forms';
import { initials, normalize } from '../../shared/text';

const REFRESH_MS = 15_000;

/** Regla 8: registros de los demás estudiantes, actualizados "en línea". */
@Component({
  selector: 'app-directory-page',
  imports: [DatePipe],
  templateUrl: './directory-page.html',
})
export class DirectoryPage {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionStore);

  protected readonly initials = initials;
  protected readonly myId = computed(() => this.session.student()?.id ?? null);

  protected readonly students = signal<PublicStudent[]>([]);
  protected readonly query = signal('');
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly lastUpdated = signal<Date | null>(null);

  /** Búsqueda por nombre o materia, sin distinguir tildes ni mayúsculas. */
  protected readonly filtered = computed(() => {
    const q = normalize(this.query());
    if (!q) return this.students();
    return this.students().filter(
      (s) => normalize(s.fullName).includes(q) || s.subjects.some((sub) => normalize(sub).includes(q)),
    );
  });

  constructor() {
    // Consulta inmediata y luego cada 15 s mientras la pantalla esté abierta
    timer(0, REFRESH_MS)
      .pipe(
        switchMap(() =>
          this.api.getDirectory().pipe(
            catchError((err) => {
              this.error.set(toProblem(err).detail ?? 'No se pudo cargar el directorio.');
              this.loading.set(false);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((list) => {
        this.students.set(list);
        this.lastUpdated.set(new Date());
        this.error.set(null);
        this.loading.set(false);
      });
  }
}
