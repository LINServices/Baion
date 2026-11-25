using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Groups;

public class ServerGroupCrudTests(OrchestratorFactory factory) : IClassFixture<OrchestratorFactory>
{
    private const string GroupsUrl = "api/server-groups";

    private readonly GroupTestEnvironment _environment = new(factory);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_ConNombreVacioOSoloEspacios_Devuelve400NameRequired(string nombre)
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest(nombre, null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.NameRequired, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
    }

    [Fact]
    public async Task Crear_ConNombreDe201Caracteres_Devuelve400NameTooLong()
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest(new string('a', 201), null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.NameTooLong, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
    }

    [Fact]
    public async Task Crear_ConDescripcionDe1001Caracteres_Devuelve400DescriptionTooLong()
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("web", new string('d', 1001), null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.DescriptionTooLong, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
    }

    [Fact]
    public async Task Crear_ConNombreEnElLimiteDe200Caracteres_Devuelve200ConElNombreRecortado()
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest($"  {new string('a', 200)}  ", null, null), GroupTestEnvironment.Json);

        var grupo = await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(respuesta);
        Assert.Equal(new string('a', 200), grupo.Name);
    }

    [Theory]
    [InlineData("produccion", "produccion")]
    [InlineData("produccion", "PRODUCCION")]
    public async Task Crear_ConNombreDuplicadoEnElTenant_Devuelve409NameTaken(string primero, string segundo)
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        var inicial = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest(primero, null, null), GroupTestEnvironment.Json);
        Assert.True(inicial.IsSuccessStatusCode);

        var duplicado = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest(segundo, null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal(GroupErrorCodes.NameTaken, await GroupTestEnvironment.ErrorCodeAsync(duplicado));
    }

    [Fact]
    public async Task Crear_ConElNombreDeOtroTenant_Devuelve200SinConflicto()
    {
        var primero = await _environment.CreateTenantAsync();
        var segundo = await _environment.CreateTenantAsync();
        using var clientePrimero = _environment.CreateAdminClient(primero);
        using var clienteSegundo = _environment.CreateAdminClient(segundo);

        var inicial = await clientePrimero.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("compartido", null, null), GroupTestEnvironment.Json);
        Assert.True(inicial.IsSuccessStatusCode);

        var respuesta = await clienteSegundo.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("compartido", null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_ConUnServidorInexistente_Devuelve404YNoCreaNada()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var existente = await _environment.CreateServerAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("fantasma", null, [existente, Guid.CreateVersion7()]), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.ServerNotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        Assert.Empty(await ListarAsync(cliente));
    }

    [Fact]
    public async Task Crear_ConUnServidorDeOtroTenant_Devuelve404YNoCreaNada()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var servidorAjeno = await _environment.CreateServerAsync(ajeno);
        using var cliente = _environment.CreateAdminClient(propio);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("intruso", null, [servidorAjeno]), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.ServerNotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        Assert.Empty(await ListarAsync(cliente));
    }

    [Fact]
    public async Task Actualizar_ConUnServidorInexistenteOAjeno_Devuelve404YNoCambiaNada()
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var servidorPropio = await _environment.CreateServerAsync(propio);
        var servidorAjeno = await _environment.CreateServerAsync(ajeno);
        var grupoId = await _environment.CreateGroupAsync(propio, [servidorPropio], "original");
        using var cliente = _environment.CreateAdminClient(propio);

        var respuesta = await cliente.PutAsJsonAsync($"{GroupsUrl}/{grupoId}", new UpdateServerGroupRequest("renombrado", null, [servidorAjeno]), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.ServerNotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));

        var intacto = await ObtenerAsync(cliente, grupoId);
        Assert.Equal("original", intacto.Name);
        Assert.Equal(servidorPropio, Assert.Single(intacto.Members).ServerId);
    }

    [Fact]
    public async Task Crear_ConIdsRepetidos_DeduplicaLosMiembros()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("repetidos", null, [servidorId, servidorId, servidorId]), GroupTestEnvironment.Json);

        var grupo = await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(respuesta);
        Assert.Equal(servidorId, Assert.Single(grupo.Members).ServerId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Crear_ConServerIdsNuloOVacio_CreaUnGrupoSinMiembros(bool vacio)
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateAdminClient(tenantId);

        IReadOnlyList<Guid>? ids = vacio ? [] : null;
        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("sin-miembros", null, ids), GroupTestEnvironment.Json);

        var grupo = await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(respuesta);
        Assert.Empty(grupo.Members);
    }

    [Fact]
    public async Task Actualizar_ConServerIdsNuloORepetidos_VaciaODeduplicaLosMiembros()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var uno = await _environment.CreateServerAsync(tenantId);
        var dos = await _environment.CreateServerAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [uno, dos]);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var deduplicado = await cliente.PutAsJsonAsync($"{GroupsUrl}/{grupoId}", new UpdateServerGroupRequest("g", null, [uno, uno]), GroupTestEnvironment.Json);
        Assert.Equal(uno, Assert.Single((await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(deduplicado)).Members).ServerId);

        var vaciado = await cliente.PutAsJsonAsync($"{GroupsUrl}/{grupoId}", new UpdateServerGroupRequest("g", null, null), GroupTestEnvironment.Json);
        Assert.Empty((await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(vaciado)).Members);
    }

    [Fact]
    public async Task Crear_SinRolAdmin_Devuelve403()
    {
        var tenantId = await _environment.CreateTenantAsync();
        using var cliente = _environment.CreateClient(tenantId, "lector@baion.local", GroupTestEnvironment.ViewerRole);

        var respuesta = await cliente.PostAsJsonAsync(GroupsUrl, new CreateServerGroupRequest("prohibido", null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Actualizar_SinRolAdmin_Devuelve403()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var grupoId = await _environment.CreateGroupAsync(tenantId, []);
        using var cliente = _environment.CreateClient(tenantId, "lector@baion.local", GroupTestEnvironment.ViewerRole);

        var respuesta = await cliente.PutAsJsonAsync($"{GroupsUrl}/{grupoId}", new UpdateServerGroupRequest("prohibido", null, null), GroupTestEnvironment.Json);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_SinRolAdmin_Devuelve403()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var grupoId = await _environment.CreateGroupAsync(tenantId, []);
        using var cliente = _environment.CreateClient(tenantId, "lector@baion.local", GroupTestEnvironment.ViewerRole);

        var respuesta = await cliente.DeleteAsync($"{GroupsUrl}/{grupoId}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.NotNull(await ObtenerGrupoDeBaseAsync(tenantId, grupoId));
    }

    [Fact]
    public async Task Listar_ConUsuarioSinRolAdmin_Devuelve200()
    {
        var tenantId = await _environment.CreateTenantAsync();
        await _environment.CreateGroupAsync(tenantId, []);
        using var cliente = _environment.CreateClient(tenantId, "lector@baion.local", GroupTestEnvironment.ViewerRole);

        var respuesta = await cliente.GetAsync(GroupsUrl);

        Assert.Single(await GroupTestEnvironment.ReadAsync<List<ServerGroupSummary>>(respuesta));
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/{id}")]
    [InlineData("POST", "")]
    [InlineData("PUT", "/{id}")]
    [InlineData("DELETE", "/{id}")]
    public async Task Peticion_SinToken_Devuelve401(string metodo, string sufijo)
    {
        using var cliente = _environment.CreateAnonymousClient();
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), GroupsUrl + sufijo.Replace("{id}", Guid.CreateVersion7().ToString()));

        if (metodo is "POST" or "PUT")
            peticion.Content = JsonContent.Create(new CreateServerGroupRequest("anonimo", null, null), options: GroupTestEnvironment.Json);

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Peticion_ConGrupoDeOtroTenantOInexistente_Devuelve404GroupNotFound(string metodo)
    {
        var propio = await _environment.CreateTenantAsync();
        var ajeno = await _environment.CreateTenantAsync();
        var grupoAjeno = await _environment.CreateGroupAsync(ajeno, []);
        using var cliente = _environment.CreateAdminClient(propio);

        foreach (var grupoId in new[] { grupoAjeno, Guid.CreateVersion7() })
        {
            using var peticion = new HttpRequestMessage(new HttpMethod(metodo), $"{GroupsUrl}/{grupoId}");

            if (metodo is "PUT")
                peticion.Content = JsonContent.Create(new UpdateServerGroupRequest("x", null, null), options: GroupTestEnvironment.Json);

            var respuesta = await cliente.SendAsync(peticion);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal(GroupErrorCodes.NotFound, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        }

        Assert.NotNull(await ObtenerGrupoDeBaseAsync(ajeno, grupoAjeno));
    }

    [Fact]
    public async Task Actualizar_QuitandoYAnadiendoMiembros_DejaMembersIgualAServerIdsYConservaLosNoTocados()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var conservado = await _environment.CreateServerAsync(tenantId);
        var quitado = await _environment.CreateServerAsync(tenantId);
        var anadido = await _environment.CreateServerAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [conservado, quitado]);
        var membresiasPrevias = await ObtenerMembresiasAsync(tenantId, grupoId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.PutAsJsonAsync($"{GroupsUrl}/{grupoId}", new UpdateServerGroupRequest("actualizado", "nueva descripción", [conservado, anadido]), GroupTestEnvironment.Json);

        var grupo = await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(respuesta);
        Assert.Equal("actualizado", grupo.Name);
        Assert.Equal(new[] { conservado, anadido }.Order(), grupo.Members.Select(m => m.ServerId).Order());

        var membresias = await ObtenerMembresiasAsync(tenantId, grupoId);
        Assert.Equal(membresiasPrevias[conservado], membresias[conservado]);
        Assert.DoesNotContain(quitado, membresias.Keys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Eliminar_ConTareaProgramadaActivaOPausada_Devuelve409YConservaElGrupo(bool pausada)
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId]);
        var tarea = await _environment.CreateTaskAsync(tenantId, scriptId, serverGroupId: grupoId);

        if (pausada)
            await PausarTareaAsync(tenantId, tarea.Id);

        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.DeleteAsync($"{GroupsUrl}/{grupoId}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(GroupErrorCodes.InUseByScheduledTasks, await GroupTestEnvironment.ErrorCodeAsync(respuesta));
        Assert.Equal(servidorId, Assert.Single((await ObtenerAsync(cliente, grupoId)).Members).ServerId);
    }

    [Fact]
    public async Task Eliminar_ConLotesPreviosYSinTareas_Devuelve204YDejaLosLotesSinGrupo()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId);
        var scriptId = await _environment.CreateScriptAsync(tenantId);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId]);
        var loteId = await _environment.CreateBatchAsync(tenantId, grupoId, scriptId);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var respuesta = await cliente.DeleteAsync($"{GroupsUrl}/{grupoId}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();
        var lote = await context.ExecutionBatches.AsNoTracking().SingleAsync(batch => batch.Id == loteId);

        Assert.Null(lote.ServerGroupId);
        Assert.Equal("lote-previo", lote.ServerGroupName);
        Assert.True(await context.Servers.AnyAsync(server => server.Id == servidorId));
        Assert.Empty(await context.ServerGroupMembers.ToListAsync());
    }

    [Fact]
    public async Task Listar_ConGruposMixtos_DevuelveElRepartoPorPlataforma()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var linux = await _environment.CreateServersAsync(tenantId, 2, ServerPlatform.Linux);
        var windows = await _environment.CreateServersAsync(tenantId, 1, ServerPlatform.Windows);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [.. linux, .. windows]);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var grupos = await ListarAsync(cliente);

        var resumen = Assert.Single(grupos);
        Assert.Equal(grupoId, resumen.Id);
        Assert.Equal((3, 2, 1), (resumen.MemberCount, resumen.LinuxCount, resumen.WindowsCount));
    }

    [Fact]
    public async Task Obtener_ConGrupoPropio_DevuelveLosMiembrosConSuPlataformaYEstado()
    {
        var tenantId = await _environment.CreateTenantAsync();
        var servidorId = await _environment.CreateServerAsync(tenantId, ServerPlatform.Windows, ServerStatus.Disabled);
        var grupoId = await _environment.CreateGroupAsync(tenantId, [servidorId]);
        using var cliente = _environment.CreateAdminClient(tenantId);

        var grupo = await ObtenerAsync(cliente, grupoId);

        var miembro = Assert.Single(grupo.Members);
        Assert.Equal((servidorId, ServerPlatform.Windows, ServerStatus.Disabled), (miembro.ServerId, miembro.Platform, miembro.Status));
    }

    private static async Task<IReadOnlyList<ServerGroupSummary>> ListarAsync(HttpClient cliente)
    {
        return await GroupTestEnvironment.ReadAsync<List<ServerGroupSummary>>(await cliente.GetAsync(GroupsUrl));
    }

    private static async Task<ServerGroupDetail> ObtenerAsync(HttpClient cliente, Guid grupoId)
    {
        return await GroupTestEnvironment.ReadAsync<ServerGroupDetail>(await cliente.GetAsync($"{GroupsUrl}/{grupoId}"));
    }

    private async Task<ServerGroup?> ObtenerGrupoDeBaseAsync(Guid tenantId, Guid grupoId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ServerGroups.AsNoTracking().SingleOrDefaultAsync(group => group.Id == grupoId);
    }

    // Id de cada pertenencia por servidor, para comprobar que las no tocadas siguen siendo la misma fila.
    private async Task<Dictionary<Guid, Guid>> ObtenerMembresiasAsync(Guid tenantId, Guid grupoId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var membresias = await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ServerGroupMembers.AsNoTracking().Where(member => member.ServerGroupId == grupoId).ToListAsync();

        return membresias.ToDictionary(member => member.ServerId, member => member.Id);
    }

    private async Task PausarTareaAsync(Guid tenantId, Guid taskId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var pausada = await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().DisableAsync(taskId, CancellationToken.None);

        Assert.True(pausada.IsSuccess, pausada.Error?.Message);
    }
}
