import React from "react";

function ProviderIcon({ label, size = 18 }) {
  const providerName = String(label ?? "").trim().toLowerCase();
  const iconStyle = {
    width: `${size}px`,
    height: `${size}px`,
    display: "block",
    flexShrink: 0,
  };

  if (providerName === "google") {
    return (
      <svg viewBox="0 0 533.5 544.3" style={iconStyle} aria-hidden="true" focusable="false">
        <path
          fill="#4285f4"
          d="M533.5 278.4c0-17.4-1.6-34.1-4.7-50.3H272v95.2h146.9c-6.3 34.1-25 63-53.4 82.3v68h86.4c50.5-46.5 81.6-115.1 81.6-195.2z"
        />
        <path
          fill="#34a853"
          d="M272 544.3c72.6 0 133.6-24 178.1-65.2l-86.4-68c-24 16.2-54.6 25.8-91.7 25.8-70.5 0-130.2-47.6-151.6-111.3h-89.7v69.9c44.5 88.1 136 148.8 241.3 148.8z"
        />
        <path
          fill="#fbbc04"
          d="M120.4 325.6c-10.2-30.4-10.2-63.2 0-93.6v-69.9h-89.7c-38.6 76.8-38.6 166.7 0 243.5l89.7-69.9z"
        />
        <path
          fill="#ea4335"
          d="M272 107.7c39.5-.6 77.6 14.1 106.6 41.1l79.3-79.3C407.7 24.5 345.3-1.1 272 0 166.7 0 75.2 60.7 30.7 148.8l89.7 69.9C141.8 155.3 201.5 107.7 272 107.7z"
        />
      </svg>
    );
  }

  if (providerName === "microsoft") {
    return (
      <svg viewBox="0 0 23 23" style={iconStyle} aria-hidden="true" focusable="false">
        <rect fill="#f25022" x="1" y="1" width="10" height="10" />
        <rect fill="#7fba00" x="12" y="1" width="10" height="10" />
        <rect fill="#00a4ef" x="1" y="12" width="10" height="10" />
        <rect fill="#ffb900" x="12" y="12" width="10" height="10" />
      </svg>
    );
  }

  if (providerName === "github") {
    return (
      <svg viewBox="0 0 24 24" style={iconStyle} aria-hidden="true" focusable="false">
        <path
          fill="#181717"
          d="M12 .5C5.73.5.5 5.73.5 12c0 5.1 3.29 9.43 7.86 10.96.57.1.78-.25.78-.55v-2.1c-3.2.7-3.87-1.54-3.87-1.54-.53-1.35-1.29-1.7-1.29-1.7-1.05-.72.08-.71.08-.71 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.7 1.26 3.36.96.1-.75.4-1.26.73-1.55-2.55-.29-5.23-1.28-5.23-5.69 0-1.26.45-2.29 1.19-3.1-.12-.29-.52-1.45.11-3.02 0 0 .97-.31 3.18 1.18a11.06 11.06 0 0 1 2.9-.39c.99 0 1.99.13 2.9.39 2.21-1.49 3.18-1.18 3.18-1.18.63 1.57.23 2.73.11 3.02.74.81 1.19 1.84 1.19 3.1 0 4.42-2.69 5.4-5.25 5.68.41.35.78 1.04.78 2.1v3.12c0 .3.21.66.79.55A11.51 11.51 0 0 0 23.5 12C23.5 5.73 18.27.5 12 .5z"
        />
      </svg>
    );
  }

  if (providerName === "apple") {
    return (
      <svg viewBox="0 0 24 24" style={iconStyle} aria-hidden="true" focusable="false">
        <path
          fill="currentColor"
          d="M16.37 12.3c.03 3.15 2.77 4.2 2.8 4.21-.02.07-.44 1.52-1.46 3.01-.88 1.29-1.79 2.58-3.22 2.61-1.4.03-1.86-.83-3.47-.83-1.61 0-2.12.8-3.44.85-1.38.05-2.44-1.38-3.33-2.66-1.81-2.62-3.2-7.41-1.34-10.64.93-1.6 2.58-2.62 4.38-2.65 1.37-.03 2.66.92 3.47.92.81 0 2.35-1.13 3.96-.96.67.03 2.54.27 3.74 2.03-.1.06-2.23 1.3-2.19 3.11ZM14.28 4.75c.74-.9 1.24-2.14 1.1-3.38-1.06.04-2.34.7-3.1 1.6-.68.79-1.27 2.05-1.11 3.25 1.18.09 2.38-.6 3.11-1.47Z"
        />
      </svg>
    );
  }

  if (providerName === "linkedin") {
    return (
      <svg viewBox="0 0 24 24" style={iconStyle} aria-hidden="true" focusable="false">
        <path
          fill="#0a66c2"
          d="M20.45 20.45h-3.56v-5.58c0-1.33-.02-3.03-1.85-3.03-1.85 0-2.13 1.44-2.13 2.93v5.68H9.35V9h3.42v1.56h.05c.48-.9 1.64-1.85 3.38-1.85 3.62 0 4.29 2.38 4.29 5.47v6.27ZM5.34 7.43a2.06 2.06 0 1 1 0-4.11 2.06 2.06 0 0 1 0 4.11ZM7.12 20.45H3.56V9h3.56v11.45Z"
        />
      </svg>
    );
  }

  if (providerName === "okta") {
    return (
      <svg viewBox="0 0 24 24" style={iconStyle} aria-hidden="true" focusable="false">
        <circle cx="12" cy="12" r="9" fill="#007dc1" />
        <circle cx="12" cy="12" r="4.25" fill="#ffffff" />
      </svg>
    );
  }

  return null;
}

export default ProviderIcon;
