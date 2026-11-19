import { Component } from '@angular/core';

/** Panel informativo compartido por login y registro: explica las reglas del programa. */
@Component({
  selector: 'app-auth-hero',
  // display: contents -> la <section> participa directamente en la grilla .auth
  host: { style: 'display: contents' },
  template: `
    <section class="auth__hero">
      <span class="brand__mark brand__mark--lg">RE</span>
      <h1>Registro de Estudiantes</h1>
      <p>Inscríbete en línea en el programa de créditos y conoce a tus compañeros de clase.</p>
      <ul class="rules">
        <li><strong>3 materias</strong> de 3 créditos cada una: 9 créditos en total.</li>
        <li><strong>Profesores distintos</strong>: cada profesor dicta 2 materias y solo puedes tomar una.</li>
        <li><strong>Compañeros</strong>: mira con quién compartirás cada clase.</li>
      </ul>
    </section>
  `,
})
export class AuthHero {}
