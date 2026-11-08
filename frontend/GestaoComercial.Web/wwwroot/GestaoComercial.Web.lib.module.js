export function afterWebStarted(blazor) {
    const desktop = window.matchMedia('(min-width: 1000px)');
    const updateMenu = () => {
        const menu = document.querySelector('.sidebar details');
        if (menu) menu.open = desktop.matches;
    };
    desktop.addEventListener('change', updateMenu);
    blazor.addEventListener('enhancedload', updateMenu);
    updateMenu();
}
