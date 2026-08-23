# Product

<!-- impeccable:product-schema 1 -->

## Platform

ios

## Users

The primary user is a child who mainly uses an iPad mini and is still learning to read.
The app also adapts to iPhone. A parent sets up the TMDB token at build time; there is no
in-app parent configuration in v1.

## Product Purpose

Good Movies helps a child discover which G- and PG-rated movies are coming to U.S.
theaters, understand when they arrive, save favorites, hear descriptions read aloud,
and open official trailers.

Success means the child can browse independently without encountering unrated or
older-audience titles and without needing to decode dense movie metadata.

## Positioning

The catalog is safety-filtered before display: every title must have a verified U.S.
limited or wide theatrical release and an exact G or PG certification. Unrated titles
are excluded rather than treated as safe.

## Operating Context

- Used primarily on an iPad mini in landscape, often held with two hands.
- Also used on an iPhone in portrait.
- Catalog data may be viewed offline from a validated local cache.
- Search operates only within the current safe catalog.
- Official trailers play in a locked-down in-app player; links, popups, and navigation away from the selected trailer are blocked.

## Capabilities and Constraints

- Show releases from 13 days ago through 12 calendar months ahead.
- Remove a title and its favorite entry at local midnight 14 days after release.
- Three top-level sections: Coming soon, My favorites, and Find a movie.
- Persist favorites locally.
- Show posters, details, release status, and a simple genre label.
- Read descriptions aloud with current-word highlighting and tap-a-word speech.
- Load cached data first and revalidate after six hours or on explicit refresh.
- Target iPhone and iPad in v1; keep non-UI code platform-neutral.

## Brand Commitments

- Product name: **Good Movies**.
- Purple is the defining color because it is the primary user's favorite.
- The selected visual direction is Design E / Big Buttons: solid color blocks, huge
  touch targets, very few words, and no glass effects.

## Evidence on Hand

- Approved interactive prototype:
  `prototypes/ipad-e-bigbuttons.html`
- Prototype movie titles and posters are synthetic placeholders. Production content
  comes from TMDB; no testimonials, ratings claims, or commercial proof exist.

## Product Principles

1. Prove safety; never infer it from missing data.
2. Pictures carry meaning first and words reinforce them.
3. Every important action is large, direct, and recoverable.
4. Cached content remains useful offline without weakening expiration rules.
5. Plain language beats movie-industry metadata.

## Accessibility & Inclusion

- Designed for an early reader: large text, generous line spacing, no all-caps labels,
  spelled-out dates, and “sleeps” instead of weeks.
- Minimum 60-point primary touch targets.
- VoiceOver labels, logical focus order, Dynamic Type, sufficient contrast, reduced
  motion, and word-level read-aloud feedback are required.
