using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed partial class DamageModifierByItemComponent : Component
    {

        [DataField("itemIds")]
        public List<string> ItemIds = new();

        [DataField("damage")]
        public DamageSpecifier Damage = new();
    }
}
