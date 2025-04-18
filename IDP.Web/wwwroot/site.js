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

document.getElementById('login-btn').addEventListener('click', function (e) {

    debugger;
    e.preventDefault();

    // Reset previous errors
    document.getElementById('general-error').style.display = 'none';
    document.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
    document.querySelectorAll('.invalid-feedback').forEach(el => el.style.display = 'none');

    // Get form values
    const username = document.getElementById('userName');
    const password = document.getElementById('password');
    let isValid = true;

    // Validate username
    if (!username.value.trim()) {
        username.classList.add('is-invalid');
        username.nextElementSibling.style.display = 'block';
        isValid = false;
    }

    // Validate password
    if (!password.value.trim()) {
        password.classList.add('is-invalid');
        password.nextElementSibling.style.display = 'block';
        isValid = false;
    }

    // If basic validation passes, check credentials
    if (isValid) {
        // Simulate API call
        setTimeout(() => {
            if (username.value !== "demo" || password.value !== "password123") {
                document.getElementById('general-error').style.display = 'block';
            } else {
                alert('Login successful!');
                // window.location.href = '/dashboard';
            }
        }, 500);
    }
});