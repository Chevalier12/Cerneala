(function () {
  'use strict';

  const progress = document.getElementById('reading-progress');
  const copyButtons = Array.from(document.querySelectorAll('[data-copy-target]'));

  function updateReadingProgress() {
    const scrollRange = document.documentElement.scrollHeight - window.innerHeight;
    const ratio = scrollRange > 0 ? window.scrollY / scrollRange : 0;
    progress.style.width = Math.max(0, Math.min(1, ratio)) * 100 + '%';
  }

  async function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }

    const input = document.createElement('textarea');
    input.value = text;
    input.setAttribute('readonly', '');
    input.style.position = 'fixed';
    input.style.opacity = '0';
    document.body.appendChild(input);
    input.select();
    document.execCommand('copy');
    input.remove();
  }

  copyButtons.forEach(function (button) {
    button.addEventListener('click', async function () {
      const target = document.getElementById(button.dataset.copyTarget);
      const originalLabel = button.textContent;

      try {
        await copyText(target.innerText);
        button.textContent = 'Copied';
        button.classList.add('is-copied');
      } catch (error) {
        button.textContent = 'Copy failed';
      }

      window.setTimeout(function () {
        button.textContent = originalLabel;
        button.classList.remove('is-copied');
      }, 1700);
    });
  });

  window.addEventListener('scroll', updateReadingProgress, { passive: true });
  updateReadingProgress();
}());
