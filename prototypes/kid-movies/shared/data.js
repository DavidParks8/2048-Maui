/*
 * Sample data for the Kid-Friendly "Coming Soon" movie app prototypes.
 *
 * NOTE: These are placeholder titles and CSS-generated poster art so the
 * prototypes run offline and avoid using copyrighted artwork. The real app
 * will pull live data from TMDB:
 *   GET /discover/movie?certification_country=US&certification.lte=PG
 *       &primary_release_date.gte=<today>&sort_by=primary_release_date.asc
 *   GET /movie/{id}/videos   -> YouTube trailer keys
 *   https://image.tmdb.org/t/p/w500{poster_path} -> posters
 */

const MOVIES = [
  {
    id: 1,
    title: 'Comet & the Cardboard Kingdom',
    date: '2026-09-04',
    rating: 'PG',
    runtime: 98,
    genres: ['Animation', 'Adventure'],
    studio: 'Northlight Animation',
    emoji: '🚀',
    c1: '#5B8CFF',
    c2: '#B06BFF',
    score: 92,
    overview:
      'When a homemade rocket actually works, two siblings pilot a cardboard ship across a kingdom of forgotten toys to bring their dog home before bedtime.',
  },
  {
    id: 2,
    title: 'Pip the Unstoppable',
    date: '2026-09-04',
    rating: 'G',
    runtime: 88,
    genres: ['Animation', 'Comedy'],
    studio: 'Sunbeam Pictures',
    emoji: '🐹',
    c1: '#FFB347',
    c2: '#FF6B8B',
    score: 88,
    overview:
      'The smallest hamster in the pet shop enters the world championship of wheel racing, and discovers that being tiny is a superpower.',
  },
  {
    id: 3,
    title: 'The Last Dragon Librarian',
    date: '2026-09-18',
    rating: 'PG',
    runtime: 112,
    genres: ['Fantasy', 'Family'],
    studio: 'Emberhall Studios',
    emoji: '🐉',
    c1: '#FF7A45',
    c2: '#C2185B',
    score: 95,
    overview:
      'A shy girl inherits a library where every book hides a sleeping dragon, and only she can read them back to sleep before the town notices.',
  },
  {
    id: 4,
    title: 'Sock Puppet Detectives',
    date: '2026-09-25',
    rating: 'G',
    runtime: 84,
    genres: ['Animation', 'Mystery'],
    studio: 'Laundry Line Films',
    emoji: '🧦',
    c1: '#37D5D6',
    c2: '#2C7BE5',
    score: 79,
    overview:
      'Every missing sock in the neighborhood leads to one dryer. Two puppet detectives take the case of the century.',
  },
  {
    id: 5,
    title: 'Moonpie',
    date: '2026-10-02',
    rating: 'PG',
    runtime: 101,
    genres: ['Animation', 'Sci-Fi'],
    studio: 'Halcyon Lab',
    emoji: '🌙',
    c1: '#7C4DFF',
    c2: '#1A237E',
    score: 90,
    overview:
      'A bakery on the moon runs out of flour, so the smallest baker rides a comet all the way to Earth for one more bag.',
  },
  {
    id: 6,
    title: 'Grandpa Robot',
    date: '2026-10-09',
    rating: 'PG',
    runtime: 106,
    genres: ['Family', 'Comedy'],
    studio: 'Copperfield Co.',
    emoji: '🤖',
    c1: '#8D9EFF',
    c2: '#4A5568',
    score: 86,
    overview:
      'A boy rebuilds his grandfather’s old workshop robot and accidentally gives it every one of grandpa’s worst jokes.',
  },
  {
    id: 7,
    title: 'Tide Pool Twins',
    date: '2026-10-16',
    rating: 'G',
    runtime: 91,
    genres: ['Animation', 'Adventure'],
    studio: 'Saltwater Animation',
    emoji: '🐙',
    c1: '#26C6DA',
    c2: '#00695C',
    score: 83,
    overview:
      'Two octopus siblings map every tide pool on the coast, until one pool turns out to be much, much deeper than the map allows.',
  },
  {
    id: 8,
    title: 'The Great Pumpkin Heist',
    date: '2026-10-23',
    rating: 'PG',
    runtime: 95,
    genres: ['Comedy', 'Family'],
    studio: 'Harvest Row',
    emoji: '🎃',
    c1: '#FF9800',
    c2: '#6A1B9A',
    score: 81,
    overview:
      'A team of very polite raccoons plans the most elaborate, least criminal heist in the history of the county fair.',
  },
  {
    id: 9,
    title: 'Snowbound',
    date: '2026-11-06',
    rating: 'PG',
    runtime: 108,
    genres: ['Adventure', 'Family'],
    studio: 'Northlight Animation',
    emoji: '❄️',
    c1: '#82CFFF',
    c2: '#3949AB',
    score: 89,
    overview:
      'Snowed in at a mountain lodge, a kid and a very stubborn husky discover a trail that only appears once every hundred winters.',
  },
  {
    id: 10,
    title: 'Bloop!',
    date: '2026-11-13',
    rating: 'G',
    runtime: 86,
    genres: ['Animation', 'Comedy'],
    studio: 'Bubblewrap Toons',
    emoji: '🫧',
    c1: '#4DD0E1',
    c2: '#7B1FA2',
    score: 77,
    overview:
      'A shy little blob wants to make one friend. Unfortunately, it duplicates every time it gets nervous.',
  },
  {
    id: 11,
    title: 'Paper Airplane Pilots',
    date: '2026-11-20',
    rating: 'PG',
    runtime: 99,
    genres: ['Family', 'Adventure'],
    studio: 'Windward Films',
    emoji: '✈️',
    c1: '#FFD54F',
    c2: '#F4511E',
    score: 87,
    overview:
      'A classroom paper airplane contest turns into a real flight across the city when one plane refuses to land.',
  },
  {
    id: 12,
    title: 'The Nutcracker Next Door',
    date: '2026-12-04',
    rating: 'G',
    runtime: 104,
    genres: ['Fantasy', 'Musical'],
    studio: 'Gilded Lantern',
    emoji: '🎄',
    c1: '#E53935',
    c2: '#1B5E20',
    score: 91,
    overview:
      'The toy soldier in the neighbor’s window has been watching all year, and on Christmas Eve he finally knocks.',
  },
  {
    id: 13,
    title: 'Dino Camp',
    date: '2026-12-11',
    rating: 'PG',
    runtime: 97,
    genres: ['Adventure', 'Comedy'],
    studio: 'Big Fern Pictures',
    emoji: '🦕',
    c1: '#66BB6A',
    c2: '#00838F',
    score: 84,
    overview:
      'Summer camp gets complicated when the counselors turn out to be extremely friendly, extremely large dinosaurs.',
  },
  {
    id: 14,
    title: 'Starlight Express Delivery',
    date: '2026-12-18',
    rating: 'G',
    runtime: 89,
    genres: ['Animation', 'Family'],
    studio: 'Halcyon Lab',
    emoji: '⭐',
    c1: '#FFD180',
    c2: '#512DA8',
    score: 93,
    overview:
      'One package. One night. Every constellation between here and home. The galaxy’s smallest courier will not be late.',
  },
  {
    id: 15,
    title: 'The Kite That Ate Tuesday',
    date: '2027-01-15',
    rating: 'PG',
    runtime: 93,
    genres: ['Fantasy', 'Comedy'],
    studio: 'Windward Films',
    emoji: '🪁',
    c1: '#EC407A',
    c2: '#283593',
    score: 80,
    overview:
      'A kite gets loose on a windy Tuesday and takes the whole day with it. Getting Tuesday back is going to take until Friday.',
  },
  {
    id: 16,
    title: 'Mud Season',
    date: '2027-01-22',
    rating: 'PG',
    runtime: 110,
    genres: ['Family', 'Drama'],
    studio: 'Barnboard',
    emoji: '🐴',
    c1: '#A1887F',
    c2: '#33691E',
    score: 88,
    overview:
      'A city kid spends the muddiest month of the year on a farm and meets a horse who trusts absolutely no one.',
  },
  {
    id: 17,
    title: 'Robo-Recess',
    date: '2027-02-05',
    rating: 'G',
    runtime: 85,
    genres: ['Animation', 'Comedy'],
    studio: 'Copperfield Co.',
    emoji: '⚙️',
    c1: '#4FC3F7',
    c2: '#F06292',
    score: 76,
    overview:
      'The new playground equipment is a little too smart, and it really wants everyone to have fun. Right now. Forever.',
  },
  {
    id: 18,
    title: 'Song of the Deep Woods',
    date: '2027-02-19',
    rating: 'PG',
    runtime: 115,
    genres: ['Fantasy', 'Adventure'],
    studio: 'Emberhall Studios',
    emoji: '🦌',
    c1: '#4DB6AC',
    c2: '#1A237E',
    score: 94,
    overview:
      'A girl who can hum any sound she hears follows a melody into a forest that has been quiet for a hundred years.',
  },
];

/* ---------- shared helpers used by every prototype ---------- */

/* The chooser page embeds each prototype in an iframe with ?embed. */
if (typeof location !== 'undefined' && location.search.includes('embed')) {
  document.documentElement.classList.add('embed');
}

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];
const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

/** Parse 'YYYY-MM-DD' as a local date (avoids UTC off-by-one). */
function parseDate(iso) {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
}

/** "Fri, Sep 4" */
function formatDate(iso) {
  const d = parseDate(iso);
  return `${DAYS[d.getDay()]}, ${MONTHS[d.getMonth()].slice(0, 3)} ${d.getDate()}`;
}

/** "September 2026" */
function formatMonth(iso) {
  const d = parseDate(iso);
  return `${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
}

/** Days from "today" (pinned so the prototype always looks the same). */
const TODAY = new Date(2026, 7, 21); // Aug 21, 2026

function daysUntil(iso) {
  const ms = parseDate(iso) - TODAY;
  return Math.max(0, Math.round(ms / 86400000));
}

function countdownLabel(iso) {
  const n = daysUntil(iso);
  if (n === 0) return 'Today';
  if (n === 1) return 'Tomorrow';
  if (n < 7) return `${n} days`;
  if (n < 30) {
    const w = Math.round(n / 7);
    return `${w} week${w === 1 ? '' : 's'}`;
  }
  const mo = Math.round(n / 30);
  return `${mo} month${mo === 1 ? '' : 's'}`;
}

/** Group movies into [{ key, label, items }] by month. */
function groupByMonth(list) {
  const map = new Map();
  for (const m of list) {
    const key = m.date.slice(0, 7);
    if (!map.has(key)) map.set(key, { key, label: formatMonth(m.date), items: [] });
    map.get(key).items.push(m);
  }
  return [...map.values()];
}

/** Group movies into [{ key, label, sub, items }] by exact release date. */
function groupByDate(list) {
  const map = new Map();
  for (const m of list) {
    if (!map.has(m.date)) {
      map.set(m.date, {
        key: m.date,
        label: formatDate(m.date),
        sub: countdownLabel(m.date),
        items: [],
      });
    }
    map.get(m.date).items.push(m);
  }
  return [...map.values()];
}

/** CSS-generated poster art, so no copyrighted images are needed. */
function posterStyle(m) {
  return `background:
     radial-gradient(120% 90% at 20% 0%, ${m.c1}ee 0%, transparent 60%),
     radial-gradient(120% 110% at 90% 100%, ${m.c2}ee 0%, transparent 62%),
     linear-gradient(160deg, ${m.c1} 0%, ${m.c2} 100%)`;
}

/** Simple favorite store shared by the prototypes. */
const Favorites = {
  key: 'kidmovies-favs',
  ids: new Set(),
  load() {
    try {
      const raw = localStorage.getItem(this.key);
      if (raw) this.ids = new Set(JSON.parse(raw));
      else this.ids = new Set([3, 5, 14]); // a few pre-picked so the tab isn't empty
    } catch {
      this.ids = new Set([3, 5, 14]);
    }
    return this;
  },
  save() {
    try {
      localStorage.setItem(this.key, JSON.stringify([...this.ids]));
    } catch { /* prototype only */ }
  },
  has(id) {
    return this.ids.has(id);
  },
  toggle(id) {
    if (this.ids.has(id)) this.ids.delete(id);
    else this.ids.add(id);
    this.save();
    return this.ids.has(id);
  },
};
