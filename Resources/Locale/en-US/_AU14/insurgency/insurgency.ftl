# INSFOR faction featureset

# Shown to each member when their faction definition is applied for the round.
insfor-faction-applied-popup = Your cell has been organized under { $title }.

# Debug apply command feedback.
cmd-insforapplytest-desc = Applies a minimal test INSFOR faction so the apply pipeline can be checked in-game.
cmd-insforapplytest-help = Usage: insforapplytest [title]
cmd-insforapplytest-applied = Applied test INSFOR faction "{ $title }" to { $count } member(s).

# A Package loadout delivery.
insfor-a-package-received = You have received a package. Use it in hand when you are ready.

# Heavy Cell Kit deployment.
insfor-cell-kit-title = Heavy Cell Kit
insfor-cell-kit-deploy = Deploy
insfor-cell-kit-no-faction = The cell has no orders yet. Wait until your faction is organized.
insfor-cell-kit-empty = The cell kit is empty.
insfor-cell-kit-deployed = You set out a piece of the cell's equipment. { $remaining } left.

# Leader faction selection popup.
insfor-select-title = Choose Your Cell's Faction
insfor-select-default-header = Default Factions
insfor-select-custom-header = Custom Factions
insfor-select-custom-refresh = Refresh
insfor-select-govfor = Opposing GOVFOR faction: { $name }
insfor-select-govfor-unknown = Opposing GOVFOR faction: not chosen yet
insfor-select-empty = No Default factions are available.
insfor-select-not-opposed = Does not oppose this round's GOVFOR faction.
insfor-select-custom-locked = You are not authorized to use Custom factions.
insfor-select-custom-empty = You have no custom factions saved on this machine.
insfor-select-choose = Choose

# In-viewport button to reopen the selection popup after it was closed.
insfor-reopen-faction-select-button = Choose Faction

# Faction reveal popup, shown to members once a faction is applied.
insfor-reveal-title = Your Faction
insfor-reveal-untitled = Unnamed Cell
insfor-reveal-roleplay-header = How to play this faction
insfor-reveal-about-header = About
insfor-reveal-close = Got it

# Faction editor pickers.
insfor-picker-search = Search...
insfor-picker-entity-title = Select an entity
insfor-picker-job-title = Select a job
insfor-picker-platoon-title = Select a GOVFOR faction (platoon)
insfor-picker-icon-title = Select a status icon
insfor-picker-flag-title = Select a flag

# Marker job used only as an INSFOR editor whitelist key.
au14-job-name-insfor-editor = INSFOR Editor Access

# INSFOR faction editor help window.
insfor-editor-help-title = INSFOR Faction Editor - Help
insfor-editor-help-intro = An INSFOR faction is one insurgent cell the CLF leader can pick after spawning. You fill in who they are, what money buys them points, what their leader's Heavy Cell Kit can drop, and what each role gets in their "A Package". Nothing here needs a prototype id typed by hand: every entity, job, and icon is chosen from a searchable picker. The server re-checks and clamps everything you save, so you cannot break the round with a bad value.

insfor-editor-help-list-heading = The faction list (left) and the  *  mark
insfor-editor-help-list-body = The left column lists every saved faction plus the built-in vanilla CLF at the top. A faction shows a  *  next to its name when it is set to oppose the GOVFOR side the current round rolled, i.e. it is a valid pick this round. No star just means it does not target this round's GOVFOR; it is still fine to edit. Click a faction to edit it, or New faction to start blank.

insfor-editor-help-identity-heading = Identity
insfor-editor-help-identity-body = Title: the faction's name, shown in the pick list and the reveal popup.
    Recruited message: the briefing a freshly recruited member reads (for example via the tattoo gun). Blank uses the default CLF line.
    Description / Roleplay style: shown in the antag briefing and the reveal popup so members know who they are and how they are meant to play.
    Flag entity: an in-world flag prop, picked from the catalog (optional).
    Status icon: the faction membership icon members show to each other, picked from the icon list.

insfor-editor-help-default-heading = Default faction (checkbox)
insfor-editor-help-default-body = On: this faction is host-authored and saved in the server database; it is offered to leaders whose GOVFOR matches the Opposed list below. Off: it is a personal/Custom faction. The Save buttons at the bottom control where it is written.

insfor-editor-help-opposed-heading = Opposed GOVFOR factions
insfor-editor-help-opposed-body = The GOVFOR platoons (USMC, TWE RMC, UPP, and so on) this faction is allowed to oppose. If the round's GOVFOR is in this list, the faction is offered to the leader and gets the  *  in the list. Add as many as you like.

insfor-editor-help-economy-heading = Economy - dollars to points
insfor-editor-help-economy-body = Dollars to points rate: how intel dollars convert to the cell's vendor points.
    Also accept plain dollars: when ticked, cash still converts at the analyzer even if you add custom submittables below. Untick it for a faction whose economy should ignore money entirely.

insfor-editor-help-analyzer-heading = Analyzer - submittable for points
insfor-editor-help-analyzer-body = What the analyzer machine accepts and turns into cell points, beyond plain cash. Each row is an item (picked, never typed) and a ratio with two modes:
      - items per point: it takes that many of the item to make one point (good for cheap goods).
      - points per item: one item is worth that many points (good for valuable goods).
    Leave the list empty to keep the plain-dollars behavior. The value is always at least 1 so a submission can never mint free points.

insfor-editor-help-machines-heading = Default cell-kit machines
insfor-editor-help-machines-body = Tick the well-known CLF machines (analyzer, intel computer, objectives console, tech tree console, fax) you want the leader's Heavy Cell Kit to be able to place. Their money-to-points wiring is the normal CLF behavior; no extra setup is needed.

insfor-editor-help-placeables-heading = Cell kit - other placeable entities
insfor-editor-help-placeables-body = Any additional single entities the leader can free-place from the Heavy Cell Kit (lamps, barricades, props, and so on). Each is picked from the entity picker.

insfor-editor-help-vendors-heading = Cell kit - vendors
insfor-editor-help-vendors-body = Each vendor the leader can deploy from the kit. Per vendor:
      - Vendor name: the name shown on the deployed vendor and in the kit list.
      - Base model: an existing vendor entity used only for its sprite/collision; its arsenal is replaced by your sections.
      - Wrenchable: can be wrenched down and moved after placing.
      - Invulnerable: the placed vendor will not break or change on damage.
      - Uses cell intel points: items are paid from the cell's shared intel points (money at the intel computer stocks it) instead of the buyer's own points.
      - Use base model's own arsenal: ignore the sections below and keep the base entity's built-in stock. Only for reusing a fully-made vendor (like the CLF requisitions rack); leave off for a normal custom vendor.

insfor-editor-help-vendor-items-heading = Vendor sections and items
insfor-editor-help-vendor-items-body = A vendor is split into sections (categories). Per section:
      - Section name.
      - Category limit: two optional caps - how many one player may take from this category, and how many all players together may.
    Inside a section, each item row is:
      - the entity (picked),
      - points: its cost (0 = free),
      - amount: how many are in stock,
      - max: the ceiling it restocks to.
    Leave points blank to make an item free-by-stock only.

insfor-editor-help-loadouts-heading = Role loadouts - A Package
insfor-editor-help-loadouts-body = Because the faction is chosen after players spawn, each role's kit is delivered afterwards as an "A Package" box. Add a row per role: pick the Role (job) and the Contents (entities) it hands out.

insfor-editor-help-saving-heading = Saving and applying
insfor-editor-help-saving-body = Save (server / Default): writes it to the server database as a host faction.
    Save as local Custom: writes it to your machine only, so it shows up in the leader's Custom list.
    Apply for round: immediately applies this faction to the current round's cell.
    Delete: removes a saved faction (the built-in vanilla CLF cannot be deleted).
