using Robust.Shared.Configuration;

namespace Content.Server._RuCM.Discord;

[CVarDefs]
public static class RuMCModerationRegistryCVars
{
    public static readonly CVarDef<string> Webhook =
        CVarDef.Create("discord.rumc_moderation_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
