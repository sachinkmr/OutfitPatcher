using log4net;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Code.OutfitPatcher.Armor;
using Code.OutfitPatcher.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.OutfitPatcher.Utils
{
    public class WeaponUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WeaponUtils));
        public static IEnumerable<TWeapon> GetMatchingWeapons(TArmor Body, IEnumerable<TWeapon> weapons, bool addAll = false)
        {
            Dictionary<string, Dictionary<int, TWeapon>> matchedMap = new();
            foreach (var weapon in weapons)
            {
                int matchingWords = HelperUtils.GetMatchingWordCount(Body.Name, weapon.Name, false);
                if (matchingWords > 0)
                    matchedMap.GetOrAdd(weapon.Type).GetOrAdd(matchingWords, () => weapon);
            }
            return matchedMap.Values.Select(x => x.OrderBy(k => k.Key).Last().Value).Distinct();
        }

        public static string ResolveItemName(IWeaponGetter item)
        {
            return item.Name == null || item.Name.String.Length < 1 ? item.EditorID : item.Name.ToString();
        }

        public static Comparer<IWeaponGetter> GetWeaponComparer(string fullName)
        {
            return Comparer<IWeaponGetter>.Create((a, b) =>
            {
                return HelperUtils.GetMatchingWordCount(fullName, ResolveItemName(a))
                    .CompareTo(HelperUtils.GetMatchingWordCount(fullName, ResolveItemName(b)));
            });
        }

        public static string GetFullName(IWeaponGetter item)
        {
            var words = HelperUtils.SplitString(item.EditorID + (item.Name == null ? "" : item.Name.String)).Split(' ');
            return string.Join(" ", new HashSet<string>(words));
        }
    }
}
