(() => {
  const pageClasses = [
    'page-home','page-shop','page-category','page-product','page-studio',
    'page-checkout','page-login','page-account','page-admin','page-legal','page-page'
  ];

  const resolvePath = (rawPath) => {
    const path = String(rawPath || '').split(/[?#]/)[0].replace(/^\/+|\/+$/g, '').toLowerCase();
    if (!path) return 'home';
    if (/^(shop\/category|category|categories)\//.test(path)) return 'category';
    if (/^(product|products)\//.test(path)) return 'product';
    if (path === 'shop' || path === 'products') return 'shop';
    if (path.startsWith('customize/')) return 'studio';
    if (path === 'checkout' || path.startsWith('payment') || path.startsWith('order-success')) return 'checkout';
    if (path === 'login' || path === 'register') return 'login';
    if (path === 'account' || path.startsWith('account/')) return 'account';
    if (path === 'admin' || path.startsWith('admin/')) return 'admin';
    if (/^(about|terms|rules|privacy|returns|shipping-policy|contact)$/.test(path) || path.startsWith('pages/')) return 'legal';
    return 'page';
  };

  const setPage = (page, path) => {
    const body = document.body;
    if (!body) return;

    const normalized = page || 'page';
    body.dataset.page = normalized;
    body.dataset.route = String(path || '').split(/[?#]/)[0];
    body.classList.remove(...pageClasses);
    body.classList.add(`page-${normalized}`);

    // Route changes in Blazor do not recreate the document. Reset viewport-only UI.
    body.classList.remove('mobile-filter-open');
    document.documentElement.style.removeProperty('--mobile-vh');
    document.documentElement.style.setProperty('--mobile-vh', `${window.innerHeight * 0.01}px`);
  };

  const updateViewport = () => {
    document.documentElement.style.setProperty('--mobile-vh', `${window.innerHeight * 0.01}px`);
  };

  window.addEventListener('resize', updateViewport, { passive: true });
  window.addEventListener('orientationchange', updateViewport, { passive: true });
  window.tatakaeResponsive = { setPage, resolvePath };
  setPage(resolvePath(window.location.pathname), `${window.location.pathname}${window.location.search}`);
  updateViewport();
})();
