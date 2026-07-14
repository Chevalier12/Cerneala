const progress = document.getElementById('reading-progress');
const modeButtons = Array.from(document.querySelectorAll('[data-duel-mode]'));
const duelRows = Array.from(document.querySelectorAll('.duel-row'));

function updateProgress() {
  const scrollable = document.documentElement.scrollHeight - window.innerHeight;
  const ratio = scrollable > 0 ? window.scrollY / scrollable : 0;
  progress.style.width = `${Math.min(1, Math.max(0, ratio)) * 100}%`;
}

function setDuelMode(mode) {
  modeButtons.forEach(button => {
    const active = button.dataset.duelMode === mode;
    button.classList.toggle('is-active', active);
    button.setAttribute('aria-pressed', String(active));
  });

  duelRows.forEach(row => {
    const wpfValue = mode === 'time' ? row.dataset.timeWpf : row.dataset.allocationWpf;
    const cuiValue = mode === 'time' ? row.dataset.timeCui : row.dataset.allocationCui;
    const share = mode === 'time' ? row.dataset.timeShare : row.dataset.allocationShare;
    row.querySelector('[data-wpf-value]').textContent = wpfValue;
    row.querySelector('[data-cui-value]').textContent = cuiValue;
    row.style.setProperty('--cui-share', `${share}%`);
  });
}

modeButtons.forEach(button => {
  button.addEventListener('click', () => setDuelMode(button.dataset.duelMode));
});

window.addEventListener('scroll', updateProgress, { passive: true });
window.addEventListener('resize', updateProgress);
setDuelMode('time');
updateProgress();
