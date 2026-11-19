// Contratos de la API (espejo de Application/Contracts/Dtos.cs)

export interface StudentProfile {
  id: number;
  firstName: string;
  lastName: string;
  documentNumber: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  student: StudentProfile;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  documentNumber: string;
  email: string;
}

export interface RegisterRequest extends UpdateProfileRequest {
  password: string;
}

export interface Subject {
  id: number;
  code: string;
  name: string;
  description: string | null;
  credits: number;
  professorId: number;
  professorName: string;
  enrolledCount: number;
}

/** Regla 8: lo único que se ve de otros estudiantes. */
export interface PublicStudent {
  id: number;
  fullName: string;
  subjects: string[];
  totalCredits: number;
}

/** Regla 9: classmates contiene solo nombres. */
export interface MyClass {
  subjectId: number;
  code: string;
  name: string;
  credits: number;
  professorName: string;
  classmates: string[];
}

export interface MyEnrollment {
  totalCredits: number;
  requiredCredits: number;
  classes: MyClass[];
}

/** Respuesta de error ProblemDetails (RFC 7807). */
export interface ApiProblem {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
