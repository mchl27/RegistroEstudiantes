import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SessionStore } from '../core/session.store';
import { initials } from '../shared/text';

/** Marco de la aplicación autenticada: barra superior + contenido. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
})
export class Shell {
  private readonly router = inject(Router);
  protected readonly session = inject(SessionStore);

  protected readonly fullName = computed(() => {
    const student = this.session.student();
    return student ? `${student.firstName} ${student.lastName}` : '';
  });
  protected readonly initials = computed(() => initials(this.fullName()));

  protected readonly links = [
    { path: '/inscripcion', label: 'Inscripción' },
    { path: '/mis-clases', label: 'Mis clases' },
    { path: '/directorio', label: 'Directorio' },
    { path: '/perfil', label: 'Mi perfil' },
  ];

  protected logout(): void {
    this.session.clear();
    this.router.navigate(['/login']);
  }
}
