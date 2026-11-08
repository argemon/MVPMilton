// Keep the menu visible on desktop and initially collapsed on smaller screens.
(() => {
    const desktop = window.matchMedia('(min-width: 1000px)');
    const updateMenu = () => {
        const menu = document.querySelector('.sidebar details');
        if (menu) menu.open = desktop.matches;
    };
    desktop.addEventListener('change', updateMenu);
    document.addEventListener('DOMContentLoaded', updateMenu);
    if (document.readyState !== 'loading') updateMenu();
})();
