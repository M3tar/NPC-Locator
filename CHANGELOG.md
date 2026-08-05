# Changelog

## 0.1.0 - Release

- Renamed the mod from Multiplayer NPC Locator to NPC Locator to reflect full solo, host, and farmhand support.
- Added host-authoritative NPC location and standard daily schedule queries for solo players, hosts, and farmhands.
- Added localized NPC search, current locations, schedules, tile coordinates, manual refresh, and clear unavailable states.
- Added one-NPC tracking with current location, next stop, coordinates, same-map direction, and approximate distance.
- Added a hoverable close button on the tracker overlay to stop the current manual or quest-linked target directly.
- Fixed search text containing the default `E` menu key closing the locator, and matched the tracker close-button hover state to the parchment UI's brown-gold palette.
- Reworked the tracker into aligned label, location, and coordinate columns, with direction and distance on a separate row and hover details for exceptionally long custom locations.
- Tightened the tracker width and unified its parchment surface by removing coordinate badges while retaining right-aligned coordinates.
- Replaced the tracker's stretched texture-box background with the same aspect-cropped in-game parchment texture used by the search menu, eliminating fixed horizontal shading bands.
- Refined the tracker background to retain the inventory-style native menu border and shadow while filling its center from a stable pixel in the same game texture instead of stretching the shaded center tile.
- Applied the same inventory-style panel to delivery-quest prompts and replaced white button states with distinct brown-gold primary and secondary actions.
- Sorted localized NPC names by pinyin when the game language is Chinese, with English fallback names grouped afterward.
- Added local standard item-delivery quest detection, deduplicated prompts, active quest browsing, held counts, deadlines, and task-linked tracking cleanup.
- Added F3 menu restoration for the currently tracked NPC or delivery quest.
- Added optional GMCM settings and English/Simplified Chinese translations.
- Added protocol validation, request IDs, timeouts, host permission controls, rate limiting, disconnect cleanup, and safe update/build scripts.
