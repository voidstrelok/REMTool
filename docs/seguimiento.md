# Seguimiento de indicadores y convenios

`SeguimientoService` carga los registros y aplica filtros de establecimiento, sector y período. `EvaluadorSeguimiento` produce una única evaluación que consumen las rutas JSON y `SeguimientoPdf`. Las pantallas no recalculan indicadores, estados ni agregados.

## Contrato y compatibilidad

- Las rutas `getIndicadores/{ano}/{tipo}`, su variante `/filtrado`, `getIndicador/{id}`, `getConvenios/{ano}` y `getConvenioIndicadores/{id}/{ano}` aceptan `mesCorte` (1–12), `sectorId` y `establecimientoId`. Cero en los filtros territoriales equivale a todos.
- Los listados conservan su respuesta de arreglo. `incluirResumen=true` devuelve un objeto con `items`, resumen del contexto, `mesCorte` y `criterios`. Convenios devuelve el promedio general en `cumplimiento`.
- Los campos anteriores mantienen su escala: `actual` es la razón numerador/denominador; `meta` es fracción para porcentajes. `avance` es razón de cumplimiento, limitada por indicador en listados y sin límite en el detalle. Los agregados limitan cada aporte a 100%.
- `evaluacion` distingue `resultado`, `meta`, `esperado` y `brecha` en unidades de presentación: porcentaje o tasa. `cumplimiento` siempre es porcentaje. La brecha porcentual se expresa en puntos porcentuales.
- Sin registros, `resultado` y `cumplimiento` son nulos (`SinDatos`). Registros con denominador no positivo son `SinDenominador`; meta no positiva, `SinMeta`. Numerador cero con denominador válido es un resultado cero evaluable.
- `mesCorte` explícito se respeta aunque el último registro sea anterior. Sin corte se usa el último mes disponible del conjunto consultado (enero si no hay registros). Los enlaces fijan el corte efectivo para que listado, detalle y PDF compartan contexto. `ultimoMesDatos` permite distinguir antigüedad de los datos.
- En semestrales, julio–noviembre evalúan el acumulado hasta junio y diciembre evalúa hasta diciembre. Antes de junio la evaluación es provisional. La matriz secundaria muestra registros mensuales, separados de la evolución acumulada.
- El denominador octubre–septiembre excluye octubre–diciembre del año actual e incorpora octubre–diciembre del anterior. El denominador colaborativo se toma una sola vez en cada período, no se suma por establecimiento. Los registros de serie P se incluyen según las reglas de denominador fijo/comunal.
- Metas sanitarias mantiene el ajuste de 6,25 puntos del agregado asociado al primer indicador según orden. No se redistribuyen pesos de indicadores sin evaluación. Convenios conserva el promedio simple, incluidos convenios sin indicadores.

## Respaldo

Desplegar la API antes del nuevo frontend (o ambos juntos): el frontend nuevo requiere el objeto `evaluacion` y los resúmenes adicionales. No hay cambios de esquema ni migraciones de base de datos en esta implementación.

Las rutas `informeMensual/{tipo}/{ano}` y `detalleIndicador/{id}` conservan su función. `informeConvenios/{ano}` agrega el resumen de convenios y sus indicadores, con detalles deduplicados por ID. Los informes incluyen corte, contexto y fecha de generación.

El informe de indicadores admite `buscar`, `estado` y `orden` (`brecha` o `cumplimiento`) como la tabla. El resumen conserva el contexto completo y las filas/detalles reflejan la búsqueda. Las brechas se ordenan por unidad (porcentajes, luego tasas). Los estilos del módulo están aislados bajo `.sg-page`.

## Verificación

```powershell
dotnet run --project tests/Seguimiento.Tests/Seguimiento.Tests.csproj --no-launch-profile
dotnet run --project tests/Seguimiento.Tests/Seguimiento.Tests.csproj --no-launch-profile -- --pdf
cd frontend
node --test tests/seguimiento.test.mjs tests/seguimiento-render.test.mjs
npm run build
```

La opción `--pdf` genera PDF y JSON sintéticos en `tests/Seguimiento.Tests/tmp/seguimiento-qa`. `--serve` inicia una API de prueba en `http://localhost:62910/api/`; usa exclusivamente datos sintéticos y no inicia la API principal, no conecta con la base de datos ni ejecuta migraciones. Permite revisar las cuatro pantallas con `NEXT_PUBLIC_API=http://localhost:62910/api/` al iniciar el frontend de desarrollo.

Para verificación manual: probar escritorio y móvil, teclado, búsqueda y filtros, ida/vuelta convenio–indicador conservando contexto, semestrales en marzo/junio/julio/diciembre, recuperación de errores y comparación con el PDF descargado.
