cmu-medical-scanner-body-map-header        = Карта тела
cmu-medical-scanner-pulse-label            = Пульс:
cmu-medical-scanner-blood-label            = Кровь:
cmu-medical-scanner-body-parts-header      = Части тела
cmu-medical-scanner-organs-header          = Органы
cmu-medical-scanner-fractures-header       = Переломы
cmu-medical-scanner-bleeds-header          = Внутреннее кровотечение
cmu-medical-scanner-pulse-stopped          = [color=red][bold]Нет пульса - сердце остановилось[/bold][/color]
cmu-medical-scanner-pulse-bpm              = { $bpm } уд/мин
cmu-medical-scanner-part-line              = { $part }: { $current }/{ $max } HP
cmu-medical-scanner-part-suffix-splinted   = (шина)
cmu-medical-scanner-part-suffix-cast       = (в гипсе)
cmu-medical-scanner-part-suffix-wounds     = ({ $count } { $count ->
    [one] рана
    [few] раны
   *[other] ран
})
cmu-medical-scanner-organ-line             = { $organ }: { $stage } ({ $current }/{ $max })
cmu-medical-scanner-organ-removed          = { $organ }: [color=red]УДАЛЁН[/color]
cmu-medical-scanner-fracture-line-exact    = { $part }: перелом { $severity }
cmu-medical-scanner-fracture-line-vague    = { $part }: обнаружен перелом
cmu-medical-scanner-fracture-suppressed    = (подавлено)
cmu-medical-scanner-bleed-exact            = { $part }: { $rate } кровопотеря/сек
cmu-medical-scanner-bleed-vague            = Внутреннее кровотечение (местоположение неизвестно)

cmu-medical-stethoscope-pulse              = Частота сердечных сокращений { $bpm }.
cmu-medical-stethoscope-pulse-qualitative  = Пульс { $description }.
cmu-medical-stethoscope-no-pulse           = Сердцебиение не обнаружено.
cmu-medical-stethoscope-no-heart           = В груди пациента нет сердца.
cmu-medical-stethoscope-lungs-precise      = Лёгкие: { $stage }.
cmu-medical-stethoscope-lungs-qualitative  = Лёгкие звучат { $description }.
cmu-medical-stethoscope-no-lungs           = В груди пациента нет лёгких.

cmu-medical-scanner-section-head           = Голова
cmu-medical-scanner-section-torso          = Торс
cmu-medical-scanner-section-arms           = Руки
cmu-medical-scanner-section-legs           = Ноги
cmu-medical-scanner-section-organs         = Органы
cmu-medical-scanner-hp                     = HP
cmu-medical-scanner-bone                   = Кость
cmu-medical-scanner-fracture               = Перелом: { $severity }
cmu-medical-scanner-fracture-vague         = Перелом: обнаружен
cmu-medical-scanner-bleed-internal         = Внутр. кровотечение
cmu-medical-scanner-pain-unknown           = Боль: ?
cmu-medical-scanner-pain-none              = Боль: нет
cmu-medical-scanner-pain-mild              = Боль: слабая
cmu-medical-scanner-pain-moderate          = Боль: умеренная
cmu-medical-scanner-pain-severe            = Боль: сильная
cmu-medical-scanner-pain-shock             = Боль: шок
cmu-medical-scanner-pain-risk-unknown      = ?
cmu-medical-scanner-pain-risk-low          = Низкий
cmu-medical-scanner-pain-risk-elevated     = Повышенный
cmu-medical-scanner-pain-risk-high         = Высокий
cmu-medical-scanner-pain-risk-imminent     = Критический
cmu-medical-scanner-pain-risk-active       = Активный
cmu-medical-scanner-pain-risk-suppressed-suffix = (подавл.)

cmu-medical-scanner-card-body              = Тело
cmu-medical-scanner-card-organs            = Органы
cmu-medical-scanner-card-reagents          = Реагенты в крови
cmu-medical-scanner-card-recommended       = Рекомендации
cmu-medical-scanner-card-patient           = Пациент
cmu-medical-scanner-card-damage            = Профиль повреждений
cmu-medical-scanner-loading                = Получение данных сканирования
cmu-medical-scanner-loading-subtext        = синхронизация с сервером

cmu-medical-scanner-stat-health            = ЗДОРОВЬЕ
cmu-medical-scanner-stat-pulse             = ПУЛЬС уд/мин
cmu-medical-scanner-stat-blood             = КРОВЬ
cmu-medical-scanner-stat-temp              = ТЕМП °C
cmu-medical-scanner-stat-shock-risk        = РИСК ШОКА
cmu-medical-scanner-stat-pulse-stopped     = 0
cmu-medical-scanner-stat-deceased-short    = МЁРТВ

cmu-medical-scanner-status-stable          = СТАБИЛЕН
cmu-medical-scanner-status-serious         = СЕРЬЁЗНОЕ
cmu-medical-scanner-status-critical        = КРИТИЧЕСКОЕ
cmu-medical-scanner-status-deceased        = ПОГИБ

cmu-medical-scanner-severity-healthy       = Здоров
cmu-medical-scanner-severity-bruised       = Ушиб
cmu-medical-scanner-severity-damaged       = Повреждён
cmu-medical-scanner-severity-critical      = Критично
cmu-medical-scanner-severity-severed       = Ампутирован

cmu-medical-scanner-chip-fracture-vague    = Перелом
cmu-medical-scanner-chip-suppressed-suffix = (подавл.)
cmu-medical-scanner-chip-bleed             = ВК
cmu-medical-scanner-chip-bleeding          = Кровотечение
cmu-medical-scanner-chip-splint            = Шина
cmu-medical-scanner-chip-cast              = Гипс
cmu-medical-scanner-chip-tourniquet        = Жгут
cmu-medical-scanner-wound-small            = небольшая рана
cmu-medical-scanner-wound-deep             = глубокая рана
cmu-medical-scanner-wound-gaping           = зияющая рана
cmu-medical-scanner-wound-massive          = массивная рана
cmu-medical-scanner-eschar                 = некроз
cmu-medical-scanner-chip-wounds            = { $count } { $count ->
    [one] рана
    [few] раны
   *[other] ран
}

cmu-medical-scanner-skill-hint-fractures   = Недостаточно навыков для обнаружения переломов и внутренних кровотечений (требуется Мед-1).
cmu-medical-scanner-skill-hint-organs      = Недостаточно навыков для оценки повреждений органов (требуется Мед-2).
cmu-medical-scanner-synthetic-physiology   = Обнаружена синтетическая физиология

cmu-medical-scanner-vitals-pain            = Боль
cmu-medical-scanner-stable-summary         = Стабилен: { $list }
cmu-medical-scanner-acute-issues-header    = Острые проблемы
cmu-medical-scanner-acute-severed          = Ампутирован: { $part }
cmu-medical-scanner-acute-fracture         = Перелом { $severity }: { $part }
cmu-medical-scanner-acute-fracture-vague   = Перелом: { $part }
cmu-medical-scanner-acute-bleed            = Внутр. кровотечение: { $part }
cmu-medical-scanner-acute-bleed-vague      = Обнаружено внутреннее кровотечение
cmu-medical-scanner-acute-organ            = { $stage }: { $organ }
cmu-medical-scanner-acute-organ-removed    = Удалён: { $organ }
cmu-medical-scanner-organ-removed-short    = Удалён

cmu-medical-scanner-organ-heart            = Сердце
cmu-medical-scanner-organ-lungs            = Лёгкие
cmu-medical-scanner-organ-liver            = Печень
cmu-medical-scanner-organ-brain            = Мозг
cmu-medical-scanner-organ-kidneys          = Почки
cmu-medical-scanner-organ-stomach          = Желудок
cmu-medical-scanner-organ-eyes             = Глаза
cmu-medical-scanner-organ-ears             = Уши

cmu-medical-stethoscope-pain-mild          = Пациент чувствует дискомфорт.
cmu-medical-stethoscope-pain-moderate      = Пациент испытывает заметную боль.
cmu-medical-stethoscope-pain-severe        = Пациент испытывает сильную боль.
cmu-medical-stethoscope-pain-shock         = Пациент в состоянии шока.

# Названия частей тела
cmu-medical-scanner-part-name =
    { $symmetry ->
        [left] Левая { $type ->
            [arm]  рука
            [hand] кисть
            [leg]  нога
            [foot] стопа
           *[other] { $type }
        }
        [right] Правая { $type ->
            [arm]  рука
            [hand] кисть
            [leg]  нога
            [foot] стопа
           *[other] { $type }
        }
       *[none] { $type ->
            [head]  Голова
            [torso] Торс
           *[other] { $type }
        }
    }

# Стадии повреждения органов
cmu-medical-scanner-organ-stage-healthy  = Здоров
cmu-medical-scanner-organ-stage-bruised  = Ушиб
cmu-medical-scanner-organ-stage-damaged  = Повреждён
cmu-medical-scanner-organ-stage-failing  = Отказывает
cmu-medical-scanner-organ-stage-dead     = Мёртв
