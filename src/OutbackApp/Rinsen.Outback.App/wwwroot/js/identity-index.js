// Identity/Index: format session dates in local time
document.addEventListener("DOMContentLoaded", function () {
    const expiresCells = document.querySelectorAll(".format-time");
    expiresCells.forEach(cell => {
        const expiresIso = cell.getAttribute("data-time");
        if (expiresIso) {
            const localDate = new Date(expiresIso).toLocaleString();
            cell.textContent = localDate;
        }
    });
});
