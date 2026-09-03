# Guía de Estilo de Código – C#

Convenciones de estilo personalizadas para mantener el código consistente, legible y mantenible.

## Tabla de contenidos

- [Convenciones generales](#convenciones-generales)
- [Nombrado](#nombrado)
- [Constructores y métodos en una sola línea](#constructores-y-métodos-en-una-sola-línea)
- [Clases, records y structs](#clases-records-y-structs)
- [Modificadores de acceso](#modificadores-de-acceso)
- [Constantes y campos readonly](#constantes-y-campos-readonly)
- [Miembros static vs instancia](#miembros-static-vs-instancia)
- [Propiedades y miembros expression-bodied](#propiedades-y-miembros-expression-bodied)
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

## Convenciones generales

- Indentación: 4 espacios, sin tabs.
- Estilo de llaves: **Allman** — la llave `{` siempre va en línea nueva.
- Longitud máxima de línea recomendada: 120 caracteres.
- Un archivo por tipo público (clase, interfaz, record, enum).

```csharp
// ✅ Correcto (Allman)
public class FacturaService
{
    public void Procesar()
    {
        if (esValida)
        {
            // ...
        }
    }
}
```

---

## Nombrado

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, records, structs | PascalCase | `FacturaService` |
| Interfaces | PascalCase con prefijo `I` | `IFacturaRepository` |
| Métodos | PascalCase | `ObtenerFacturaPorId()` |
| Propiedades | PascalCase | `NumeroFactura` |
| Variables locales / parámetros | camelCase | `numeroFactura` |
| Campos privados | `_camelCase` | `_facturaRepository` |
| Constantes | PascalCase | `MaximoIntentos` |
| Métodos async | PascalCase + sufijo `Async` | `ObtenerFacturaAsync()` |

---

## Constructores y métodos en una sola línea

Regla general: **constructores primarios y métodos van siempre en una sola línea**, sin importar la longitud de la firma o de la lista de parámetros. No se parten en varios renglones.

La **única excepción** son las cadenas de llamadas anidadas tipo builder o LINQ (`.Where().Select().ToList()`, fluent builders, etc.), donde sí se permite y se prefiere partir en varias líneas para legibilidad.

```csharp
// ✅ Constructor primario siempre en una sola línea, sin importar cuántos parámetros
public class FacturaService(IFacturaRepository repository, IClienteRepository clienteRepository, ILogger<FacturaService> logger)
{
    // ✅ Método en una sola línea
    public async Task<Factura?> ObtenerAsync(int id) => await repository.GetByIdAsync(id);
}
```

```csharp
// ✅ Única excepción: cadenas LINQ/builder sí se parten en varias líneas
var facturasVencidas = facturas
    .Where(f => f.EstaVencida)
    .OrderBy(f => f.FechaVencimiento)
    .Select(f => f.ToDto())
    .ToList();
```

---

## Clases, records y structs

- `record` se reserva **exclusivamente para DTOs** (modelos de transferencia de datos, inmutables).
- `class` se usa para entidades, servicios y todo lo que tenga comportamiento o identidad.
- `sealed` solo se aplica cuando la clase **explícitamente no debe heredarse** (no es el default automático).

```csharp
// DTO -> record
public record FacturaDto(int Id, string Numero, decimal Total, DateOnly Fecha);

// Entidad / servicio -> class
public class FacturaService(IFacturaRepository repository)
{
}

// Clase que explícitamente no debe extenderse
public sealed class FacturaValidator
{
}
```

---

## Modificadores de acceso

Los modificadores de acceso se escriben **siempre de forma explícita**, incluso cuando coinciden con el valor por defecto del lenguaje.

```csharp
// ✅ Correcto
private readonly IFacturaRepository _repository;

internal class FacturaHelper
{
    private const int MaximoIntentos = 3;
}

// ❌ Evitar (dejar el modificador implícito)
readonly IFacturaRepository _repository;
```

---

## Constantes y campos readonly

- `const`: cuando el valor es fijo en tiempo de compilación (no cambia nunca, tipo primitivo o string).
- `readonly` / `static readonly`: para el resto de valores fijos que se calculan o asignan en tiempo de ejecución (constructor o inicialización estática).

```csharp
private const int MaximoIntentos = 3;                 // fijo en compilación
private readonly IFacturaRepository _repository;      // asignado en el constructor
private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30); // calculado en runtime
```

---

## Miembros static vs instancia

Dentro de una clase, los **miembros de instancia van primero**, y los **miembros static al final**.

```csharp
public class FacturaService(IFacturaRepository repository)
{
    // Miembros de instancia primero
    public async Task<Factura?> ObtenerAsync(int id) => await repository.GetByIdAsync(id);

    // Miembros static al final
    private static string FormatearNumero(int id) => $"FAC-{id:D6}";
}
```

---

## Propiedades y miembros expression-bodied

Expression-bodied (`=>`) se usa **solo en propiedades calculadas simples**, no como regla general para métodos.

```csharp
public class Factura
{
    public int Id { get; init; }
    public decimal Total { get; init; }

    // ✅ Propiedad calculada simple -> expression-bodied
    public bool EstaVencida => FechaVencimiento < DateOnly.FromDateTime(DateTime.Now);
}

public class FacturaService(IFacturaRepository repository)
{
    // ✅ Método en una sola línea, aunque no sea una propiedad
    public async Task<Factura?> ObtenerAsync(int id) => await repository.GetByIdAsync(id);
}
```

> Nota: expression-bodied (`=>`) no se limita a propiedades calculadas — se usa en general para mantener constructores y métodos en una sola línea. Un método con varias sentencias (como guard clauses) necesariamente usa llaves y varias líneas; ahí la regla de una sola línea no aplica porque no es una sola expresión.

---

## Uso de `var`

Se usa `var` **siempre que el tipo sea evidente** por el lado derecho de la asignación.

```csharp
var factura = new Factura();          // ✅ tipo evidente
var facturas = await repository.GetAllAsync(); // ✅ evidente por el método
decimal total = CalcularImpuesto(x, y); // tipo explícito si no es evidente a simple vista
```

---

## Namespaces y `using`

- File-scoped namespaces **siempre**.
- Los `using` van **fuera del namespace**, con `System.*` primero y el resto en orden alfabético.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Q10.Modulo.Facturacion.Repositories;

namespace Q10.Modulo.Facturacion;

public class FacturaService
{
}
```

---

## Inyección de dependencias

Siempre se inyectan **interfaces**, nunca clases concretas. Las interfaces son `public` y viven a nivel raíz del proyecto de su capa; las implementaciones son `internal` y viven en `Implementations/`, de forma que otras capas no puedan referenciarlas directamente. Cada capa registra sus propios servicios mediante una extensión de `IServiceCollection`.

```csharp
// public, a nivel raíz de Q10.Modulo.Persistence
public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(int id);
}

// internal, dentro de Implementations/
internal class FacturaRepository(FacturaDbContext context) : IFacturaRepository
{
    public async Task<Factura?> GetByIdAsync(int id) => await context.Facturas.FindAsync(id);
}

// Extensión de DI propia de la capa
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services) =>
        services.AddScoped<IFacturaRepository, FacturaRepository>();
}
```

---

## Controllers y validación

- Los endpoints de API usan **Controllers tradicionales** (`[ApiController]`), no Minimal APIs.
- La validación de datos de entrada se hace **manualmente dentro del servicio o handler**, no con Data Annotations ni FluentValidation.

```csharp
[ApiController]
[Route("api/facturas")]
public class FacturasController(IFacturaService facturaService) : ControllerBase
{
    /// <summary>Obtiene una factura por su identificador.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerAsync(int id)
    {
        var resultado = await facturaService.ObtenerAsync(id);
        return resultado.IsSuccess ? Ok(resultado.Value) : NotFound(resultado.Error);
    }
}
```

---

## Manejo de errores (Result/OneOf)

En lugar de lanzar excepciones para errores esperados (validaciones, reglas de negocio, "no encontrado"), se usa un patrón **Result/OneOf**. Las excepciones quedan reservadas para errores realmente excepcionales/no controlados.

```csharp
public async Task<Result<Factura>> ObtenerAsync(int id)
{
    var factura = await _repository.GetByIdAsync(id);

    return factura is null
        ? Result<Factura>.Failure("Factura no encontrada")
        : Result<Factura>.Success(factura);
}
```

---

## Async/Await

- No se usa `ConfigureAwait(false)` (proyectos ASP.NET Core, no librerías standalone).
- `CancellationToken` se propaga **solo cuando la operación es larga o puede cancelarse** (no en todos los métodos por sistema).

```csharp
// Operación larga / cancelable -> se propaga el token
public async Task<Factura?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
    await _repository.GetByIdAsync(id, cancellationToken);

// Operación corta -> sin CancellationToken
public async Task<bool> ExisteAsync(int id) =>
    await _repository.ExisteAsync(id);
```

---

## Logging

Se usa `ILogger` con **logging estructurado y placeholders**, nunca interpolación de strings en el mensaje de log (para preservar los datos como campos estructurados).

```csharp
// ✅ Correcto
logger.LogInformation("Factura {FacturaId} procesada para cliente {ClienteId}", factura.Id, cliente.Id);

// ❌ Evitar
logger.LogInformation($"Factura {factura.Id} procesada para cliente {cliente.Id}");
```

---

## Nulabilidad

- Nullable Reference Types **habilitado siempre** (`<Nullable>enable</Nullable>`).
- Preferir `is null` / `is not null` sobre `== null`.

```csharp
if (factura is null)
{
    return Result<Factura>.Failure("Factura no encontrada");
}
```

---

## Control de flujo: switch y pattern matching

- Se prefieren **switch expressions** sobre switch statements tradicionales, siempre que la lógica lo permita.
- Pattern matching se usa **siempre que sea posible** (`is Tipo variable`, patrones de propiedad, etc.).

```csharp
// ✅ Switch expression
public string ObtenerEstadoTexto(EstadoFactura estado) => estado switch
{
    EstadoFactura.Pendiente => "Pendiente",
    EstadoFactura.Pagada => "Pagada",
    EstadoFactura.Vencida => "Vencida",
    _ => "Desconocido"
};

// ✅ Pattern matching
if (resultado is { IsSuccess: true, Value: FacturaDto dto })
{
    return Ok(dto);
}
```

---

## Guard clauses

Las validaciones van **al inicio del método**, con guard clauses y `return` temprano, evitando anidar lógica en `if/else`.

```csharp
public async Task<Result<Factura>> ProcesarAsync(int id)
{
    if (id <= 0)
    {
        return Result<Factura>.Failure("El id debe ser mayor a cero");
    }

    var factura = await _repository.GetByIdAsync(id);
    if (factura is null)
    {
        return Result<Factura>.Failure("Factura no encontrada");
    }

    // lógica principal, sin anidar
    factura.Procesar();
    return Result<Factura>.Success(factura);
}
```

---

## Strings

Se usa `string.Format` en los casos donde aporta claridad (formatos reutilizables, plantillas), combinado con string interpolation cuando es más directo.

```csharp
var numeroFactura = string.Format("FAC-{0:D6}", id);
```

---

## Colecciones y LINQ

- Se prefiere **LINQ** (`Where`, `Select`, `FirstOrDefault`, etc.) sobre bucles `for`/`foreach` tradicionales.
- Se usan **collection expressions** (C# 12) para inicializar colecciones.

```csharp
// ✅ LINQ simple, cabe en una línea
var facturasVencidas = facturas.Where(f => f.EstaVencida).ToList();

// ✅ LINQ encadenado más largo -> se parte en varias líneas (única excepción a "una sola línea")
var resumen = facturas
    .Where(f => f.EstaVencida)
    .OrderBy(f => f.FechaVencimiento)
    .Select(f => f.ToDto())
    .ToList();

// ✅ Collection expression
int[] diasHabiles = [1, 2, 3, 4, 5];
List<string> estados = ["Pendiente", "Pagada", "Vencida"];
```

---

## Comentarios y documentación XML

- `///` (XML doc comments) se usa en:
  - **Endpoints de API**: `summary` corto y simple, una línea si es posible.
  - **Interfaces**: comentario simple describiendo el propósito del miembro.
- No se documentan implementaciones internas ni métodos privados triviales.

```csharp
public interface IFacturaRepository
{
    /// <summary>Obtiene una factura por su identificador.</summary>
    Task<Factura?> GetByIdAsync(int id);
}

[ApiController]
[Route("api/facturas")]
public class FacturasController : ControllerBase
{
    /// <summary>Obtiene una factura por su identificador.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerAsync(int id) { /* ... */ }
}
```

---

## Organización de archivos

Cada capa es una **biblioteca (proyecto) independiente** dentro de la solución, no una carpeta dentro de un único proyecto.

- **Q10.Modulo.Presentacion**: controladores y filtros de la capa web/API.
- **Q10.Modulo.Services**: interfaces `public` a nivel raíz del proyecto; implementaciones `internal` dentro de `Implementations/`.
- **Q10.Modulo.Extensions**: métodos de extensión (`this` helpers) compartidos.
- **Q10.Modulo.Persistence**: contexto de datos (EF Core u otro) e interfaces de repositorio `public` a nivel raíz, con sus implementaciones `internal` en `Implementations/`.
- **Q10.Modulo.Models**: entidades y DTOs, referenciada por el resto de proyectos.
- Cada capa expone su **propia extensión de registro de DI** (`AddXxx(this IServiceCollection services)`), para que el proyecto de arranque solo llame `services.AddServices().AddPersistence()...` sin conocer las implementaciones internas.

```
Q10.Modulo.sln
├── Q10.Modulo.Presentacion/
│   ├── Controllers/
│   ├── Filters/
│   └── Q10.Modulo.Presentacion.csproj
├── Q10.Modulo.Services/
│   ├── IFacturaService.cs                  (public, a nivel raíz)
│   ├── Implementations/
│   │   └── FacturaService.cs               (internal)
│   ├── ServiceCollectionExtensions.cs       (AddServices())
│   └── Q10.Modulo.Services.csproj
├── Q10.Modulo.Extensions/
│   └── Q10.Modulo.Extensions.csproj
├── Q10.Modulo.Persistence/
│   ├── Context/
│   ├── IFacturaRepository.cs               (public, a nivel raíz)
│   ├── Implementations/
│   │   └── FacturaRepository.cs            (internal)
│   ├── ServiceCollectionExtensions.cs       (AddPersistence())
│   └── Q10.Modulo.Persistence.csproj
└── Q10.Modulo.Models/
    ├── Entities/
    ├── Dtos/
    └── Q10.Modulo.Models.csproj
```

```csharp
// Q10.Modulo.Services/IFacturaService.cs
public interface IFacturaService
{
    Task<Result<Factura>> ObtenerAsync(int id);
}

// Q10.Modulo.Services/Implementations/FacturaService.cs
internal class FacturaService(IFacturaRepository repository) : IFacturaService
{
    public async Task<Result<Factura>> ObtenerAsync(int id) => await repository.GetByIdAsync(id);
}

// Q10.Modulo.Services/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services) =>
        services.AddScoped<IFacturaService, FacturaService>();
}
```

---

> Esta guía refleja el estilo de codificación real del autor. Es un documento vivo: se ajusta a medida que el estilo evoluciona.
