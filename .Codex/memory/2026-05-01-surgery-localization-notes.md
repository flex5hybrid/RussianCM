## Локализация хирургии 2026-05-01

- CMU-хирургия локализуется в основном через:
  - `Resources/Locale/en-US/_CMU14/medical/surgery.ftl`
  - `Resources/Locale/en-US/_CMU14/medical/surgery_ux.ftl`
- Для русской локализации добавлены зеркальные файлы в:
  - `Resources/Locale/ru-RU/_CMU14/medical/`
- Важная особенность: названия операций в UI брались не из FTL напрямую, а из `displayName` в `Resources/Prototypes/_CMU14/Medical/surgery_step_metadata.yml`.
- Для корректной локализации списка операций `displayName` переведены на loc-key, а `Content.Server/_CMU14/Medical/Surgery/CMUSurgeryDispatchSystem.cs` теперь резолвит их через `Loc.GetString(...)`.
- Без этой правки окно хирургии продолжало бы показывать английские названия операций даже при наличии русских FTL-строк.
