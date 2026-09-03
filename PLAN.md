# LIN Cloud Baion

Orquestador de servidores (VMs, VPS, bare metal) para replicar scripts, programar tareas y recolectar métricas. Producto standalone que también se integra con el ecosistema LIN, diseñado para escalar a miles de agentes conectados simultáneamente.

## Tabla de contenidos

- [Visión general](#visión-general)
- [Arquitectura de conexión](#arquitectura-de-conexión)
- [Escalamiento](#escalamiento)
- [Identidad multi-tenant](#identidad-multi-tenant)
- [Ejecución de scripts](#ejecución-de-scripts)
- [Estructura de soluciones](#estructura-de-soluciones)
- [Entidades principales](#entidades-principales)
- [Plan de trabajo por fases](#plan-de-trabajo-por-fases)

---

## Visión general

| Aspecto | Decisión |
|---|---|
| Servidores gestionados | VM, VPS, bare metal |
| Modo de uso | Standalone o integrado con LIN |
| Conexión agente–orquestador | WebSocket saliente (pull a nivel de red, bidireccional una vez conectado) |
| Escala objetivo | Miles de agentes concurrentes |
| Bus de mensajería | RabbitMQ |
| Base de datos | SQL Server, base separada `lin_baion` |
| Multi-tenancy | `tenant_id` por fila (mismo esquema para todos los tenants) |
| Métricas | RAM, CPU, disco |
| Front-end | `cliente/` — Blazor Web App (Interactive Server) con Tailwind, aplicación aparte que habla con la API por HTTP |

## Arquitectura de conexión

El agente abre la conexión (WebSocket saliente), pero una vez establecida el canal es bidireccional:

- **Compatibilidad de red total**: el agente solo necesita salida a internet; funciona detrás de NAT y firewalls sin abrir puertos.
- **Push real sin serlo de verdad**: el orquestador puede enviar comandos al agente en cualquier momento por el socket ya abierto (ejecutar script, forzar actualización).
- **Heartbeat nativo**: la caída del socket indica al instante que el servidor está offline.
- **Reconexión resiliente**: backoff exponencial + jitter para evitar thundering herd ante reinicios del orquestador.

## Escalamiento

El orquestador corre en **múltiples instancias horizontales**. RabbitMQ resuelve el enrutamiento entre instancias:

- **Exchange direct/topic** con routing key por agente (`agent.{agentId}`) para comandos dirigidos — solo la instancia que tiene esa conexión activa lo despacha.
- **Exchange fanout** para eventos globales (ej. "agente X se desconectó").
- **Registro de presencia distribuido** (`agentId → instanceId`, TTL corto, refrescado en cada heartbeat).
- Métricas se persisten directo desde la instancia que recibe el heartbeat, sin ida y vuelta por RabbitMQ.

## Identidad multi-tenant

Capa de abstracción `IIdentityProvider` con dos implementaciones, para que Baion funcione integrado con LIN o completamente autogestionado:

- **`LinIdentityProvider`**: delega en LIN Cloud Identity Platform.
- **`SelfManagedIdentityProvider`**: tablas propias (`tenants`, `users`, `roles`, credenciales) en `lin_baion`.

El `tenant_id` de Baion es agnóstico del proveedor; opcionalmente referencia un `ExternalTenantId` cuando el tenant proviene de LIN.

## Ejecución de scripts

- **Concurrencia**: N ejecuciones en paralelo por agente, limitadas por un semáforo configurable (`MaxConcurrentExecutions`).
- **Cadenas (`ScriptChain`)**: secuencia de pasos orquestada desde el servidor central (no desde el agente); cada paso espera el resultado del anterior y aplica una política de fallo (`StopChain` / `ContinueNext`). Varias cadenas pueden correr en paralelo respetando el límite de concurrencia del agente.
- **Modo de ejecución**: `Attached` (el agente espera el resultado) o `Detached` (fire-and-forget, útil para procesos de larga duración).
- **Sin límite de tamaño de output**: stdout/stderr se transmiten en streaming por chunks vía WebSocket, evitando acumular todo en memoria del agente.
- **Timeout configurable** por ejecución, con `Process.Kill(entireProcessTree: true)` al vencer.
- **Multiplataforma**: `IScriptExecutor` con implementaciones `LinuxScriptExecutor` (bash/sh) y `WindowsScriptExecutor` (PowerShell), resueltas por DI según la plataforma detectada en el agente.

## Estructura de soluciones

```
Baion/
├── Baion.Orchestrator.sln
├── Baion.Agent.sln
├── Directory.Build.props          (net10.0, nullable, warnings-as-errors)
├── Directory.Packages.props       (versiones centralizadas de NuGet)
├── nuget.config                   (nuget.org + feed LI con package source mapping)
├── .editorconfig                  (CODING_STYLE.md aplicado en build)
├── src/
│   ├── Baion.Contracts/           (DTOs/records del protocolo WS, compartido por ambas soluciones)
│   ├── Orchestrator/
│   │   ├── Baion.Orchestrator.Presentacion/   (Controllers, Filters, endpoint WebSocket — proyecto de arranque)
│   │   ├── Baion.Orchestrator.Services/       (interfaces raíz públicas + Implementations/ internal)
│   │   ├── Baion.Orchestrator.Identity/       (IIdentityProvider + LinIdentityProvider / SelfManagedIdentityProvider)
│   │   ├── Baion.Orchestrator.Messaging/      (IRabbitMqPublisher/Consumer + Implementations/)
│   │   ├── Baion.Orchestrator.Persistence/    (BaionDbContext, repositorios raíz públicos + Implementations/)
│   │   ├── Baion.Orchestrator.Extensions/
│   │   └── Baion.Orchestrator.Models/         (Entities/, Dtos/)
│   └── Agent/
│       ├── Baion.Agent.Core/       (websocket client, protocolo, coordinador de concurrencia)
│       ├── Baion.Agent.Execution/  (IScriptExecutor + Linux/Windows en Implementations/)
│       ├── Baion.Agent.Metrics/    (IMetricsCollector + Linux/Windows en Implementations/)
│       └── Baion.Agent.Host/       (Program.cs, resuelve plataforma, arma DI, publish per-RID)
└── tests/
    └── Baion.Contracts.Tests/
```

Cada capa expone su propia extensión `AddXxx(this IServiceCollection)`. Interfaces públicas a nivel raíz de cada proyecto; implementaciones `internal` en `Implementations/`.


## Cliente web

Panel de administración en `cliente/`, con su propia solución. Es una aplicación **separada** que consume la
API del orquestador por HTTP: no referencia sus proyectos ni comparte base de datos, así que puede desplegarse
y escalarse por su cuenta.

| Aspecto | Decisión |
|---|---|
| Framework | Blazor Web App, modo de render `InteractiveServer` |
| Estilos | Tailwind CSS 4, compilado en el build de .NET (target de MSBuild, sin paso manual) |
| Sesión | Cookie cifrada y HttpOnly que lleva dentro el JWT de la API; el navegador nunca lo ve |
| Login | Página SSR estática (`[ExcludeFromInteractiveRouting]`), porque firmar la cookie necesita el `HttpContext` |
| Contratos | DTOs propios en `Models/`, no compartidos con el servidor, para no atar el panel a su versión exacta |

```
cliente/
├── Baion.Cliente.sln
└── Baion.Cliente.Web/
    ├── Components/
    │   ├── Layout/     (MainLayout, NavMenu, TopBar, EmptyLayout)
    │   ├── Pages/      (Login, Dashboard, Servers)
    │   └── Shared/     (StatCard, StatusBadge, ServerTable, PageHeader, AlertMessage, EmptyState)
    ├── Models/         (reflejo de los contratos de la API)
    ├── Services/       (IBaionApiClient + Implementations/)
    ├── Styles/app.css  (fuente de Tailwind)
    └── wwwroot/css/    (CSS generado)
```

Endpoints añadidos al orquestador para alimentarlo: `GET /api/servers`, `GET /api/servers/{id}` y
`GET /api/dashboard/summary`.

## Entidades principales

- **`Tenant`**: `IdentityMode` (SelfManaged/Lin), `ExternalTenantId` opcional.
- **`Server`**: `Kind` (Vm/Vps/BareMetal), `Platform` (Linux/Windows), estado, versión del agente, instancia de orquestador actual.
- **`ServerGroup`**: agrupación de servidores para ejecución/scheduling masivo.
- **`Script`**: contenido, checksum, versión, `Runtime` (Bash, Sh, PowerShellCore, WindowsPowerShell, PythonCross).
- **`ScriptExecution`**: resultado por ejecución — exit code, stdout/stderr, estado, `Mode`, referencia opcional a `ScriptChainStep`.
- **`ScriptChain`** / **`ScriptChainStep`**: secuencias de scripts con política de fallo por paso.
- **`ScheduledTask`**: cron, destino (`Server` o `ServerGroup`, mutuamente excluyentes).
- **`Metric`**: RAM/CPU/disco, particionada por fecha, índice `(ServerId, Timestamp)`.

## Plan de trabajo por fases

### Fase 0 — Setup y contratos ✅
Ambas soluciones compilan; mensajes base (`ExecuteScriptMessage`, `MetricsReportMessage`, `ForceUpdateMessage`) definidos en `Baion.Contracts` sobre dos jerarquías polimórficas (`ServerToAgentMessage` / `AgentToServerMessage`) con discriminador `type` y `BaionProtocol.JsonOptions` compartidas por ambos extremos.
**Aceptación:** build limpio en ambas soluciones (0 warnings, `TreatWarningsAsErrors`), sin tipos duplicados; round-trip del protocolo verificado en `Baion.Contracts.Tests`.

### Fase 1 — Persistencia `lin_baion` ✅
`BaionDbContext` con las 10 entidades y convención `snake_case` en tablas, columnas, claves e índices.
Aislamiento por tenant en dos frentes: filtro global de consulta aplicado por reflexión a todo lo que implemente
`ITenantOwned`, y `TenantStampInterceptor` que sella el `tenant_id` al insertar y rechaza cualquier escritura
sobre filas de otro tenant. Sin tenant resuelto en el scope, el filtro no devuelve filas y la escritura falla.
`AuditTimestampsInterceptor` mantiene `created_at` / `updated_at`.
**Aceptación:** la migración `InitialCreate` aplica sobre SQL Server; 6 tests de integración contra una base
desechable confirman el aislamiento en lectura, en escritura y a través del repositorio genérico.

> La partición por fecha de `metrics` quedó pendiente aquí y se resolvió en la fase 4.

### Fase 2 — Identidad ✅
`IIdentityProvider` cubre solo la **verificación de credenciales**; el token lo emite y valida siempre Baion
(`ITokenService`, JWT HS256), de modo que el resto del sistema no cambia según el modo del tenant.
`SelfManagedIdentityProvider` funcional sobre las tablas `users` / `roles` / `user_roles`, con hash PBKDF2-HMAC-SHA512
(formato versionado de ASP.NET Core Identity, con rehash automático), bloqueo por intentos fallidos y el mismo error
para usuario inexistente y contraseña incorrecta. `LinIdentityProvider` registrado y en su sitio, rechazando con un
error explícito hasta que se implemente. `AuthenticationService` resuelve el tenant por slug, fija el `ITenantContext`
y elige el proveedor por `IdentityMode`.
**Aceptación:** login y emisión de token verificados de extremo a extremo — `POST /api/auth/login` devuelve el JWT con
`tid`, `stamp` y `roles`, y las credenciales inválidas devuelven 401 con `ProblemDetails`. 8 tests de integración
cubren login correcto, contraseña incorrecta, tenant inexistente, bloqueo por intentos, modo LIN, alta de usuarios
y validación de token manipulado.

> Añadido fuera del enunciado, porque sin él no hay forma de obtener las primeras credenciales:
> `IdentityBootstrapHostedService` crea tenant y administrador iniciales desde `Identity:Bootstrap` si no existen.
> Es idempotente y viene desactivado por defecto.
>
> Pendiente: refresh tokens, revocación por `SecurityStamp` al validar, y los endpoints de administración de
> tenants y usuarios — quedan a la espera de que exista autorización en la API.

### Fase 3 — WebSocket + onboarding ✅
Handshake en dos tiempos: las credenciales viajan en cabeceras y se validan **antes** de aceptar el socket
(un agente sin permiso recibe 401 y no llega a establecer conexión); ya dentro, el agente manda `HelloMessage`
con plataforma, RID y `MachineId`, y el orquestador responde `WelcomeMessage`. El token de instalación solo se
usa una vez: en el enrolamiento el agente recibe una **credencial permanente** que persiste con permisos
restringidos y con la que reconecta a partir de ahí. `MachineId` hace idempotente el reenrolamiento, de modo que
reinstalar el agente no duplica el servidor. `BaionMessageChannel` vive en `Baion.Contracts` y encuadra los
mensajes en los dos extremos, para que el formato de trama no pueda divergir.
Reconexión con retroceso exponencial y jitter completo dentro de la ventana; `InstancePresenceHostedService`
suelta al arrancar la presencia que la instancia dejó colgada tras una caída.
Instalación con `deploy/linux/` (unidad `Type=notify` + `install.sh`) y `deploy/windows/Install-BaionAgent.ps1`.
**Aceptación:** verificada de extremo a extremo con procesos reales — el agente se enrola, se le mata el
orquestador, reintenta con esperas de 1,0 s y 1,7 s (jitter visible) y vuelve a conectar con el mismo `ServerId`
contra la instancia nueva. 9 tests de integración sobre WebSocket real cubren enrolamiento Linux y Windows,
reconexión con credencial, reinicio de instancia, reenrolamiento idempotente, 401 previo al upgrade, token
revocado, versión de protocolo distinta y latido. 9 tests más cubren el backoff y el estado del agente.

> Añadido fuera del enunciado, porque el token de instalación tenía que salir de algún sitio: autenticación
> JWT Bearer en la API, `TenantResolutionMiddleware` que traslada el claim `tid` al `ITenantContext`, y
> `POST /api/agents/enrollment-tokens` restringido al rol Admin.
>
> Pendiente: el despacho de comandos hacia un agente conectado a **otra** instancia es la fase 8; hoy
> `IAgentRegistry` solo conoce los sockets de su propio proceso.

### Fase 4 — Métricas ✅
`LinuxMetricsCollector` lee `/proc/stat`, `/proc/meminfo` y `/proc/loadavg`; `WindowsMetricsCollector` llama
directamente a `GetSystemTimes` y `GlobalMemoryStatusEx` de kernel32 — se descartó `PerformanceCounter` porque
depende de una infraestructura que en servidores recortados y contenedores no siempre está. Los dos son
singleton y con estado: el uso de CPU sale de la diferencia entre dos muestras de contadores acumulados, no de
una lectura instantánea. Los resuelve `AddMetricsCollection` según la plataforma detectada.
El agente reporta desde su propio bucle a través de `IOrchestratorChannel`, un canal que publica la sesión en
curso para que quien reporta no tenga que conocer el socket ni sus reconexiones.
**Del lado del orquestador**, el hilo del socket solo encola: `MetricIngestQueue` es un `Channel` acotado con
descarte al llenarse (ante sobrecarga se prefiere perder telemetría antes que frenar los sockets), y
`MetricIngestHostedService` lo vacía en lotes agrupados por tenant. El refresco de `last_seen_at` también sale
de ahí, en bloque, en lugar de una escritura por mensaje.
`metrics` quedó **particionada por mes** sobre `captured_at`: índice agrupado `(server_id, captured_at, id)`
sobre el esquema de partición, con `MetricPartitionMaintenanceHostedService` creando los límites mensuales por
delante. Con esto se salda lo que quedó pendiente en la fase 1.
**Aceptación:** verificada con procesos reales — el agente de Windows reportó 32 % y 41 % de CPU sobre 12
núcleos, 23 GB de RAM y el volumen C: como JSON, y las filas cayeron en la partición del mes en curso.
14 tests de integración cubren métricas de agentes Linux y Windows concurrentes, el detalle persistido, que
enviar 100 muestras no bloquee el socket, el refresco de `last_seen_at` y la partición efectiva de la tabla.

### Fase 5 — Ejecución de scripts ✅
`ProcessScriptExecutor` concentra lo común — materializar el script, lanzar el proceso, sacar su salida y
terminarlo si se pasa de tiempo — y `LinuxScriptExecutor` / `WindowsScriptExecutor` solo aportan el intérprete
y la extensión. La salida se lee **en bloques, no por líneas**: una única línea muy larga no puede hacer crecer
la memoria del agente sin límite. Al vencer el plazo se llama a `Process.Kill(entireProcessTree: true)`, de modo
que un script que dejó hijos no los deja huérfanos.
`ScriptRuntimeCompatibility` vive en los contratos y la aplican los dos extremos: el orquestador rechaza el
despacho antes de crear la fila, y el agente vuelve a comprobarlo sin fiarse. El agente también **verifica el
checksum antes de escribir nada en disco**.
`ScriptExecutionCoordinator` admite las órdenes sin bloquear —el bucle del socket nunca espera un hueco— y las
lleva en paralelo hasta el tope que fija el orquestador en la bienvenida. El `ExecutionId` actúa de clave de
idempotencia frente a reenvíos tras una reconexión.
**Del lado del orquestador**, salida y desenlace comparten un único buzón ordenado (`IScriptEventQueue`): así
nadie puede ver una ejecución terminada con la salida todavía a medias. Los fragmentos se juntan por ejecución
y flujo y se añaden con `.WRITE` de SQL Server, que no relee ni reescribe lo ya guardado.
**Aceptación:** verificada con procesos reales — cuatro ejecuciones sobre el mismo servidor Windows: dos
`Attached` (una con salida por stdout y código 0, otra con stderr y código 7), una `Detached` que volvió al
instante sin código de salida, y una que se colgó y quedó en `TimedOut` tras matarse su árbol de procesos.
El tope de concurrencia llegó del orquestador. 8 tests ejecutan procesos de verdad sobre la plataforma anfitriona
y 6 de integración cubren el despacho en paralelo, el orden de la salida troceada en 200 fragmentos, el rechazo
por plataforma incompatible y el agente desconectado.

> Pendiente: cancelar una ejecución en curso desde el orquestador, y una política de retención para
> `std_out` / `std_err`, que hoy crecen sin tope por diseño.

### Fase 6 — Cadenas de scripts ✅
El recorrido lo conduce el orquestador paso a paso: el agente solo ve ejecuciones sueltas, así que varias
cadenas en paralelo compiten por su semáforo igual que cualquier otra ejecución, sin nada específico de cadenas
en el agente. Tras persistir el desenlace de un paso —nunca antes—, `ScriptChainService.AdvanceAsync` evalúa su
`ChainFailurePolicy` y despacha el siguiente si toca.
**Sin tabla de recorridos:** el estado se deduce de las ejecuciones que comparten `chain_run_id`, que es
justo para lo que existe esa columna. Un índice único sobre `(chain_run_id, script_chain_step_id)` hace
idempotente el avance frente a un desenlace reprocesado.
Al arrancar se valida la **cadena entera** contra la plataforma del servidor: es preferible rechazarla que
dejarla a medias porque el paso tres resulte incompatible. Los pasos van siempre `Attached` — sin código de
salida no habría con qué evaluar la política.
**Aceptación:** verificada con procesos reales — dos cadenas de 3 pasos en paralelo sobre el mismo servidor
Windows, ambas con el paso 2 fallando con código 9. La de política `StopChain` quedó en `stopped` con el tercer
paso sin lanzar (`executionId: null`); la de `ContinueNext` llegó al final y quedó en `completedWithFailures`.
7 tests de integración cubren el orden de ejecución, las dos políticas ante un fallo intermedio, dos cadenas
en paralelo sin mezclar pasos, el rechazo por plataforma incompatible y el avance idempotente.

> El estado del recorrido se calcula al consultarlo, no se almacena. Basta para responder "¿cómo va mi
> cadena?", pero no permite listar recorridos ni filtrarlos por estado sin agregar sobre las ejecuciones.
> Si eso hace falta, toca una entidad `ScriptChainRun`.

### Fase 7 — Scheduling ✅
`ScheduledTask` con cron (Cronos, formatos de 5 y 6 campos) y zona horaria IANA, disparando sobre un `Server`
o sobre todos los miembros de un `ServerGroup`, con script o con cadena. Un servidor caído no impide que la
tarea corra en el resto del grupo.
**Reparto entre instancias sin coordinación:** `SchedulerHostedService` corre en todas y cada disparo se
reserva avanzando `next_run_at` con una escritura condicional. Gana una instancia, las demás siguen de largo;
nadie necesita saber de las otras.

**Agente offline en el disparo** — lo que el plan dejaba a definir — se resuelve con un **margen por tarea**:
- `OfflineGraceSeconds > 0`: la ejecución queda `Pending` con un plazo y el planificador la entrega en cuanto
  el agente vuelve. Una desconexión suele ser un reinicio o un corte pasajero, no un motivo para perder el
  disparo. Vencido el plazo, se marca fallida con el motivo.
- `OfflineGraceSeconds = 0`: falla en el acto.

Una orden pedida por API sigue fallando de inmediato: quien la lanzó está esperando la respuesta.

**Aceptación:** verificada con procesos reales — una tarea `* * * * *` en `America/Bogota` disparó sola a las
14:57 hora de Bogotá (19:57 UTC), ejecutó el script en el agente y avanzó su calendario. Se mató el agente,
el disparo de 19:59 quedó en espera con su plazo, y al volver el agente se entregó y terminó con código 0.
10 tests de integración cubren el disparo sobre un grupo de 3 servidores pasando por la reserva real,
las dos rutas de agente caído, tareas con cadena, la reserva exclusiva entre instancias, la zona horaria y
el rechazo de cron, destino y referencias inválidos.

> Pendiente: pausar y reprogramar tareas desde la API (hoy solo alta, consulta y disparo manual), y decidir
> qué hacer si un disparo se solapa con el anterior todavía en curso.

### Fase 8 — RabbitMQ multi-instancia
Exchange direct/topic por agente + fanout para presencia. Registro de presencia distribuido.
**Aceptación:** con 2 instancias del orquestador, un comando emitido desde A llega al agente conectado en B.

### Fase 9 — Auto-actualización del agente
`ForceUpdateMessage` → agente descarga binario según su RID, reemplaza y reconecta.
**Aceptación:** forzar update desde el orquestador sobre un agente Linux y uno Windows, confirmar nueva versión reportada.

---

> Documento vivo — se ajusta a medida que el diseño evoluciona durante la implementación.
