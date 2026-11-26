#!/usr/bin/env bash
# Limpia la instalación actual de Baion Agent e instala de nuevo.
# Pide el token de instalación por teclado (no queda en el historial del shell).
#
#   ./reinstall.sh
#   bash <(curl -fsSL https://github.com/LINServices/Baion/releases/latest/download/reinstall.sh)
#   VERSION=v0.4 ORCHESTRATOR=wss://api.linplatform.com/baion ./reinstall.sh
#
# Por defecto usa la última release de GitHub; VERSION fija una concreta.
#
# Si el token ya viene en BAION_TOKEN, no lo pregunta.
set -euo pipefail

VERSION="${VERSION:-latest}"
ORCHESTRATOR="${ORCHESTRATOR:-wss://api.linplatform.com/baion}"
ZIP_NAME="baion-agent-linux-x64.zip"
REPO_URL="https://github.com/LINServices/Baion/releases"
if [[ "$VERSION" == "latest" ]]; then
  BASE_URL="$REPO_URL/latest/download"
else
  BASE_URL="$REPO_URL/download/$VERSION"
fi
URL="$BASE_URL/$ZIP_NAME"
WORK_DIR="$HOME/baion"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

TOKEN="${BAION_TOKEN:-}"
if [[ -z "$TOKEN" ]]; then
  read -r -s -p "Token de instalación: " TOKEN
  echo
fi
if [[ -z "$TOKEN" ]]; then
  echo "El token no puede estar vacío." >&2
  exit 1
fi

echo "==> Dependencias"
sudo apt update
sudo apt install -y unzip wget

echo "==> Limpiando instalación actual"
UNINSTALL="$SCRIPT_DIR/uninstall.sh"
if [[ ! -f "$UNINSTALL" ]]; then
  # Ejecutado sin el repositorio (p. ej. con curl): se baja de la release.
  UNINSTALL="$(mktemp)"
  wget -q -O "$UNINSTALL" "$BASE_URL/uninstall.sh"
fi
bash "$UNINSTALL"

echo "==> Descargando release: $VERSION"
rm -rf "$WORK_DIR" && mkdir -p "$WORK_DIR"
cd "$WORK_DIR"
wget -q --show-progress "$URL"
unzip -q "$ZIP_NAME"
chmod +x install.sh

echo "==> Instalando"
sudo ./install.sh --orchestrator "$ORCHESTRATOR" --token "$TOKEN"

echo "==> Listo. Estado: systemctl status baion-agent"
