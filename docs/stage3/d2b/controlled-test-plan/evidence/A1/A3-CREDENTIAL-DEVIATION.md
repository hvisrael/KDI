# Desviación declarada — credenciales de los logins de aplicación

Fecha: 2026-10-21 (sesión local). Autoridad: Human Orchestrator. Gate: **D2-B, OPEN/BLOCKED**.

## Decisión

**No se rotan las credenciales** de `kdi_migrator`, `kdi_app` y `kdi_test_runner`.

Decisión expresa del Human Orchestrator, registrada aquí como desviación declarada y no como
limitación pendiente. La rotación se Preparing de forma disponible (`ROTATE-MIGRATOR-PASSWORD.sql`)
y no se ejecuta por decisión.

## Qué ocurrió

Durante la preparación de A3, el Human Orchestrator creó en la raíz del workspace el fichero
`sql.txt` con las tres credenciales, para tenerlas a mano al teclear los prompts de `sqlcmd`.

Consecuencias medidas:

1. **El inventario congelado de D2-A se rompió.** `tools/d2a-manifest.py` inventaría los
   archivos sueltos de la raíz; `sql.txt` pasó a ser una 122.ª entrada frente a las 121 del
   manifiesto, y `verify.py` devolvió `AssertionError: Candidate drift`, exit 1.
   **Resuelto** moviendo el fichero fuera del workspace. `verify.py` vuelve a dar exit 0.
2. **Las credenciales quedaron fuera de la invariante del proyecto.** D2-A las capturó en
   diálogos protegidos y **nunca las guardó en ningún archivo**; por eso `kdi_migrator` no está
   almacenado en ninguna parte. La invariante «las credenciales solo existen en la consola de
   quien las teclea» **deja de valer para esta instancia**.
3. **Las credenciales quedaron en la conversación y en el almacén de sesión.**
   `C:\Users\Israel\.local\share\opencode\opencode.db` persiste la sesión, y el contenido del
   fichero se leyó en esta conversación. Es decir: las tres credenciales están en un archivo de
   texto plano fuera del workspace, en el almacén local de la herramienta y en el historial de la
   conversación.

## Comprobaciones realizadas

| Comprobación | Resultado |
|---|---|
| El workspace contiene credenciales | **No** — búsqueda en todo el árbol: ninguna coincidencia |
| Los 25 ficheros de evidencia contienen el patrón de la contraseña | **No** — el log de A3 capturó solo el texto `Password: `; `sqlcmd` lee la contraseña de la consola y no la vuelca al log |
| `verify.py` de D2-A tras retirar el fichero | **exit 0** |
| `freeze.py --verify` | **exit 0** |
| Tablas D2-B en la base | **0** — la migración fallida revirtió por completo |

## Riesgo residual aceptado

- Las tres credenciales de `KulaniKDI_Test` y `KulaniKDI_Dev` deben considerarse **expuestas**:
  un atacante con lectura del fichero, del almacén de sesión o del historial de la conversación
  podría autenticar como `kdi_migrator` (que puede ejecutar DDL en los esquemas), `kdi_app` o
  `kdi_test_runner`.
- El alcance del daño está acotado a un servidor de desarrollo local con **datos sintéticos**,
  sin información de negocio y sin exposición a redes externas. La instancia `MSSQLSERVER` es
  local (`DESKTOP-F25O3MF`) y no se ha configurado como servicio accesible en red.
- **A3 se aplicará con una credencial que ya se considera expuesta.** El artefacto resultante
  (las 8 tablas de D2-B) queda, por tanto, gestionado por un login cuya credencial está
  comprometida. Esto es una consecuencia directa y aceptada de la decisión.
- La mitigación disponible y no aplicada es la rotación. El script está preparado, con guarda de
  marcador, la contraseña como parámetro de `sp_executesql` y verificación de política sin
  exponer el secreto: [ROTATE-MIGRATOR-PASSWORD.sql](../../ROTATE-MIGRATOR-PASSWORD.sql).

## Límites declarados

- **No se ha rotado ninguna credencial**, ni se ha ejecutado ningún `ALTER LOGIN`.
- No se ha copiado, registrado ni reproducido ninguna credencial en la documentación. Este
  documento no contiene el secreto, ni su longitud, ni su forma.
- No se ha ejecutado ninguna operación SQL fuera de: A1 (solo lectura), A2 (backup), y las
  pruebas reversibles de opciones SET described en el resultado de A3.
- No se ha aplicado ninguna migración: el intento falló con `Msg 1934` y revirtió por completo.
