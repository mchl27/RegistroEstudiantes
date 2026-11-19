import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth';

// Cada pantalla se carga bajo demanda (lazy loading)
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Iniciar sesión · Registro de Estudiantes',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'registro',
    canActivate: [guestGuard],
    title: 'Crear cuenta · Registro de Estudiantes',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inscripcion' },
      {
        path: 'inscripcion',
        title: 'Inscripción · Registro de Estudiantes',
        loadComponent: () =>
          import('./features/enrollment/enrollment-page').then((m) => m.EnrollmentPage),
      },
      {
        path: 'mis-clases',
        title: 'Mis clases · Registro de Estudiantes',
        loadComponent: () =>
          import('./features/my-classes/my-classes-page').then((m) => m.MyClassesPage),
      },
      {
        path: 'directorio',
        title: 'Directorio · Registro de Estudiantes',
        loadComponent: () =>
          import('./features/directory/directory-page').then((m) => m.DirectoryPage),
      },
      {
        path: 'perfil',
        title: 'Mi perfil · Registro de Estudiantes',
        loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
