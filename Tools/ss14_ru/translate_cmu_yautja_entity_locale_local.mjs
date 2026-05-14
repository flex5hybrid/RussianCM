import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const root = "Resources/Locale/ru-RU/_CMU14/Entities/Yautja";

const exactNames = new Map([
  ["Predalien Larva", "личинка предалиена"],
  ["Abomination", "Абоминация"],
  ["CMUYautjaMask", "биомаска"],
  ["Yautja hunting gear rack", "стойка охотничьего снаряжения яутжа"],
  ["Yautja encryption key", "ключ шифрования яутжа"],
  ["Yautja overseer encryption key", "ключ шифрования надзирателя яутжа"],
  ["bad blood encryption key", "ключ шифрования дурной крови"],
  ["Hellhound Observation Pad", "пульт наблюдения адских гончих"],
  ["YM4 pattern clan armor", "клановая броня образца YM4"],
  ["YM4 pattern clan greaves", "клановые поножи образца YM4"],
];

const exactDescriptions = new Map([
  ["An ancient Yautja hunting-ground structure.", "Древняя постройка охотничьих угодий яутжа."],
  ["A beautifully designed alien hunting mask, both ornate and functional.", "Изысканная чужая охотничья маска, одновременно церемониальная и практичная."],
  ["A towering hybrid xenomorph born from dishonored Yautja blood.", "Громадный гибрид ксеноморфа, рождённый из обесчещенной крови яутжа."],
  ["Ancient armor made from a strange alloy. It feels cold with an alien weight.", "Древняя броня из странного сплава. От неё веет чужим холодом и тяжестью."],
  ["A suit of armor made entirely out of stone. It looks incredibly heavy.", "Комплект брони, полностью сделанный из камня. Выглядит невероятно тяжёлым."],
  ["A suit of armor with light padding. It looks old, yet functional.", "Комплект брони с лёгкой подкладкой. Он выглядит старым, но исправным."],
  ["A human-sized suit of alien thrall armor.", "Человеческого размера комплект чужой брони раба."],
  ["A suit of armor with heavy padding. It looks old, yet functional.", "Комплект брони с тяжёлой подкладкой. Он выглядит старым, но исправным."],
  ["A suit of Yautja armor rebuilt from scavenged and mismatched plates.", "Комплект брони яутжа, собранный из добытых и разномастных пластин."],
  ["A set of very fine chainlink in a meshwork for comfort and utility.", "Очень тонкая кольчужная сетка, сделанная для удобства и практичности."],
  ["A strange alloy weave in the form of a vest. It feels cold with an alien weight.", "Жилет из плетения странного сплава. От него веет чужим холодом и тяжестью."],
  ["A light alien mesh undersuit sized for a human thrall.", "Лёгкий чужой сетчатый поддоспешник, рассчитанный на человеческого раба."],
  ["Greaves made from scraps of cloth and a strange alloy. They feel cold with an alien weight.", "Поножи из обрывков ткани и странного сплава. От них веет чужим холодом и тяжестью."],
  ["Heavy stone greaves balanced for hunters who can carry the impossible.", "Тяжёлые каменные поножи, сбалансированные для охотников, способных нести невозможное."],
  ["A pair of armored, perfectly balanced boots. Ideal for running through the jungle.", "Пара бронированных, идеально сбалансированных сапог. Отлично подходят для бега по джунглям."],
  ["Human-sized alien greaves for a Yautja thrall.", "Чужие поножи человеческого размера для раба яутжа."],
  ["A battle-worn cape passed down by elder Yautja.", "Потрёпанная в боях накидка, передаваемая старейшинами яутжа."],
  ["Bright blue Yautja dissolving gel, eating through field equipment.", "Ярко-синий растворяющий гель яутжа, разъедающий полевое снаряжение."],
  ["A heavy wrist computer packed with alien hunting tools.", "Тяжёлый наручный компьютер, набитый чужими охотничьими инструментами."],
  ["A simplified alien bracer that binds a thrall to a Yautja hunter.", "Упрощённый чужой наруч, связывающий раба с охотником-яутжа."],
  ["A full hunting bracer entrusted to a blooded thrall.", "Полноценный охотничий наруч, доверенный окровлённому рабу."],
  ["A small case containing a thrall bracer and emergency Yautja medical supplies.", "Небольшой футляр с наручем раба и аварийными медикаментами яутжа."],
  ["An alien hunting mask with a built-in tactical visor.", "Чужая охотничья маска со встроенным тактическим визором."],
  ["A compact identification wafer deployed from a Yautja bracer.", "Компактная идентификационная пластина, выдвигаемая из наруча яутжа."],
  ["A bracer-grown crystal that rapidly stabilises alien biology.", "Кристалл, выращенный наручем и быстро стабилизирующий чужую биологию."],
  ["A bracer-grown crystal tuned for humans and thralls.", "Кристалл, выращенный наручем и настроенный на людей и рабов."],
  ["A disposable bracer-fabricated capsule of emergency predator medicine.", "Одноразовая аварийная лечебная капсула, напечатанная наручем."],
  ["A small ceremonial ornament that clips into a Yautja bio-mask.", "Небольшое церемониальное украшение, крепящееся к биомаске яутжа."],
  ["A compact field harness tuned for Yautja optical camouflage.", "Компактный полевой генератор, настроенный на оптическую маскировку яутжа."],
  ["A razor-edged Yautja throwing disc keyed to its owner.", "Метательный диск яутжа с бритвенной кромкой, привязанный к владельцу."],
  ["A compact alien plasma weapon that draws power from a hunting bracer.", "Компактное чужое плазменное оружие, питающееся от охотничьего наруча."],
  ["Retractable twin blades keyed to a hunting bracer.", "Парные выдвижные клинки, привязанные к охотничьему наручу."],
  ["A long alien hunting blade with a brutal monomolecular edge.", "Длинный чужой охотничий клинок с жестокой мономолекулярной кромкой."],
  ["A heavy gauntlet weapon with compact linked chain drives.", "Тяжёлое оружие-перчатка с компактными цепными приводами."],
  ["A compact hard-light shield projected from the hunting bracer.", "Компактный щит из твёрдого света, проецируемый охотничьим наручем."],
  ["A compact alien poultice for closing savage wounds.", "Компактная чужая припарка для закрытия тяжёлых ран."],
  ["A cool alien salve for sealing burns and caustic injuries.", "Прохладная чужая мазь для заживления ожогов и кислотных повреждений."],
  ["A dense alien coagulant that knits together broad trauma.", "Плотный чужой коагулянт, стягивающий обширные травмы."],
  ["A fast-setting alien gel for buying time through shock and blood loss.", "Быстро схватывающийся чужой гель, помогающий выиграть время при шоке и кровопотере."],
  ["A predator surgical clamp made to seal catastrophic bleeding.", "Хирургический зажим хищника для остановки катастрофического кровотечения."],
  ["A compact alien applicator for rapid emergency treatment.", "Компактный чужой аппликатор для быстрой экстренной помощи."],
  ["A bio-scanner tuned for alien physiology and battlefield triage.", "Биосканер, настроенный на чужую физиологию и полевую сортировку раненых."],
  ["An alien autoinjector loaded with a strong trauma and burn treatment cocktail.", "Чужой автоинъектор, заряженный сильным коктейлем против травм и ожогов."],
  ["A small case packed with Yautja trauma poultices and burn salves.", "Небольшой футляр с травматологическими припарками и мазями яутжа от ожогов."],
  ["A predator compact medical kit stocked for solo field recovery.", "Компактная аптечка хищника для одиночного восстановления в поле."],
  ["A restricted event rack for alien hunter loadout adjustments.", "Закрытая событийная стойка для настройки снаряжения чужих охотников."],
  ["An alien cipher chip tuned to hunter communicators.", "Чужой шифровальный чип, настроенный на коммуникаторы охотников."],
  ["An alien cipher chip tuned to hunter and overseer channels.", "Чужой шифровальный чип, настроенный на каналы охотников и надзирателей."],
  ["An alien cipher chip tuned to renegade hunter channels.", "Чужой шифровальный чип, настроенный на каналы охотников-изгоев."],
  ["A compact alien communicator used by Yautja hunters.", "Компактный чужой коммуникатор охотников-яутжа."],
  ["A Yautja hunting pouch made from thick tanned hide, used for devices, tools, and trophies.", "Охотничий подсумок яутжа из толстой дублёной кожи для устройств, инструментов и трофеев."],
  ["A device covered in sacred text. It whirrs and beeps every couple of seconds.", "Устройство, покрытое священными письменами. Оно жужжит и пищит каждые несколько секунд."],
  ["A small vial of bright blue dissolving gel for destroying loose field equipment.", "Небольшой флакон ярко-синего растворяющего геля для уничтожения полевого снаряжения."],
  ["A grim trophy from a fallen prey.", "Мрачный трофей с павшей добычи."],
  ["A prying tool made from an unusual alien alloy.", "Монтировка из необычного чужого сплава."],
  ["A wrench with many common uses, made from bizarre alien bone and alloy.", "Универсальный ключ из причудливой чужой кости и сплава."],
  ["Alien wirecutters shaped with razor-sharp teeth.", "Чужие кусачки с лезвиями, похожими на острые зубы."],
  ["A high-precision alien screwdriver with a wicked point.", "Высокоточная чужая отвёртка с зловещим остриём."],
  ["Top-notch alien tech for opening and reconfiguring machines.", "Первоклассная чужая технология для вскрытия и перенастройки машин."],
  ["A complex chemical welding device. Keep it away from youngbloods.", "Сложное химическое сварочное устройство. Держите подальше от молодокровных."],
  ["A modular alien utility belt with clips for field tools.", "Модульный чужой пояс с креплениями для полевых инструментов."],
  ["Heavy alien chains with sharpened links and a brutal weld pattern.", "Тяжёлые чужие цепи с заточенными звеньями и грубым сварным узором."],
  ["A portable alien camera console used for remotely overwatching hellhounds.", "Портативная чужая консоль камер для удалённого наблюдения за адскими гончими."],
  ["A bad blood device used to break a xenomorph's hivemind link and enthrall the serpent.", "Устройство дурной крови для разрыва связи ксеноморфа с разумом улья и порабощения змея."],
  ["A cleaned skull taken as a Yautja hunting trophy.", "Очищенный череп, взятый как охотничий трофей яутжа."],
  ["A cleaned ribcage taken as a Yautja hunting trophy.", "Очищенная грудная клетка, взятая как охотничий трофей яутжа."],
  ["A resin-scarred alien skull taken from a fallen serpent.", "Покрытый следами смолы чужой череп, взятый с павшего змея."],
  ["A strip of alien hide cured as a hunter's trophy.", "Полоса выделанной чужой шкуры, подготовленная как трофей охотника."],
  ["A grisly sheet of skin removed during Yautja butchery.", "Жуткий пласт кожи, снятый во время разделки яутжа."],
  ["A stripped spine taken from a butchered body.", "Очищенный позвоночник, взятый из разделанного тела."],
  ["A heap of remains left by ritual Yautja butchery.", "Куча останков после ритуальной разделки яутжа."],
  ["A heap of acid-stained remains left by ritual Yautja butchery.", "Куча покрытых кислотой останков после ритуальной разделки яутжа."],
  ["A ceremonial Yautja belt for displaying claimed trophies and ritual victories.", "Церемониальный пояс яутжа для демонстрации добытых трофеев и ритуальных побед."],
  ["A finely woven cloth used by Yautja to clean and polish bone trophies.", "Тонко сотканная ткань, которой яутжа чистят и полируют костяные трофеи."],
  ["A compact alien snare that clamps onto living prey when armed.", "Компактная чужая ловушка, захлопывающаяся на живой добыче после взведения."],
]);

const phraseNames = [
  ["bad blood", "дурной крови"],
  ["alternate", "альтернативный"],
  ["ancient", "древний"],
  ["alien", "чужой"],
  ["bio-mask", "биомаска"],
  ["mask", "маска"],
  ["armor", "броня"],
  ["greaves", "поножи"],
  ["shield", "щит"],
  ["clan", "клановый"],
  ["thrall", "раб"],
  ["blooded", "окровлённый"],
  ["hunter", "охотник"],
  ["hunting", "охотничий"],
  ["bracer", "наруч"],
  ["smart disc", "умный диск"],
  ["plasma caster", "плазмомёт"],
  ["wrist blades", "запястные клинки"],
  ["combi scimitar", "комби-скимитар"],
  ["chain gauntlet", "цепная перчатка"],
  ["healing", "лечебный"],
  ["stabilizer", "стабилизатор"],
  ["stabilising", "стабилизирующий"],
  ["crystal", "кристалл"],
  ["capsule", "капсула"],
  ["ornament", "украшение"],
  ["field harness", "полевой генератор"],
  ["trauma pack", "травматологический пакет"],
  ["ointment", "мазь"],
  ["gel vial", "флакон геля"],
  ["gel", "гель"],
  ["wound clamp", "раневой зажим"],
  ["gun", "пистолет"],
  ["health analyzer", "анализатор здоровья"],
  ["autoinjector", "автоинъектор"],
  ["herbs case", "футляр с травами"],
  ["medicomp", "медикомп"],
  ["communicator", "коммуникатор"],
  ["overseer", "надзиратель"],
  ["stranded", "застрявший"],
  ["pouch", "подсумок"],
  ["relay beacon", "релейный маяк"],
  ["cleanser", "очиститель"],
  ["scalp", "скальп"],
  ["crowbar", "монтировка"],
  ["wrench", "ключ"],
  ["wirecutters", "кусачки"],
  ["screwdriver", "отвёртка"],
  ["multitool", "мультитул"],
  ["welding tool", "сварочный инструмент"],
  ["toolbelt", "пояс для инструментов"],
  ["metal chains", "металлические цепи"],
  ["hivebreaker", "разрушитель улья"],
  ["spawner", "спавнер"],
  ["dummy", "манекен"],
  ["skull", "череп"],
  ["pelt", "шкура"],
  ["ribcage", "грудная клетка"],
  ["bone", "кость"],
  ["left", "левая"],
  ["right", "правая"],
  ["arm", "рука"],
  ["hand", "кисть"],
  ["leg", "нога"],
  ["foot", "стопа"],
  ["trophy", "трофей"],
  ["belt", "пояс"],
  ["polishing rag", "полировальная ткань"],
  ["hunting trap", "охотничья ловушка"],
  ["battle-worn", "боевой"],
  ["cape", "накидка"],
  ["poncho", "пончо"],
  ["body mesh", "нательная сетка"],
  ["mesh suit", "сетчатый костюм"],
  ["chainshirt", "кольчужная рубаха"],
  ["stone", "каменный"],
  ["heavy", "тяжёлый"],
  ["patchwork", "лоскутный"],
  ["lunatic", "безумный"],
  ["scavenger", "мусорщицкий"],
  ["scav", "мусорщицкий"],
  ["venator", "венаторский"],
  ["commando", "коммандос"],
  ["bane", "гибельный"],
  ["emissary", "эмиссарский"],
  ["classic", "классический"],
  ["desert", "пустынный"],
  ["jungle", "джунглевый"],
  ["snow", "снежный"],
  ["urban", "городской"],
  ["elite", "элитный"],
  ["cleopatra", "клеопатра"],
  ["plated", "пластинчатый"],
  ["legacy", "наследный"],
  ["collector", "коллекционер"],
  ["dragon", "дракон"],
  ["enforcer", "каратель"],
  ["swamp", "болотный"],
  ["bronze", "бронзовый"],
  ["silver", "серебряный"],
  ["crimson", "багровый"],
  ["ebony", "эбеновый"],
  ["bone", "костяной"],
  ["blue", "синий"],
  ["redglow", "красное свечение"],
  ["white", "белый"],
]);

function translateName(value) {
  if (exactNames.has(value)) {
    return exactNames.get(value);
  }

  let output = value;
  if (/^yautja\s+/i.test(output)) {
    output = output.replace(/^yautja\s+/i, "") + " yautja";
  }

  for (const [source, target] of phraseNames) {
    output = output.replace(new RegExp(`\\b${source.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\b`, "gi"), target);
  }

  output = output.replace(/\byautja\b/gi, "яутжа");
  output = output.replace(/\s+/g, " ").trim();
  return output;
}

function translateDescription(value) {
  if (exactDescriptions.has(value)) {
    return exactDescriptions.get(value);
  }

  if (value.includes("Yautja hunting trophy")) {
    return value
      .replace("A cleaned arm bone taken as a Yautja hunting trophy.", "Очищенная кость руки, взятая как охотничий трофей яутжа.")
      .replace("A cleaned hand bone taken as a Yautja hunting trophy.", "Очищенная кость кисти, взятая как охотничий трофей яутжа.")
      .replace("A cleaned leg bone taken as a Yautja hunting trophy.", "Очищенная кость ноги, взятая как охотничий трофей яутжа.")
      .replace("A cleaned foot bone taken as a Yautja hunting trophy.", "Очищенная кость стопы, взятая как охотничий трофей яутжа.");
  }

  return `Предмет или постройка яутжа: ${translateName(value.toLowerCase())}.`;
}

let translated = 0;

for (const file of readdirSync(root).filter((name) => name.endsWith(".ftl"))) {
  const path = join(root, file);
  const lines = readFileSync(path, "utf8").split(/\r?\n/);
  for (let i = 0; i < lines.length; i++) {
    let match = lines[i].match(/^(ent-[A-Za-z0-9_.-]+ = )(.*)$/);
    if (match && match[2].trim()) {
      const next = translateName(match[2]);
      if (next !== match[2]) {
        lines[i] = `${match[1]}${next}`;
        translated++;
      }
      continue;
    }

    match = lines[i].match(/^(\s+\.desc = )(.*)$/);
    if (match && match[2].trim()) {
      const next = translateDescription(match[2]);
      if (next !== match[2]) {
        lines[i] = `${match[1]}${next}`;
        translated++;
      }
    }
  }

  writeFileSync(path, `${lines.join("\n").replace(/\n+$/u, "")}\n`, "utf8");
}

console.log(`Locally translated CMU Yautja entity entries=${translated}`);
