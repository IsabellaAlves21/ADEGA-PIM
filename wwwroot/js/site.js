(() => {
  'use strict';
  const toggle = document.querySelector('[data-menu-toggle]');
  const nav = document.getElementById('menu-categorias');
  if (toggle && nav) {
    toggle.addEventListener('click', () => {
      const expanded = toggle.getAttribute('aria-expanded') === 'true';
      toggle.setAttribute('aria-expanded', String(!expanded));
      nav.classList.toggle('open', !expanded);
    });
  }

  const slider = document.querySelector('[data-slider]');
  if (!slider) return;
  const slides = Array.from(slider.querySelectorAll('[data-slide]'));
  const dots = Array.from(slider.querySelectorAll('[data-dot]'));
  if (slides.length < 2) return;
  let current = 0;
  const display = (index) => {
    current = (index + slides.length) % slides.length;
    slides.forEach((slide, i) => slide.classList.toggle('active', i === current));
    dots.forEach((dot, i) => {
      dot.classList.toggle('active', i === current);
      dot.setAttribute('aria-current', i === current ? 'true' : 'false');
    });
  };
  slider.querySelector('[data-prev]')?.addEventListener('click', () => display(current - 1));
  slider.querySelector('[data-next]')?.addEventListener('click', () => display(current + 1));
  dots.forEach((dot, i) => dot.addEventListener('click', () => display(i)));
  display(current);
})();
