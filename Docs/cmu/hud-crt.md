# CRT HUD

HUD styling uses the existing UI setup pipeline rather than a separate appearance preset.

- `CmuHudStyle` resolves colours from `CrtTerminalPalette`; static slot surfaces read the current palette when drawing.
- `CmuHudStyleBox` draws phosphor corner marks, open edges and a bright active-state rail. It retains a neutral presentation when CRT is disabled.
- `CmuHudPanel` adds the existing `CrtScanlineOverlay` above live controls. The game viewport and item sprites are not captured into a replacement render target.
- Chat, equipment, hand/pocket groups and the menu strip share these surfaces. Side status icons have transparent backgrounds. The large chat workspace and its native input, filters, tabs and history remain in place.
- HUD labels use the existing terminal font stack in CRT mode. Font loading goes through the existing UI font preference mechanism. Chat keeps its separate readability/font-size options.

## Settings

The existing UI setup controls drive the HUD:

| Setting | HUD behaviour |
| --- | --- |
| CRT enabled / disabled | Phosphor presentation or neutral fallback |
| CRT colour | Panel tones, labels, menu icons, slot outlines and active states |
| UI font | Existing font-family override, including terminal labels |
| CRT effect intensity and pitch | Shared raster shader |
| Chat CRT haze | Enables/disables the raster over the chat surface independently |
| Chat opacity | Changes opacity without baking the current palette into the panel |

`StylesheetManager.PreviewCrtUi`, save, and reset already rebuild and restyle open controls. HUD rules join the same stylesheet; there is no extra configuration file, copied colour selector or restart requirement in the implementation.

## Validation status

This CRT revision has not been built, tested or launched, at the user's request. Earlier validation of the preceding visual direction does not establish the correctness of this revision. Runtime review and feedback are left to the user.

## Layout and native menus

- Statuses use a side rail, limited to 62 logical pixels in width and 45% of the viewport height. Alert names and descriptions remain in their native tooltips. The left/right preference is retained; opening equipment does not move this rail. The environment-information toggle has been removed.
- Equipment opens from a fixed 64-by-64 toggle beside the lower item dock. Its horizontal position follows the actual dock content rather than a fixed screen-corner offset. The drawer stays above the dock and inline storage; opening it does not stretch its toggle to the drawer width.
- Default HUD supplies the shared coordinator with the screen that actually parents its widgets and the floating chat's bounds. A short chat leaves the footer centred across the screen and places right-side statuses below the chat when they fit; a tall chat reserves its occupied width for the footer and statuses. The top action grid always avoids the menu/chat column. Separated HUD retains its existing pane-based coordinate space and side preference.
- Default chat dimensions are normalized after layout, including archived sizes, UI scale changes and window resizing. Expanded chat has a 320-pixel minimum width and 420-pixel minimum height when space permits; compact mode has separate smaller limits. Its menu strip follows the same width, updating both TopRight margins so its right edge stays ten pixels from the screen edge. Expand restores the user's prior dimensions and bottom attachment. Legacy chat-size setters no longer also write alert margins; the coordinator is the sole owner of alert positioning.
- Standard window backgrounds, titles, close controls and native button shapes share the CRT palette when CRT is enabled. The rules include positive/negative buttons and segmented shapes. Disabling CRT restores the existing stylesheet rules.
- Standard tab containers, their active/inactive tabs, input fields, scrollbar thumbs and divider panels also share the palette. Administration separators expose a stylesheet colour with their authored colour retained as the non-CRT fallback; this removes the hardcoded blue rule in the native admin menu without modifying its actions.
- Escape-menu status rows use theme colours rather than fixed grey overrides. Its raster respects the existing menu-effect toggle as well as the shared intensity and pitch settings.

## Glass and communication actions

`CMUCrtHudGlass` shades the live HUD with curved scanlines, a rounded glass lip, an upper reflection, corner falloff and faint grain. It does not resample chat text or capture scrolling content; text and pointer targets remain aligned. The existing menu CRT pass applies geometric barrel distortion to its captured content. UI setup now exposes the existing curvature and vignette CVars alongside intensity. Chat haze, menu effects, palette and CRT-disable settings continue to gate their respective surfaces.

The options window uses `CrtScanlineOverlay` over live controls rather than a captured barrel-distorted image. Scanlines stay stationary while signal grain animates, following the existing menu-effect toggle, intensity, pitch and static settings. Curvature and vignette still configure glass surfaces; they no longer bend the settings page or move its drawn controls away from their pointer targets.

The area above chat history provides world interactions and an emote catalogue. The interaction target comes from the native right-click verb menu; local verbs appear first and the normal server response completes the list. The panel retains the chosen target when the context popup closes, shows its name, supports explicit refresh and refreshes after execution. It excludes debug/admin verb categories, never requests forced access and uses `VerbSystem.ExecuteVerb`; verbs marked for confirmation use the existing `ConfirmButton`. A player change, target deletion, system unload or removal from the UI tree clears the selection and captured verbs. No hotbar actions are copied into this panel.

Only one tool section is expanded at a time. A single method sets both buttons' `Pressed` state and both panels' `Visible` state, because changing `Pressed` programmatically does not emit `OnToggled`. Compact mode and switching input channels also close the emote section. The emote catalogue lists prototypes permitted for the current character and rechecks availability before invoking the existing emote action. Channel selection remains in the native input; per-channel drafts are retained. Search, filters, latest-message navigation and compact mode remain in the header.

The tall chat column uses a straight raster and animated signal grain without glass curvature, rim or reflection, so its tabs, tool sections and message history no longer accumulate rounded edges. Tool buttons use flat command styling. The item dock and equipment toggle share the same five-pixel bottom inset.

## Item storage

Native storage windows use palette-aware cells, item silhouettes, sidebars, close/back controls and a thin outer frame in CRT mode. The original textures still determine measurement and hit geometry; item rendering, marks, saved-location highlights, dragging and nested-storage navigation keep their existing paths. Other HUD themes and CRT-disabled mode retain the native textures. A single straight raster over the storage window follows the existing menu-effect preference, intensity and pitch.

Known limitation: the reported occupied-slot assertion during `ResetPredictedEntities` remains unresolved. This UI change does not modify `RobustToolbox` or claim to fix container prediction.

## Radial and context menus

Radial sector fills, borders and separators resolve the current CRT palette during drawing. The central close/back control uses a matching circular surface and line glyph. Native sector geometry, icons, hover targets, layer navigation and emote dispatch remain unchanged; disabling CRT restores the authored colours and central textures.

Context popups and their entity, verb, confirmation and disabled rows join the shared CRT stylesheet. Rows use a flat fill and an active side rail; nested-menu arrows follow the palette and confirmation rows retain a danger colour. Fixed content margins prevent text shifting when hovered. Text selectors account for the actual intermediate `BoxContainer` in each row.

The changelog no longer selects the old `Interface` stylesheet. Its frame, heading, tabs and footer inherit the live UI setup stylesheet, including changes made while the window is open. Side status indicators draw only their native icons, values and cooldowns, with no panel or cell frame on either side of the screen.

The Shift-click examine popup uses the shared CRT surface, heading/body colours and styled examine buttons, including hover, pressed and disabled states. It uses a mixed-case Noto heading/body font through the existing UI font mechanism, with 1.25 body line spacing, a separate header and ten-pixel padding. The heading's weight comes from its font rather than an inline bold tag selecting a different font. Its straight raster follows menu-effect, intensity and pitch preferences at a local intensity scale of 0.45; other surfaces retain their full configured strength. The legacy grey tint is a non-CRT stylesheet fallback rather than a control override, so palette preview/reset also affects an already open popup. The body remains the first panel child, preserving the existing examine-update hierarchy; native portrait, description, hover verbs and clickable verbs keep their existing paths.

## Effect coverage

`CmuCrtWindowsUIController` listens for windows opening in the existing window, modal and popup roots. It attaches one menu CRT overlay to ordinary windows and context menus that have no dedicated `CrtScreenControl` or `CrtScanlineOverlay`. Detection happens when the window is added, with no per-frame tree scan or retained window cache. Dedicated terminal/vendor passes remain in charge of their own surfaces. Radial menus are excluded so a rectangular overlay cannot darken their transparent surroundings. The main-menu connection panel owns its own `CmuHudPanel` raster.

Both live overlay shaders now draw animated phosphor signal grain as well as scanlines. Light and dark grain are composed with premultiplied alpha, so grain remains visible on dark fills without the white reflection patch. Ordinary window glass follows curvature/vignette preferences; straight chat and options surfaces retain signal texture without warping their contents. CRT-off, zero intensity, menu effects and chat haze continue to gate their respective paths. The transparent side-status rail receives no overlay.
