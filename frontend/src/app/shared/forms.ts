import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, Validators } from '@angular/forms';
import { ApiProblem } from '../core/models';

export type ServerErrors = Record<string, string[]>;

/** Mismas reglas que FluentValidation en el backend: el usuario se entera antes de enviar. */
export const profileValidators = {
  firstName: [Validators.required, Validators.maxLength(80)],
  lastName: [Validators.required, Validators.maxLength(80)],
  documentNumber: [Validators.required, Validators.pattern(/^[0-9]{5,20}$/)],
  email: [Validators.required, Validators.email, Validators.maxLength(150)],
};

export const passwordValidators = [
  Validators.required,
  Validators.minLength(8),
  Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/),
];

/** Mensaje de error de un campo: primero el del servidor, luego el de validación local. */
export function controlError(
  control: AbstractControl,
  options: { server?: string; pattern?: string } = {},
): string | null {
  if (options.server) return options.server;
  if (!control.touched || control.valid) return null;

  const errors = control.errors ?? {};
  if (errors['required']) return 'Este campo es obligatorio.';
  if (errors['email']) return 'Ingresa un correo válido.';
  if (errors['minlength']) return `Mínimo ${errors['minlength'].requiredLength} caracteres.`;
  if (errors['maxlength']) return `Máximo ${errors['maxlength'].requiredLength} caracteres.`;
  if (errors['pattern']) return options.pattern ?? 'Formato no válido.';
  return 'Valor no válido.';
}

/** Convierte cualquier error HTTP en un ProblemDetails legible. */
export function toProblem(error: unknown): ApiProblem {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return {
        status: 0,
        title: 'Sin conexión',
        detail: 'No se pudo conectar con el servidor. Verifica que la API esté en ejecución.',
      };
    }
    const body = error.error;
    if (body && typeof body === 'object') {
      return {
        status: error.status,
        title: body.title ?? 'Error',
        detail: body.detail,
        code: body.code,
        errors: body.errors,
      };
    }
    return { status: error.status, title: 'Error', detail: error.message };
  }
  return { status: -1, title: 'Error', detail: 'Ocurrió un error inesperado.' };
}

/** Errores de conflicto (409) que pertenecen a un campo concreto del formulario. */
const CONFLICT_FIELDS: Record<string, string> = {
  'student.email_taken': 'email',
  'student.document_taken': 'documentNumber',
};

export function fieldErrorsFrom(problem: ApiProblem): ServerErrors {
  if (problem.errors) return problem.errors;
  const field = problem.code ? CONFLICT_FIELDS[problem.code] : undefined;
  return field && problem.detail ? { [field]: [problem.detail] } : {};
}
