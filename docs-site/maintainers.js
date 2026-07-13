(function () {
  'use strict';

  const progress = document.getElementById('reading-progress');
  const tabs = Array.from(document.querySelectorAll('[data-workflow-tab]'));
  const panels = Array.from(document.querySelectorAll('[data-workflow-panel]'));
  const copyButtons = Array.from(document.querySelectorAll('[data-copy-target]'));

  function updateReadingProgress() {
    const scrollRange = document.documentElement.scrollHeight - window.innerHeight;
    const ratio = scrollRange > 0 ? window.scrollY / scrollRange : 0;
    progress.style.width = Math.max(0, Math.min(1, ratio)) * 100 + '%';
  }

  function selectWorkflow(name) {
    tabs.forEach(function (tab) {
      const selected = tab.dataset.workflowTab === name;
      tab.classList.toggle('is-active', selected);
      tab.setAttribute('aria-selected', selected.toString());
      tab.tabIndex = selected ? 0 : -1;
    });

    panels.forEach(function (panel) {
      const selected = panel.dataset.workflowPanel === name;
      panel.classList.toggle('is-active', selected);
      panel.hidden = !selected;
    });
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

  tabs.forEach(function (tab, index) {
    tab.addEventListener('click', function () {
      selectWorkflow(tab.dataset.workflowTab);
    });

    tab.addEventListener('keydown', function (event) {
      if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
      event.preventDefault();
      const direction = event.key === 'ArrowRight' ? 1 : -1;
      const nextIndex = (index + direction + tabs.length) % tabs.length;
      selectWorkflow(tabs[nextIndex].dataset.workflowTab);
      tabs[nextIndex].focus();
    });
  });

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
