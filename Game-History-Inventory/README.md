# Game-history evidence collection

Collection project for a later preference analysis, not a game recommendation engine.
The earlier inventory contains 171 curated games. The corrected final handoff combines that
inventory with a verified 382-title Xbox master list and selected achievement-detail evidence.

Final outputs recovered from C:/Users/micha/iCloudDrive/gaming_history_ai_handoff.json and .md.
Earlier evidence: C:/Users/micha/Documents/Codex/2026-10-04/i-want-you-to-inspect-my/game_history_inventory.json.
All three are backed up under ignored local-private/; account evidence is not shareable source code.

## Recreate on a fresh PC/account

Use the user's authorized signed-in account pages. Collect titles, source URLs, available hours,
earned/total achievements and Gamerscore. Preserve all master rows before filtering.
Validate row count, unique titles, unique detail URLs and aggregate totals against the source footer.
The original final checks were 382 records/unique titles/URLs; 7,108 of 15,777 achievements and
153,025 of 356,207 Gamerscore. Those numbers describe the historical snapshot only.

Keep missing Xbox hours null, not zero. Achievement completion does not prove liking, hours or
campaign completion. Quick/easy achievements need explicit non-exhaustive cautions. Preserve
explicit user notes separately from AI interpretation. Research game details only in a separate
analysis pass. Never extract login tokens to imitate a developer Xbox API.

No maintained standalone scraper/generator was found in the workspace. Collection used account
page inspection and one-off processing. The private recovered session code is partial provenance,
not a complete reproducible collector. The personal handoff MD supplies its original interpretation prompt.
