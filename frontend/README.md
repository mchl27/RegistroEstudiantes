# Registro de Estudiantes · Web

Frontend en Angular 20. Las instrucciones completas están en el [README principal](../README.md).

```powershell
npm install
npm start        # http://localhost:4200 (redirige /api a http://localhost:5080 con proxy.conf.json)
npm run build    # compilación de producción en dist/
```

## Estructura

```
src/app/
├── core/       api.service, session.store (signals), interceptor JWT y guards, modelos
├── shared/     validaciones de formularios, utilidades de texto
├── layout/     shell: barra superior + contenido
└── features/   auth (login, registro), enrollment, my-classes, directory, profile
```
