const configuredApi = process.env.NEXT_PUBLIC_API?.trim();

export const API_BASE = `${(configuredApi || "/api").replace(/\/+$/, "")}/`;

export function apiUrl(path: string) {
  return `${API_BASE}${path.replace(/^\/+/, "")}`;
}
