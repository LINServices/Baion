#!/usr/bin/env bash
# Desinstala Baion Agent (servicio, binarios, configuración y estado).
#
#   ./uninstall.sh
#   ./uninstall.sh --keep-config
#   ./uninstall.sh --keep-state
set -euo pipefail

UNIT_NAME="baion-agent.service"
INSTALL_DIR="/opt/baion-agent"
CONFIG_DIR="/etc/baion-agent"
STATE_DIR="/var/lib/baion-agent"

KEEP_CONFIG=0
KEEP_STATE=0

for arg in "$@"; do
  case "$arg" in
    --keep-config) KEEP_CONFIG=1 ;;
    --keep-state)  KEEP_STATE=1 ;;
    *) echo "Argumento desconocido: $arg" >&2; echo "Uso: $0 [--keep-config] [--keep-state]" >&2; exit 1 ;;
  esac
done

echo "==> Baion Agent - Desinstalación"

if ! sudo -v; then
  echo "No se pudieron obtener privilegios de sudo." >&2
  exit 1
fi

echo "==> Deteniendo y eliminando el servicio"
sudo systemctl disable --now "$UNIT_NAME" 2>/dev/null || true
sudo rm -f "/etc/systemd/system/$UNIT_NAME"
sudo systemctl daemon-reload
sudo systemctl reset-failed "$UNIT_NAME" 2>/dev/null || true

echo "==> Eliminando instalación: $INSTALL_DIR"
sudo rm -rf "$INSTALL_DIR"

if [[ "$KEEP_CONFIG" -eq 0 ]]; then
  echo "==> Eliminando configuración: $CONFIG_DIR"
  sudo rm -rf "$CONFIG_DIR"
else
  echo "==> Conservando configuración: $CONFIG_DIR"
fi

if [[ "$KEEP_STATE" -eq 0 ]]; then
  echo "==> Eliminando estado y credenciales: $STATE_DIR"
  sudo rm -rf "$STATE_DIR"
else
  echo "==> Conservando estado: $STATE_DIR"
fi

echo "==> Baion Agent desinstalado."
