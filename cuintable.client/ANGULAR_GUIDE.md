# Guía del Proyecto Angular — Cuintable Client

> Referencia para desarrolladores con experiencia en React/Vue que se incorporan al proyecto.

---

## 1. Versión de Angular

**Angular 21** — específicamente `^21.1.0` (ver `package.json`). Angular lanza una versión mayor cada 6 meses aproximadamente.

---

## 2. Arquitectura: NgModule (NO Standalone)

Angular tiene dos formas de organizar componentes:
- **NgModule** (la forma clásica) — la que usa este proyecto
- **Standalone** (la nueva, desde Angular 14) — marcada explícitamente como `standalone: true`

En este proyecto todos los componentes tienen `standalone: false`, lo que significa que **necesitan estar declarados dentro de un `NgModule`** para existir. Es la arquitectura que más se usó históricamente.

---

## 3. Estructura de folders

```
src/
  main.ts                  ← Equivalente a main.tsx en React (punto de entrada)
  app/
    app.ts                 ← Componente raíz (como <App /> en React)
    app-module.ts          ← El "registro global" de la app (NO existe en React/Vue)
    app-routing-module.ts  ← El router (como react-router o vue-router)

    core/                  ← Lógica singleton de la app
      guards/              ← Middleware de rutas (proteger páginas)
      interceptors/        ← Middleware HTTP (como axios interceptors)
      services/            ← Stores/hooks de datos (equivalente a useQuery / Pinia)

    features/              ← Páginas/vistas de la app
      auth/                ← Login, Register
      dashboard/           ← Vista principal con gráficas
      incomes/             ← Ingresos (lista + formulario)
      expenses/            ← Gastos
      taxable-expenses/    ← Gastos deducibles
      tax-payments/        ← Pagos de impuestos
      credit-cards/        ← Tarjetas de crédito

    layouts/               ← Estructura visual de la app (shell)
      main-layout/         ← El "wrapper" con sidebar + topbar
      sidebar/             ← Menú lateral
      topbar/              ← Barra superior

    shared/                ← Componentes reutilizables entre features
      components/
        summary-card/      ← Tarjeta de resumen (KPI card)
```

---

## 4. Conceptos Angular vs React/Vue

### `NgModule` — El concepto más raro si vienes de React

En React simplemente importas un componente y lo usas. En Angular con NgModule, **cada componente debe estar registrado en un módulo** antes de poder usarse.

```ts
// app-module.ts — Equivalente a no tener nada en React,
// pero en Angular es OBLIGATORIO declarar cada componente aquí
@NgModule({
  declarations: [App, LoginComponent, IncomeListComponent, ...],  // ← "registro"
  imports: [BrowserModule, HttpClientModule, DashboardModule, ...], // ← módulos externos
  providers: [JwtInterceptor],   // ← servicios globales
  bootstrap: [App]               // ← componente raíz
})
export class AppModule { }
```

**Analogía React**: imagina que en lugar de simplemente hacer `import Login from './Login'`, tuvieras que registrar `Login` en un archivo central para que el framework lo reconozca.

---

### Componentes

En Angular un componente son **3 archivos** (o puede estar todo en uno):
- `.ts` — lógica (el `.tsx` de React)
- `.html` — template separado
- `.css` — estilos

```ts
// En React harías: export default function App() { return <div>...</div> }
// En Angular:
@Component({
  selector: 'app-root',       // ← el tag HTML: <app-root />
  templateUrl: './app.html',  // ← el JSX está en otro archivo
  standalone: false
})
export class App implements OnInit {
  ngOnInit() { /* como useEffect(() => {}, []) */ }
}
```

---

### Servicios — Equivalente a Stores/Hooks

Los servicios son clases singleton inyectadas automáticamente. En React usarías `useQuery` + `fetch`; aquí todo está encapsulado en servicios:

```ts
// income.service.ts — Equivalente a un custom hook o Pinia store
@Injectable({ providedIn: 'root' })  // ← singleton global, como React Context
export class IncomeService {
  constructor(private http: HttpClient) { }   // ← el HttpClient se inyecta solo

  getAll(): Observable<Income[]> {            // ← Observable = Promise pero reactivo
    return this.http.get<Income[]>('/api/incomes');
  }
}
```

**RxJS / Observable** es el equivalente de Promises, pero más poderoso (cancelable, combinable). En lugar de `.then()` usas `.subscribe()`:

```ts
// React:    fetch('/api').then(data => setIncomes(data))
// Angular:  incomeService.getAll().subscribe(data => this.incomes = data)
```

---

### Guards — Middleware de rutas

```ts
// auth.guard.ts — Equivalente al PrivateRoute de React Router
export class AuthGuard implements CanActivate {
  canActivate(): boolean {
    if (this.authService.isLoggedIn) return true;
    this.router.navigate(['/login']);
    return false;
  }
}
```

---

### Interceptors — Middleware HTTP

```ts
// jwt.interceptor.ts — Equivalente al interceptor de Axios
// Automáticamente agrega el token JWT a TODAS las peticiones HTTP
intercept(req, next) {
  const token = this.authService.token;
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next.handle(req);
}
```

---

### Template Syntax — Lo más diferente vs React

```html
<!-- React JSX -->
{loading && <div>Loading...</div>}
{items.map(item => <div key={item.id}>{item.name}</div>)}
<button onClick={() => setShow(true)}>Add</button>
<input value={name} onChange={e => setName(e.target.value)} />

<!-- Angular Template -->
<div *ngIf="loading">Loading...</div>
<div *ngFor="let item of items">{{item.name}}</div>
<button (click)="showForm = true">Add</button>
<input [(ngModel)]="name" />   <!-- two-way binding como Vue v-model -->
```

---

## 5. Flujo completo de la app

```
main.ts
  └── AppModule (app-module.ts)
        ├── bootstrap: App (app.ts)  ←  <router-outlet> renderiza la ruta activa
        ├── AppRoutingModule
        │     ├── /login      → LoginComponent
        │     ├── /register   → RegisterComponent
        │     └── / (protegido con AuthGuard)
        │           └── MainLayoutComponent (sidebar + topbar + <router-outlet>)
        │                 ├── /dashboard        → DashboardComponent
        │                 ├── /incomes          → IncomeListComponent
        │                 ├── /expenses         → ExpenseListComponent
        │                 ├── /taxable-expenses → TaxableExpenseListComponent
        │                 ├── /tax-payments     → TaxPaymentListComponent
        │                 └── /credit-cards     → CreditCardListComponent
        └── JwtInterceptor  ← agrega token JWT a todas las peticiones HTTP
```

---

## 6. Tabla de equivalencias React/Vue → Angular

| React / Vue              | Angular                          |
|--------------------------|----------------------------------|
| `useState`               | Property de clase (`loading = true`) |
| `useEffect`              | `ngOnInit()`                     |
| `useContext` / Pinia     | `@Injectable Service`            |
| `React.memo` / `computed`| `get` getter                     |
| Axios interceptors       | `HttpInterceptor`                |
| Route guards (PrivateRoute) | `CanActivate Guard`           |
| `import Component`       | `declarations` en NgModule       |
| `.then()`                | `.subscribe()` (RxJS)            |
| `v-model` (Vue)          | `[(ngModel)]`                    |
| `@click` / `onClick`     | `(click)`                        |
| `:prop` / `prop={}`      | `[prop]`                         |
| `{variable}`             | `{{variable}}`                   |
| `v-if` / `&&`            | `*ngIf`                          |
| `v-for` / `.map()`       | `*ngFor`                         |

---

## 7. Librerías clave del proyecto

| Librería           | Propósito                                      |
|--------------------|------------------------------------------------|
| `@ngx-translate`   | i18n (EN/ES) — `{{ 'KEY' \| translate }}`      |
| `ngx-toastr`       | Notificaciones toast (éxito, error, etc.)      |
| `chart.js` + `ng2-charts` | Gráficas en el dashboard               |
| `tailwindcss` + `daisyui` | Estilos CSS utilitarios + componentes UI |
| `rxjs`             | Programación reactiva (Observables)            |
