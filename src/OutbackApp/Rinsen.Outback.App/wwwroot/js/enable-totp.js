// Identity/EnableTotp: render the TOTP QR code and copy the secret
(function () {
    var options = JSON.parse(document.getElementById('totp-options').textContent);

    var qrEl = document.querySelector('.totp-qr');
    try {
        qrEl.appendChild(QRCode.toSVG(options.uri, { scale: 5, margin: 4 }));
    } catch (error) {
        console.warn('Could not render the TOTP QR code.', error);
        qrEl.innerHTML = '<span class="text-secondary small">QR unavailable — enter the secret manually.</span>';
    }

    var copyBtn = document.querySelector('.totp-copy');
    copyBtn.addEventListener('click', function () {
        navigator.clipboard.writeText(options.secret).then(function () {
            copyBtn.textContent = 'Copied!';
            setTimeout(function () { copyBtn.textContent = 'Copy'; }, 2000);
        });
    });
})();
