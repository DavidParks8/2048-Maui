# Good Movies design system

## Direction

**Big Buttons** is a solid, purple, early-reader interface. It rejects glass,
translucency, dense movie metadata, and miniature controls. Posters lead; plain words
confirm what the picture means; every primary choice is obvious from arm's length.

## Color

| Role | Value |
| --- | --- |
| Page | `#190A3A` |
| Primary surface | `#341C86` |
| Raised/group surface | `#4527A8` |
| Selected/action | `#D8B4FE` |
| Secondary text | `#E8DFFD` |
| Tertiary text | `#D4C5F2` |
| Read-aloud highlight | `#FFE36B` |

Use solid fills. Depth comes from a neutral downward shadow and press translation, never
blur, transparency, or a colored glow. Lilac marks selection, primary actions, and the
saved state.

## Type and reading

- System font with Dynamic Type enabled.
- 44pt hero, 27pt title, 21pt body/action, 16pt supporting floor.
- No all-caps labels, forced hyphenation, compressed metadata rows, or abbreviations.
- Dates are spelled out. iPad includes the weekday; compact phones use month and day so
  the date remains whole beside its status pill.
- Future releases use `1 sleep` / `N sleeps`; released titles use `In theaters today`
  and `In theaters now`.
- Synopsis words remain paragraph-like while retaining 44pt tap targets and a yellow
  current-word highlight.

## Structure

- **Expanded landscape (1080pt+ and wider than tall):** 262pt navigation rail, two-column
  grouped movie feed, full 44pt page title.
- **Compact:** one-column feed and three large bottom tabs. iPad portrait retains the
  44pt title; iPhone uses 32pt.
- Layout changes by available width and orientation, never by device model.
- Content respects container safe areas; the search feed responds to the software
  keyboard without relocating the global navigation.

## Components

- Navigation tiles are equal, large blocks. Selected state flips to lilac/dark-purple.
- Movie cards use poster, title, release status, a G/PG or rating-pending badge, one kind
  chip, and a separate favorite target.
- Date headers group the feed and carry a count/status pill.
- Detail uses native Shell navigation, poster-first content, trailer handoff,
  read-aloud, favorite, and a tappable-word story.
- Loading uses skeleton blocks. Empty and error states teach the next action in plain
  language.

## Interaction

- Primary targets are at least 60pt; individually tappable words follow Apple's 44pt
  minimum.
- Buttons press downward into their neutral offset shadow.
- Native navigation and edge-swipe back are preserved.
- Motion communicates press, selection, loading, or navigation state only and must
  respect Reduce Motion.

## Accessibility

- Each card is one VoiceOver summary plus one independent favorite action; decorative
  poster and metadata children are excluded from duplicate focus.
- Group headers expose one semantic date/count description.
- Search receives focus only after the user chooses Find a movie.
- Text and controls must remain legible at larger Dynamic Type sizes; no meaning relies
  on color or emoji alone.
