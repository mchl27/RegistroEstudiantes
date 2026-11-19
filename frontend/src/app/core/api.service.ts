import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  AuthResponse,
  LoginRequest,
  MyEnrollment,
  PublicStudent,
  RegisterRequest,
  StudentProfile,
  Subject,
  UpdateProfileRequest,
} from './models';

/** Cliente HTTP de la API. En desarrollo, /api se redirige a la API con proxy.conf.json. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  login(body: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.base}/auth/login`, body);
  }

  register(body: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.base}/auth/register`, body);
  }

  getProfile() {
    return this.http.get<StudentProfile>(`${this.base}/students/me`);
  }

  updateProfile(body: UpdateProfileRequest) {
    return this.http.put<StudentProfile>(`${this.base}/students/me`, body);
  }

  deleteAccount() {
    return this.http.delete<void>(`${this.base}/students/me`);
  }

  getDirectory() {
    return this.http.get<PublicStudent[]>(`${this.base}/students`);
  }

  getSubjects() {
    return this.http.get<Subject[]>(`${this.base}/subjects`);
  }

  getMyEnrollment() {
    return this.http.get<MyEnrollment>(`${this.base}/enrollments/me`);
  }

  enroll(subjectIds: number[]) {
    return this.http.put<MyEnrollment>(`${this.base}/enrollments/me`, { subjectIds });
  }
}
