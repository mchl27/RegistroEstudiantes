import { Injectable, computed, signal } from '@angular/core';
import { AuthResponse, StudentProfile } from './models';

const STORAGE_KEY = 'registro-estudiantes.session';

/**
 * Estado de la sesión con signals. No depende de HttpClient para que el
 * interceptor pueda usarlo sin crear una dependencia circular.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly session = signal<AuthResponse | null>(this.restore());

  readonly student = computed(() => this.session()?.student ?? null);
  readonly token = computed(() => this.session()?.token ?? null);
  readonly isAuthenticated = computed(() => {
    const current = this.session();
    return !!current && new Date(current.expiresAt).getTime() > Date.now();
  });

  start(response: AuthResponse): void {
    this.persist(response);
  }

  updateStudent(student: StudentProfile): void {
    const current = this.session();
    if (current) this.persist({ ...current, student });
  }

  clear(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* almacenamiento no disponible: la sesión vive solo en memoria */
    }
  }

  private persist(value: AuthResponse): void {
    this.session.set(value);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      /* almacenamiento no disponible */
    }
  }

  // Se ejecuta una sola vez al crear el servicio (al abrir o recargar la página).
  // Si el token guardado ya venció, lo ignoramos: así el usuario va directo al login
  // en vez de entrar y recibir un 401 en la primera petición.
  private restore(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const saved = JSON.parse(raw) as AuthResponse;
      return new Date(saved.expiresAt).getTime() > Date.now() ? saved : null;
    } catch {
      return null;
    }
  }
}
