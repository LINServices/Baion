# Guía de Estilo de Código – C#

Convenciones de estilo personalizadas para mantener el código consistente, legible y mantenible.

## Tabla de contenidos

- [Idioma del código y comentarios](#idioma-del-código-y-comentarios)
- [Código limpio](#código-limpio)
- [Convenciones generales](#convenciones-generales)
- [Nombrado](#nombrado)
- [Constructores y métodos en una sola línea](#constructores-y-métodos-en-una-sola-línea)
- [Clases, records y structs](#clases-records-y-structs)
- [Modificadores de acceso](#modificadores-de-acceso)
- [Constantes y campos readonly](#constantes-y-campos-readonly)
- [Propiedades](#propiedades)
- [Orden de miembros y campos](#orden-de-miembros-y-campos)
- [Uso de `var`](#uso-de-var)
- [Namespaces y `using`](#namespaces-y-using)
- [Inyección de dependencias](#inyección-de-dependencias)
- [Controllers y validación](#controllers-y-validación)
- [Manejo de errores (Result/OneOf)](#manejo-de-errores-resultoneof)
- [Async/Await](#asyncawait)
- [Logging](#logging)
- [Nulabilidad](#nulabilidad)
- [Control de flujo: switch y pattern matching](#control-de-flujo-switch-y-pattern-matching)
- [Guard clauses](#guard-clauses)
- [Strings](#strings)
- [Colecciones y LINQ](#colecciones-y-linq)
- [Comentarios y documentación XML](#comentarios-y-documentación-xml)
- [Organización de archivos](#organización-de-archivos)

---

## Idioma del código y comentarios

- El código (nombres de clases, métodos, propiedades, variables, mensajes de excepción, valores de enum, etc.) se escribe **siempre en inglés**.
- Los comentarios se escriben **solo cuando son necesarios** (no se documenta lo obvio) y, cuando existen, van **en español**.
- Esto incluye **todos** los comentarios, también los XML doc comments (`///`): `<summary>`, `<param>`, `<returns>`, etc.

```csharp
// ✅ Correcto: código en inglés, comentario en español solo donde aporta valor
internal class InvoiceService(IInvoiceRepository repository) : IInvoiceService
{
    public async Task<Result<Invoice>> GetAsync(int id)
    {
        // El repositorio devuelve null para facturas archivadas, no solo inexistentes
        var invoice = await repository.GetByIdAsync(id);
        return invoice is null ? Result<Invoice>.Failure("Invoice not found") : Result<Invoice>.Success(invoice);
    }
}
```

---

## Código limpio

El código debe verse limpio y cuidado, como si lo hubiera escrito alguien que conoce el proyecto. Estas reglas aplican igual al código escrito a mano y al generado por IA.

### Comentarios

- Nada de comentarios que narren el código (`// Get the invoice`, `// Return the result`).
- Nada de marcadores de cierre (`} // end if`, `} // end method`).
- Nada de placeholders (`// TODO: implement`, `// your code here`).
- Nada de emojis en comentarios de código.
- Si un comentario explica *qué* hace el código, el nombre está mal: se renombra y se borra el comentario.

```csharp
// ❌ Evitar
// Get the invoice from the repository
var invoice = await repository.GetByIdAsync(id);

// Check if the invoice is null
if (invoice is null)
{
    return Result<Invoice>.Failure("Invoice not found");
} // end if

// ✅ Correcto
var invoice = await repository.GetByIdAsync(id);
if (invoice is null)
    return Result<Invoice>.Failure("Invoice not found");
```

### Nombres

- Nombres del dominio, no genéricos: `overdueInvoices`, no `data`, `result2`, `items`, `temp`, `info`.
- Sin el tipo en el nombre: `invoices`, no `invoiceList` ni `invoiceArray`.
- Sin nombres inflados: `GetOverdue()`, no `GetAllInvoicesThatAreCurrentlyOverdue()`.
- Sin sufijos comodín (`Helper`, `Manager`, `Util`, `Processor`, `Handler`) salvo que el nombre describa algo real.
- Lambdas con una letra que represente la entidad: `invoices.Where(i => i.IsOverdue)`, no `x` ni `item`.

### Tamaño y forma

- Métodos cortos que hacen una sola cosa; si necesita un comentario de sección ("// Paso 2"), es otro método.
- Un nivel de anidación como máximo dentro de un método (un `if` o un bucle); si hace falta un segundo nivel, se extrae a un método o se resuelve con guard clauses/LINQ.
- Una línea en blanco separa bloques lógicos; nunca dos líneas en blanco seguidas.
- Sin `else` después de un `return`.
- Sin `if (x) return true; else return false;` → `return x;`.

```csharp
// ❌ Evitar
if (invoice.Total > 0)
{
    return true;
}
else
{
    return false;
}

// ✅ Correcto
return invoice.Total > 0;
```

### Sin código de más

- Sin variables, métodos, `using` ni parámetros sin usar.
- Sin código comentado; para eso está el control de versiones.
- Sin `Console.WriteLine` ni código de depuración; se usa `ILogger`.
- Sin wrappers que solo delegan a otro método sin aportar nada.
- Antes de crear algo nuevo se busca si ya existe en el proyecto; no se duplica lógica.
- Sin dependencias nuevas si una existente ya resuelve el problema.

### Manejo de errores con criterio

- Sin `try/catch` genéricos alrededor de código que no puede fallar.
- Sin validar `null` en valores que el tipo ya garantiza como no nulos.
- Los `catch` solo existen donde realmente se maneja algo (llamadas externas, I/O, red), y capturan la excepción concreta.

```csharp
// ❌ Evitar
try
{
    var total = invoice.Lines.Sum(l => l.Amount);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error");
}

// ✅ Correcto
var total = invoice.Lines.Sum(l => l.Amount);
```

### Consistencia con el proyecto

- El código nuevo imita el estilo del código que lo rodea (nombres, estructura, patrones ya usados).
- No se introducen patrones, librerías ni características del lenguaje que el proyecto no use, salvo que se acuerde.
- Los cambios se limitan a lo pedido: no se refactorizan archivos ajenos a la tarea.

### Checklist antes de entregar

- [ ] ¿Podría borrar un tercio de los comentarios y el código quedaría igual de claro?
- [ ] ¿Cada nombre dice qué contiene, no qué tipo es?
- [ ] ¿Hay algún `try/catch` que no maneja nada real?
- [ ] ¿Hay código, variables o `using` sin usar?
- [ ] ¿Algo de esto ya existía en el proyecto?
- [ ] ¿Se ve como el resto del código del proyecto?

---

## Convenciones generales

- Indentación: 4 espacios, sin tabs.
- Estilo de llaves: **Allman** — la llave `{` siempre va en línea nueva.
- `if`, `for`, `foreach` y `while` **sin llaves** cuando el cuerpo es una sola sentencia que cabe en una línea. La sentencia va en la línea siguiente, indentada.
- Si el cuerpo tiene más de una sentencia, o la sentencia ocupa varias líneas, se usan llaves.
- En un `if/else`, si alguna de las ramas lleva llaves, ambas las llevan.
- Sin límite duro de longitud de línea: las firmas (constructores primarios, métodos) nunca se parten. Solo las cadenas LINQ/builder se dividen (ver [Constructores y métodos en una sola línea](#constructores-y-métodos-en-una-sola-línea)).
- Los atributos (`[HttpGet]`, `[Parameter]`, `[Required]`, etc.) van siempre en su propia línea, encima del elemento que decoran; nunca en la misma línea.
- Un archivo por elemento (clase, interfaz, record, enum, struct), sea público o interno. Ver [Organización de archivos](#organización-de-archivos).

```csharp
// ✅ Correcto (Allman)
public class InvoiceService
{
    public void Process(IEnumerable<Invoice> invoices)
    {
        foreach (var invoice in invoices)
        {
            invoice.Validate();
            invoice.Process();
        }
    }
}

// ✅ Una sola sentencia -> sin llaves, en la línea siguiente
if (invoice is null)
    return Result<Invoice>.Failure("Invoice not found");

foreach (var line in invoice.Lines)
    line.Recalculate();

// ❌ Evitar: llaves para una sola sentencia
if (invoice is null)
{
    return Result<Invoice>.Failure("Invoice not found");
}

// ❌ Evitar: todo en la misma línea
if (invoice is null) return Result<Invoice>.Failure("Invoice not found");
```

---

## Nombrado

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, records, structs | PascalCase | `InvoiceService` |
| Interfaces | PascalCase con prefijo `I` | `IInvoiceRepository` |
| Métodos | PascalCase | `GetInvoiceById()` |
| Propiedades | PascalCase | `InvoiceNumber` |
| Variables locales / parámetros | camelCase | `invoiceNumber` |
| Campos privados | `_camelCase` | `_invoiceRepository` |
| Constantes | PascalCase | `MaxRetries` |
| Métodos async | PascalCase + sufijo `Async` | `GetInvoiceAsync()` |

---

## Constructores y métodos en una sola línea

Regla general: **los constructores primarios van siempre en una sola línea**, sin importar la longitud de la firma o de la lista de parámetros. No se parten en varios renglones.

Los **arrows (`=>`) se usan solo en propiedades**, nunca en métodos. Un método siempre lleva llaves `{ }` con su(s) sentencia(s) dentro, aunque el cuerpo sea una sola línea.

La única excepción a "no partir en varias líneas" son las cadenas de llamadas anidadas tipo builder o LINQ (`.Where().Select().ToList()`, fluent builders, etc.), donde sí se permite y se prefiere partir en varias líneas para legibilidad.

```csharp
// ✅ Constructor primario siempre en una sola línea, sin importar cuántos parámetros
internal class InvoiceService(IInvoiceRepository repository, ICustomerRepository customerRepository, ILogger<InvoiceService> logger) : IInvoiceService
{
    // ✅ Método con llaves, nunca con arrow
    public async Task<Invoice?> GetAsync(int id)
    {
        return await repository.GetByIdAsync(id);
    }
}
```

```csharp
// ✅ Única excepción: cadenas LINQ/builder sí se parten en varias líneas
var overdueInvoices = invoices
    .Where(i => i.IsOverdue)
    .OrderBy(i => i.DueDate)
    .Select(i => i.ToDto())
    .ToList();
```

---

## Clases, records y structs

- `record` se reserva **exclusivamente para DTOs** (modelos de transferencia de datos, inmutables).
- `class` se usa para entidades, servicios y todo lo que tenga comportamiento o identidad.
- `sealed` solo se aplica cuando la clase **explícitamente no debe heredarse** (no es el default automático).

```csharp
// DTO -> record
public record InvoiceDto(int Id, string Number, decimal Total, DateOnly Date);

// Entidad / servicio -> class
internal class InvoiceService(IInvoiceRepository repository) : IInvoiceService
{
}

// Clase que explícitamente no debe extenderse
public sealed class InvoiceValidator
{
}
```

---

## Modificadores de acceso

Los modificadores de acceso se escriben **siempre de forma explícita**, incluso cuando coinciden con el valor por defecto del lenguaje.

```csharp
// ✅ Correcto
private readonly ConcurrentDictionary<int, Invoice> _cache = new();

internal class InvoiceNumberGenerator
{
    private const int MaxRetries = 3;
}

// ❌ Evitar (dejar el modificador implícito)
readonly ConcurrentDictionary<int, Invoice> _cache = new();
```

---

## Constantes y campos readonly

- `const`: cuando el valor es fijo en tiempo de compilación (no cambia nunca, tipo primitivo o string).
- `readonly` / `static readonly`: para el resto de valores fijos que se calculan o asignan en tiempo de ejecución (constructor o inicialización estática).

```csharp
private const int MaxRetries = 3;                 // fijo en compilación
private readonly ConcurrentDictionary<int, Invoice> _cache = new(); // asignado en inicialización
private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30); // calculado en runtime
```

---

## Propiedades

- `init` en lugar de `set` cuando el valor no debe cambiar después de construir el objeto.
- Las propiedades calculadas usan `=>` (es el único lugar donde se permite el arrow; ver [Constructores y métodos en una sola línea](#constructores-y-métodos-en-una-sola-línea)).

```csharp
public class Invoice
{
    public int Id { get; init; }
    public decimal Total { get; init; }
    public DateOnly DueDate { get; init; }

    public bool IsOverdue => DueDate < DateOnly.FromDateTime(DateTime.UtcNow);
}
```

---

## Orden de miembros y campos

Dentro de una clase, los miembros van en este orden:

1. Constantes (`const`)
2. Campos (`static readonly`, `readonly`, resto)
3. Propiedades
4. Constructores (si no se usa constructor primario)
5. Métodos públicos
6. Métodos privados
7. Miembros `static` (al final, en el mismo orden público → privado)

Con constructor primario, las dependencias se usan **directamente por el nombre del parámetro** (`repository`), sin copiarlas a un campo `_repository`. Los campos `_camelCase` solo existen cuando se declaran de forma explícita (estado propio de la clase, no dependencias inyectadas).

```csharp
internal class InvoiceService(IInvoiceRepository repository) : IInvoiceService
{
    private const int MaxRetries = 3;

    private readonly ConcurrentDictionary<int, Invoice> _cache = new();

    public async Task<Result<Invoice>> GetAsync(int id)
    {
        var invoice = await repository.GetByIdAsync(id);
        return invoice is null ? Result<Invoice>.Failure("Invoice not found") : Result<Invoice>.Success(invoice);
    }

    private static string FormatNumber(int id)
    {
        return $"INV-{id:D6}";
    }
}
```

---

## Uso de `var`

Se usa `var` **siempre que el tipo sea evidente** por el lado derecho de la asignación.

```csharp
var invoice = new Invoice();          // ✅ tipo evidente
var invoices = await repository.GetAllAsync(); // ✅ evidente por el método
decimal total = CalculateTax(x, y); // tipo explícito si no es evidente a simple vista
```

---

## Namespaces y `using`

- File-scoped namespaces **siempre**.
- Los `using` van **fuera del namespace**, con `System.*` primero y el resto en orden alfabético.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LIN.Module.Persistence.Repositories;

namespace LIN.Module.Services.Implementations;

internal class InvoiceService : IInvoiceService
{
}
```

---

## Inyección de dependencias

Siempre se inyectan **interfaces**, nunca clases concretas, aunque hoy exista una sola implementación: la interfaz es el contrato público de la capa y la implementación queda `internal`, así ninguna otra capa puede acoplarse a ella. Las interfaces son `public` y viven en la carpeta de su tipo (`Services/`, `Repositories/`); las implementaciones son `internal` y viven en su subcarpeta `Implementations/`, de forma que otras capas no puedan referenciarlas directamente. Cada capa registra sus propios servicios mediante una extensión de `IServiceCollection`.

```csharp
// public, en Repositories/ de LIN.Module.Persistence
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
}

// internal, en Repositories/Implementations/
internal class InvoiceRepository(InvoiceDbContext context) : IInvoiceRepository
{
    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await context.Invoices.FindAsync(id);
    }
}

// Extensión de DI propia de la capa, en Extensions/
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services.AddScoped<IInvoiceRepository, InvoiceRepository>();
    }
}
```

---

## Controllers y validación

- Los endpoints de API usan **Controllers tradicionales** (`[ApiController]`), no Minimal APIs.
- La validación de datos de entrada se hace **manualmente dentro del servicio**, no con Data Annotations ni FluentValidation.

```csharp
[ApiController]
[Route("api/invoices")]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    /// <summary>
    /// Obtiene una factura por su identificador.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        var result = await invoiceService.GetAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
```

---

## Manejo de errores (Result/OneOf)

En lugar de lanzar excepciones para errores esperados (validaciones, reglas de negocio, "no encontrado"), se usa un patrón **Result/OneOf**. Las excepciones quedan reservadas para errores realmente excepcionales/no controlados.

```csharp
public async Task<Result<Invoice>> GetAsync(int id)
{
    var invoice = await repository.GetByIdAsync(id);

    return invoice is null
        ? Result<Invoice>.Failure("Invoice not found")
        : Result<Invoice>.Success(invoice);
}
```

---

## Async/Await

- No se usa `ConfigureAwait(false)` (proyectos ASP.NET Core, no librerías standalone).
- `CancellationToken` se propaga **solo cuando la operación es larga o puede cancelarse** (no en todos los métodos por sistema).

```csharp
// Operación larga / cancelable -> se propaga el token
public async Task<Invoice?> GetAsync(int id, CancellationToken cancellationToken)
{
    return await repository.GetByIdAsync(id, cancellationToken);
}

// Operación corta -> sin CancellationToken
public async Task<bool> ExistsAsync(int id)
{
    return await repository.ExistsAsync(id);
}
```

---

## Logging

Se usa `ILogger` con **logging estructurado y placeholders**, nunca interpolación de strings en el mensaje de log (para preservar los datos como campos estructurados).

```csharp
// ✅ Correcto
logger.LogInformation("Invoice {InvoiceId} processed for customer {CustomerId}", invoice.Id, customer.Id);

// ❌ Evitar
logger.LogInformation($"Invoice {invoice.Id} processed for customer {customer.Id}");
```

---

## Nulabilidad

- Nullable Reference Types **habilitado siempre** (`<Nullable>enable</Nullable>`).
- Preferir `is null` / `is not null` sobre `== null`.

```csharp
if (invoice is null)
    return Result<Invoice>.Failure("Invoice not found");
```

---

## Control de flujo: switch y pattern matching

- Se prefieren **switch expressions** sobre switch statements tradicionales, siempre que la lógica lo permita.
- Pattern matching se usa **siempre que sea posible** (`is Tipo variable`, patrones de propiedad, etc.).

```csharp
// ✅ Switch expression (el método sigue llevando llaves; el arrow es del switch, no del método)
public string GetStatusText(InvoiceStatus status)
{
    return status switch
    {
        InvoiceStatus.Pending => "Pending",
        InvoiceStatus.Paid => "Paid",
        InvoiceStatus.Overdue => "Overdue",
        _ => "Unknown"
    };
}

// ✅ Pattern matching
if (result is { IsSuccess: true, Value: InvoiceDto dto })
    return Ok(dto);
```

---

## Guard clauses

Las validaciones van **al inicio del método**, con guard clauses y `return` temprano, evitando anidar lógica en `if/else`.

```csharp
public async Task<Result<Invoice>> ProcessAsync(int id)
{
    if (id <= 0)
        return Result<Invoice>.Failure("Id must be greater than zero");

    var invoice = await repository.GetByIdAsync(id);
    if (invoice is null)
        return Result<Invoice>.Failure("Invoice not found");

    invoice.Process();
    return Result<Invoice>.Success(invoice);
}
```

---

## Strings

- Por defecto, **string interpolation** (`$"..."`).
- `string.Format` solo cuando la plantilla vive aparte del uso (una constante, un recurso, configuración) y se reutiliza.
- Nunca concatenación con `+` para construir mensajes.

```csharp
// ✅ Interpolación por defecto
var invoiceNumber = $"INV-{id:D6}";

// ✅ string.Format solo con plantilla reutilizable
private const string InvoiceNumberTemplate = "INV-{0:D6}";
var invoiceNumber = string.Format(InvoiceNumberTemplate, id);
```

---

## Colecciones y LINQ

- Se prefiere **LINQ** (`Where`, `Select`, `FirstOrDefault`, etc.) sobre bucles `for`/`foreach` tradicionales.
- Se usan **collection expressions** (C# 12) para inicializar colecciones.

```csharp
// ✅ LINQ simple, cabe en una línea
var overdueInvoices = invoices.Where(i => i.IsOverdue).ToList();

// ✅ LINQ encadenado más largo -> se parte en varias líneas (única excepción a "una sola línea")
var overdueSummaries = invoices
    .Where(i => i.IsOverdue)
    .OrderBy(i => i.DueDate)
    .Select(i => i.ToDto())
    .ToList();

// ✅ Collection expression
int[] businessDays = [1, 2, 3, 4, 5];
List<string> statuses = ["Pending", "Paid", "Overdue"];
```

---

## Comentarios y documentación XML

- `///` (XML doc comments) se usa en:
  - **Endpoints de API**: resumen corto y simple.
  - **Interfaces**: comentario simple describiendo el propósito del miembro.
- No se documentan implementaciones internas ni métodos privados triviales.
- Los XML docs son la excepción a "no narrar": documentan el contrato (Swagger, IntelliSense). Aun así, cuando el nombre ya lo dice todo, el `<summary>` aporta lo que el nombre no: qué pasa si no existe, qué filtra, qué efectos tiene.
- El `<summary>` siempre respeta cada etiqueta en su propia línea: la apertura `<summary>`, el texto, el cierre `</summary>`, y cada `<param>`/`<paramref>` en su propia línea. Nunca se comprime todo en una sola línea.

```csharp
// ❌ Evitar: todo comprimido en una sola línea
/// <summary>Lista los servicios del sistema del servidor. <paramref name="filter"/> acota por subcadena del nombre.</summary>

// ✅ Correcto: cada línea respetada
/// <summary>
/// Lista los servicios del sistema del servidor.
/// </summary>
/// <param name="serverId">Id del servidor a consultar.</param>
/// <param name="filter">Acota por subcadena del nombre.</param>
public interface IInvoiceRepository
{
    /// <summary>
    /// Obtiene una factura por su identificador. Devuelve null si no existe o está archivada.
    /// </summary>
    Task<Invoice?> GetByIdAsync(int id);
}

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    /// <summary>
    /// Obtiene una factura por su identificador.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        // ...
    }
}
```

---

## Organización de archivos

Cada capa es una **biblioteca (proyecto) independiente** dentro de la solución, no una carpeta dentro de un único proyecto.

- **LIN.Module.Presentation**: controladores y filtros de la capa web/API.
- **LIN.Module.Services**: interfaces de servicio `public` a nivel de su carpeta; implementaciones `internal` en la subcarpeta `Implementations/`.
- **LIN.Module.Extensions**: métodos de extensión (`this` helpers) compartidos.
- **LIN.Module.Persistence**: contexto de datos (EF Core u otro) e interfaces de repositorio `public` a nivel de su carpeta, con sus implementaciones `internal` en `Implementations/`.
- **LIN.Module.Models**: entidades, DTOs y enums (cada uno en su carpeta), referenciada por el resto de proyectos.
- Cada capa expone su **propia extensión de registro de DI** (`AddXxx(this IServiceCollection services)`), para que el proyecto de arranque solo llame `services.AddServices().AddPersistence()...` sin conocer las implementaciones internas.

### Un archivo por elemento

Cada elemento va en **su propio archivo**: clase, interfaz, record, enum, struct, delegate. Esto aplica también a los tipos `internal` y a los DTOs pequeños; nunca se agrupan varios tipos en un mismo archivo. El nombre del archivo es exactamente el nombre del tipo.

```
✅ Correcto
Dtos/
├── InvoiceDto.cs
└── InvoiceLineDto.cs
Enums/
└── InvoiceStatus.cs

❌ Evitar
Dtos/
└── InvoiceModels.cs      (InvoiceDto + InvoiceLineDto + InvoiceStatus juntos)
```

### Carpetas por tipo de elemento

Dentro de cada proyecto, los archivos se agrupan en carpetas según **qué tipo de elemento son**. Nunca quedan sueltos en la raíz del proyecto (salvo el `.csproj`).

| Tipo de elemento | Carpeta |
|---|---|
| Entidades | `Entities/` |
| DTOs | `Dtos/` |
| Enums | `Enums/` |
| Clases de configuración (`IOptions<T>`) | `Options/` |
| Interfaces de servicio | `Services/` |
| Implementaciones de servicio | `Services/Implementations/` |
| Interfaces de repositorio | `Repositories/` |
| Implementaciones de repositorio | `Repositories/Implementations/` |
| Métodos de extensión (incluido el registro de DI) | `Extensions/` |
| Constantes | `Constants/` |
| Controladores | `Controllers/` |
| Filtros | `Filters/` |

Si un proyecto no tiene elementos de un tipo, esa carpeta simplemente no existe.

### Estructura semántica

**Cada carpeta contiene solo lo que su nombre indica, en todos los niveles.** El nombre de la carpeta es un contrato: quien abre `Dtos/` espera encontrar DTOs y nada más.

- Una carpeta puede subdividirse en **subcarpetas por subtipo** cuando tenga sentido (ej. `Dtos/Requests/`, `Dtos/Outputs/`, `Services/Implementations/`).
- La subcarpeta también cumple la regla: dentro de `Dtos/Requests/` solo hay DTOs de entrada.
- Nunca se mezclan elementos que no correspondan al nombre de la carpeta donde están: interfaces, DTOs, entidades, enums u options van cada uno en su carpeta, aunque pertenezcan a la misma funcionalidad.
- No se crean subcarpetas por crearlas: solo cuando agrupan un subtipo real y hay suficientes archivos para que ayuden a encontrarlos.

```
✅ Correcto: subcarpetas por subtipo
Dtos/
├── Requests/
│   ├── CreateInvoiceRequest.cs
│   └── UpdateInvoiceRequest.cs
└── Outputs/
    ├── InvoiceOutput.cs
    └── InvoiceLineOutput.cs

❌ Evitar: elementos que no corresponden al nombre de la carpeta
Dtos/
├── Requests/
│   ├── CreateInvoiceRequest.cs
│   └── InvoiceRequestValidator.cs   (no es un DTO)
├── InvoiceOutput.cs
└── InvoiceStatus.cs                 (enum: va en Enums/)
```

### Subcarpetas por dominio

Cuando una aplicación tiene varios dominios o subdominios (ej. `Mail`, `Calendar`, `Notes`), las carpetas de tipo se subdividen **por dominio** para que no crezcan sin control. El orden es siempre **tipo → dominio → subtipo**:

- Primero la carpeta de tipo (`Services/`, `Dtos/`, `Enums/`…), que sigue mandando: todo lo que haya debajo es de ese tipo.
- Dentro, una subcarpeta por dominio (`Services/Mail/`, `Services/Calendar/`).
- Dentro del dominio, las subcarpetas por subtipo si hacen falta (`Services/Mail/Implementations/`, `Dtos/Mail/Requests/`).
- Un dominio puede tener subdominios con la misma lógica (`Services/Mail/Attachments/`).
- Si un dominio usa servicios de otro (Mail usa Calendar para las invitaciones), los consume por su interfaz; cada servicio sigue viviendo en la carpeta de **su** dominio, nunca se copia ni se mueve al dominio que lo usa.

```
✅ Correcto: tipo → dominio → subtipo
Services/
├── Mail/
│   ├── IMailService.cs
│   ├── IInboxService.cs
│   └── Implementations/
│       ├── MailService.cs              (usa ICalendarService para invitaciones)
│       └── InboxService.cs
├── Calendar/
│   ├── ICalendarService.cs
│   └── Implementations/
│       └── CalendarService.cs
└── Notes/
    ├── INoteService.cs
    └── Implementations/
        └── NoteService.cs
Dtos/
├── Mail/
│   ├── Requests/
│   │   └── SendMailRequest.cs
│   └── Outputs/
│       └── MailOutput.cs
└── Calendar/
    ├── Requests/
    │   └── CreateEventRequest.cs
    └── Outputs/
        └── EventOutput.cs

❌ Evitar: todos los servicios sueltos en la misma carpeta
Services/
├── IMailService.cs
├── IInboxService.cs
├── ICalendarService.cs
├── INoteService.cs
└── Implementations/
    ├── MailService.cs
    ├── InboxService.cs
    ├── CalendarService.cs
    └── NoteService.cs

❌ Evitar: dominio primero, mezclando tipos dentro
Mail/
├── IMailService.cs
├── SendMailRequest.cs
└── MailStatus.cs
```

Si la aplicación tiene un solo dominio, no se crea la subcarpeta de dominio.

```
❌ Evitar: carpeta que mezcla tipos
Invoices/
├── IInvoiceService.cs        (interfaz)
├── InvoiceService.cs         (implementación)
├── InvoiceDto.cs             (DTO)
├── InvoiceStatus.cs          (enum)
└── InvoiceOptions.cs         (options)

✅ Correcto: cada tipo en su carpeta
Services/
├── IInvoiceService.cs
└── Implementations/
    └── InvoiceService.cs
Dtos/
└── InvoiceDto.cs
Enums/
└── InvoiceStatus.cs
Options/
└── InvoiceOptions.cs
```

```
LIN.Module.sln
├── LIN.Module.Presentation/
│   ├── Controllers/
│   │   └── InvoicesController.cs
│   ├── Filters/
│   │   └── ExceptionFilter.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs       (AddPresentation())
│   └── LIN.Module.Presentation.csproj
├── LIN.Module.Services/
│   ├── Services/
│   │   ├── IInvoiceService.cs                   (public)
│   │   └── Implementations/
│   │       └── InvoiceService.cs                (internal)
│   ├── Options/
│   │   └── InvoiceOptions.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs       (AddServices())
│   └── LIN.Module.Services.csproj
├── LIN.Module.Extensions/
│   ├── Extensions/
│   │   └── StringExtensions.cs
│   └── LIN.Module.Extensions.csproj
├── LIN.Module.Persistence/
│   ├── Context/
│   │   └── InvoiceDbContext.cs
│   ├── Repositories/
│   │   ├── IInvoiceRepository.cs                (public)
│   │   └── Implementations/
│   │       └── InvoiceRepository.cs             (internal)
│   ├── Options/
│   │   └── DatabaseOptions.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs       (AddPersistence())
│   └── LIN.Module.Persistence.csproj
└── LIN.Module.Models/
    ├── Entities/
    │   └── Invoice.cs
    ├── Dtos/
    │   └── InvoiceDto.cs
    ├── Enums/
    │   └── InvoiceStatus.cs
    └── LIN.Module.Models.csproj
```

```csharp
// LIN.Module.Services/Services/IInvoiceService.cs
public interface IInvoiceService
{
    Task<Result<Invoice>> GetAsync(int id);
}

// LIN.Module.Services/Services/Implementations/InvoiceService.cs
internal class InvoiceService(IInvoiceRepository repository) : IInvoiceService
{
    public async Task<Result<Invoice>> GetAsync(int id)
    {
        var invoice = await repository.GetByIdAsync(id);
        return invoice is null ? Result<Invoice>.Failure("Invoice not found") : Result<Invoice>.Success(invoice);
    }
}

// LIN.Module.Services/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        return services.AddScoped<IInvoiceService, InvoiceService>();
    }
}
```

---

> Esta guía refleja el estilo de codificación real del autor. Es un documento vivo: se ajusta a medida que el estilo evoluciona.
