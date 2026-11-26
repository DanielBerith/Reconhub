/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Absolute API origin, e.g. http://localhost:5080. Empty: same origin via the Vite dev proxy. */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
