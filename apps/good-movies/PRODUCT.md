# Product

<!-- impeccable:product-schema 1 -->

## Platform

ios

## Users

The primary user is a child who mainly uses an iPad mini and is still learning to read.
The app also adapts to iPhone. A parent sets up the TMDB token at build time; there is no
in-app parent configuration in v1.

## Product Purpose

Good Movies helps a child discover family-appropriate movies coming to U.S. theaters,
understand when they arrive, save favorites, hear descriptions read aloud, and open
official trailers.

Success means the child can browse independently without encountering known
older-audience titles or needing to decode dense movie metadata.

## Positioning

The catalog is safety-filtered before display. Every title must have a verified U.S.
limited or wide theatrical release when discovered. Already-tracked titles redirected
to digital or TV release remain eligible with a verified U.S. release date. Every title
must either carry an exact G or PG certification,
or have no published certification yet and pass the stricter English-language,
family/animation genre, and popularity checks. Titles with any known rating above PG are
excluded.

## Operating Context

- Used primarily on an iPad mini in landscape, often held with two hands.
- Also used on an iPhone in portrait.
- Catalog data may be viewed offline from a validated local cache.
- Search operates only within the current safe catalog.
- Official trailers play in a locked-down in-app player; links, popups, and navigation away from the selected trailer are blocked.

## Capabilities and Constraints

- Show upcoming releases through 12 calendar months ahead and safe movies on TMDB's
  U.S. Now Playing list, regardless of how long ago they opened.
- Offer an In theaters only filter alongside the existing rating filters, favorites,
  and search. Theater status appears on movie cards and details.
- Persist whether each movie has actually appeared on the U.S. Now Playing list.
  Remove it and its favorite after a successful, complete refresh confirms its
  theatrical run has ended.
- Keep already-tracked movies that never enter theaters through day 13 after their
  U.S. release; expire the movie and its favorite at local midnight on day 14.
  Redirected digital/TV titles use their verified digital/TV release date.
- Keep the last fetched theater status while offline. Local midnight updates
  countdowns and expires never-theatrical releases, but does not age out movies
  still known to be in theaters. A failed/incomplete refresh cannot invent a theater exit.
- Discovery remains theater-focused; the app does not discover new streaming-only titles.
- Three top-level sections: Coming soon, My favorites, and Find a movie.
- Persist favorites locally.
- Show posters, details, release status, and a simple genre label.
- Read descriptions aloud with current-word highlighting and tap-a-word speech.
- Load cached data first and revalidate after six hours or on explicit refresh.
- Target iPhone and iPad in v1; keep non-UI code platform-neutral.

## Brand Commitments

- Product name: **Good Movies**.
- Purple is the defining color because it is the primary user's favorite.
- The selected visual direction is Big Buttons: solid color blocks, huge
  touch targets, very few words, and no glass effects.

## Content source

Production content comes from TMDB. Simulator sample titles and posters are synthetic
placeholders; no testimonials, ratings claims, or commercial proof exist.

## Product Principles

1. Prove safety; never infer it from missing data.
2. Pictures carry meaning first and words reinforce them.
3. Every important action is large, direct, and recoverable.
4. Cached content remains useful offline without inventing current theater status.
5. Plain language beats movie-industry metadata.

## Accessibility & Inclusion

- Designed for an early reader: large text, generous line spacing, no all-caps labels,
  spelled-out dates, and “sleeps” instead of weeks.
- Minimum 60-point primary touch targets.
- VoiceOver labels, logical focus order, Dynamic Type, sufficient contrast, reduced
  motion, and word-level read-aloud feedback are required.
