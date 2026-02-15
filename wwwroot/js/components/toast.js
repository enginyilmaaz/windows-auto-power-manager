// Toast notification component
const Toast = {
    _icons: {
        success: '\ue86c',
        warn: '\ue002',
        info: '\ue88e',
        error: '\ue5c9'
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
