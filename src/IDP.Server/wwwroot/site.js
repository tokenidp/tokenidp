window.idpAuth = window.idpAuth || {};

function focusElement(selector) {
    const element = document.querySelector(selector);
    if (element) {
        element.focus();
    }
}

window.readClipboard = async () => {
    try {
        return await navigator.clipboard.readText();
    } catch (error) {
        console.error('Failed to read clipboard:', error);
        return '';
    }
};

window.idpAuth.localLogin = async (request, antiforgeryToken) => {
    try {
        const response = await fetch('/local-login', {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                ...(antiforgeryToken ? { 'X-XSRF-TOKEN': antiforgeryToken } : {})
            },
            body: JSON.stringify(request)
        });

        const payload = await response.json();

        if (response.ok) {
            return payload;
        }

        return {
            isSuccess: false,
            error: payload?.value?.error || payload?.error || 'Invalid login.'
        };
    } catch (error) {
        return {
            isSuccess: false,
            error: error instanceof Error
                ? error.message
                : 'An error occurred during authentication.'
        };
    }
};

window.initializeLoginButton = async () => {
    const loginBtn = document.getElementById('login-btn');
    if (loginBtn) {
        loginBtn.addEventListener('click', function (e) {
            e.preventDefault();

            document.getElementById('general-error').style.display = 'none';
            document.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
            document.querySelectorAll('.invalid-feedback').forEach(el => el.style.display = 'none');

            const username = document.getElementById('userName');
            const password = document.getElementById('password');
            let isValid = true;

            if (!username.value.trim()) {
                username.classList.add('is-invalid');
                username.nextElementSibling.style.display = 'block';
                isValid = false;
            }

            if (!password.value.trim()) {
                password.classList.add('is-invalid');
                password.nextElementSibling.style.display = 'block';
                isValid = false;
            }

            if (isValid) {
                setTimeout(() => {
                    if (username.value !== 'demo' || password.value !== 'password123') {
                        document.getElementById('general-error').style.display = 'block';
                    } else {
                        alert('Login successful!');
                    }
                }, 500);
            }
        });
    }
};