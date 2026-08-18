# Manual de usuario — Panel REM

**REMTool · DESAM Monte Patria**  
Edición 1.0 · 11 de agosto de 2026

## 1. ¿Qué es el Panel REM?

El Panel REM es el espacio de REMTool para revisar una o varias planillas REM en un mismo período. Analiza cada archivo `.xlsm`, identifica automáticamente la serie y el establecimiento, informa inconsistencias, calcula un resumen por serie y permite exportar un informe de revisión.

El panel también permite adjuntar planillas complementarias a una planilla base de Serie A para sumar sus valores y descargar un REM consolidado.

## 2. Antes de comenzar

- Ten disponible la planilla REM en formato `.xlsm`.
- Verifica que el archivo sea una planilla REM vigente y que incluya la hoja `NOMBRE`.
- Selecciona primero el mes, el año y, si corresponde, el sector.
- Necesitas conexión con REMTool para analizar archivos y calcular el resumen.

El sistema lee desde la planilla el CodDEIS, mes, versión y serie, y los contrasta con la configuración del sistema. El establecimiento y su sector se obtienen desde el catálogo de REMTool.

## 3. Cómo ingresar

En la barra superior selecciona **Panel REM**. El panel abre con el período anterior al mes actual como selección inicial. Puedes cambiar el mes, el año y el sector en cualquier momento.

## 4. Vista general de la pantalla

1. **Filtros del período:** mes, año y sector.
2. **Zona de carga:** permite arrastrar una o varias planillas o seleccionarlas desde el equipo.
3. **Archivos procesados:** muestra el estado de cada archivo de la carga actual.
4. **Resumen del período:** presenta los puntos resumen calculados, agrupados por serie y categoría.
5. **Listado de planillas:** organiza los archivos por sector y serie.
6. **Detalle de una planilla:** se abre al seleccionar un archivo y muestra sus metadatos, validaciones, errores, advertencias y planillas complementarias.

## 5. Procedimiento recomendado

### Paso 1. Define el contexto de revisión

En **Mes**, **Año** y **Sector** selecciona el contexto que quieres revisar. Si dejas **Todos los sectores**, el panel muestra los archivos de todos los sectores disponibles para ese período.

> Recomendación: define el contexto antes de cargar archivos. Así las validaciones de mes, año y establecimiento quedan asociadas a la carga y el resumen se actualiza con mayor claridad.

### Paso 2. Carga las planillas

Arrastra una o varias planillas `.xlsm` a la zona de carga o haz clic para buscarlas. Los archivos se analizan individualmente y la aplicación detecta automáticamente la serie y el establecimiento.

Solo se aceptan archivos con extensión `.xlsm`. Si seleccionas otro tipo de archivo, la carga queda con error.

### Paso 3. Revisa el resultado de la carga

En **Archivos procesados** cada archivo puede aparecer como:

- **Pendiente:** está esperando ser procesado.
- **Analizando...:** el sistema está leyendo la planilla.
- **Revisado:** fue analizada y quedó incluida en el resumen.
- **Revisado · excluido del resumen:** fue analizada, pero no cumple alguno de los filtros o datos esperados.
- **Error:** el archivo no pudo ser procesado.

### Paso 4. Consulta el resumen del período

El resumen se recalcula automáticamente cada vez que cambia la selección o se incorpora una planilla. Los resultados se muestran por **Serie** y por **Categoría** mediante tarjetas con:

- nombre del punto resumen;
- valor total calculado;
- cantidad de planillas consideradas.

Solo se consideran las planillas marcadas como coincidentes. Las planillas excluidas siguen visibles para facilitar la revisión, pero no aportan al total.

### Paso 5. Revisa cada planilla

En **Planillas que cumplen los filtros**, abre el grupo de la serie y selecciona el archivo. Se abrirá un panel lateral con:

- establecimiento, archivo y fecha de carga;
- serie, versión, mes, año, CodDEIS y sector;
- inconsistencias con el contexto seleccionado;
- errores de revisión;
- advertencias;
- planillas complementarias asociadas.

Si no hay hallazgos, el detalle indica que la planilla fue analizada sin errores ni advertencias.

### Paso 6. Corrige y vuelve a revisar

Cuando una planilla tenga errores o inconsistencias, corrige el archivo original y vuelve a cargarlo. Si necesitas quitar una revisión, usa **Eliminar de la revisión** dentro del detalle del archivo.

### Paso 7. Adjunta y suma planillas complementarias — Serie A

Para una planilla base de **Serie A**, usa **Adjuntar / sumar** o **Adjuntar** desde el detalle. Selecciona otra planilla `.xlsm` para asociarla a la base.

Las planillas complementarias:

- se analizan antes de ser asociadas;
- quedan visibles en el detalle de la planilla base;
- suman sus valores al resumen y a la descarga consolidada;
- pueden quitarse individualmente con el ícono de eliminar.

La funcionalidad de adjuntar y descargar está habilitada inicialmente solo para Serie A.

### Paso 8. Descarga un REM consolidado — Serie A

En la fila de una planilla Serie A, selecciona **Descargar**. REMTool genera un archivo `.xlsm` consolidado utilizando la planilla base y sus complementarias.

Mientras se genera el archivo, el botón muestra **Generando...**. Si el servidor devuelve un error, el panel informa el motivo.

### Paso 9. Exporta el informe de revisión

Usa **Exportar revisión** para descargar un informe compatible con Word. El informe incluye el período, el sector seleccionado y, para cada planilla, sus inconsistencias, errores y advertencias. También incorpora los archivos que no pudieron analizarse durante la carga.

## 6. Cómo interpretar los estados

| Señal | Significado | Acción sugerida |
|---|---|---|
| Verde | Sin errores ni advertencias | Puedes continuar con el resumen. |
| Amarillo | Advertencia o exclusión del resumen | Abre el detalle y confirma si corresponde corregir o justificar. |
| Rojo | Error de revisión o de procesamiento | Corrige la planilla y vuelve a cargarla. |

## 7. Validaciones automáticas

Durante el análisis, REMTool verifica principalmente:

- extensión `.xlsm` y existencia de la hoja `NOMBRE`;
- lectura de un mes válido;
- existencia de versión configurada en el sistema;
- identificación del establecimiento por CodDEIS;
- correspondencia entre serie del encabezado y serie de la versión;
- correspondencia con el mes y año seleccionados;
- correspondencia con el establecimiento esperado, cuando aplica;
- reglas de validación configuradas para la versión;
- hojas y prestaciones habilitadas para extraer datos.

Una diferencia de serie, mes, año o establecimiento se registra como inconsistencia y deja la planilla excluida del resumen. Una regla configurada como advertencia se muestra en amarillo; una regla configurada como error se muestra en rojo.

## 8. Persistencia y limpieza de revisiones

Las revisiones quedan guardadas en el almacenamiento local del navegador utilizado. Esto permite cambiar de pantalla o volver al Panel REM sin perder las cargas del navegador actual.

Considera lo siguiente:

- las revisiones no se comparten automáticamente entre computadores, navegadores o perfiles;
- limpiar los datos del navegador puede eliminar las revisiones guardadas;
- **Limpiar revisiones** borra todas las revisiones almacenadas en ese navegador, previa confirmación;
- antes de limpiar, exporta el informe si necesitas conservar evidencia de la revisión.

## 9. Demostración breve para usuarios

Para mostrar la funcionalidad en una capacitación:

1. Entra a **Panel REM** y selecciona un mes, año y sector.
2. Carga dos o más planillas `.xlsm`.
3. Muestra el estado de procesamiento de cada archivo.
4. Abre una planilla con advertencias o exclusión y explica el panel lateral.
5. Vuelve al resumen y muestra cómo los puntos se calculan por serie.
6. En una planilla Serie A, adjunta una complementaria y descarga el REM consolidado.
7. Finaliza con **Exportar revisión** para mostrar la evidencia descargable.

## 10. Observaciones de la revisión funcional

- El Panel REM analiza y resume archivos cargados en la sesión del navegador; no es un repositorio central de planillas.
- La descarga consolidada y el adjuntar/sumar están implementados inicialmente para Serie A.
- El resumen se calcula por serie usando los puntos resumen configurados en el sistema.
- La interfaz cuenta con métricas de cobertura en la lógica del panel, pero actualmente esas tarjetas están ocultas; por eso el usuario ve principalmente los puntos resumen y el estado de cada planilla.
- El informe de revisión se descarga con extensión `.doc` compatible con Word, aunque no es un `.docx` nativo.

## 11. Soporte

Para consultas o sugerencias: **ricardocontreras@mpatria.cl**.
