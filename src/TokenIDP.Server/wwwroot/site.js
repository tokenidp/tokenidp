window.idpAuth = window.idpAuth || {};

function focusElement(selector) {
    const element = document.querySelector(selector);
    if (element) {
        element.focus();
    }
}

function getAuthErrorMessage(payload) {
    if (!payload) {
        return "Invalid login.";
    }

    if (typeof payload === "string") {
        return payload;
    }

    if (typeof payload.error === "string") {
        return payload.error;
    }

    if (typeof payload.value?.error === "string") {
        return payload.value.error;
    }

    if (typeof payload.error?.error === "string") {
        return payload.error.error;
    }

    if (typeof payload.error?.message === "string") {
        return payload.error.message;
    }

    if (typeof payload.value?.message === "string") {
        return payload.value.message;
    }

    if (typeof payload.message === "string") {
        return payload.message;
    }

    if (Array.isArray(payload.errors) && payload.errors.length > 0) {
        const firstError = payload.errors[0];
        if (typeof firstError === "string") {
            return firstError;
        }

        if (typeof firstError?.error === "string") {
            return firstError.error;
        }

        if (typeof firstError?.message === "string") {
            return firstError.message;
        }
    }

    if (Array.isArray(payload.error?.errors) && payload.error.errors.length > 0) {
        const firstEntry = payload.error.errors[0];
        if (typeof firstEntry === "string") {
            return firstEntry;
        }
    }

    return "Invalid login.";
}

window.readClipboard = async () => {
    try {
        return await navigator.clipboard.readText();
    } catch (error) {
        console.error("Failed to read clipboard:", error);
        return "";
    }
};

window.idpAuth.localLogin = async (request, antiforgeryToken) => {
    try {
        const response = await fetch("/local-login", {
            method: "POST",
            credentials: "include",
            headers: {
                "Content-Type": "application/json",
                ...(antiforgeryToken ? { "X-XSRF-TOKEN": antiforgeryToken } : {})
            },
            body: JSON.stringify(request)
        });

        const payload = await response.json();

        if (response.ok) {
            return payload;
        }

        return {
            isSuccess: false,
            error: getAuthErrorMessage(payload)
        };
    } catch (error) {
        return {
            isSuccess: false,
            error: error instanceof Error
                ? error.message
                : "An error occurred during authentication."
        };
    }
};

function showLoginLoader() {
    if (document.getElementById("static-login-loader")) {
        return;
    }

    const overlay = document.createElement("div");
    overlay.id = "static-login-loader";
    overlay.className = "loading-overlay";
    overlay.setAttribute("role", "status");
    overlay.setAttribute("aria-live", "polite");
    overlay.setAttribute("aria-label", "Loading");
    overlay.innerHTML = '<div class="loading-spinner"></div><div class="loading-text">Loading...</div>';
    document.body.appendChild(overlay);
}

function wireLoginFormLoader() {
    const form = document.getElementById("local-login-form");
    if (!form || form.dataset.loaderWired === "true") {
        return;
    }

    form.dataset.loaderWired = "true";
    form.addEventListener("submit", () => {
        const submitButton = form.querySelector("[data-login-submit]");
        if (submitButton) {
            submitButton.disabled = true;
        }

        showLoginLoader();
    });
}

document.addEventListener("DOMContentLoaded", wireLoginFormLoader);
window.addEventListener("pageshow", wireLoginFormLoader);
