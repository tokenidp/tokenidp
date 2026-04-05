export function createStorage(mode) {
  if (mode === "localStorage") return window.localStorage;
  if (mode === "sessionStorage") return window.sessionStorage;

  // memory storage fallback
  let mem = {};
  return {
    getItem: (k) => (k in mem ? mem[k] : null),
    setItem: (k, v) => {
      mem[k] = String(v);
    },
    removeItem: (k) => {
      delete mem[k];
    },
    clear: () => {
      mem = {};
    },
  };
}