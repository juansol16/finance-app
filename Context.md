Soy un ingeniero de software que trabaja remoto para una empresa estadounidense. Mi situación financiera y fiscal es la siguiente:

Ingresos:

Nómina (Empresa US): Me depositan quincenal/mensualmente en mi cuenta mexicana. El monto llega en MXN, pero mi salario está definido en USD. Necesito registrar: empresa (mi empleador), fecha, monto en MXN recibido, y la tasa de cambio USD/MXN aplicada ese día. La app debería calcular automáticamente el monto en USD equivalente (o permitirme ingresarlo).

Honorarios (Proyectos Independientes): Recibo pagos esporádicos de clientes mexicanos directamente en MXN. Para estos, solo necesito registrar: cliente, fecha y monto en MXN.

Facturas de Ingresos: Mi contador genera facturas por todos mis ingresos. Quiero poder adjuntar los archivos (PDF, XML, o un ZIP que contenga ambos) a cada registro de ingreso. El almacenamiento será en Google Cloud Storage.

Gastos (Aquí viene lo interesante): Los gastos se dividen en dos categorías que debo modelar cuidadosamente:

Gastos Generales (No Deducibles o Mixtos): Son salidas de dinero que no necesariamente son deducibles de impuestos, o que son el "contenedor" de gastos deducibles.

Ejemplos:

Pago total de la tarjeta de crédito (que incluye compras personales y compras deducibles).

Transferencias a mi pareja.

Pago del coche (sale de débito).

Retiros de efectivo.

Pago de honorarios a mi contador.

Funcionalidad deseada: Poder dar de alta mis tarjetas de crédito (banco, nickname, últimos 4 dígitos). Al registrar un "Pago de Tarjeta" como gasto general, poder seleccionar de una lista cuál tarjeta estoy pagando.

Gastos Facturables (Deducibles de Impuestos): Son compras específicas que son deducibles en RESICO.

Ejemplos: Pago de luz (CFE), pago de internet, pago de plan celular, compras en Amazon de equipos de cómputo, etc.

Características:

Se pagan casi siempre con tarjeta de crédito.

Necesito guardar su factura (PDF/XML/ZIP) en Google Cloud Storage.

Sería ideal, aunque no es un requisito duro en la primera versión, poder vincular este gasto facturable al "Pago de Tarjeta" (gasto general) que lo cubrió. Como alternativa inicial, puedo simplemente registrarlos de forma independiente, pero la app debe "entender" que ambos existen.

Funcionalidad extra: Si subo un XML, la app debería intentar parsearlo para extraer y mostrar información como RFC del emisor, monto, UUID (folio fiscal), etc., y guardar esos metadatos.

Obligaciones Fiscales (RESICO):

Estoy en el régimen RESICO. La aplicación debe ayudarme a calcular y dar seguimiento a mis pagos de impuestos.

Cálculo de Impuestos: La app debe, para un mes/año seleccionado:

Sumar todos los Ingresos (Nómina + Honorarios).
Sumar todos los Gastos Facturables (Deducibles).
Calcular la Utilidad Fiscal (Ingresos - Gastos Facturables).
Calcular el Impuesto Estimado a Pagar. Esto es crítico: Yo no conozco la fórmula exacta del RESICO. Necesito que, como parte de tu guía, investigues y expliques la tabla de tasas de ISR para personas físicas con actividades empresariales en RESICO (la que va por excedentes de límite inferior, cuota fija y tasa sobre excedente). Tu explicación debe ser clara para que yo pueda implementar la lógica, o bien, proponer una fórmula simplificada inicial y un plan para refinarla después.
Gestión de Pagos al SAT:

Mi contador me proporciona cada mes una "determinación" con el monto exacto a pagar, la fecha límite, y un PDF con la línea de captura. Necesito un módulo donde pueda cargar este PDF y crear un registro con: Mes/Año, Monto, Fecha Límite, Estatus ("Pendiente").
Cuando realizo el pago, quiero poder actualizar el estatus a "Pagado", añadir la Fecha de Pago y adjuntar el comprobante de pago (PDF o imagen).
Requisitos Técnicos y de Desarrollo (¡Para un Portafolio de 10!)
Backend: ASP.NET Core (preferiblemente .NET 8). Debe ser una API RESTful.

Frontend: Angular (última versión estable).

Base de Datos: La que recomiendes (SQL Server, PostgreSQL, etc.). Usaremos Entity Framework Core como ORM.

Autenticación: JWT.

Almacenamiento de Archivos: Google Cloud Storage. Dame instrucciones sobre cómo configurar un bucket y las credenciales.

Calidad de Código:

Pruebas unitarias (en backend, y si aplica, en frontend).

Documentación automática de la API con Swagger.

Código limpio, con principios SOLID y patrones de diseño adecuados.

Frontend UI/UX:

Usar Ripple UI o una librería similar para un diseño moderno y atractivo.

Implementar modo oscuro (dark mode) toggle.

La aplicación debe ser completamente bilingüe (inglés/español) con un botón para cambiar el idioma. Esto es crucial para mi búsqueda de trabajo en Estados Unidos, quiero que los reclutadores gringos puedan navegarla sin problema.

Demo y Portafolio:

Debe existir un usuario dummy con datos precargados (ingresos, gastos, gastos facturables, archivos de ejemplo) para que quien vea la demo (reclutadores, CTOs) pueda explorar todas las funcionalidades sin tener que crear datos desde cero. Dame instrucciones de cómo generar estos datos de semilla (seed data).

La aplicación debe ser desplegable (idealmente en servicios gratuitos como Firebase Hosting + Render/Google Cloud Run) y debo tener un README.md increíble que explique la arquitectura, tecnologías usadas, features y cómo correr el proyecto localmente.

Tu Tarea: El Plan de Ataque Definitivo
Actuando como mi mentor, necesito que me proporciones un documento o serie de instrucciones extremadamente detalladas, modulares y prácticas. Tu respuesta debe incluir:

Arquitectura de Alto Nivel: Diagrama de bloques o descripción de cómo se comunicarán Angular, ASP.NET Core, GCS y la BD.

Modelado de Datos: Define las entidades principales (User, Income, Expense, TaxableExpense, CreditCard, TaxPayment, etc.) con sus propiedades y relaciones. Explica el porqué de tus decisiones, especialmente la relación (o no-relación) entre Expense (gasto general de tarjeta) y TaxableExpense.

Plan de Implementación Paso a Paso (Divide y Vencerás):

Fase 0: Setup Inicial. Crear proyectos, configurar autenticación JWT, conectar a BD, configurar Google Cloud Storage.

Fase 1: Core de Ingresos. CRUD de ingresos con subida de archivos a GCS.

Fase 2: Core de Gastos. CRUD de tarjetas de crédito y gastos generales.

Fase 3: Gastos Facturables. CRUD de gastos facturables con subida de archivos, parseo de XML (si es posible) y la lógica de vinculación opcional con tarjetas.

Fase 4: Cerebro Fiscal. Implementar el módulo de cálculo de impuestos RESICO (aquí es donde necesito tu "pensamiento de contador" para la fórmula). Crear el dashboard financiero.

Fase 5: Gestión de Pagos al SAT. Módulo para cargar, dar seguimiento y actualizar pagos de impuestos.

Fase 6: Frontend Angular. Para cada fase del backend, guíame en la creación de los componentes, servicios, guards, interceptors y la implementación del cambio de idioma y modo oscuro. Dame sugerencias de librerías de Angular para i18n (@angular/localize, ngx-translate) y para UI.

Fase 7: Datos Dummy y Toques Finales. Crear scripts de seed para el usuario dummy y sus datos. Mejorar el README.md. Sugerencias para el despliegue.

Librerías Específicas: Para cada paso, recomienda paquetes NuGet y módulos npm concretos (ej. Google.Cloud.Storage.V1, BCrypt.Net-Next, Newtonsoft.Json, Moq, xunit, @angular/material, @ngx-translate/core, etc.).

Consideraciones y Desafíos: Señala los puntos más complejos (manejo de archivos en GCS, la lógica de vinculación de gastos, el cálculo de RESICO) y cómo los abordaríamos técnicamente.

Por favor, empieza tu respuesta con un resumen ejecutivo de cómo abordarás este proyecto y luego desarrolla el plan completo. ¡Estoy listo para construir mi portafolio soñado!