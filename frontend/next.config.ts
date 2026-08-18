import path from "node:path";
import type { NextConfig } from "next";

const projectRoot = path.resolve(__dirname);

const nextConfig: NextConfig = {
  output: "export",
  trailingSlash: true,
  outputFileTracingRoot: projectRoot,
  turbopack: {
    root: projectRoot,
  },

};

export default nextConfig;
