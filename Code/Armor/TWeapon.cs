using log4net;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Code.OutfitPatcher.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.OutfitPatcher.Armor
{
    public class TWeapon
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TWeapon));
        public FormKey FormKey { get; }
        public string Type  { get; }
        public string Name { get; }

        public TWeapon(IWeaponGetter weapon)
        {
            FormKey = weapon.FormKey;
            Name = WeaponUtils.GetFullName(weapon);
            Type = weapon.Data.AnimationType.ToString();
        }
    }
}
