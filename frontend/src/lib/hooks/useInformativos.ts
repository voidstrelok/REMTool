"use client";

import { useEffect, useState } from "react";
import { apiUrl } from "@/lib/api";

export type Informativo = {
  id: number;
  tipo: "Noticia" | "Aviso" | "Informativo" | string;
  titulo: string;
  contenido: string;
  url: string | null;
  texto_enlace: string | null;
  fecha_publicacion: string;
  destacado: boolean;
};

type InformativosResponse = {
  informativos?: Informativo[];
};

export function useInformativos() {
  const [items, setItems] = useState<Informativo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    fetch(apiUrl("informativos"), { signal: controller.signal, cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error("No se pudieron cargar los informativos.");
        const data = (await response.json()) as InformativosResponse;
        setItems(Array.isArray(data.informativos) ? data.informativos : []);
        setError(null);
      })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === "AbortError") return;
        setError("No se pudieron cargar los informativos.");
        setItems([]);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, []);

  return { items, loading, error };
}

export function formatInformativoDate(value: string) {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value;
}
