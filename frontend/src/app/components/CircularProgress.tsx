import React from "react";

interface CircularProgressProps {
  percent: number; // 0-100
  size?: number;
  strokeWidth?: number;
  color?: string;
  bgColor?: string;
  label?: string;
}

const CircularProgress: React.FC<CircularProgressProps> = ({
  percent,
  size = 80,
  strokeWidth = 8,
  color,
  bgColor = "var(--surface-alt, #f3f4f6)",
  label = undefined,
}) => {
  const radius = (size - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference * (1 - percent / 100);

  // Color dinámico según el porcentaje (usando la nueva paleta)
  let dynamicColor = "var(--primary, #000084)";
  if (percent >= 90) dynamicColor = "var(--success, #16a34a)";
  else if (percent >= 50) dynamicColor = "var(--warning, #d97706)";
  if (color) dynamicColor = color;

  return (
    <svg width={size} height={size} style={{ display: "block" }}>
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke={bgColor}
        strokeWidth={strokeWidth}
      />
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke={dynamicColor}
        strokeWidth={strokeWidth}
        strokeDasharray={circumference}
        strokeDashoffset={offset}
        strokeLinecap="round"
        style={{ transition: "stroke-dashoffset 0.5s, stroke 0.5s" }}
      />
      <text
        x="50%"
        y="50%"
        textAnchor="middle"
        dominantBaseline="central"
        fontSize={size * 0.19}
        fontWeight={700}
        fill="var(--primary, #000084)"
      >
        {percent.toFixed(2)}%
      </text>
      {label && (
        <text
          x="50%"
          y={size * 0.78}
          textAnchor="middle"
          fontSize={size * 0.16}
          fill="var(--text-light, #6b7280)"
        >
          {label}
        </text>
      )}
    </svg>
  );
};

export default CircularProgress;
