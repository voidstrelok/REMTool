const $ = selector => document.querySelector(selector);
const state = { lista: [], detalle: null, vista: "dashboard" };

function mostrarVista(nombre) {
  state.vista = nombre;
  $("#vista-dashboard").hidden = nombre !== "dashboard";
  $("#vista-detalle").hidden = nombre !== "detalle";
  $("#vista-items").hidden = nombre !== "items";
  $("#vista-gastos").hidden = nombre !== "gastos";
  window.scrollTo({ top: 0, behavior: "instant" });
}

function toast(mensaje) {
  const el = $("#toast"); el.textContent = mensaje; el.hidden = false;
  clearTimeout(toast.timer); toast.timer = setTimeout(() => el.hidden = true, 2800);
}

function payload(form) {
  const data = Object.fromEntries(new FormData(form));
  ["presupuestoAsignado","monto","anio"].forEach(k => { if (k in data) data[k] = Number(data[k]); });
  if (data.itemPresupuestarioId === "") data.itemPresupuestarioId = null;
  else if (data.itemPresupuestarioId) data.itemPresupuestarioId = Number(data.itemPresupuestarioId);
  if (data.subItemPresupuestarioId === "") data.subItemPresupuestarioId = null;
  else if (data.subItemPresupuestarioId) data.subItemPresupuestarioId = Number(data.subItemPresupuestarioId);
  return data;
}

function abrirModal(titulo, campos, guardar) {
  const modal = $("#modal"), form = $("#formulario"), boton = $("#guardar"), error = $("#form-error");
  $("#modal-titulo").textContent = titulo; $("#campos").innerHTML = campos; error.hidden = true;
  const cerrar = () => { form.reset(); modal.close("cancel"); };
  $("#cancelar-modal").onclick = cerrar; $("#cerrar-modal").onclick = cerrar;
  form.onsubmit = async event => {
    event.preventDefault(); if (!form.reportValidity()) return;
    boton.disabled = true; boton.textContent = "Guardando…"; error.hidden = true;
    try { await guardar(payload(form)); modal.close("saved"); await refrescarVista(); toast("Cambios guardados correctamente."); }
    catch (e) { error.textContent = e.message; error.hidden = false; }
    finally { boton.disabled = false; boton.textContent = "Guardar"; }
  };
  modal.showModal();
}

async function refrescarVista() {
  await cargarDashboard();
  if (state.detalle) {
    const id = state.detalle.id; state.detalle = await api(`convenios-financieros/${id}`);
    if (state.vista === "items") renderItems(); else renderDetalle();
  }
}

async function cargarDashboard() {
  const query = new URLSearchParams();
  [["anio","f-anio"],["estado","f-estado"],["responsable","f-responsable"],["nombre","f-nombre"]].forEach(([key,id]) => { const value = $(`#${id}`).value.trim(); if (value) query.set(key,value); });
  state.lista = await api(`convenios-financieros?${query}`);
  const total = state.lista.reduce((a,x) => ({ presupuesto:a.presupuesto+x.presupuesto, ejecutado:a.ejecutado+x.ejecutado, disponible:a.disponible+x.disponible }), {presupuesto:0,ejecutado:0,disponible:0});
  $("#totales").innerHTML = [
    ["Presupuesto total",total.presupuesto,"kpi-blue"],["Ejecutado",total.ejecutado,"kpi-amber"],["Disponible",total.disponible,"kpi-green"]
  ].map(([label,value,cls]) => `<article class="kpi ${cls}"><span>${label}</span><strong>${clp(value)}</strong></article>`).join("");
  $("#cantidad-convenios").textContent = `${state.lista.length} convenio${state.lista.length === 1 ? "" : "s"} en esta vista`;
  $("#vacio").hidden = state.lista.length > 0;
  $("#lista").innerHTML = state.lista.map(c => `<tr><td><button class="table-link" data-convenio="${c.id}">${escapeHtml(c.nombre)}</button><small>${escapeHtml(c.codigo || `Año ${c.anio}`)}</small></td><td>${escapeHtml(c.responsable)}<small><span class="status status-${c.estado.toLowerCase()}">${escapeHtml(c.estado)}</span></small></td><td>${clp(c.presupuesto)}</td><td>${clp(c.ejecutado)}</td><td class="money-positive">${clp(c.disponible)}</td><td><div class="progress-cell"><span>${pct(c.porcentajeEjecucion)}</span><div class="progress"><i style="width:${Math.min(100,c.porcentajeEjecucion)}%"></i></div></div></td><td><button class="icon-button row-action" data-convenio="${c.id}" aria-label="Ver detalle">›</button></td></tr>`).join("");
  document.querySelectorAll("[data-convenio]").forEach(b => b.onclick = () => abrirDetalle(b.dataset.convenio));
}

async function abrirDetalle(id) { state.detalle = await api(`convenios-financieros/${id}`); renderDetalle(); mostrarVista("detalle"); }

function renderDetalle() {
  const d=state.detalle,r=d.resumen;
  $("#vista-detalle").innerHTML = `<header class="page-header detail-header"><div><button id="volver-dashboard" class="back-button">← Volver al dashboard</button><p class="eyebrow">Convenio ${d.anio}</p><h1>${escapeHtml(d.nombre)}</h1><p class="lead">${escapeHtml(d.responsable)} · <span class="status status-${d.estado.toLowerCase()}">${escapeHtml(d.estado)}</span></p></div><div class="header-actions"><button id="eliminar-convenio" class="button button-danger">Eliminar convenio</button><button id="editar-convenio" class="button">Editar convenio</button><button id="gestionar-items" class="button">Gestionar ítems</button><button id="nuevo-movimiento" class="button button-primary">+ Registrar gasto</button></div></header>
  <section class="kpi-grid detail-kpis">${[["Presupuesto",r.presupuesto,"kpi-blue"],["Ejecutado",r.ejecutado,"kpi-amber"],["Disponible",r.disponible,"kpi-green"]].map(x=>`<article class="kpi ${x[2]}"><span>${x[0]}</span><strong>${clp(x[1])}</strong></article>`).join("")}<article class="kpi"><span>Ejecución</span><strong>${pct(r.porcentajeEjecucion)}</strong><div class="progress"><i style="width:${Math.min(100,r.porcentajeEjecucion)}%"></i></div></article></section>
  <section class="surface"><header class="surface-header"><div><h2>Movimientos</h2><p>${d.movimientos.length} registro${d.movimientos.length===1?"":"s"} financiero${d.movimientos.length===1?"":"s"}</p></div></header><div class="table-wrap"><table class="data-table detail-table"><thead><tr><th>Fecha</th><th>Descripción</th><th>Ítem / Subítem</th><th>Documento</th><th>Proveedor</th><th>Monto</th><th></th></tr></thead><tbody>${d.movimientos.map(m=>`<tr><td class="nowrap">${fechaCl(m.fecha)}</td><td class="main-cell">${escapeHtml(m.descripcion)}</td><td><span class="tag">${escapeHtml(`${m.itemNombre||"—"} / ${m.subItemNombre||"Sin subítem"}`)}</span></td><td>${escapeHtml([m.tipoDocumento,m.numeroDocumento].filter(Boolean).join(" ")||"—")}</td><td>${escapeHtml(m.proveedor||"—")}</td><td class="amount">${clp(m.monto)}</td><td class="actions"><button class="icon-button" data-edit-mov="${m.id}" title="Editar">✎</button><button class="icon-button danger" data-delete-mov="${m.id}" title="Eliminar">×</button></td></tr>`).join("")}</tbody></table></div>${d.movimientos.length?"":`<div class="empty-state"><strong>Aún no hay movimientos</strong><span>Registra el primer gasto de este convenio.</span></div>`}</section>`;
  $("#volver-dashboard").onclick=()=>{state.detalle=null;mostrarVista("dashboard")};
  $("#eliminar-convenio").onclick=()=>eliminarConvenio(); $("#editar-convenio").onclick=()=>formConvenio(d); $("#gestionar-items").onclick=()=>{renderItems();mostrarVista("items")}; $("#nuevo-movimiento").onclick=()=>formMovimiento();
  document.querySelectorAll("[data-edit-mov]").forEach(b=>b.onclick=()=>formMovimiento(d.movimientos.find(m=>m.id==b.dataset.editMov)));
  document.querySelectorAll("[data-delete-mov]").forEach(b=>b.onclick=()=>eliminarMovimiento(b.dataset.deleteMov));
}

function renderItems() {
  const d=state.detalle;
  $("#vista-items").innerHTML=`<header class="page-header"><div><button id="volver-detalle" class="back-button">← Volver al convenio</button><p class="eyebrow">Distribución presupuestaria</p><h1>Ítems de ${escapeHtml(d.nombre)}</h1><p class="lead">El presupuesto de cada ítem corresponde a la suma de sus subítems.</p></div><button id="nuevo-item" class="button button-primary">+ Nuevo ítem</button></header>${d.items.map(i=>`<section class="surface item-group"><header class="surface-header"><div><h2>${escapeHtml(i.nombre)}</h2><p>${clp(i.ejecutado)} ejecutado de ${clp(i.presupuestoAsignado)} · Disponible ${clp(i.disponible)}</p></div><div class="header-actions"><span class="status status-${i.activo?"activo":"inactivo"}">${i.activo?"Activo":"Inactivo"}</span><button class="button button-small" data-new-subitem="${i.id}">+ Subítem</button><button class="button button-small" data-edit-item="${i.id}">Editar ítem</button><button class="button button-small button-danger" data-delete-item="${i.id}">Eliminar</button></div></header><div class="table-wrap"><table class="data-table"><thead><tr><th>Subítem</th><th>Presupuesto</th><th>Ejecutado</th><th>Disponible</th><th>Estado</th><th></th></tr></thead><tbody>${i.subItems.map(s=>`<tr><td class="main-cell">${escapeHtml(s.nombre)}</td><td class="amount">${clp(s.presupuestoAsignado)}</td><td class="amount">${clp(s.ejecutado)}</td><td class="money-positive">${clp(s.disponible)}</td><td><span class="status status-${s.activo?"activo":"inactivo"}">${s.activo?"Activo":"Inactivo"}</span></td><td class="actions"><button class="button button-small" data-edit-subitem="${s.id}" data-item="${i.id}">Editar</button><button class="button button-small button-danger" data-delete-subitem="${s.id}" data-item="${i.id}">Eliminar</button></td></tr>`).join("")}</tbody></table></div>${i.subItems.length?"":`<div class="empty-state"><strong>Sin subítems</strong><span>Crea un subítem y asigna su presupuesto para habilitar gastos en este ítem.</span></div>`}</section>`).join("")}${d.items.length?"":`<section class="surface"><div class="empty-state"><strong>Sin ítems presupuestarios</strong><span>Crea el primer ítem y luego agrega sus subítems con presupuesto.</span></div></section>`}`;
  $("#volver-detalle").onclick=()=>{renderDetalle();mostrarVista("detalle")}; $("#nuevo-item").onclick=()=>formItem();
  document.querySelectorAll("[data-edit-item]").forEach(b=>b.onclick=()=>formItem(d.items.find(i=>i.id==b.dataset.editItem)));
  document.querySelectorAll("[data-delete-item]").forEach(b=>b.onclick=()=>eliminarItem(Number(b.dataset.deleteItem)));
  document.querySelectorAll("[data-new-subitem]").forEach(b=>b.onclick=()=>formSubItem(Number(b.dataset.newSubitem)));
  document.querySelectorAll("[data-edit-subitem]").forEach(b=>{const item=d.items.find(i=>i.id==b.dataset.item);b.onclick=()=>formSubItem(item.id,item.subItems.find(s=>s.id==b.dataset.editSubitem))});
  document.querySelectorAll("[data-delete-subitem]").forEach(b=>b.onclick=()=>eliminarSubItem(Number(b.dataset.item),Number(b.dataset.deleteSubitem)));
}

function formConvenio(d=null) { abrirModal(d?"Editar convenio":"Nuevo convenio",convenioCampos(d||{}),p=>api(`convenios-financieros${d?`/${d.id}`:""}`,{method:d?"PUT":"POST",body:JSON.stringify(p)})); }
function formItem(i=null) { abrirModal(i?"Editar ítem":"Nuevo ítem",input("nombre","Nombre","text",i?.nombre,true)+select("activo","Estado",[{value:"true",label:"Activo"},{value:"false",label:"Inactivo"}],String(i?.activo??true)),p=>{p.activo=p.activo==="true";return api(`convenios-financieros/${state.detalle.id}/items${i?`/${i.id}`:""}`,{method:i?"PUT":"POST",body:JSON.stringify(p)})}); }
function formSubItem(itemId,s=null) { abrirModal(s?"Editar subítem":"Nuevo subítem",input("nombre","Nombre","text",s?.nombre,true)+input("presupuestoAsignado","Presupuesto asignado","number",s?.presupuestoAsignado,true,'min="0.01" step="0.01"')+select("activo","Estado",[{value:"true",label:"Activo"},{value:"false",label:"Inactivo"}],String(s?.activo??true)),p=>{p.activo=p.activo==="true";return api(`convenios-financieros/${state.detalle.id}/items/${itemId}/subitems${s?`/${s.id}`:""}`,{method:s?"PUT":"POST",body:JSON.stringify(p)})}); }
async function formMovimiento(m=null) { const proveedores=await api("convenios-financieros/proveedores"); abrirModal(m?"Editar gasto":"Registrar gasto",movimientoCampos(m||{},state.detalle.items,proveedores),p=>api(m?`convenios-financieros/movimientos/${m.id}`:`convenios-financieros/${state.detalle.id}/movimientos`,{method:m?"PUT":"POST",body:JSON.stringify(p)})); }
async function eliminarMovimiento(id) { if(!confirm("¿Eliminar este movimiento? Esta acción no se puede deshacer."))return;try{await api(`convenios-financieros/movimientos/${id}`,{method:"DELETE"});await refrescarVista();toast("Movimiento eliminado.")}catch(e){alert(e.message)} }
async function eliminarConvenio() { if(!confirm(`¿Eliminar el convenio "${state.detalle.nombre}"? Esta acción no se puede deshacer.`))return;try{await api(`convenios-financieros/${state.detalle.id}`,{method:"DELETE"});state.detalle=null;await cargarDashboard();mostrarVista("dashboard");toast("Convenio eliminado.")}catch(e){alert(e.message)} }
async function eliminarItem(id) { const item=state.detalle.items.find(x=>x.id===id);if(!confirm(`¿Eliminar el ítem "${item.nombre}"? Esta acción no se puede deshacer.`))return;try{await api(`convenios-financieros/${state.detalle.id}/items/${id}`,{method:"DELETE"});await refrescarVista();toast("Ítem eliminado.")}catch(e){alert(e.message)} }
async function eliminarSubItem(itemId,subItemId) { const item=state.detalle.items.find(x=>x.id===itemId),sub=item.subItems.find(x=>x.id===subItemId);if(!confirm(`¿Eliminar el subítem "${sub.nombre}"? Esta acción no se puede deshacer.`))return;try{await api(`convenios-financieros/${state.detalle.id}/items/${itemId}/subitems/${subItemId}`,{method:"DELETE"});await refrescarVista();toast("Subítem eliminado.")}catch(e){alert(e.message)} }

async function abrirDashboardGastos() {
  mostrarVista("gastos");
  const convenios=await api("convenios-financieros");
  const options=convenios.map(c=>`<option value="${c.id}">${escapeHtml(c.nombre)}</option>`).join("");
  const anioInicial=$("#f-anio").value || new Date().getFullYear();
  $("#vista-gastos").innerHTML=`<header class="page-header"><div><button id="volver-gastos" class="back-button">← Volver a convenios</button><p class="eyebrow">Análisis de ejecución</p><h1>Dashboard de gastos</h1><p class="lead">Consulta cuánto se ha gastado por período y convenio.</p></div></header><form id="filtros-gastos" class="filter-bar dashboard-filters"><label>Año<input name="anio" type="number" value="${anioInicial}" min="2000" max="2100" required></label><label>Mes<select name="mes"><option value="">Todos</option>${["Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"].map((m,i)=>`<option value="${i+1}">${m}</option>`).join("")}</select></label><label>Convenio<select name="convenioId"><option value="">Todos</option>${options}</select></label><button class="button button-primary">Actualizar</button></form><div id="dashboard-resultado"></div>`;
  $("#volver-gastos").onclick=()=>mostrarVista("dashboard"); $("#filtros-gastos").onsubmit=e=>{e.preventDefault();cargarDashboardGastos(new FormData(e.currentTarget))};
  await cargarDashboardGastos(new FormData($("#filtros-gastos")));
}
async function cargarDashboardGastos(data) {
  const q=new URLSearchParams();for(const [k,v] of data)if(v)q.set(k,v);const d=await api(`convenios-financieros/dashboard?${q}`),max=Math.max(1,...d.convenios.map(x=>x.ejecutado));
  $("#dashboard-resultado").innerHTML=`<section class="kpi-grid"><article class="kpi kpi-amber"><span>Gasto del período</span><strong>${clp(d.ejecutado)}</strong></article><article class="kpi kpi-blue"><span>Movimientos</span><strong>${d.cantidadMovimientos}</strong></article><article class="kpi"><span>Convenios con gasto</span><strong>${d.convenios.length}</strong></article></section><section class="surface"><header class="surface-header"><div><h2>Gasto por convenio</h2><p>Comparación para el período seleccionado</p></div></header><div class="spend-list">${d.convenios.map(x=>`<article><div><strong>${escapeHtml(x.convenio)}</strong><span>${x.cantidadMovimientos} movimiento${x.cantidadMovimientos===1?"":"s"}</span></div><div class="spend-value"><strong>${clp(x.ejecutado)}</strong><div class="progress"><i style="width:${x.ejecutado/max*100}%"></i></div></div></article>`).join("")}</div>${d.convenios.length?"":`<div class="empty-state"><strong>Sin gastos</strong><span>No hay movimientos en el período seleccionado.</span></div>`}</section>`;
}

$("#filtros").onsubmit=e=>{e.preventDefault();cargarDashboard().catch(x=>alert(x.message))};
$("#nuevo-convenio").onclick=()=>formConvenio();
$("#ver-dashboard-gastos").onclick=()=>abrirDashboardGastos();
$("#f-anio").value=new Date().getFullYear();
cargarDashboard().catch(e=>alert(e.message));
