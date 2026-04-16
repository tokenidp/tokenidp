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

window.initializeLoginButton = async () => {
    const loginBtn = document.getElementById("login-btn");
    if (loginBtn) {
        loginBtn.addEventListener("click", function (e) {
            e.preventDefault();

            document.getElementById("general-error").style.display = "none";
            document.querySelectorAll(".is-invalid").forEach(el => el.classList.remove("is-invalid"));
            document.querySelectorAll(".invalid-feedback").forEach(el => el.style.display = "none");

            const username = document.getElementById("userName");
            const password = document.getElementById("password");
            let isValid = true;

            if (!username.value.trim()) {
                username.classList.add("is-invalid");
                username.nextElementSibling.style.display = "block";
                isValid = false;
            }

            if (!password.value.trim()) {
                password.classList.add("is-invalid");
                password.nextElementSibling.style.display = "block";
                isValid = false;
            }

            if (isValid) {
                setTimeout(() => {
                    if (username.value !== "demo" || password.value !== "password123") {
                        document.getElementById("general-error").style.display = "block";
                    } else {
                        alert("Login successful!");
                    }
                }, 500);
            }
        });
    }
};
