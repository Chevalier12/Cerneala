(function () {
  'use strict';

  const progress = document.getElementById('reading-progress');
  const map = document.getElementById('roadmap-map');
  const connections = document.getElementById('roadmap-connections');
  const nodes = Array.from(document.querySelectorAll('[data-roadmap-node]'));
  const filterButtons = Array.from(document.querySelectorAll('[data-control-filter]'));
  const controlFamilies = Array.from(document.querySelectorAll('[data-control-state]'));
  const svgNamespace = 'http://www.w3.org/2000/svg';
  let redrawFrame = 0;

  function updateReadingProgress() {
    const scrollRange = document.documentElement.scrollHeight - window.innerHeight;
    const ratio = scrollRange > 0 ? window.scrollY / scrollRange : 0;
    progress.style.width = Math.max(0, Math.min(1, ratio)) * 100 + '%';
  }

  function pointFor(element, parentRect) {
    const orb = element.querySelector('.roadmap-node__orb');
    const rect = orb.getBoundingClientRect();
    return {
      x: rect.left - parentRect.left + rect.width / 2,
      y: rect.top - parentRect.top + rect.height / 2
    };
  }

  function pathBetween(from, to) {
    const dx = to.x - from.x;
    const dy = to.y - from.y;

    if (Math.abs(dy) < 28) {
      const bend = Math.max(45, Math.abs(dx) * .42);
      const direction = Math.sign(dx) || 1;
      return `M ${from.x} ${from.y} C ${from.x + bend * direction} ${from.y}, ${to.x - bend * direction} ${to.y}, ${to.x} ${to.y}`;
    }

    if (Math.abs(dx) < 28) {
      const bend = Math.max(45, Math.abs(dy) * .42);
      const direction = Math.sign(dy) || 1;
      return `M ${from.x} ${from.y} C ${from.x} ${from.y + bend * direction}, ${to.x} ${to.y - bend * direction}, ${to.x} ${to.y}`;
    }

    const midpoint = from.y + dy * .5;
    return `M ${from.x} ${from.y} C ${from.x} ${midpoint}, ${to.x} ${midpoint}, ${to.x} ${to.y}`;
  }

  function connectionState(fromNode, toNode) {
    const fromState = fromNode.dataset.state;
    const toState = toNode.dataset.state;
    if (fromState === 'done' && toState === 'done') return 'done';
    if (fromState === 'active' || toState === 'active') return 'active';
    return 'planned';
  }

  function drawConnections() {
    cancelAnimationFrame(redrawFrame);
    redrawFrame = requestAnimationFrame(function () {
      const parentRect = map.getBoundingClientRect();
      connections.setAttribute('viewBox', `0 0 ${parentRect.width} ${parentRect.height}`);
      connections.setAttribute('width', parentRect.width);
      connections.setAttribute('height', parentRect.height);
      connections.replaceChildren();

      nodes.slice(0, -1).forEach(function (node, index) {
        const nextNode = nodes[index + 1];
        const path = document.createElementNS(svgNamespace, 'path');
        path.setAttribute('d', pathBetween(pointFor(node, parentRect), pointFor(nextNode, parentRect)));
        path.setAttribute('class', `roadmap-connection roadmap-connection--${connectionState(node, nextNode)}`);
        path.dataset.connectionIndex = index.toString();
        connections.appendChild(path);
      });
    });
  }

  function setControlFilter(filter) {
    filterButtons.forEach(function (button) {
      button.classList.toggle('is-active', button.dataset.controlFilter === filter);
    });

    controlFamilies.forEach(function (family) {
      family.classList.toggle('is-filtered', filter !== 'all' && family.dataset.controlState !== filter);
    });
  }

  filterButtons.forEach(function (button) {
    button.addEventListener('click', function () {
      setControlFilter(button.dataset.controlFilter);
    });
  });

  nodes.forEach(function (node, index) {
    node.addEventListener('mouseenter', function () {
      connections.querySelectorAll('path').forEach(function (path) {
        const pathIndex = Number(path.dataset.connectionIndex);
        path.style.strokeWidth = pathIndex === index || pathIndex === index - 1 ? '3' : '1.5';
        path.style.opacity = pathIndex === index || pathIndex === index - 1 ? '1' : '.45';
      });
    });

    node.addEventListener('mouseleave', function () {
      connections.querySelectorAll('path').forEach(function (path) {
        path.style.strokeWidth = '';
        path.style.opacity = '';
      });
    });
  });

  window.addEventListener('scroll', updateReadingProgress, { passive: true });
  window.addEventListener('resize', drawConnections, { passive: true });

  if ('ResizeObserver' in window) {
    new ResizeObserver(drawConnections).observe(map);
  }

  document.fonts.ready.then(drawConnections);
  updateReadingProgress();
  drawConnections();
}());
