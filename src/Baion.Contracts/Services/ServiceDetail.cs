using System;
using System.Collections.Generic;
using Baion.Contracts.Enums;

namespace Baion.Contracts.Services;

/// <summary>Detalle de un servicio del sistema. Los campos que una plataforma no expone van en <c>null</c>.</summary>
/// <param name="Id">Nombre de la unidad de systemd o del servicio de Windows.</param>
/// <param name="SubState">Sub-estado de systemd (<c>running</c>, <c>dead</c>, <c>exited</c>); null en Windows.</param>
/// <param name="MainProcessId">PID del proceso principal, si el servicio está en marcha.</param>
/// <param name="ActiveSince">Momento en que el servicio entró en su estado actual.</param>
/// <param name="MemoryBytes">Memoria residente del servicio, si el sistema la reporta.</param>
/// <param name="ExecPath">Ruta de la unidad (<c>FragmentPath</c>) o del binario (<c>BinaryPathName</c>).</param>
/// <param name="ExitCode">Último código de salida conocido, si el servicio terminó.</param>
/// <param name="Dependencies">Otros servicios de los que este depende.</param>
public record ServiceDetail(
    string Id,
    string DisplayName,
    string? Description,
    ServiceRuntimeState State,
    string RawState,
    string? SubState,
    ServiceStartupMode StartupMode,
    int? MainProcessId,
    DateTimeOffset? ActiveSince,
    long? MemoryBytes,
    string? ExecPath,
    int? ExitCode,
    IReadOnlyList<string> Dependencies);
