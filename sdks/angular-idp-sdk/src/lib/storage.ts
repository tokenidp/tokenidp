export interface TokenIdpStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
  clear(): void;
}

export function createMemoryStorage(): TokenIdpStorage {
  let mem: Record<string, string> = {};

  return {
    getItem: (key) => (key in mem ? mem[key] : null),
    setItem: (key, value) => {
      mem[key] = String(value);
    },
    removeItem: (key) => {
      delete mem[key];
    },
    clear: () => {
      mem = {};
    },
  };
}
