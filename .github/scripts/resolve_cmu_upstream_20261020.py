#!/usr/bin/env python3
"""Deterministic resolution of the 2026-10-20 CMU upstream merge conflicts.

RuCMU-specific gameplay parameters and Russian translations are preserved.
Upstream functional code changes and newly introduced guide sections are included.
Full untouched conflict blocks are preserved in the workflow artifact.
"""
import re
from pathlib import Path

ROOT = Path(".")
PATTERN = re.compile(r"(?ms)^<<<<<<< HEAD\r?\n(.*?)^=======\r?\n(.*?)^>>>>>>> [^\r\n]+(?:\r?\n|$)")

HAZOPS = "Content.CMU/Resources/Prototypes/CMU14/Economy/Vendors/hazopsvendors.yml"
AMBASSADOR = "Content.CMU/Resources/Prototypes/CMU14/Entities/Structures/Machines/ambassador_consoles.yml"
LOGISTICS = "Content.CMU/Resources/Prototypes/CMU14/Roles/Military/GovFor/Platoon/Command/Officer_Logistics.yml"
EXPLOSIVES = "Content.CMU/Resources/ServerInfo/Guidebook/CMU14/Guides/CMUExplosives.xml"
CALLSIGN = "Content.CMU/Server/Callsigns/AU14CallsignSystem.cs"
ROLE = "Content.CMU/Shared/Callsigns/AU14CallsignRoleComponent.cs"
CHANGELOG = "Resources/Changelog/CMU.yml"
GUIDE = "Resources/ServerInfo/Guidebook/_RMC14/Guides/RMCGuideRoleSGO.xml"

expected = {HAZOPS: 1, AMBASSADOR: 1, LOGISTICS: 1, EXPLOSIVES: 2,
            CALLSIGN: 1, ROLE: 1, CHANGELOG: 8, GUIDE: 2}

explosive_blocks = [
"""  ## Стрельба из гранатомёта
  - Любую гранату с пометкой о совместимости можно зарядить в гранатомёт.
  - Гранатомёт взводит гранату при выстреле. Большинство гранат взрывается примерно через секунду после выхода из ствола.
  - [bold]M47 HSDP[/bold] — дымовая граната с контактным взрывателем: она срабатывает при достижении точки прицеливания.
  - [bold]M40 HEDP[/bold], [bold]HEFA[/bold] и [bold]M15[/bold] не имеют контактного взрывателя и используют таймер гранатомёта.
""",
"""  - [bold]M40 HEDP[/bold]: стандартная фугасная граната.
  - [bold]HEFA[/bold]: осколочная граната, разбрасывающая большое облако осколков. Смертоносна на открытом месте и опасна для всех поблизости.
  - [bold]M15[/bold]: старая осколочная граната с большим количеством осколков. Её постепенно заменяют более безопасными гранатами серии 30 мм.
  - [bold]Тип 6[/bold]: осколочная граната.
  - [bold]M51A BFAB[/bold]: выпрыгивающая граната. После приземления взведённая граната подскакивает и разбрасывает осколки сверху. Эффективна против солдат за низкими укрытиями. Бросьте взведённую гранату или выстрелите из гранатомёта, чтобы она подскочила в месте приземления.
"""
]

battery_section = """  Дополнительные батареи можно заказать в отделе снабжения ящиком. Извлеките разряженную батарею через [bold]Eject → Power cell[/bold] в контекстном меню оружия и зарядите её в зарядном устройстве. Начальный комплект включает одну батарею внутри оружия и одну запасную.

"""
aim_assist_section = """  ### Помощь в прицеливании:
  Оружие имеет встроенные средства повышения точности и компенсации отдачи. Включите [bold]Aim Assist[/bold], чтобы направлять выстрелы в видимого противника в пределах двух тайлов от курсора. Стрелять по-прежнему нужно самостоятельно.

  Система отдаёт приоритет противникам в сознании, а не находящимся в критическом состоянии. Включённая система распознавания «свой-чужой» исключает союзников. Стены, плотный дым и слепота препятствуют захвату цели.

  Выстрелы с автоматической коррекцией получают [bold]штраф 25% к точности[/bold]. Прямое наведение курсора на цель избавляет от штрафа; наличие шлема на него не влияет. Помощь в прицеливании расходует энергию: полной батареи хватает примерно на [bold]750 выстрелов[/bold], поэтому используйте её по необходимости.

"""

def changelog_last(ours, theirs, whole):
    # Ours has RU-specific entries; theirs continues the shared CMU log.
    marker = "\n- author:"
    if marker not in theirs:
        raise ValueError("Expected upstream changelog entries after the common #2210 entry")
    new = "- author:" + theirs.split(marker, 1)[1]
    existing = [int(n) for n in re.findall(r"(?m)^  id: (\d+)\s*$", whole)]
    next_id = max(existing, default=0) + 1
    entries = re.split(r"(?=^- author:)", new, flags=re.M)
    output = []
    for entry in entries:
        if not entry.strip():
            continue
        entry, count = re.subn(r"(?m)^  id: \d+[ \t]*$", f"  id: {next_id}", entry, count=1)
        if count != 1:
            raise ValueError(f"Upstream changelog entry missing an ID: {entry[:100]}")
        output.append(entry)
        next_id += 1
    if not output:
        raise ValueError("No new upstream changelog entries")
    return ours.rstrip("\n") + "\n" + "".join(output)

total = 0
for relative_path, count in expected.items():
    path = ROOT / relative_path
    original = path.read_text(encoding="utf-8")
    blocks = list(PATTERN.finditer(original))
    if len(blocks) != count:
        raise ValueError(f"{relative_path}: expected {count} blocks, got {len(blocks)}")
    pieces = []
    offset = 0
    for idx, match in enumerate(blocks):
        ours, theirs = match.group(1), match.group(2)
        pieces.append(original[offset:match.start()])
        if relative_path in {HAZOPS, AMBASSADOR, LOGISTICS}:
            resolved = ours
        elif relative_path == EXPLOSIVES:
            resolved = explosive_blocks[idx]
        elif relative_path in {CALLSIGN, ROLE}:
            resolved = theirs
        elif relative_path == GUIDE:
            resolved = (battery_section if idx == 0 else aim_assist_section) + ours
        elif relative_path == CHANGELOG:
            resolved = ours if idx < count - 1 else changelog_last(ours, theirs, original)
        else:
            raise AssertionError(relative_path)
        pieces.append(resolved)
        offset = match.end()
        total += 1
    pieces.append(original[offset:])
    updated = "".join(pieces)
    if re.search(r"(?m)^(<<<<<<<|=======|>>>>>>>)", updated):
        raise ValueError(f"Conflict markers remain in {relative_path}")
    path.write_text(updated, encoding="utf-8", newline="")
    print(f"Resolved {relative_path}: {count} blocks")

assert total == 17, total
# Every upstream entry that was absent in RuCMU should have a unique changelog id.
text = (ROOT / CHANGELOG).read_text(encoding="utf-8")
ids = [int(x) for x in re.findall(r"(?m)^  id: (\d+)\s*$", text)]
duplicates = sorted({x for x in ids if ids.count(x) > 1})
if duplicates:
    raise ValueError(f"Duplicate changelog IDs: {duplicates}")
print(f"Resolved {total} conflict blocks in {len(expected)} files; changelog IDs unique")
