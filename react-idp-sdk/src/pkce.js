function base64UrlEncode(arrayBuffer) {
  const bytes = new Uint8Array(arrayBuffer);
  let str = "";
  for (let i = 0; i < bytes.byteLength; i++)
    str += String.fromCharCode(bytes[i]);
  return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

export function generateCodeVerifier(length = 64) {
  const charset =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
  const randomValues = new Uint8Array(length);
  crypto.getRandomValues(randomValues);
  let verifier = "";
  for (let i = 0; i < randomValues.length; i++) {
    verifier += charset[randomValues[i] % charset.length];
  }
  return verifier;
}

export async function generateCodeChallenge(verifier) {
  const enc = new TextEncoder();
  const data = enc.encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(digest);
}