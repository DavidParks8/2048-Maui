# Kid-Friendly "Coming Soon" — design prototypes

Clickable prototypes for an app that shows **upcoming G and PG movies only**, so a kid
can browse what's coming to theaters without seeing anything they shouldn't.

Seven designs, all built for an **iPad mini in landscape** and for a **kid who is still
learning to read**. Three are frosted "liquid glass"; four are not glass at all. One of
those (**G**) has no cards anywhere.

## How to look at them

Open `index.html` in a browser — it shows every design side by side, live and tappable.
Or serve the folder if your browser is strict about local files:

```bash
python3 -m http.server 8777 --directory prototypes/kid-movies
# then open http://127.0.0.1:8777/index.html
```

### The iPad designs

| Design | File | The idea |
| --- | --- | --- |
| **A · Glass Timeline** | `ipad-a-glass-timeline.html` | Glass. Calm and list-first — one movie per row in release order, under frosted "Friday, September 4" day headers. Detail slides up as a glass sheet. |
| **B · Cinema Wall** | `ipad-b-cinema-wall.html` | Glass. Poster-first. Day rail down the left, a wall of large posters, floating glass dock. |
| **C · Countdown Calendar** | `ipad-c-countdown.html` | Glass. A real month calendar with a dot on every release day, next to a giant "how many sleeps" countdown. |
| **D · Sticker Book** | `ipad-d-sticker.html` | Not glass. Bright sticker-album look — chunky cards, thick outlines, favoriting slaps a big heart on it. |
| **E · Big Buttons** | `ipad-e-bigbuttons.html` | Not glass. The simplest. Three enormous nav buttons, two movies at a time, the largest type of any design. |
| **F · Cinema** | `ipad-f-cinema.html` | Not glass. Dark and grown-up like Netflix or Disney+. Side-scrolling rows, posters carry everything. |
| **G · Big Screen** | `ipad-g-bigscreen.html` | Not glass, **and no cards**. One movie at a time filling the screen; artwork *is* the page. Bare poster filmstrip along the bottom, plain-word menu with no pills or tabs. |

### The phone sketches

`proto-a-glass-timeline.html`, `proto-b-cinema-wall.html` and `proto-c-countdown.html`
are the original iPhone drafts of A, B and C. They are kept for reference but were made
before the early-reader rules below; the iPad versions are the real comparison.

## Every design implements the same feature set

So it's a fair comparison:

- Upcoming releases grouped and sorted by date
- Favoriting (persisted to `localStorage`), plus a dedicated favorites view
- Search by title
- Detail view with poster, synopsis and a trailer player
- Read-aloud, with the current word highlighted, and tap-any-word-to-hear-it

## Built for a kid learning to read

These rules are enforced in `shared/tablet.css` and `shared/flat.css`, and are the main
reason the iPad designs look different from the phone sketches:

- **Nothing under 15px.** Body text is 21px.
- **No ALL CAPS.** Lowercase word shapes are much easier to decode.
- Line-height 1.6+, extra `word-spacing`, and never a broken/hyphenated word.
- **One idea per line.** No dense metadata rows — a single "kind" chip instead of a
  genre list, no runtimes, no review scores.
- **Spelled-out dates**: "Friday, September 4", not "Fri, Sep 4".
- **Plain counting**: "14 sleeps", not "in 2 weeks".
- Touch targets 60px+.
- Pictures carry the meaning first; words back them up.
- Every block of prose has a **Read it to me** button.

The read-aloud in the prototypes uses the browser's `speechSynthesis`. In the real MAUI
app that maps to `Microsoft.Maui.Media.TextToSpeech`.

## Two design systems

Do not mix these — `glass.css` sets global text colors that fight `flat.css`.

| Designs | Stylesheets |
| --- | --- |
| A, B, C (glass) | `shared/glass.css` + `shared/tablet.css` |
| D, E, F, G (not glass) | `shared/flat.css` only |

`flat.css` is deliberately theme-agnostic: each design sets its own
`--page / --surface / --surface-2 / --line / --text / --text-2 / --text-3 / --accent`,
so D can be cream-and-bright while F is near-black. There is **no `backdrop-filter`
anywhere** in the non-glass designs.

## Where the real data comes from

[TMDB](https://developer.themoviedb.org/reference/intro/getting-started) — free for
non-commercial use, and the only free source that covers all four things this app needs
(upcoming releases, US kid ratings, posters, trailers).

| Need | Endpoint |
| --- | --- |
| Upcoming, G/PG only | `GET /discover/movie?certification_country=US&certification.lte=PG&primary_release_date.gte=<today>&sort_by=primary_release_date.asc&with_release_type=2\|3` |
| Trailers | `GET /movie/{id}/videos` → YouTube keys |
| Posters | `https://image.tmdb.org/t/p/w500{poster_path}` |
| Search | `GET /search/movie` (then filter by certification) |

Notes for implementation:

- An API key is free but must not be committed. Read it from a secret / build-time
  config, and keep it out of source control.
- TMDB **requires attribution** ("This product uses the TMDB API but is not endorsed
  or certified by TMDB") plus their logo.
- `certification.lte=PG` also lets through unrated titles, so filter those out
  explicitly rather than trusting the API alone — this app's whole promise is that a
  kid can't stumble into something inappropriate.
- Trailers play via YouTube, which is itself not age-filtered. Embed the player
  directly rather than deep-linking into the YouTube app.

## About the sample content

Titles, synopses and poster art here are **invented placeholders**, and the posters are
CSS gradients plus one big emoji. Nothing in these prototypes uses copyrighted artwork;
the real app swaps in live TMDB data.

`TODAY` is pinned in `shared/data.js` so the countdowns are stable and screenshots are
reproducible.

## Files

```
index.html                     chooser page — every design side by side

ipad-a-glass-timeline.html     A · glass
ipad-b-cinema-wall.html        B · glass
ipad-c-countdown.html          C · glass
ipad-d-sticker.html            D · not glass
ipad-e-bigbuttons.html         E · not glass
ipad-f-cinema.html             F · not glass
ipad-g-bigscreen.html          G · not glass, no cards

proto-a-glass-timeline.html    original iPhone sketch of A
proto-b-cinema-wall.html       original iPhone sketch of B
proto-c-countdown.html         original iPhone sketch of C

shared/data.js                 sample movies + date/grouping/favorites helpers
shared/reader.js               read-aloud with word highlighting, plus fitFrame()
shared/glass.css               liquid-glass material, phone frame (glass designs)
shared/tablet.css              iPad frame + early-reader rules (glass designs)
shared/flat.css                opaque foundation + early-reader rules (non-glass designs)
```

Add `?embed` to any prototype URL to drop the device frame's outer padding and the
caption — that's how `index.html` renders the previews.
