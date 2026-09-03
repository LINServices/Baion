#!/usr/bin/env bash
# Instala el agente de Baion como unidad de systemd.
#
#   sudo ./install.sh --orchestrator wss://baion.example.com --token <token-de-instalacion> [--source ./publish]
set -euo pipefail

INSTALL_DIR=/opt/baion-agent
STATE_DIR=/var/lib/baion-agent
UNIT_NAME=baion-agent.service
SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/publish"
ORCHESTRATOR=""
TOKEN=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --orchestrator) ORCHESTRATOR="$2"; shift 2 ;;
    --token)        TOKEN="$2";        shift 2 ;;
    --source)       SOURCE_DIR="$2";   shift 2 ;;
    *) echo "Opción desconocida: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$ORCHESTRATOR" ]]; then
  echo "Falta --orchestrator (por ejemplo wss://baion.example.com)" >&2
  exit 1
fi

if [[ $EUID -ne 0 ]]; then
  echo "Este script necesita privilegios de root." >&2
  exit 1
fi

if [[ ! -x "$SOURCE_DIR/Baion.Agent.Host" ]]; then
  echo "No se encontró el binario publicado en $SOURCE_DIR" >&2
  echo "Publícalo antes con: dotnet publish src/Agent/Baion.Agent.Host -c Release -r linux-x64 -o $SOURCE_DIR" >&2
  exit 1
fi

echo "==> Deteniendo el servicio si ya estaba instalado"
systemctl stop "$UNIT_NAME" 2>/dev/null || true

echo "==> Copiando binarios a $INSTALL_DIR"
install -d -m 0755 "$INSTALL_DIR"
cp -a "$SOURCE_DIR"/. "$INSTALL_DIR"/
chmod 0755 "$INSTALL_DIR/Baion.Agent.Host"

echo "==> Preparando $STATE_DIR"
install -d -m 0700 "$STATE_DIR"

# El token de instalación solo hace falta hasta el primer enrolamiento: después el agente
# usa la credencial permanente que guarda en el directorio de estado.
echo "==> Escribiendo configuración"
cat > "$INSTALL_DIR/appsettings.Production.json" <<JSON
{
  "Agent": {
    "OrchestratorUri": "$ORCHESTRATOR",
    "EnrollmentToken": "$TOKEN",
    "StateDirectory": "$STATE_DIR"
  }
}
JSON
chmod 0600 "$INSTALL_DIR/appsettings.Production.json"

echo "==> Instalando la unidad de systemd"
install -m 0644 "$(dirname "${BASH_SOURCE[0]}")/$UNIT_NAME" "/etc/systemd/system/$UNIT_NAME"
systemctl daemon-reload
systemctl enable "$UNIT_NAME"
systemctl restart "$UNIT_NAME"

echo "==> Listo. Estado del servicio:"
systemctl --no-pager status "$UNIT_NAME" || true
