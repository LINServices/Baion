# Fuentes del panel

El sistema de diseño usa **Gilroy** como única familia. Es una tipografía comercial: no está en
Google Fonts ni en ningún CDN público y **no puede versionarse en el repositorio** sin la licencia
correspondiente, así que los archivos hay que dejarlos aquí a mano.

Archivos que espera `Styles/app.css` (las rutas están declaradas en sus `@font-face`):

| Archivo | Peso | Para qué |
|---|---|---|
| `gilroy-regular.woff2` | 400 | Texto corrido, etiquetas |
| `gilroy-medium.woff2` | 500 | Títulos, cifras, controles — es el peso dominante |
| `gilroy-semibold.woff2` | 600 | Solo las cifras dentro de una frase de insight |

Los demás pesos que trae Gilroy (Thin a Heavy) no se usan: en producción solo hay 400 y 500, más el
600 puntual. Añadir más rompe la jerarquía del sistema.

## Mientras no estén

La cadena de la familia es `"Gilroy", "Gilroy Fallback", ui-sans-serif, system-ui, sans-serif`.
`Gilroy Fallback` es Arial con las métricas ya ajustadas (`size-adjust`, `ascent-override`…), así que
el panel se ve correcto y **el layout no salta** cuando la fuente real aparezca. Lo que se pierde sin
los `.woff2` son las formas geométricas y los numerales anchos de Gilroy, que es lo que sostiene la
jerarquía de los datos grandes; el espaciado y los tamaños son los mismos.

Al copiar los archivos no hay que tocar nada más: se sirven como estáticos y `font-display: swap`
los aplica en cuanto cargan.
