import { defineConfig } from "tsup";

export default defineConfig({
  entry: ["src/index.js"],
  format: ["esm", "cjs"],
  sourcemap: true,
  clean: true,
  dts: false,
  external: ["react", "react-dom", "react-router-dom"]
});