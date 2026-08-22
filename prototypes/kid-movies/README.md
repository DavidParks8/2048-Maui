# Kid-Friendly "Coming Soon" — design prototypes

Three clickable prototypes for an app that shows **upcoming G and PG movies only**,
so a kid can browse what's coming to theaters without seeing anything they shouldn't.

## How to look at them

Open `index.html` in a browser — it shows all three side by side, live and tappable.
Or serve the folder if your browser is strict about local files:

```bash
python3 -m http.server 8777 --directory prototypes/kid-movies
# then open http://127.0.0.1:8777/index.html
```

| Prototype | File | The idea |
| --- | --- | --- |
| **A · Glass Timeline** | `proto-a-glass-timeline.html` | Calm and list-first. Sticky glass month/date headers, filter chips, bottom tab bar, detail as a bottom sheet. |
| **B · Cinema Wall** | `proto-b-cinema-wall.html` | Poster-first and cinematic. Big "next up" hero, swipeable date rail, 3-across poster wall, full-screen detail. |
| **C · Countdown Calendar** | `proto-c-countdown.html` | Light frosted glass, built for a kid. Giant "how many sleeps" counter, real tappable calendar, big friendly rows. |

All three implement the same feature set so they're a fair comparison:

- Upcoming releases grouped/sorted by date
- Favoriting (persisted to `localStorage`), plus a dedicated favorites view
- Search across title, genre and studio
- Filtering (rating, genre, timeframe)
- Detail view with poster, synopsis and a trailer player
- iOS "liquid glass" material: real `backdrop-filter` blur + saturation, specular
  edge highlights, and an animated sheen, over a wallpaper the glass refracts

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
generated from CSS gradients. Nothing in these prototypes uses copyrighted artwork; the
real app swaps in live TMDB data.

## Files

```
index.html                     chooser page — all three side by side
proto-a-glass-timeline.html    prototype A
proto-b-cinema-wall.html       prototype B
proto-c-countdown.html         prototype C
shared/glass.css               liquid-glass material, phone frame, shared components
shared/data.js                 sample movies + date/grouping/favorites helpers
```
