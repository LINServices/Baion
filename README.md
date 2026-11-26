# Baion

Orquestador de servidores: replica scripts, programa tareas y recolecta métricas sobre los agentes conectados.

## Agente en Linux

Desinstalar la versión actual y volver a instalar la última release (pide el token por teclado):

```bash
bash <(curl -fsSL https://github.com/LINServices/Baion/releases/latest/download/uninstall.sh)
bash <(curl -fsSL https://github.com/LINServices/Baion/releases/latest/download/reinstall.sh)
```

`reinstall.sh` ya limpia la instalación anterior por su cuenta; ejecutar `uninstall.sh` antes es opcional.
Variables: `VERSION` (por defecto `latest`; p. ej. `v0.4` para fijar una), `ORCHESTRATOR` y `BAION_TOKEN` (si se define, no pregunta el token).

Cada release publica `uninstall.sh` y `reinstall.sh` (de `deploy/linux/`) como assets, junto a `baion-agent-linux-x64.zip` (lo hace `.github/workflows/release-agent.yml`).
