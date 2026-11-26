// Identity/Login: toggle password visibility
document.getElementById('togglePassword').addEventListener('click', function () {
    const passwordField = document.getElementById('password');
    const icon = this.querySelector('.bi');
    if (passwordField.type === 'password') {
        passwordField.type = 'text';
        icon.classList.remove('bi-eye');
        icon.classList.add('bi-eye-slash');
        this.setAttribute('aria-label', 'Hide password');
        this.title = 'Hide password';
    } else {
        passwordField.type = 'password';
        icon.classList.remove('bi-eye-slash');
        icon.classList.add('bi-eye');
        this.setAttribute('aria-label', 'Show password');
        this.title = 'Show password';
    }
});
