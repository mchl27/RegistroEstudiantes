import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Subject } from '../../core/models';
import { toProblem } from '../../shared/forms';
import { initials } from '../../shared/text';

/** Estado visual de cada materia en el selector. */
type SubjectState = 'selected' | 'professor-taken' | 'limit-reached' | 'available';

interface ProfessorGroup {
  professorId: number;
  professorName: string;
  subjects: Subject[];
}

const MAX_SUBJECTS = 3;
const CREDITS_PER_SUBJECT = 3;

/** Reglas 3 a 7: selección de exactamente 3 materias con profesores distintos. */
@Component({
  selector: 'app-enrollment-page',
  imports: [RouterLink],
  templateUrl: './enrollment-page.html',
})
export class EnrollmentPage {
  private readonly api = inject(ApiService);

  protected readonly totalCredits = MAX_SUBJECTS * CREDITS_PER_SUBJECT;
  protected readonly initials = initials;

  protected readonly subjects = signal<Subject[]>([]);
  /** Selección en edición (el orden se conserva para los "cupos"). */
  protected readonly selected = signal<number[]>([]);
  /** Lo que está guardado en el servidor. */
  protected readonly saved = signal<number[]>([]);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal(false);

  /** Las 10 materias agrupadas por su profesor (5 grupos de 2). */
  protected readonly groups = computed<ProfessorGroup[]>(() => {
    const byProfessor = new Map<number, ProfessorGroup>();
    for (const subject of this.subjects()) {
      const group = byProfessor.get(subject.professorId) ?? {
        professorId: subject.professorId,
        professorName: subject.professorName,
        subjects: [],
      };
      group.subjects.push(subject);
      byProfessor.set(subject.professorId, group);
    }
    return [...byProfessor.values()];
  });

  protected readonly credits = computed(() => this.selected().length * CREDITS_PER_SUBJECT);
  protected readonly isComplete = computed(() => this.selected().length === MAX_SUBJECTS);
  protected readonly hasChanges = computed(
    () => [...this.selected()].sort().join() !== [...this.saved()].sort().join(),
  );

  /** Los 3 cupos de la barra de resumen (null = cupo libre). */
  protected readonly slots = computed(() =>
    Array.from({ length: MAX_SUBJECTS }, (_, i) => this.findSubject(this.selected()[i]) ?? null),
  );

  constructor() {
    this.load();
  }

  protected stateOf(subject: Subject): SubjectState {
    const selected = this.selected();
    if (selected.includes(subject.id)) return 'selected';
    // Regla 7: no dos materias con el mismo profesor
    if (selected.some((id) => this.findSubject(id)?.professorId === subject.professorId)) {
      return 'professor-taken';
    }
    // Regla 5: máximo 3 materias
    if (selected.length >= MAX_SUBJECTS) return 'limit-reached';
    return 'available';
  }

  protected toggle(subject: Subject): void {
    const state = this.stateOf(subject);
    if (state === 'selected') {
      this.selected.update((ids) => ids.filter((id) => id !== subject.id));
    } else if (state === 'available') {
      this.selected.update((ids) => [...ids, subject.id]);
    } else {
      return;
    }
    this.error.set(null);
    this.success.set(false);
  }

  protected reset(): void {
    this.selected.set(this.saved());
    this.error.set(null);
  }

  protected save(): void {
    if (!this.isComplete()) return;
    this.saving.set(true);
    this.error.set(null);

    this.api.enroll(this.selected()).subscribe({
      next: (enrollment) => {
        const ids = enrollment.classes.map((c) => c.subjectId);
        this.saved.set(ids);
        this.selected.set(ids);
        this.success.set(true);
        this.saving.set(false);
        this.refreshCounts();
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No se pudo guardar la inscripción.');
        this.saving.set(false);
      },
    });
  }

  private load(): void {
    forkJoin({ subjects: this.api.getSubjects(), mine: this.api.getMyEnrollment() }).subscribe({
      next: ({ subjects, mine }) => {
        const ids = mine.classes.map((c) => c.subjectId);
        this.subjects.set(subjects);
        this.saved.set(ids);
        this.selected.set(ids);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(toProblem(err).detail ?? 'No se pudieron cargar las materias.');
        this.loading.set(false);
      },
    });
  }

  /** Actualiza el número de inscritos por materia después de guardar. */
  private refreshCounts(): void {
    this.api.getSubjects().subscribe((subjects) => this.subjects.set(subjects));
  }

  private findSubject(id: number | undefined): Subject | undefined {
    return id === undefined ? undefined : this.subjects().find((s) => s.id === id);
  }
}
