# Plan del generador local de consolidados REM

Fecha: 9 de septiembre de 2026. Estado: propuesta de implementación.

## Resultado esperado

Generar en la aplicación Windows `tools` un Excel interactivo por serie y año, con todos los establecimientos y meses disponibles. El usuario sube los archivos terminados al servidor y el frontend ofrece su descarga. Los filtros de establecimiento, mes y sector se utilizan dentro del Excel descargado.

Se excluyen la generación por corte, la generación en la API, las consultas a la base por descarga y la subida automática. La primera implementación funcional será Serie A; BM, D y P se incorporarán después de validar sus plantillas y reglas particulares.

## Arquitectura

```text
Base de datos configurada en tools
    → lectura de registros vigentes y estructura
    → normalización y compactación local
    → plantilla Excel local
    → datos internos, tabla dinámica, segmentadores y fórmulas
    → validación y guardado local
    → subida manual del usuario
    → carpeta persistente servida por nginx
    → enlace de descarga en el frontend
```

La creación del libro, las agregaciones y el cálculo se ejecutan en el equipo que corre `tools`. Si la conexión apunta a PostgreSQL en el servidor, esa lectura sigue consumiendo recursos allí: se limita a la serie/año solicitado, se proyectan únicamente campos necesarios y las series se procesan secuencialmente. No es necesario crear vistas materializadas ni servicios de cálculo para esta función.

## Pantalla en tools

Agregar un botón «Generar consolidados» en `Main`, que abre un formulario dedicado.

Entradas:

- Año, obligatorio y explícito.
- Series a generar, limitadas a las que tengan una plantilla validada.
- Plantilla configurada por serie/año y versión de salida; selector de archivo cuando aún no exista configuración.
- Carpeta de salida, recordada para siguientes ejecuciones.

La pantalla muestra meses encontrados, establecimientos con registros, versiones detectadas y compatibilidad con la plantilla. No ofrece filtros de establecimiento, sector ni rango de meses para generar variantes. Siempre incluye todos los datos disponibles del año y serie elegidos.

Acciones: «Validar configuración», «Generar», «Cancelar» y «Abrir carpeta». Mostrar progreso por etapa, serie en curso, filas leídas/exportadas, tiempo y resultado. Evitar congelar WinForms: trabajo asíncrono con un DbContext propio, cancelación y recuperación del estado de los controles en `finally`. No compartir el DbContext del formulario principal entre hilos.

## Preparación de datos

1. Consultar por `Reporte.Año` y serie real, no por fecha de versión ni por año del computador.
2. Seleccionar una carga vigente por establecimiento, comuna cuando corresponda, año, mes y serie. Como compatibilidad inicial, usar el reporte de mayor ID entre los que contienen esa serie, igual que la consulta actual; documentar que esto significa última carga, no aprobación clínica/administrativa.
3. Resolver recargas antes de sumar o excluir ceros. No sumar dos cargas que se sustituyen. Si una carga contiene varias versiones contradictorias, emitir un conflicto explícito.
4. Leer sin seguimiento de EF, con proyecciones limitadas y sin cargar árboles completos de entidades. Obtener datos y metadatos de una misma instantánea consistente, cerrar la lectura antes de generar Excel y evitar consultas por prestación o celda.
5. Leer el arreglo numérico directamente. No convertirlo a texto ni dividirlo por comas.
6. Mapear código y columna de origen a la versión de salida usando estructura, `CodigoColumna` y coordenadas. La igualdad de posiciones o códigos entre versiones no basta para demostrar equivalencia semántica. Conservar ceros iniciales de los códigos.
7. Mantener un registro de equivalencias por plantilla cuando sea necesario. Rechazar longitudes incompatibles, columnas sin correspondencia y valores con datos que no se puedan ubicar; no truncar arreglos ni rellenar cambios de estructura silenciosamente.
8. Preparar una fila por establecimiento, mes y prestación normalizada, con columnas de valores numéricos. Conservar las dimensiones necesarias para que Excel pueda filtrar después. La normalización no debe sumar meses ni establecimientos antes de exportar.
9. Omitir filas cuyos valores sean todos cero, conservando por separado catálogo y evidencia de cobertura. No ocultar valores negativos, decimales o errores de conversión mediante esta regla.

La copia de Serie A revisada contenía 388.864 filas A, de las cuales 14.267 tenían algún valor distinto de cero. Esta cifra justifica la compactación, pero no es un objetivo fijo ni representa necesariamente la base actual.

### Cobertura y reportes completamente en cero

El extractor actual no guarda un reporte cuando todo el archivo tiene valores cero. Además, la serie del reporte se deduce de sus registros. Con ese modelo, la ausencia de registros no demuestra que faltó entregar el REM ni permite reconocer todas las entregas en cero.

La primera versión mostrará «Con registros disponibles» y «Sin evidencia en la base», sin inventar estados de entrega. Para distinguir formalmente entregado en cero de no entregado, se requiere una mejora acotada del registro de cargas: serie/versión y estado de procesamiento persistidos incluso cuando no existen registros de actividad. Revisar también la sustitución por una carga totalmente en cero, que actualmente retorna antes del commit y conserva la carga anterior.

Este punto se resuelve explícitamente antes de ofrecer estados de entrega. La generación inicial puede funcionar con la evidencia disponible y esa limitación visible.

## Plantilla y libro generado

Preparar una plantilla limpia derivada del consolidado del usuario, conservando hojas REM, códigos, formatos, áreas de impresión, totales, validaciones y fórmulas necesarias. La preparación se hace una vez por versión; no importar la antigua caché de cientos de miles de filas en cada ejecución.

Estructura del archivo:

- `FILTRO`: controles de establecimiento, mes y sector, tabla dinámica y metadatos de generación.
- Hojas REM: presentación existente y BUSCARV conservados inicialmente.
- `DATOS`: tabla interna compacta con las dimensiones y valores necesarios; oculta para facilitar el uso, sin considerarla una medida de control de acceso.
- `COBERTURA`: evidencia disponible por establecimiento/mes, versiones y observaciones de compatibilidad.

Decisiones de construcción:

- Un único origen interno y una única caché compartida para los segmentadores de actividad.
- Crear la tabla dinámica y sus segmentadores contra `DATOS`; no conservar vínculos a `BaseConsolidado2026.xlsx`, ODBC ni consultas externas.
- Mantener el contrato que esperan los BUSCARV: código en la primera columna de la tabla dinámica, orden explícito de campos numéricos y cabecera auxiliar compatible. Evitar desplazamientos al regenerar FILTRO.
- Dimensionar los rangos según el catálogo de salida, eliminando los límites arbitrarios como 9.999 filas y las referencias a un millón de filas.
- Guardar valores como números, incluidas las columnas 46–48. El número y significado de columnas se definen por plantilla, no mediante un supuesto global de 48 para todas las series.
- Mostrar meses en orden cronológico. Selección inicial explícita y reproducible de todos los datos disponibles para Serie A.
- Establecimientos o meses sin actividad deben seguir siendo visibles en cobertura. Si se requiere que aparezcan también en los segmentadores de actividad, comprobar una solución compatible con la caché durante el prototipo, sin multiplicar ceros por todas las prestaciones ni inventar códigos.
- Si no existe actividad para la serie/año, mostrar el estado en tools y no reemplazar una publicación anterior por un libro aparentemente normal lleno de ceros.
- Conservar las fórmulas de totales; no sumar otra vez totales ya incluidos como detalle. Registrar los errores heredados de plantilla y resolver los que impidan certificar el resultado.
- Guardar datos de generación, serie, año, versión de plantilla y meses disponibles. Los filtros permiten explorar una instantánea; actualizar la BD requiere regenerar y volver a publicar el archivo.

### Prueba técnica obligatoria antes de la interfaz completa

Usar EPPlus, ya presente en `tools` (8.1.0), y Open XML para inspección/validación estructural cuando corresponda. La documentación de EPPlus contempla pivotes y segmentadores, pero no demuestra que esta plantilla concreta quede correcta con la versión instalada.

El prototipo debe escribir datos internos, reconstruir la caché y los segmentadores, preparar resultados iniciales y comprobar BUSCARV. Distinguir el cálculo interno de la tabla dinámica de la escritura de su representación en celdas: un `Calculate()` exitoso no prueba que FILTRO haya quedado actualizado para BUSCARV.

Abrir, filtrar, guardar y reabrir en Excel de escritorio. Verificar ausencia de reparación, vínculos externos y resultados antiguos. No sustituir el comportamiento interactivo por valores fijos si la biblioteca presenta una limitación. Si hace falta, evaluar primero un ajuste puntual de versión; como alternativa explícita, una finalización con Excel instalado en el equipo local. La elección se cierra al terminar el prototipo.

## Publicación manual y frontend

El frontend actual se exporta estáticamente y se sirve con nginx. Agregar una carpeta persistente del host montada como solo lectura en el contenedor del frontend, separada de la imagen y del build.

Salida propuesta:

```text
consolidados/
  catalogo.json
  2026/
    A/
      consolidado-rem-a-2026-<id-generacion>.xlsx
```

`catalogo.json` registra serie, año, nombre de archivo, fecha de generación, meses disponibles, versión de plantilla, tamaño y SHA-256. Es metadata de publicaciones, sin datos de actividad ni rutas locales. El ID de generación permite conservar la publicación anterior durante una sustitución.

Tools administra el catálogo de una carpeta de publicación local elegida por el usuario y preserva sus otras entradas. Actualiza una entrada solo cuando el Excel correspondiente está terminado y validado. El catálogo se deriva de archivos presentes en esa carpeta, sin sobrescribir a ciegas un catálogo remoto que no conoce. Para trabajar desde otro equipo se debe sincronizar primero esa carpeta/catálogo.

Procedimiento: generar y revisar localmente, subir el nuevo `.xlsx`, verificar que está completo y reemplazar `catalogo.json` al final mediante un renombrado atómico. El usuario realiza la subida. No hacen falta un endpoint de carga ni credenciales de publicación en tools.

Cambios web:

- Añadir un acceso «Consolidados Excel» en la navegación existente.
- Página ligera que lee el catálogo estático en tiempo de ejecución y muestra enlaces, año, serie, fecha y meses disponibles. Esto permite publicar archivos sin reconstruir el frontend.
- nginx atiende `/consolidados/` directamente, con 404 real para archivos inexistentes y sin redirigir al `index.html` de Next.js.
- Catálogo con revalidación de caché; archivos con nombres únicos por generación. Descarga directa mediante enlace, sin cargar el Excel entero en memoria JavaScript.
- El despliegue debe preservar la carpeta entre reconstrucciones/reinicios. La ruta queda disponible para la misma audiencia autorizada a descargar el consolidado completo; no se introducen controles de acceso nuevos sin un requisito específico.

## Componentes propuestos

| Componente | Responsabilidad |
| --- | --- |
| `tools/GenerarConsolidadosForm.cs` y Designer | Configuración, progreso, cancelación y resultados |
| `tools/Consolidados/ConsolidadoDataReader.cs` | Lectura consistente y selección de cargas |
| `tools/Consolidados/ConsolidadoMapper.cs` | Compatibilidad de versiones y compactación |
| `tools/Consolidados/ConsolidadoExcelWriter.cs` | Plantilla, tabla interna, pivote, segmentadores y cálculo |
| `tools/Consolidados/ConsolidadoValidator.cs` | Reconciliación y validación del archivo guardado |
| `tools/Consolidados/ConsolidadoGenerationService.cs` | Orquestación, cancelación y publicación local atómica |
| `tools/Consolidados/ConsolidadoCatalogWriter.cs` | Catálogo local de descargas |
| `frontend/src/app/Consolidados/page.tsx` | Listado y enlaces desde catálogo estático |
| `frontend/nginx.conf` y compose | Ruta estática y volumen persistente |

Reutilizar entidades de `shared`. No llamar controladores HTTP desde tools ni concentrar el generador completo en `Main.cs`. Los archivos de implementación de esta tabla son propuestas, aún no creados.

## Secuencia de implementación

1. **Prototipo Serie A:** preparar copia de plantilla, eliminar dependencia externa y demostrar filtrado/reapertura con un conjunto pequeño conocido. Resolver la materialización de FILTRO y compatibilidad de EPPlus.
2. **Lectura y reglas:** consultas, cargas vigentes, equivalencias de versiones, cobertura y tipos numéricos. Pruebas independientes de las sumas y correspondencias.
3. **Generador completo Serie A:** libro compacto, cálculo, validación, archivos temporales y sustitución solo después de éxito.
4. **Integración WinForms:** formulario, configuración persistente, ejecución en segundo plano, progreso, cancelación y generación secuencial.
5. **Descarga estática:** catálogo, volumen nginx, página y documentación de subida manual. Una publicación fallida no debe retirar el archivo anterior.
6. **Otras series:** auditar BM/D/P e incorporar sus plantillas. Para D, comprobar precisión numérica desde el origen; el generador no puede recuperar decimales ya descartados en la extracción. Para P, definir la selección/agregación de períodos antes de habilitarla: no asumir que las existencias o poblaciones se suman entre meses. No se ofrecerán series sin estas reglas verificadas.

## Criterios de aceptación

- Un Excel de Serie A/año que contiene todos los datos disponibles de su ámbito y funciona sin conexión a la BD o al libro base.
- Selección individual y múltiple de establecimientos/meses, sector, limpieza de filtros y combinaciones sin actividad coherentes entre segmentadores, pivote y hojas REM.
- Igualdad con sumas independientes por prestación/columna para casos conocidos, incluidas columnas 46–48, ceros y código con cero inicial.
- Recarga duplicada, cambio de versión, código sin correspondencia y longitud de arreglo incompatible cubiertos por pruebas significativas.
- Diferenciar ausencia de evidencia de un cero observado. No afirmar cobertura completa solo porque una suma sea cero.
- Primera apertura correcta, sin actualización externa ni reparación; filtros funcionales después de guardar y reabrir en Excel.
- Medir lectura, compactación, escritura, cálculo, memoria máxima, tamaño del archivo y tiempo de filtrado. Comparar con el consolidado anterior sin prometer de antemano una duración.
- Cancelar o fallar no deja un archivo final incompleto ni reemplaza el catálogo vigente.
- Descargas no ejecutan consultas a PostgreSQL ni generación en la API. Archivo inexistente devuelve 404 y reemplazos de catálogo no quedan ocultos por la caché.
- Subir una nueva generación no requiere recompilar frontend; los archivos sobreviven a un reinicio/recreación de contenedores.

## Referencias técnicas consultadas

- [EPPlus: tablas dinámicas](https://github.com/EPPlusSoftware/EPPlus/wiki/Pivot-Tables).
- [EPPlus: cálculo de tablas dinámicas](https://github.com/EPPlusSoftware/EPPlus/wiki/Calculating-Pivot-tables).
- [EPPlus: filtros y segmentadores](https://github.com/EPPlusSoftware/EPPlus/wiki/Pivot-table-filters-and-slicers).
- Código local revisado: `tools/Main.cs`, `tools/Program.cs`, `tools/Utils.Rem.cs`, `tools/RemTools.csproj`, entidades de `shared`, `frontend/Dockerfile`, `frontend/nginx.conf`, navegación y `docker-compose.prod.yml`.

Este documento planifica la herramienta. No implementa el generador ni modifica los Excel de referencia.
