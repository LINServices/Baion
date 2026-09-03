# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Qué es Baion

Orquestador de servidores (VM, VPS, bare metal): replica scripts, programa tareas y recolecta métricas
sobre miles de agentes conectados a la vez. Producto standalone que también se integra con el ecosistema LIN.

`PLAN.md` es el documento vivo del diseño y del avance por fases (0–9, con las completadas marcadas ✅).
`CODING_STYLE.md` es la guía de estilo del autor y **es normativa**: léela antes de escribir código.

## Idioma

Identificadores de tipos y miembros en **inglés**. Comentarios, XML docs, mensajes de log, textos de error
y toda la interfaz del panel en **español**. Los identificadores locales dentro de un método suelen ir en
español; sigue lo que veas en el archivo que estés tocando.

## Las tres soluciones

| Solución | Qué es | Puerto local |
|---|---|---|
| `Baion.Orchestrator.sln` | API + endpoint WebSocket de agentes | 5199 |
| `Baion.Agent.sln` | Agente que se instala en cada servidor | — |
| `cliente/Baion.Cliente.sln` | Panel Blazor + Tailwind | 5100 |

`src/Baion.Contracts` es el **protocolo compartido** entre orquestador y agente: está en ambas soluciones.
El panel **no** lo referencia: duplica los DTOs a mano en `Models/ApiModels.cs`, a propósito, porque es una
aplicación aparte que solo habla HTTP y compartir ensamblados la ataría a la versión exacta del servidor.

## Comandos

```bash
# Compilar (los tres han de quedar con 0 warnings)
dotnet build Baion.Orchestrator.sln
dotnet build Baion.Agent.sln
dotnet build cliente/Baion.Cliente.sln

# Arrancar en local (ver "Arranque local" más abajo para el orden)
dotnet run --project src/Orchestrator/Baion.Orchestrator.Presentacion
dotnet run --project cliente/Baion.Cliente.Web
dotnet run --project src/Agent/Baion.Agent.Host

# Tests: por proyecto
dotnet test tests/Baion.Orchestrator.Persistence.Tests/Baion.Orchestrator.Persistence.Tests.csproj

# Tests: una clase o un caso concreto
dotnet test tests/Baion.Orchestrator.Agents.Tests/Baion.Orchestrator.Agents.Tests.csproj \
  --filter "FullyQualifiedName~ScriptChainTests"
dotnet test tests/Baion.Agent.Execution.Tests/Baion.Agent.Execution.Tests.csproj \
  --filter "FullyQualifiedName~ExecuteAsync_AlVencerElTimeout"

# Migraciones (proyecto y startup son el mismo: Persistence tiene su IDesignTimeDbContextFactory)
dotnet ef migrations add <Nombre> \
  --project src/Orchestrator/Baion.Orchestrator.Persistence \
  --startup-project src/Orchestrator/Baion.Orchestrator.Persistence --output-dir Migrations
dotnet ef database update \
  --project src/Orchestrator/Baion.Orchestrator.Persistence \
  --startup-project src/Orchestrator/Baion.Orchestrator.Persistence

# Tailwind al vuelo mientras editas componentes (el build ya lo compila solo)
cd cliente/Baion.Cliente.Web && npm run css:watch
```

Variables de entorno útiles: `BAION_DESIGN_CONNECTION` (cadena para `dotnet ef`),
`BAION_TEST_CONNECTION` (servidor SQL para los tests, por defecto LocalDB).

## Restricciones del build que muerden

- **`ImplicitUsings` está desactivado** en todo el repo (`Directory.Build.props`). Declara todos los `using`.
  En Razor el equivalente es `cliente/Baion.Cliente.Web/Components/_Imports.razor`.
- **`TreatWarningsAsErrors` está activo.** Un warning rompe el build.
- **Gestión central de paquetes**: las versiones van en `Directory.Packages.props`. Un `PackageReference`
  con `Version=` inline falla. `dotnet add package` deja `Version="*"`, que también falla (NU1011): fija la
  versión a mano en el archivo central.
- **`nuget.config` con package source mapping**: `*` → nuget.org, `LIN.*` / `Global.*` / `Http.Utils` → feed
  interno `LI`. Un paquete nuevo de un prefijo no mapeado no restaura.
- **`.editorconfig` traduce `CODING_STYLE.md` a reglas de análisis** (file-scoped namespaces, modificadores
  explícitos, `_camelCase`, sufijo `Async`…). Las migraciones generadas están exentas.

## Arquitectura

### Convención de capas (orquestador y agente)

Cada capa es un proyecto con **interfaces `public` en la raíz** e **implementaciones `internal` en
`Implementations/`**, y expone su propia extensión `AddXxx(IServiceCollection, IConfiguration)`. El proyecto
de arranque solo encadena esas extensiones y no conoce ninguna implementación.

Errores esperados con `Result<T>` / `Error` (`Models/Results/`), nunca excepciones. `ErrorKind` decide el
código HTTP en `Presentacion/ResultExtensions.cs`.

### Multi-tenancy — el invariante más importante

Aislamiento en **dos frentes**, ambos en `Baion.Orchestrator.Persistence`:

1. **Lectura**: filtro global aplicado por reflexión a todo lo que implemente `ITenantOwned`, parametrizado
   contra `ITenantContext` en cada consulta. **Sin tenant resuelto, no devuelve filas** (deny by default).
2. **Escritura**: `TenantStampInterceptor` sella el `tenant_id` al insertar y lanza si se intenta escribir
   una fila de otro tenant o guardar sin tenant en el scope.

Consecuencias prácticas:
- Todo scope que vaya a tocar la base necesita `ITenantContext.SetTenant(...)` antes. En la API lo hace
  `TenantResolutionMiddleware` a partir del claim `tid`; en los servicios en segundo plano hay que hacerlo
  a mano al abrir cada scope.
- Las consultas de instancia (no de tenant) usan `IgnoreQueryFilters()` a propósito: enrolamiento por
  credencial, barrido del planificador, presencia. Están comentadas donde ocurre.

### Protocolo de agentes

El agente **siempre marca hacia fuera** (WebSocket saliente a `/ws/agent`): funciona tras NAT sin abrir
puertos. Una vez abierto, el canal es bidireccional.

- Credenciales en cabeceras, validadas **antes del upgrade** → un agente sin permiso recibe 401 y el socket
  nunca se establece (`AgentSocketController`).
- Dos credenciales: token de instalación (una sola vez) y credencial permanente que el agente persiste.
  `MachineId` hace idempotente el reenrolamiento.
- `BaionMessageChannel` (en `Baion.Contracts`) encuadra los mensajes en **los dos extremos**, para que el
  formato de trama no pueda divergir. Dos jerarquías polimórficas separadas por dirección
  (`ServerToAgentMessage` / `AgentToServerMessage`) con discriminador `type`.
- `ScriptRuntimeCompatibility` (también en Contracts) la aplican ambos lados: el orquestador para rechazar
  antes de crear filas, el agente para no fiarse de él.

### El hilo del socket nunca bloquea

Regla estructural del orquestador. `AgentConnectionHandler` solo encola y sigue leyendo:

- Métricas → `IMetricIngestQueue` → `MetricIngestHostedService` escribe en lotes agrupados por tenant.
  El buzón **descarta al llenarse**: ante sobrecarga se prefiere perder telemetría a frenar los sockets.
- Salida y desenlace de ejecuciones → `IScriptEventQueue` → `ScriptEventIngestHostedService`. Comparten un
  **único buzón ordenado** para que nadie vea una ejecución terminada con la salida a medias.

Al añadir un tipo de mensaje del agente, encólalo; no escribas en la base desde el handler.

### Despacho de comandos y multi-instancia

`IAgentCommandBus` resuelve dónde vive el socket: entrega local si está en este proceso, y si no consulta la
presencia (`IAgentPresenceLookup`, sobre las columnas de `servers`) y publica en el exchange topic con clave
`agent.{serverId:N}`. Solo la instancia enlazada a esa clave lo recibe.

Con `RabbitMq:Enabled=false` todo funciona, pero cada instancia solo alcanza a **sus propios** agentes.

Para evitar ciclos entre capas, `Baion.Orchestrator.Messaging` **declara** `ILocalAgentDelivery` e
`IAgentPresenceLookup`, y los implementan Services y Persistence respectivamente.

### Cadenas y programación

- Las cadenas las conduce el orquestador paso a paso; el agente solo ve ejecuciones sueltas. No hay tabla de
  recorridos: el estado se deduce de las ejecuciones que comparten `chain_run_id`, y un índice único sobre
  `(chain_run_id, script_chain_step_id)` hace idempotente el avance.
- El planificador corre en todas las instancias y **reserva cada disparo con una escritura condicional**
  sobre `next_run_at`. Sin locks ni líder.

### Persistencia

- Convención `snake_case` aplicada globalmente en `BaionDbContext.OnModelCreating`.
- Consultas de solo lectura del panel en `IServerQueries` / `IScriptQueries`: **proyectan directamente a
  DTO**, aparte de los repositorios. No uses `Include` para pintar tablas — arrastraría `std_out`/`std_err`,
  que son `nvarchar(max)` sin tope.
- `metrics` está **particionada por mes** sobre `captured_at`; `MetricPartitionMaintenanceHostedService`
  mantiene los límites por delante.
- La salida de scripts se acumula con `.WRITE` de SQL Server, no concatenando.

### Panel

Blazor Web App con render mode `InteractiveServer`. La sesión va en una **cookie cifrada y HttpOnly que
lleva dentro el JWT de la API**, así que el navegador nunca lo ve. El login es una página **SSR estática**
(`[ExcludeFromInteractiveRouting]`) porque firmar la cookie necesita el `HttpContext`.

El token se adjunta **petición a petición** en `BaionApiClient`, no con un `DelegatingHandler`: los
manejadores de `IHttpClientFactory` viven en otro ámbito y desde ahí no se ve el usuario del circuito.

Tailwind 4 se compila en un target de MSBuild antes de recoger los estáticos. `-p:RunTailwind=false`
compila sin Node.

#### Sistema de diseño

El panel sigue el sistema **«Quiet Data»**, y `cliente/Baion.Cliente.Web/Styles/app.css` es su única
fuente de verdad: ahí están los tokens, la escala tipográfica y las clases `.baion-*`.

- **Monocromo con un solo acento**, llamado `current` y consumido siempre por escala
  (`bg-current-500`, `text-current-700`). `--gim-current` es **la única línea de marca**: cambiarla
  re-tematiza el producto entero. Ningún componente puede asumir de qué color es.
- **Ni un hex ni un color de la paleta por defecto de Tailwind en el marcado.** Si una clase de color
  no sale de `app.css`, está mal puesta. Lo mismo con los tamaños de texto: solo la escala
  `display · metric-xl · metric-lg · heading · title · body · value · label`.
- El acento **significa** algo (lo logrado, lo activo, lo actual) y aparece como mucho en tres sitios
  por vista. La acción primaria es negra, no de acento. Las tarjetas llevan hairline de 1px y **nunca
  sombra**; la sombra se reserva a overlays y al módulo glass, que va **una vez por vista**.
- Lo pendiente o proyectado se dibuja con **ticks fantasma** (`bg-ghost-ticks`), no en gris sólido.
  Los estados de error usan `danger`/`warn` solo en texto e iconos pequeños o en el contorno.
- Primitivas de dato en `Components/Shared`: `SegmentedBar`, `Gauge`, `DataBullet`, `MetricValue`,
  `GlassInsight`, `GhostLoader`. Los iconos son Lucide embebidos (`Icon` + `LucideIcons`), trazo 1.5.
- La tipografía es **Gilroy, autoalojada**: es comercial y sus `.woff2` no están en el repositorio.
  Ver `cliente/Baion.Cliente.Web/wwwroot/fonts/README.md`. Sin ellos el panel usa el fallback con
  métricas ajustadas y no salta el layout.

## Tests

Los de integración son **contra SQL Server de verdad** (LocalDB), con una base desechable por fixture.
`tests/Baion.TestSupport` tiene el fixture compartido. `Baion.Orchestrator.Agents.Tests` levanta la
aplicación entera con `WebApplicationFactory` y habla por **WebSockets reales**; `FakeAgent` es un agente
simulado que deja a la prueba decidir qué responde.

Tres cosas que hay que saber:

- `xunit.runner.json` de `Baion.Orchestrator.Agents.Tests` **serializa las colecciones**. No lo quites: cada
  clase levanta aplicaciones completas con su base, y en paralelo saturan LocalDB hasta colgar la suite.
- Los tests marcados con `[RequiresRabbitMqFact]` se **omiten solos** si no hay broker en `localhost:5672`.
  Para ejecutarlos: `docker run -d --name baion-rabbit -p 5672:5672 rabbitmq:4-management-alpine`.
  La sonda que lo comprueba es síncrona a propósito: se evalúa durante el descubrimiento de xUnit, y
  bloquear ahí sobre un `Task` cuelga el host de pruebas justo cuando el broker sí responde.
- **Con broker levantado, los 52 tests pasan pero el proceso de pruebas no termina** y `dotnet test`
  se queda colgado al final. Está acotado a `AutomaticRecoveryEnabled` del cliente de RabbitMQ: poniéndolo
  en false el proceso sale. No se ha corregido; conviene revisar también si afecta al apagado ordenado del
  orquestador en producción.

## Arranque local

Los `appsettings.Development.json` de los tres servicios llevan comentarios con las instrucciones (el lector
de configuración de ASP.NET Core admite comentarios en JSON). En resumen:

1. `dotnet ef database update ...` para crear el esquema.
2. Levantar el orquestador. En su primer arranque crea el tenant `dev` y su administrador
   (`Identity:Bootstrap`, idempotente): organización `dev`, `admin@baion.local` / `desarrollo-local-cambiar`.
3. Levantar el panel y entrar en http://localhost:5100.
4. Para el agente: emitir un token con `POST /api/agents/enrollment-tokens` (rol Admin) y pasarlo por
   `Agent__EnrollmentToken`. El estado del agente en desarrollo queda en `.agent-state`; borrarlo equivale a
   reinstalarlo.

El perfil `segunda-instancia` del orquestador (puerto 5299, RabbitMQ activo) sirve para probar el
enrutado multi-instancia.
