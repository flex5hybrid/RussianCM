# CRT HUD

HUD styling uses the existing UI setup pipeline rather than a separate appearance preset.

- `CmuHudStyle` resolves colours from `CrtTerminalPalette`; static slot surfaces read the current palette when drawing.
- `CmuHudStyleBox` draws thin rectangular rules and reverse-video button selection. Idle HUD cells keep transparent fills and quiet perimeters. It retains a neutral presentation when CRT is disabled.
- `CmuCrtDisplayPanel` draws its complete live subtree through `CmuCrtCapture`: backgrounds, frames, text and icons share one refracted image. Chrome styleboxes draw native fills into that image, without a separate background effect.
- Chat and menus share the terminal palette. Bottom slots, item status and menu buttons have transparent idle backgrounds; equipment has a light translucent backing. Side status icons have no background or raster. Native chat input, filters, tabs and history remain in place.
- HUD labels use the existing terminal font stack in CRT mode. Font loading goes through the existing UI font preference mechanism. Chat keeps its separate readability/font-size options.

## Settings

The visual reference is [The screens of Alien: Earth, Pushing Pixels](https://www.pushing-pixels.org/2025/11/01/the-screens-of-alien-earth-interview-with-dave-henri-and-stefan-grimm.html), including the MOTHER, Maginot and tracker images. The implementation adapts their terminal vocabulary: complete hairline frames, near-black screen planes, compact monospaced labels and bright reverse-video selections. It does not copy artwork, company marks, fictional readouts or reference-screen text. The existing colour picker still controls the phosphor hue; readable chat fonts and channel colours stay available.

CRT effects process the complete window image, including text and icons. Barrel refraction, restrained scanlines and edge shading provide the display appearance. Signal noise updates at the configured cadence; the source redraws every UI frame. There is no frame hold, afterglow, bloom, chromatic split or rolling shear on reading surfaces.

The existing UI setup controls drive the HUD:

| Setting | HUD behaviour |
| --- | --- |
| CRT enabled / disabled | Phosphor presentation or neutral fallback |
| CRT colour | Panel tones, labels, menu icons, slot outlines and active states |
| UI font | Existing font-family override, including terminal labels |
| CRT effect intensity and pitch | Strength of the full-image effect and scanline spacing |
| Simulate a CRT display | Adds barrel refraction and edge shading to the complete captured image |
| CRT screen curvature | Controls refraction strength, including text and icons |
| CRT display refresh rate | Signal texture cadence, 12–60 Hz; default 24 Hz |
| Chat CRT haze | Enables/disables the complete chat display pass independently |
| Chat opacity | Shared with Accessibility; changes backing, caption, tab backing and row tints without fading glyphs |
| New CRT interface | Off restores native stylesheet rules, hides the terminal caption and returns the ability bar to horizontal layout |

`StylesheetManager.PreviewCrtUi`, save, and reset already rebuild and restyle open controls. HUD rules join the same stylesheet; there is no extra configuration file, copied colour selector or restart requirement in the implementation.

### Display rendering

`CmuCrtCapture` owns one native-resolution sRGB render target and one shader instance per surface. It redraws the complete source subtree every frame and composites the result with `CMUCrtDisplay`. Native child rendering is skipped only when capture succeeds; disabled or failed effects retain ordinary drawing. Targets are reallocated on resize and released when their controls leave the tree or the effect is disabled.

`CmuCrtWindowsUIController` installs one neutral overlay-layout surface around the direct children of native `DefaultWindow` and `FancyWindow` instances. The native window keeps its parent, bounds, dragging and content references. Existing display panels cover chat, equipment, contextual popups and examination; storage and radial windows use the same capture helper through rendering overrides. No storage grid reparenting or positioning changes are involved.

The capture resets the drawing origin and screen scissor before rendering in target coordinates. Transparent overscan keeps edge labels in the source image during barrel refraction. The native scissor is disabled before clearing the framebuffer and restored before compositing; inherited screen-space clipping must not blank the target or leave stale pixels. Modulation from the source and its ancestors applies once. Captured alpha is preserved, including chat opacity and the radial wheel's transparent centre.

Nested display panels, authored terminal captures and scanline overlays are suppressed during an enclosing capture, so each pixel receives one effect. The former background mesh/material has been removed. The source is sampled at native resolution without a blur pass or downscaling; text now deliberately participates in refraction. Layout and input continue through the original controls.

`cmu.crt_display_effect` controls refraction and shading; `cmu.crt_display_refresh_rate` controls signal noise cadence. Menu effects and chat effects are independently gated. CRT-off and zero intensity draw ordinary controls. These are client-only preferences with no shared input-map changes.

### Scrolling and floating storage

Legacy chat uses `CmuChatOutputPanel` to apply wheel steps directly to its native scrollbar, updating both value and target together. Its scroll distance is converted to physical pixels once. Structured chat keeps its separate immediate scrollbar and only follows the bottom within one UI pixel. Both remain live independently of display effects.

Structured chat updates its range, wrapping width and follow state only when arranging the actual viewport. Floating Default HUD can measure a chat widget against the entire screen before assigning its smaller anchored rectangle; that preliminary measurement must not clamp a scrollbar drag or resume following the bottom.

Floating storage populates its grid before initial measurement and placement. Only floating windows get a scroll viewport for oversized contents. Their size and position are constrained to the window root after layout, dragging, nesting and root resize. Inline storage retains its native container. Drag ghosts use physical mouse coordinates divided by their actual UI scale, without the old extra division by two.

## Validation status

This CRT revision has not been built, tested or launched, at the user's request. Earlier validation of the preceding visual direction does not establish the correctness of this revision. Runtime review and feedback are left to the user.

## Layout and native menus

- Native status icons use their original 64-by-64 size with transparent backgrounds. Default HUD anchors them on the left, below the bounded ability block; separated HUD keeps the selected side. The targeting doll follows the same side and stays above the equipment toggle on the left. Their positions never depend on storage-window height. Environment-information alerts remain hidden.
- Equipment opens above a fixed 64-by-64 toggle twelve pixels from the lower-left edge. It contains only the clothing grid and a scroll area, without a separate title/toolbar. Its anchor and height limit are independent of belt/backpack windows. Opening it brings its subtree above inline storage without moving its toggle.
- The bottom dock centres native worn items, right item status, hands, left item status and pockets with a uniform four-pixel gap. Idle surfaces are transparent. Native item-status widths remain fixed; storage grows upward independently. Only insufficient viewport width wraps groups to another row. Dock and ghost horizontal padding use clamped `Control.Margin` inside full-width layout anchors, preventing negative constrained rectangles during a splitter resize before `OnResized` runs. Optional viewport centering likewise uses a clamped margin and retains a nonnegative game area.
- Menu buttons are at the upper left in both layouts. Ability buttons flow vertically underneath, with upright icons/key labels. Small sets use one column; larger sets use multiple columns capped at seven rows, with further wrapping when status icons need vertical space. Chat resizing never moves the menu to the right.
- Default chat stays at the upper right, with a 320-pixel minimum width and 220-pixel minimum height when space permits. Restored sizes and dragging share a 288-pixel bottom clearance for native status icons. Separated chat retains its full-height column, splitter persistence and optional viewport centering.
- Standard window backgrounds, titles, close controls and native button shapes share the CRT palette when CRT is enabled. The rules include positive/negative buttons and segmented shapes. Disabling CRT restores the existing stylesheet rules.
- Standard tab containers, their active/inactive tabs, input fields, scrollbar thumbs and divider panels also share the palette. Administration separators expose a stylesheet colour with their authored colour retained as the non-CRT fallback; this removes the hardcoded blue rule in the native admin menu without modifying its actions.
- Escape-menu status rows use theme colours rather than fixed grey overrides. Its raster respects the existing menu-effect toggle as well as the shared intensity and pitch settings.

## Glass and communication

Ordinary menus and chat use the full-image pass described above. Curvature affects backgrounds, frames, glyphs and icons together. UI setup exposes the existing curvature and vignette CVars alongside intensity. Chat haze, menu effects, palette and CRT-disable settings gate their respective surfaces.

The chat has one 24-pixel screen caption, followed by native tabs, history and input, with the original search/filter controls beside the input. The caption ignores mouse input so the floating chat's border drag remains available. The history panel and its clipped viewport explicitly use `MouseFilterMode.Pass`: native wheel routing skips controls left at the default `Ignore`, even when they override `MouseWheel`. Regression scenarios route wheel input through UI hit testing for both resizable and separated chat widgets. The upper-left menu keys share a translucent command band with no added padding or shifted hit targets. The extra interaction-target panel and emote catalogue have been removed; contextual actions and emotes remain in the native right-click menu and radial wheel. Per-channel drafts remain available.

Chat search is a local console command binding, defaulting to Ctrl+Shift+F because Ctrl+F is already used for tertiary holstering. The controls tab preserves its Command type when rebinding. It is deliberately excluded from the shared network key-function map, whose sorted numeric IDs must match the server; updating only the client must not renumber movement commands. It opens and focuses search from the viewport or message input; Escape closes search and returns to typing. The tooltip displays the current binding. The virtualized log uses one native scrollbar as its sole position source. Wheel input and scrollbar dragging invalidate the visible message window before its anchor is measured; no hidden scrollbar, per-frame position mirroring or delayed bottom-snap queue remains. A wheel step advances approximately four ordinary message lines. The latest-message button overlays the log instead of changing its viewport height.

Chat uses its own glass plane when display simulation is enabled, without a bright glass reflection over messages. Bottom HUD groups and status indicators receive no overlay. Raster darkening is reduced while animated signal grain, palette, intensity, pitch, chat haze and menu-effect preferences remain active.

## Item storage

Native storage windows use palette-aware cells, item silhouettes, sidebars, close/back controls and a thin outer frame in CRT mode. The original textures still determine measurement and hit geometry; item rendering, marks, saved-location highlights, dragging and nested-storage navigation keep their existing paths. Other HUD themes and CRT-disabled mode retain the native textures. The complete storage image uses the same CRT capture and menu preferences.

Active-screen slot containers are registered before the hotbar reload rebuilds inventory slots. The equipment controller also binds the active drawer before opening, populates it from the current inventory data and explicitly changes panel/container visibility as one view. No child visibility callback changes the visibility of its ancestor. First opening invalidates the equipment scroll measurement, and teardown closes the actual owning view even after it stops being the active screen.

The reported crash reaches the occupied-slot assertion in `RobustToolbox` during `ResetPredictedEntities`. No engine fix is included: the attempted dependency edit was withdrawn because repository instructions require explicit scope covering that dependency. The crash remains unresolved; styling changes do not establish a fix for container prediction.

## Radial and context menus

Radial sector fills, borders and separators resolve the current CRT palette during drawing. The central close/back control uses a matching circular surface and line glyph. Native sector geometry, icons, hover targets, layer navigation and emote dispatch remain unchanged; disabling CRT restores the authored colours and central textures.

Context popups and their entity, verb, confirmation and disabled rows join the shared CRT stylesheet. Rows use a flat fill and an active side rail; nested-menu arrows follow the palette and confirmation rows retain a danger colour. Fixed content margins prevent text shifting when hovered. Text selectors account for the actual intermediate `BoxContainer` in each row.

The changelog no longer selects the old `Interface` stylesheet. Its frame, heading, tabs and footer inherit the live UI setup stylesheet, including changes made while the window is open. Side status indicators draw only their native icons, values and cooldowns, with no panel or cell frame on either side of the screen.

The Shift-click examine popup uses the shared CRT display panel, heading/body colours and styled examine buttons, including hover, pressed and disabled states. It uses a mixed-case Noto heading/body font through the existing UI font mechanism, with 1.25 body line spacing, a separate header and ten-pixel padding. Its display effect follows menu-effect, intensity and pitch preferences at a local intensity scale of 0.45. The legacy grey tint is a non-CRT stylesheet fallback rather than a control override. The body remains the first panel child, preserving the existing examine-update hierarchy; native portrait, description and verbs keep their existing paths.

## Effect coverage

Full-image capture covers ordinary and FancyWindow chrome/content, settings, contextual and examine popups, equipment, storage, chat and radial menus. Text and icons participate in the same refraction as their backgrounds. Transparent pixels remain transparent. Window decoration is installed once outside drawing; storage, chat and radial menu control trees are retained.

Standalone authored terminal/vendor captures retain their own effects. Inside a captured native window, the enclosing display pass supplies the effect once for the entire image.
