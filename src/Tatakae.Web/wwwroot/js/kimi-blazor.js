
window.tatakaeKimi = window.tatakaeKimi || {
  init: function () {
    const root = document.documentElement;
    if (!window.__tatakaeKimiRevealObserver) {
      window.__tatakaeKimiRevealObserver = new IntersectionObserver((entries) => {
        entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('show'); });
      }, { threshold: 0.12 });
    }
    document.querySelectorAll('.reveal:not([data-kimi-observed])').forEach(el => {
      el.setAttribute('data-kimi-observed', '1');
      window.__tatakaeKimiRevealObserver.observe(el);
    });
    const update = () => {
      const story = document.querySelector('.hero-story');
      if (story) {
        const r = story.getBoundingClientRect();
        const max = Math.max(1, story.offsetHeight - innerHeight);
        const p = Math.min(1, Math.max(0, -r.top / max));
        root.style.setProperty('--story', p.toFixed(4));
      }
      const motion = document.querySelector('.marquee-section');
      const track = document.querySelector('.h-track');
      if (motion && track) {
        const r = motion.getBoundingClientRect();
        const max = Math.max(1, motion.offsetHeight - innerHeight);
        const p = Math.min(1, Math.max(0, -r.top / max));
        const overflow = Math.max(0, track.scrollWidth - innerWidth + 48);
        root.style.setProperty('--track', Math.round(p * overflow));
      }
    };
    if (!window.__tatakaeKimiScrollBound) {
      addEventListener('scroll', update, { passive: true });
      addEventListener('resize', update);
      window.__tatakaeKimiScrollBound = true;
    }
    requestAnimationFrame(update);
  }
};
