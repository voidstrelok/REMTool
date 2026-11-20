import { notFound } from "next/navigation";
import { ClipboardCheck } from "lucide-react";
import Breadcrumbs from "../../../components/Breadcrumbs";
import InternoGate from "../../InternoGate";
import EvaluadorApp from "../EvaluadorApp";
import { EVALUADORES, evaluador } from "../defs";

export const dynamicParams = false;
export const generateStaticParams = () => EVALUADORES.map(e => ({ id: e.id }));

export default async function EvaluadorPage({ params }: { params: Promise<{ id: string }> }) {
  const ev = evaluador((await params).id);
  if (!ev) notFound();
  return (
    <div className="mx-auto max-w-5xl py-4">
      <Breadcrumbs items={[{ label: "Panel interno", href: "/Interno" }, { label: ev.titulo }]} />
      <InternoGate>
        <div className="page-intro flex items-start gap-3">
          <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><ClipboardCheck size={21} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
          <div><h1 className="text-3xl font-bold">{ev.titulo}</h1><p>{ev.descripcion} Las evaluaciones se guardan solo en este navegador: descargue respaldos.</p></div>
        </div>
        <EvaluadorApp id={ev.id} />
      </InternoGate>
    </div>
  );
}
