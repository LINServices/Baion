using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Groups;

public class GroupDispatchTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    private const string BatchesUrl = "api/executions/batches";

    private const string RequesterEmail = "operador@baion.local";

    private readonly GroupTestEnvironment _environment = new(factory);

    [Fact]
    public async Task Lote_ConBashSobreTresLinuxYDosWindows_LanzaLosLinuxYOmiteLosWindows()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var linux1 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux2 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux3 = await _environment.ConnectAgentAsync(tenantId);
        await using var windows1 = await _environment.ConnectAgentAsync(tenantId, ServerPlatform.Windows);
        await using var windows2 = await _environment.ConnectAgentAsync(tenantId, ServerPlatform.Windows);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [linux1.ServerId, linux2.ServerId, linux3.ServerId, windows1.ServerId, windows2.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((5, 3, 2, 0, 0), (resultado.Total, resultado.Dispatched, resultado.SkippedIncompatible, resultado.SkippedDisabled, resultado.Failed));
        AssertInvariant(resultado);
        Assert.Equal(2, resultado.Targets.Count(target => target.Outcome is BatchTargetOutcome.SkippedIncompatible));

        var ejecuciones = await _environment.GetExecutionsAsync(tenantId);
        Assert.Equal(3, ejecuciones.Count);
        Assert.All(ejecuciones, ejecucion => Assert.Equal(resultado.BatchId, ejecucion.BatchId));
        Assert.Equal(new[] { linux1.ServerId, linux2.ServerId, linux3.ServerId }.Order(), ejecuciones.Select(ejecucion => ejecucion.ServerId).Order());

        var ordenes = await Task.WhenAll(linux1.NextOrderAsync(), linux2.NextOrderAsync(), linux3.NextOrderAsync());
        Assert.Equal(3, ordenes.Select(orden => orden.ExecutionId).Distinct().Count());
    }

    [Fact]
    public async Task Lote_ConPowerShellCoreSobreGrupoMixto_DespachaATodosLosMiembros()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var linux1 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux2 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux3 = await _environment.ConnectAgentAsync(tenantId);
        await using var windows1 = await _environment.ConnectAgentAsync(tenantId, ServerPlatform.Windows);
        await using var windows2 = await _environment.ConnectAgentAsync(tenantId, ServerPlatform.Windows);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [linux1.ServerId, linux2.ServerId, linux3.ServerId, windows1.ServerId, windows2.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.PowerShellCore);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((5, 5, 0, 0, 0), (resultado.Total, resultado.Dispatched, resultado.SkippedIncompatible, resultado.SkippedDisabled, resultado.Failed));
        AssertInvariant(resultado);
        Assert.Equal(5, (await _environment.GetExecutionsAsync(tenantId)).Count);
    }

    [Fact]
    public async Task Lote_ConScriptSoloWindowsSobreGrupoTodoLinux_Devuelve400SinLoteNiEjecuciones()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var linux1 = await _environment.ConnectAgentAsync(tenantId);
        await using var linux2 = await _environment.ConnectAgentAsync(tenantId);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [linux1.ServerId, linux2.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.WindowsPowerShell);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.BatchNoCompatibleServers, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Fact]
    public async Task Lote_ConTodosLosCompatiblesDeshabilitados_Devuelve400SinLoteNiEjecuciones()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var deshabilitados = await _environment.CreateServersAsync(tenantId, 2, ServerPlatform.Linux, ServerStatus.Disabled);
        var windows = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [.. deshabilitados, windows]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.BatchNoCompatibleServers, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Fact]
    public async Task Lote_ConUnCompatibleDeshabilitadoYOtrosCompatibles_CuentaElOmitidoYLanzaElResto()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var activo = await _environment.ConnectAgentAsync(tenantId);
        var deshabilitado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Linux, ServerStatus.Disabled);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [activo.ServerId, deshabilitado]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((2, 1, 0, 1, 0), (resultado.Total, resultado.Dispatched, resultado.SkippedIncompatible, resultado.SkippedDisabled, resultado.Failed));
        AssertInvariant(resultado);
        Assert.Equal(BatchTargetOutcome.SkippedDisabled, resultado.Targets.Single(target => target.ServerId == deshabilitado).Outcome);
        Assert.Equal(activo.ServerId, Assert.Single(await _environment.GetExecutionsAsync(tenantId)).ServerId);
    }

    [Fact]
    public async Task Lote_ConUnServidorIncompatibleYDeshabilitado_LoCuentaComoIncompatible()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var activo = await _environment.ConnectAgentAsync(tenantId);
        var windowsDeshabilitado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows, ServerStatus.Disabled);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [activo.ServerId, windowsDeshabilitado]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((1, 1, 0), (resultado.SkippedIncompatible, resultado.Dispatched, resultado.SkippedDisabled));
        Assert.Equal(BatchTargetOutcome.SkippedIncompatible, resultado.Targets.Single(target => target.ServerId == windowsDeshabilitado).Outcome);
    }

    [Fact]
    public async Task Lote_ConUnCompatibleSinAgenteConectado_CuentaFallidoSinFilaYLanzaElResto()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var conectado = await _environment.ConnectAgentAsync(tenantId);
        var desconectado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Linux, ServerStatus.Offline);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [conectado.ServerId, desconectado]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((2, 1, 1), (resultado.Total, resultado.Dispatched, resultado.Failed));
        AssertInvariant(resultado);

        var fallido = resultado.Targets.Single(target => target.ServerId == desconectado);
        Assert.Equal(BatchTargetOutcome.Failed, fallido.Outcome);
        Assert.Equal(GroupErrorCodes.AgentNotConnected, fallido.ErrorCode);
        Assert.Null(fallido.ExecutionId);

        var ejecuciones = await _environment.GetExecutionsAsync(tenantId);
        Assert.Equal(conectado.ServerId, Assert.Single(ejecuciones).ServerId);
    }

    [Fact]
    public async Task Lote_ConMiembrosEnCadaResultadoPosible_CumpleLaInvarianteDeContadores()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var activo = await _environment.ConnectAgentAsync(tenantId);
        var desconectado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Linux, ServerStatus.Offline);
        var deshabilitado = await _environment.CreateServerAsync(tenantId, ServerPlatform.Linux, ServerStatus.Disabled);
        var windows = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows);

        var grupoId = await _environment.CreateGroupAsync(tenantId, [activo.ServerId, desconectado, deshabilitado, windows]);
        var scriptId = await _environment.CreateScriptAsync(tenantId, ScriptRuntime.Bash);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        Assert.Equal((4, 1, 1, 1, 1), (resultado.Total, resultado.Dispatched, resultado.SkippedIncompatible, resultado.SkippedDisabled, resultado.Failed));
        AssertInvariant(resultado);

        var detalle = await GroupTestEnvironment.ReadAsync<ExecutionBatchDetail>(await cliente.GetAsync($"{BatchesUrl}/{resultado.BatchId}"));
        Assert.Equal((4, 1, 1, 1, 1), (detalle.TotalMembers, detalle.DispatchedCount, detalle.SkippedIncompatibleCount, detalle.SkippedDisabledCount, detalle.FailedCount));
        Assert.Equal(detalle.TotalMembers, detalle.DispatchedCount + detalle.SkippedIncompatibleCount + detalle.SkippedDisabledCount + detalle.FailedCount);
    }

    [Fact]
    public async Task Lote_ConGrupoSinMiembros_Devuelve400GroupEmptySinLote()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var grupoId = await _environment.CreateGroupAsync(tenantId, []);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.BatchGroupEmpty, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Fact]
    public async Task Lote_ConMasDe500Miembros_Devuelve400GroupTooLargeSinLote()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidores = await _environment.CreateServersAsync(tenantId, 501);
        var grupoId = await _environment.CreateGroupAsync(tenantId, servidores);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.BatchGroupTooLarge, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lote_ConScriptInexistenteOInactivo_Devuelve404ScriptNotFound(bool inactivo)
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var agente = await _environment.ConnectAgentAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [agente.ServerId]);
        var scriptId = inactivo ? await _environment.CreateScriptAsync(tenantId) : Guid.CreateVersion7();

        if (inactivo)
            await _environment.DeactivateScriptAsync(tenantId, scriptId);

        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.ScriptNotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Lote_ConTimeoutNoPositivo_Devuelve400TimeoutInvalid(int timeout)
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var agente = await _environment.ConnectAgentAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [agente.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var respuesta = await DespacharAsync(cliente, scriptId, grupoId, timeout);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.TimeoutInvalid, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        await AssertNothingCreatedAsync(tenantId);
    }

    [Fact]
    public async Task Lote_ConGrupoDeOtroTenantOInexistente_Devuelve404GroupNotFound()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        await using var agenteAjeno = await _environment.ConnectAgentAsync(ajeno);
        var grupoAjeno = await _environment.CreateGroupAsync(ajeno, [agenteAjeno.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(propio);
        using var cliente = _environment.CreateClient(propio, RequesterEmail, GroupTestEnvironment.ViewerRole);

        foreach (var grupoId in new[] { grupoAjeno, Guid.CreateVersion7() })
        {
            var respuesta = await DespacharAsync(cliente, scriptId, grupoId);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal(GroupErrorCodes.NotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }

        await AssertNothingCreatedAsync(ajeno);
    }

    [Fact]
    public async Task Lote_RegistraElEmailDelTokenComoSolicitante()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var agente = await _environment.ConnectAgentAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [agente.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        var detalle = await GroupTestEnvironment.ReadAsync<ExecutionBatchDetail>(await cliente.GetAsync($"{BatchesUrl}/{resultado.BatchId}"));
        Assert.Equal(RequesterEmail, detalle.RequestedBy);
        Assert.Equal(grupoId, detalle.ServerGroupId);
        Assert.Equal(Assert.Single(await _environment.GetExecutionsAsync(tenantId)).Id, Assert.Single(detalle.Executions).Id);
    }

    [Fact]
    public async Task Lote_AlTerminarSusEjecuciones_ConservaSusContadoresYLasEjecucionesSiguenSuCiclo()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var uno = await _environment.ConnectAgentAsync(tenantId);
        await using var dos = await _environment.ConnectAgentAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [uno.ServerId, dos.ServerId]);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));

        var ordenUno = await uno.NextOrderAsync();
        var ordenDos = await dos.NextOrderAsync();
        await uno.ReportCompletedAsync(ordenUno.ExecutionId, ExecutionStatus.Succeeded, 0);
        await dos.ReportCompletedAsync(ordenDos.ExecutionId, ExecutionStatus.Failed, 1);

        var detalle = await Polling.WaitForAsync(
            async () => await GroupTestEnvironment.ReadAsync<ExecutionBatchDetail>(await cliente.GetAsync($"{BatchesUrl}/{resultado.BatchId}")),
            lote => lote.Executions.Count == 2 && lote.Executions.All(ejecucion => ejecucion.Status is ExecutionStatus.Succeeded or ExecutionStatus.Failed),
            100);

        Assert.Equal((2, 2, 0, 0, 0), (detalle.TotalMembers, detalle.DispatchedCount, detalle.SkippedIncompatibleCount, detalle.SkippedDisabledCount, detalle.FailedCount));
    }

    [Fact]
    public async Task ObtenerLote_ConLoteDeOtroTenantOInexistente_Devuelve404BatchNotFound()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        await using var agenteAjeno = await _environment.ConnectAgentAsync(ajeno);
        var grupoAjeno = await _environment.CreateGroupAsync(ajeno, [agenteAjeno.ServerId]);
        var scriptAjeno = await _environment.CreateScriptAsync(ajeno);
        using var clienteAjeno = _environment.CreateClient(ajeno, RequesterEmail, GroupTestEnvironment.ViewerRole);
        var loteAjeno = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(clienteAjeno, scriptAjeno, grupoAjeno));
        using var cliente = _environment.CreateClient(propio, RequesterEmail, GroupTestEnvironment.ViewerRole);

        foreach (var loteId in new[] { loteAjeno.BatchId, Guid.CreateVersion7() })
        {
            var respuesta = await cliente.GetAsync($"{BatchesUrl}/{loteId}");

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal(GroupErrorCodes.BatchNotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }
    }

    [Fact]
    public async Task ObtenerLote_ConElGrupoEliminadoDespues_DevuelveElLoteSinGrupoYConSuNombre()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await using var agente = await _environment.ConnectAgentAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [agente.ServerId], "grupo-efimero");
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        using var cliente = _environment.CreateClient(tenantId, RequesterEmail, GroupTestEnvironment.ViewerRole);
        using var admin = _environment.CreateAdminClient(tenantId);

        var resultado = await GroupTestEnvironment.ReadAsync<DispatchGroupResult>(await DespacharAsync(cliente, scriptId, grupoId));
        var eliminado = await admin.DeleteAsync($"api/server-groups/{grupoId}");
        Assert.Equal(HttpStatusCode.NoContent, eliminado.StatusCode);

        var detalle = await GroupTestEnvironment.ReadAsync<ExecutionBatchDetail>(await cliente.GetAsync($"{BatchesUrl}/{resultado.BatchId}"));

        Assert.Null(detalle.ServerGroupId);
        Assert.Equal("grupo-efimero", detalle.ServerGroupName);
        Assert.Equal(1, detalle.DispatchedCount);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("GET")]
    public async Task Peticion_SinToken_Devuelve401(string metodo)
    {
        using var cliente = _environment.CreateAnonymousClient();
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), metodo is "GET" ? $"{BatchesUrl}/{Guid.CreateVersion7()}" : BatchesUrl);

        if (metodo is "POST")
            peticion.Content = JsonContent.Create(new DispatchGroupRequest(Guid.CreateVersion7(), Guid.CreateVersion7(), ExecutionMode.Attached, null, null, null), options: GroupTestEnvironment.Json);

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static async Task<HttpResponseMessage> DespacharAsync(HttpClient cliente, Guid scriptId, Guid grupoId, int? timeoutSeconds = null)
    {
        var request = new DispatchGroupRequest(scriptId, grupoId, ExecutionMode.Attached, timeoutSeconds, null, null);
        return await cliente.PostAsJsonAsync(BatchesUrl, request, GroupTestEnvironment.Json);
    }

    private static void AssertInvariant(DispatchGroupResult resultado)
    {
        Assert.Equal(resultado.Total, resultado.Dispatched + resultado.SkippedIncompatible + resultado.SkippedDisabled + resultado.Failed);
        Assert.Equal(resultado.Total, resultado.Targets.Count);
    }

    private async Task AssertNothingCreatedAsync(Guid tenantId)
    {
        Assert.Equal(0, await _environment.CountBatchesAsync(tenantId));
        Assert.Empty(await _environment.GetExecutionsAsync(tenantId));
    }
}
