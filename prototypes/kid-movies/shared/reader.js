/*
 * Reading support for kids who are still learning to read.
 *
 * - Read-aloud with word-by-word highlighting (follows along like a finger).
 * - Tap any single word to hear just that word.
 * - Frame scaling so a full-size iPad canvas fits any browser window.
 *
 * In the real MAUI app this maps onto Microsoft.Maui.Media.TextToSpeech.
 */

const ReadAloud = {
  supported: typeof speechSynthesis !== 'undefined',
  words: [],
  timers: [],
  active: null,

  /** Split text into tappable word spans with their character offsets. */
  prepare(el, text) {
    el.textContent = '';
    this.words = [];
    let idx = 0;

    for (const part of text.split(/(\s+)/)) {
      if (!part) continue;
      if (/^\s+$/.test(part)) {
        el.appendChild(document.createTextNode(part));
        idx += part.length;
        continue;
      }
      const span = document.createElement('span');
      span.className = 'w';
      span.textContent = part;
      span.dataset.start = idx;
      span.dataset.end = idx + part.length;
      span.addEventListener('click', (e) => {
        e.stopPropagation();
        this.sayWord(part, span);
      });
      el.appendChild(span);
      this.words.push(span);
      idx += part.length;
    }
    return this.words;
  },

  clearHighlight() {
    for (const w of this.words) w.classList.remove('on');
  },

  highlightAt(charIndex) {
    for (const w of this.words) {
      const on = charIndex >= +w.dataset.start && charIndex < +w.dataset.end;
      w.classList.toggle('on', on);
    }
  },

  stop() {
    for (const t of this.timers) clearTimeout(t);
    this.timers = [];
    this.active = null;
    if (this.supported) speechSynthesis.cancel();
    this.clearHighlight();
  },

  voice() {
    if (!this.supported) return null;
    const all = speechSynthesis.getVoices();
    // Prefer a natural-sounding English voice; fall back to whatever exists.
    return all.find((v) => /en[-_]US/i.test(v.lang) && /natural|premium|siri|samantha/i.test(v.name))
      || all.find((v) => /en[-_]US/i.test(v.lang))
      || all.find((v) => /^en/i.test(v.lang))
      || null;
  },

  /** Speak one word, briefly highlighting it. */
  sayWord(word, span) {
    this.stop();
    if (span) {
      span.classList.add('on');
      this.timers.push(setTimeout(() => span.classList.remove('on'), 900));
    }
    if (!this.supported) return;
    const u = new SpeechSynthesisUtterance(word.replace(/[^\w'’-]/g, ''));
    u.rate = 0.72;
    u.pitch = 1.05;
    const v = this.voice();
    if (v) u.voice = v;
    speechSynthesis.speak(u);
  },

  /**
   * Read the whole passage, highlighting each word as it is spoken.
   * Falls back to a timed estimate in browsers that never fire `boundary`.
   */
  speak(text, { onStart, onEnd } = {}) {
    this.stop();
    onStart?.();

    if (!this.supported) {
      this.fallback(text, onEnd);
      return;
    }

    const u = new SpeechSynthesisUtterance(text);
    u.rate = 0.78;
    u.pitch = 1.04;
    const v = this.voice();
    if (v) u.voice = v;

    let sawBoundary = false;
    u.onboundary = (e) => {
      sawBoundary = true;
      this.highlightAt(e.charIndex);
    };
    u.onend = () => {
      this.clearHighlight();
      this.active = null;
      onEnd?.();
    };
    u.onerror = () => {
      this.clearHighlight();
      this.active = null;
      onEnd?.();
    };

    this.active = u;
    speechSynthesis.speak(u);

    // Safari/Firefox may not emit boundary events — approximate instead.
    this.timers.push(setTimeout(() => {
      if (!sawBoundary && this.active === u) this.fallback(text, null, u.rate);
    }, 420));
  },

  /** Time-based word highlighting when boundary events aren't available. */
  fallback(text, onEnd, rate = 0.78) {
    const charsPerMs = (13.5 * rate) / 1000;
    let elapsed = 0;
    this.words.forEach((w) => {
      const at = elapsed;
      this.timers.push(setTimeout(() => {
        this.clearHighlight();
        w.classList.add('on');
      }, at));
      elapsed += (w.textContent.length + 1) / charsPerMs;
    });
    this.timers.push(setTimeout(() => {
      this.clearHighlight();
      onEnd?.();
    }, elapsed));
  },
};

// Voice lists load asynchronously in Chrome.
if (ReadAloud.supported) speechSynthesis.getVoices();

/** Scale a fixed-size device frame down so it always fits the window. */
function fitFrame(selector, designWidth, margin = 64) {
  const node = document.querySelector(selector);
  if (!node) return;
  const apply = () => {
    const scale = Math.min(1, (window.innerWidth - margin) / designWidth);
    node.style.transform = `scale(${scale})`;
    node.style.transformOrigin = 'top center';
    const parent = node.parentElement;
    if (parent) parent.style.height = node.offsetHeight * scale + 'px';
  };
  apply();
  window.addEventListener('resize', apply);
}
