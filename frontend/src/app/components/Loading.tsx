"use client";
import React from "react";

type LoadingProps = {
  message?: string;
  size?: number;
};

export default function Loading({ message = "Cargando...", size = 36 }: LoadingProps) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 12, justifyContent: "center", padding: 12 }}>
      <svg width={size} height={size} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden>
        <path d="M12 2a10 10 0 1 0 10 10" stroke="var(--primary, #2563eb)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" opacity="0.25" />
        <path d="M22 12a10 10 0 0 0-10-10" stroke="var(--primary, #2563eb)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="1s" repeatCount="indefinite" />
        </path>
      </svg>
      <div style={{ color: "var(--primary-dark, #1e40af)", fontSize: 14 }}>{message}</div>
    </div>
  );
}
