(() => {
  const current = new URL(window.location.href);
  const language = current.searchParams.get('lang');
  if (language !== 'en' && language !== 'zh') return;

  const destination = new URL(language === 'en' ? '/en/' : '/', current);
  destination.search = current.search;
  destination.searchParams.delete('lang');
  destination.hash = current.hash;
  window.location.replace(destination);
})();
