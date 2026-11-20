import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionStore } from './session.store';

/** Agrega el JWT a cada llamada a la API y cierra la sesión si el token ya no es válido. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const token = session.token();

  // Las peticiones HTTP son inmutables: no se modifican, se clonan con el header extra.
  // Solo lo agregamos a nuestra API, nunca a otros dominios (no regalar el token).
  const request =
    token && req.url.startsWith('/api/')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      // 401 con token = el token venció o la cuenta fue borrada -> cerramos sesión.
      // 401 sin token = login con clave incorrecta; ese error lo muestra la pantalla
      // de login, por eso aquí no redirigimos.
      if (error instanceof HttpErrorResponse && error.status === 401 && token) {
        session.clear();
        router.navigate(['/login'], { queryParams: { expired: 1 } });
      }
      return throwError(() => error);
    }),
  );
};

/** Solo estudiantes autenticados. */
export const authGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

/** Login y registro solo para quien no tiene sesión. */
export const guestGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.isAuthenticated() ? router.createUrlTree(['/inscripcion']) : true;
};
