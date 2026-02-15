// About Page
window.AboutPage = {
    render() {
        var L = Bridge.lang.bind(Bridge);
        var version = Bridge._settings.appVersion || '1.0.0.0';
        var buildId = Bridge._settings.buildId || 'dev';

        return '' +
        '<div class="card">' +
            '<div class="card-title">' +
                '<span class="mi">info</span>' +
                (L('about_menuItem') || 'About') +
            '</div>' +
            '<div class="about-content">' +
                '<div class="about-app-name">' + (L('main_FormName') || 'Windows Shutdown Helper') + '</div>' +
                '<div class="about-row">' +
                    '<span class="about-label">' + (L('about_label_version') || 'Version') + '</span>' +
                    '<span class="about-value">' + version + '</span>' +
                '</div>' +
                '<div class="about-row">' +
                    '<span class="about-label">' + (L('about_label_buildId') || 'Build ID') + '</span>' +
                    '<span class="about-value">' + buildId + '</span>' +
                '</div>' +
                '<div class="about-divider"></div>' +
                '<div class="about-row">' +
                    '<span class="about-label">' + (L('about_label_author') || 'Author') + '</span>' +
                    '<span class="about-value">enginyilmaaz</span>' +
                '</div>' +
                '<div class="about-row">' +
                    '<span class="about-label">GitHub</span>' +
                    '<a class="about-link" id="about-github-link" href="#">github.com/enginyilmaaz</a>' +
                '</div>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var link = document.getElementById('about-github-link');
        if (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                Bridge.send('openUrl', { url: 'https://github.com/enginyilmaaz' });
            });
        }
    }
};
