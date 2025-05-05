using Content.Server.Imperial.Revolutionary.Components;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.Imperial.Revolutionary;
using Content.Shared.Eui;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Server.Imperial.Revolutionary;
using Content.Server.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Content.Server.Sound.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Imperial.Revolutionary.UI
{
    /// <summary>
    /// UI для окна запроса согласия на обращение в революционеры.
    /// </summary>
    public sealed class ConsentRequestedEui : BaseEui
    {
        private readonly EntityUid _targetEntity;
        private readonly EntityUid _converterEntity;
        private readonly RevolutionaryRuleSystem _revolutionaryRuleSystem;
        private readonly ConsentRevolutionarySystem _consentRevolutionarySystem;
        private readonly PopupSystem _popupSystem;
        private readonly EntityManager _entityManager;

        [Dependency] private readonly IEntityManager _entityManagerDep = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly EmitSoundSystem _emitSoundSystem = default!;

        public ConsentRequestedEui(EntityUid target, EntityUid converter, RevolutionaryRuleSystem revRuleSystem, ConsentRevolutionarySystem consentRevSystem, PopupSystem popup, EntityManager entManager)
        {
            _targetEntity = target;
            _converterEntity = converter;
            _revolutionaryRuleSystem = revRuleSystem;
            _consentRevolutionarySystem = consentRevSystem;
            _popupSystem = popup;
            _entityManager = entManager;
        }

        public override EuiStateBase GetNewState()
        {
            // Возвращаем состояние с именем конвертера
            return new ConsentRequestedState(Identity.Name(_converterEntity, _entityManager));
        }

        public override void Opened()
        {
            base.Opened();
            StateDirty();
        }

        public override void HandleMessage(EuiMessageBase message)
        {
            base.HandleMessage(message);

            if (message is ConsentRequestedEuiMessage consentMessage && _revolutionaryRuleSystem.IsConvertable(_targetEntity))
            {
                if (!_entityManager.TryGetComponent<ConsentRevolutionaryComponent>(_targetEntity, out var targetConsentComp)
                    || !_entityManager.TryGetComponent<ConsentRevolutionaryComponent>(_converterEntity, out var converterConsentComp))
                {
                    return;
                }

                if (consentMessage.IsAccepted)
                {
                    // Преобразуем цель в революционера
                    _revolutionaryRuleSystem.ConvertEntityToRevolution(_targetEntity, _converterEntity);

                    // Отменяем запрос
                    _consentRevolutionarySystem.CancelConsentRequest(targetConsentComp.Owner, converterConsentComp.Owner);

                    // Применяем кулдаун к конвертеру
                    _consentRevolutionarySystem.ApplyConversionCooldown(_converterEntity);

                    // Проигрываем звук принятия у конвертера
                    var acceptSound = new SoundPathSpecifier("/Audio/Imperial/Revolutionary/accept.ogg");
                    PlaySoundOnEntity(_converterEntity, acceptSound);

                    // Показываем уведомление об успешном обращении
                    _popupSystem.PopupEntity(
                        Loc.GetString("rev-consent-convert-accepted", ("target", Identity.Entity(_targetEntity, _entityManager))),
                        _targetEntity,
                        _converterEntity);
                }
                else
                {
                    // Отменяем запрос с применением блокировки
                    _consentRevolutionarySystem.CancelConsentRequest(targetConsentComp.Owner, converterConsentComp.Owner);

                    // Применяем блокировку обращения к цели
                    _consentRevolutionarySystem.ApplyConversionDeny(new Entity<ConsentRevolutionaryComponent>(_targetEntity, targetConsentComp));

                    // Применяем кулдаун к конвертеру
                    _consentRevolutionarySystem.ApplyConversionCooldown(_converterEntity);

                    // Проигрываем звук отказа у конвертера
                    var denySound = new SoundPathSpecifier("/Audio/Imperial/Revolutionary/deny.ogg");
                    PlaySoundOnEntity(_converterEntity, denySound);

                    // Показываем уведомление об отказе
                    _popupSystem.PopupEntity(
                        Loc.GetString("rev-consent-convert-denied", ("target", Identity.Entity(_targetEntity, _entityManager))),
                        _targetEntity,
                        _converterEntity,
                        PopupType.SmallCaution);
                }
            }

            Close();
        }

        private void PlaySoundOnEntity(EntityUid entity, SoundSpecifier sound)
        {
            if (!_entityManager.TryGetComponent<EmitSoundOnTriggerComponent>(entity, out var emitSoundComp))
            {
                emitSoundComp = new EmitSoundOnTriggerComponent();
                _entityManager.AddComponent(entity, emitSoundComp);
            }

            emitSoundComp.Sound = sound;
            var triggerEvent = new TriggerEvent(entity);
            _entityManager.EventBus.RaiseLocalEvent(entity, triggerEvent, true);
        }
    }
}
