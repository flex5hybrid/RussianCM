cmu-criminal-records-tab-records = ДОСЬЕ

cmu-criminal-records-tab-firearms = ОРУЖИЕ

cmu-firearm-registry-subheading = За этот раунд через снабжение колонии { $count ->
    [one] приобретена { $count } единица огнестрельного оружия
    [few] приобретены { $count } единицы огнестрельного оружия
   *[other] приобретено { $count } единиц огнестрельного оружия
}

cmu-firearm-registry-footer = Оружие, приобретённое через АСИП колонии, с указанием заказчиков. Примените к консоли стреляную гильзу, оружие или криминалистический сканер, только что просканировавший их, чтобы сравнить след бойка.

cmu-firearm-registry-search-placeholder = Поиск по номеру, следу, оружию или покупателю...

cmu-firearm-registry-empty = Через снабжение колонии пока не приобретено ни одного огнестрельного оружия.

cmu-firearm-registry-no-match = Оружие по этому запросу не найдено.

cmu-firearm-registry-serial = Серийный номер

cmu-firearm-registry-signature = След

cmu-firearm-registry-buyer = Заказчик

cmu-firearm-registry-order = Заказ

cmu-firearm-registry-time = Доставлено

cmu-firearm-registry-ordered-by-them = Заказанное оружие

cmu-firearm-registry-owned-line = { $name } ({ $serial })

cmu-firearm-registry-compare-match = Баллистическое совпадение: { $name } ({ $serial }), заказчик — { $buyer }.

cmu-firearm-registry-compare-none = Зарегистрированного оружия со следом { $signature } нет.

cmu-records-photo-name = фотография: {$name}

cmu-records-photo-description = Возраст: {$age}, пол: {$gender}, рост: {$height}.

cmu-records-photo-printed = Консоль печатает фотографию.

cmu-records-print-photo = ПЕЧАТЬ ФОТО

cmu-records-no-photo = В ДЕЛЕ НЕТ ФОТОГРАФИИ

cmu-records-age = Возраст

cmu-records-gender = Пол

cmu-records-height = Рост

cmu-records-warrants = Ордера

cmu-records-warrants-none = Действующих ордеров нет.

cmu-records-warrant-issue = ВЫДАТЬ ОРДЕР

cmu-records-warrant-revoke = ОТОЗВАТЬ

cmu-records-warrant-prompt = Обвинение

cmu-records-warrant-line = {$time} // ВЫДАЛ: {$officer}

cmu-records-arrests = История арестов

cmu-records-arrests-none = Арестов не зарегистрировано.

cmu-records-arrest-log = ЗАРЕГИСТРИРОВАТЬ АРЕСТ

cmu-records-arrest-charges-prompt = Обвинения

cmu-records-arrest-sentence-prompt = Срок заключения (минуты)

cmu-records-arrest-line = {$time} // {$officer} // { $minutes ->
    [0] без заключения
    [one] {$minutes} минута
    [few] {$minutes} минуты
   *[other] {$minutes} минут
}

cmu-records-personnel = ЛИЧНОЕ ДЕЛО

cmu-records-personnel-none = Личного дела нет: колония не располагает сведениями об этом человеке.

cmu-records-gender-male = Мужской

cmu-records-gender-female = Женский

cmu-records-gender-epicene = Не указан

cmu-records-gender-neuter = Не указан

cmu-records-print-record = ПЕЧАТЬ ДОСЬЕ

cmu-records-print-printed = Консоль печатает досье.

cmu-records-print-name = уголовное досье ({$name})

cmu-records-print-warrant = {$time}: {$charge} (выдал: {$officer})

cmu-records-print-arrest = {$time}: {$charges}, { $minutes ->
    [0] без заключения
    [one] {$minutes} минута
    [few] {$minutes} минуты
   *[other] {$minutes} минут
} (арестовал: {$officer})

cmu-records-print-history = {$time}: {$crime}

cmu-records-print-content = [head=2]УГОЛОВНОЕ ДОСЬЕ[/head]
    {"["}bold]{$name}[/bold], {$job}

    Возраст: {$age}
    Пол: {$gender}
    Рост: {$height}
    Отпечатки пальцев: {$prints}
    ДНК: {$dna}

    {"["}bold]Статус:[/bold] {$status}
    Причина: {$reason}

    {"["}bold]Ордера[/bold]
    {$warrants}

    {"["}bold]История арестов[/bold]
    {$arrests}

    {"["}bold]История преступлений[/bold]
    {$history}

ent-CMUSuspectPhoto = фотография подозреваемого
    .desc = Фотография, напечатанная из базы уголовных досье.

cmu-criminal-records-heading = КРИМИНАЛЬНЫЕ РЕКОРДЫ

cmu-criminal-records-subheading = { $count ->
    [one] ДАННЫЕ БЕЗОПАСНОСТИ // 1 ПРИЗНАНИЕ
   *[other] ДАННЫЕ БЕЗОПАСНОСТИ // { $count } Упоминается
}

cmu-criminal-records-access-granted = Доступ гарантирован

cmu-criminal-records-access-read-only = ТОЛЬКО ЧТЕНИЕ

cmu-criminal-records-search = Поисковые запросы

cmu-criminal-records-search-placeholder = Тип для поиска...

cmu-criminal-records-show = показывать

cmu-criminal-records-show-all = Все статусы

cmu-criminal-records-none = Никаких документов не найдено

cmu-criminal-records-select = Выбрать рекорд

cmu-criminal-records-particulars = Особенности

cmu-criminal-records-fingerprints = Отпечатки пальцев

cmu-criminal-records-dna = ДНК

cmu-criminal-records-religion = Религия

cmu-criminal-records-squad = Отряд

cmu-criminal-records-bounty = Награда

cmu-criminal-records-set-by = Садись { $name }

cmu-criminal-records-set-status = Установить статус

cmu-criminal-records-no-access = Ваш ID не имеет доступа к записи об изменениях.

cmu-criminal-records-add-entry = ВХОД

cmu-criminal-records-delete = Удалить

cmu-criminal-records-delete-confirm = Утверждение

cmu-criminal-records-footer-edit = Разыскиваемые, подозрительные и враждебные статусы просят о причине. Записи истории маркированы по времени.

cmu-criminal-records-footer-read-only = Только для чтения: проведите ID с доступом к безопасности для внесения изменений.
