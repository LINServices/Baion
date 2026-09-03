<#
.SYNOPSIS
    Instala el agente de Baion como servicio de Windows.

.EXAMPLE
    .\Install-BaionAgent.ps1 -Orchestrator wss://baion.example.com -Token <token-de-instalacion>

.NOTES
    Publica antes el binario:
    dotnet publish src/Agent/Baion.Agent.Host -c Release -r win-x64 -o .\publish
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Orchestrator,
    [Parameter(Mandatory = $false)][string] $Token = '',
    [Parameter(Mandatory = $false)][string] $Source = (Join-Path $PSScriptRoot 'publish'),
    [Parameter(Mandatory = $false)][string] $InstallDir = 'C:\Program Files\Baion\Agent'
)

$ErrorActionPreference = 'Stop'

$ServiceName = 'BaionAgent'
$DisplayName = 'Baion Agent'
$StateDir = Join-Path $env:ProgramData 'Baion\Agent'
$Executable = Join-Path $InstallDir 'Baion.Agent.Host.exe'

$identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Este script necesita ejecutarse como administrador.'
}

if (-not (Test-Path (Join-Path $Source 'Baion.Agent.Host.exe'))) {
    throw "No se encontró el binario publicado en $Source"
}

Write-Host '==> Deteniendo el servicio si ya estaba instalado'
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    if ($existing.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force
        $existing.WaitForStatus('Stopped', '00:00:30')
    }
}

Write-Host "==> Copiando binarios a $InstallDir"
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path (Join-Path $Source '*') -Destination $InstallDir -Recurse -Force

Write-Host "==> Preparando $StateDir"
New-Item -ItemType Directory -Force -Path $StateDir | Out-Null

# Solo SYSTEM y los administradores pueden leer el estado: contiene la credencial del agente.
icacls $StateDir /inheritance:r /grant 'SYSTEM:(OI)(CI)F' /grant 'Administrators:(OI)(CI)F' | Out-Null

# El token de instalación solo hace falta hasta el primer enrolamiento: después el agente
# usa la credencial permanente que guarda en el directorio de estado.
Write-Host '==> Escribiendo configuración'
$settings = [ordered]@{
    Agent = [ordered]@{
        OrchestratorUri = $Orchestrator
        EnrollmentToken = $Token
        StateDirectory  = $StateDir
    }
}
$settingsPath = Join-Path $InstallDir 'appsettings.Production.json'
$settings | ConvertTo-Json -Depth 5 | Out-File -FilePath $settingsPath -Encoding utf8
icacls $settingsPath /inheritance:r /grant 'SYSTEM:F' /grant 'Administrators:F' | Out-Null

if ($null -eq $existing) {
    Write-Host '==> Registrando el servicio'
    New-Service -Name $ServiceName -BinaryPathName "`"$Executable`"" -DisplayName $DisplayName -StartupType Automatic -Description 'Agente de Baion: ejecuta scripts y reporta métricas al orquestador.' | Out-Null
}
else {
    Write-Host '==> Actualizando la ruta del servicio existente'
    sc.exe config $ServiceName binPath= "`"$Executable`"" start= auto | Out-Null
}

# El agente reconecta por su cuenta, pero si el proceso muere Windows lo levanta igualmente.
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

[Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Production', 'Machine')

Write-Host '==> Arrancando el servicio'
Start-Service -Name $ServiceName

Get-Service -Name $ServiceName | Format-List Name, DisplayName, Status, StartType
