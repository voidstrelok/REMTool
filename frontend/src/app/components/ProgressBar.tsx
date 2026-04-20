"use client";
import React from "react";

type ProgressBarProps = {
  numerador: number;
  denominador: number;
  percent?: number;
  metaPercent?: number;
  width?: number | string;
  height?: number;
  showLabel?: boolean;
  className?: string;
  showMetaLine?: boolean;
};

export default function ProgressBar({
  numerador,
  denominador,
  percent,
  metaPercent = 0,
  width = "100%",
  height = 12,
  showLabel = true,
  className = "",
  showMetaLine = true
}: ProgressBarProps) {

  const denom = denominador || 0;
  const num = numerador || 0;

  const computed = denom > 0 ? (num / denom) * 100 : 0;
  const effective = typeof percent === "number" ? percent : computed;

  const capped = Math.min(Math.max(effective, 0), 100);
  const safeMeta = Math.min(Math.max(metaPercent, 0), 100);

  const fillColor =
    denom === 0
      ? "var(--border, #d1d5db)"
      : effective >= safeMeta
        ? "var(--success, #16a34a)"
        : "var(--warning, #d97706)";

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 6
      }}
      className={className}
    >
      <div
        style={{
          width,
          background: "var(--surface-alt, #f3f4f6)",
          borderRadius: 8,
          height,
          overflow: "hidden",
          position: "relative" // 🔥 necesario
        }}
        aria-hidden
      >
        {/* Barra de progreso */}
        <div
          style={{
            width: `${capped}%`,
            height: "100%",
            background: fillColor,
            transition: "width 300ms ease"
          }}
        />

        {/* Línea vertical meta */}
        {safeMeta > 0 && showMetaLine && (
          <div
            style={{
              position: "absolute",
              left: `${safeMeta}%`,
              top: 0,
              bottom: 0,
              width: 2,
              background: "var(--primary-dark, #1e40af)",
              transform: "translateX(-1px)"
            }}
          />
        )}

        {/* Flechita arriba */}
        {safeMeta > 0 && (
          <div
            style={{
              position: "absolute",
              left: `${safeMeta}%`,
              top: -6,
              width: 0,
              height: 0,
              borderLeft: "6px solid transparent",
              borderRight: "6px solid transparent",
              borderTop: "6px solid var(--primary-dark, #1e40af)",
              transform: "translateX(-50%)"
            }}
          />
        )}
      </div>

      {showLabel && (
        <div
          style={{
            fontSize: 12,
            color: "var(--primary-dark, #1e40af)",
            textAlign: "center"
          }}
        >
          {denom === 0 ? (
            <span style={{ color: "var(--text-light, #6b7280)" }}>
              Sin denominador
            </span>
          ) : (
            <>
              <strong>{Math.round(effective)}%</strong> / Meta{" "}
              <strong>{Math.round(safeMeta)}%</strong>
            </>
          )}
        </div>
      )}
    </div>
  );
}