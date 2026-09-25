import path from "node:path";
import type { NextConfig } from "next";
const projectRoot = path.resolve(__dirname);
const config: NextConfig = { output: "export", trailingSlash: true, images: { unoptimized: true }, outputFileTracingRoot: projectRoot };
export default config;
