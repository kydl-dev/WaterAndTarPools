# Changelog

## 1.0.1

- Fixed: the water splash sound now comes straight from the mod's own asset bundle. Before, it was unpacked from the DLL into the temp folder and loaded back from there, which some virus scanners flagged. The mod now writes nothing to disk except its config and saved styles.

## 1.0.0 - Initial release

- Pumped tar and water fill real pools where the stream lands: dug holes, moats and walled basins, held by built walls, saved with the world and pumpable again.
- Water pumping from the sea, lakes and water pools.
- Water styles Game, Tropical, Lake and Ashlands; hose water and the nozzle stream take the pool's colour, with a water splash sound.
- Pool sign (Hammer > KY-Items): name a pool and style the name (fonts, colours per letter, emoji, preferred style). One per pool, also on tar pools.
- Pool menu (F9): every named pool with its position and closest portal, your own water style per pool, the admin's world style for all pools.
- Fish stay inside water pools.
- Jump 3 times at the edge of a pool to get lifted out.
- `[General] EnableAll` switches the mod off while playing.
- English and German.
