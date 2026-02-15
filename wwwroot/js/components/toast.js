// Toast notification component
const Toast = {
    _icons: {
        success: '\u2714',
        warn: '\u26A0',
        info: '\u2139',
        error: '\u2716'
    },

    show(title, message, type, duration) {
        type = type || 'info';
        duration = duration || 2000;

        const container = document.getElementById('toast-container');
        const toast = document.createElement('div');
        toast.className = 'toast toast-' + type;
        toast.innerHTML =
            '<span class="toast-icon">' + (this._icons[type] || this._icons.info) + '</span>' +
            '<div class="toast-content">' +
                '<div class="toast-title">' + (title || '') + '</div>' +
                '<div class="toast-message">' + (message || '') + '</div>' +
            '</div>';

        container.appendChild(toast);

        setTimeout(function () {
            toast.classList.add('toast-out');
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 300);
        }, duration);
    }
};
