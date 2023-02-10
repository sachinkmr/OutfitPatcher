using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using System.Text.RegularExpressions;

using Code.OutfitPatcher.Armor;
using Code.OutfitPatcher.Config;
using Noggog;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using System;
using log4net;


namespace Code.OutfitPatcher.Utils
{
    public class ArmorUtils
    {        
        internal static IEnumerable<TBodySlot> ArmorSlots = HelperUtils.GetEnumValues<TBodySlot>();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ArmorUtils));

        public static string GetName(IArmorGetter armor)
        {
            return armor.Name == null || armor.Name.String.IsNullOrEmpty()
                ? HelperUtils.SplitString(armor.EditorID) : armor.Name.ToString();
        }

        public static bool IsMissingMatchingArmor(ILinkCache cache, IArmorGetter armor) {
            if(!IsEligibleForMeshMapping(armor))  return false;
            var addon = armor.Armature.FirstOrDefault().Resolve<IArmorAddonGetter>(cache);
            return addon.WorldModel == null || addon.WorldModel.Male == null || addon.WorldModel.Female == null;
        }

        private static List<string> GetClothingMaterial(IArmorGetter armor, string name)
        {
            // Matching Clothing and Robes types
            List<string> results = new();
            name = name.IsNullOrEmpty()? GetFullName(armor):name;
            var matches = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, name);

            if (matches.Any()) results.AddRange(matches);

            if (!matches.Any())
                matches = HelperUtils.GetRegexBasedGroup(Settings.Patcher.ArmorTypeRegex, name);

            if (matches.Contains(TArmorType.Wizard.ToString()))
                results.Add("Mage");

            if (results.Count() > 1 && results.Contains("Mage") && results.Contains("Citizen") && (armor.ObjectEffect == null))
                results.Remove("Mage");
                
            if (!results.Any()) {
                Console.WriteLine("Unknown Clothing Type: [{0}][{1}]", armor.FormKey, name);
                results.Add(TArmorType.Unknown.ToString());
            }
            return results.Distinct().ToList();
        }

        private static List<string> GetArmorMaterial(IArmorGetter item, string name)
        {
            List<string> results = new();
            string mRegex = @"(?:Armor|Weap(?:on))Materi[ae]l(\w+)";
            ILinkCache cache = SynPatch.Settings.Cache;
            name = name.IsNullOrEmpty() ? GetFullName(item) : name;
            item.Keywords.EmptyIfNull().Where(x => !x.IsNull)
                .Select(x => cache.Resolve<IKeywordGetter>(x.FormKey).EditorID)
                .ForEach(x =>
                {
                    var match = Regex.Match(x, mRegex, RegexOptions.IgnoreCase);                    
                    if (match.Success)
                    {
                        var val = match.Groups.Values.Last().Value;
                        var cats = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, val);
                        results.AddRange(cats);
                    }
                });

            var armorType = GetArmorType(item);
            var matches = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, name)
                .Where(m=> !HelperUtils.GetRegexBasedGroup(Settings.Patcher.ArmorTypeRegex, m).Contains("Cloth"));
            if (matches.Any()) {
                if (armorType == TArmorType.Heavy)
                    results.AddRange(matches.Where(m => !HelperUtils.GetRegexBasedGroup(Settings.Patcher.ArmorTypeRegex, m).Contains("Wizard")));
                else
                    results.AddRange(matches);
            }

            if (!results.Any())
            {
                results.Add(TArmorType.Unknown.ToString());
                Console.WriteLine("Unknown Armor/Clothing Material: [{0}][{1}]", item.FormKey, name);
            }
            return results.Distinct().ToList();
        }

        public static List<string> GetMaterial(IArmorGetter item)
        {
            // Checking for material first
            string fullName = GetFullName(item);
            return IsCloth(item) ? GetClothingMaterial(item, fullName)
                : GetArmorMaterial(item, fullName);
        }

        private static bool IsCloth(IArmorGetter item)
        {
            return item.HasKeyword(Skyrim.Keyword.ArmorClothing)
                || item.BodyTemplate.ArmorType.Equals(ArmorType.Clothing);
        }

        public static bool IsValidArmor(IArmorGetter armor)
        {
            var name = GetFullName(armor);
            bool isSlutty = SynPatch.Settings.User.SkipSluttyOutfit && Regex.IsMatch(name, Settings.Patcher.SluttyRegex, RegexOptions.IgnoreCase);
            return !isSlutty && (Regex.IsMatch(name, Settings.Patcher.ValidArmorsRegex, RegexOptions.IgnoreCase)
                    || !Regex.IsMatch(name, Settings.Patcher.InvalidArmorsRegex, RegexOptions.IgnoreCase));
        }

        public static bool IsValidMaterial(string name)
        {
            return (Regex.IsMatch(name, Settings.Patcher.ValidMaterial, RegexOptions.IgnoreCase)
                    || !Regex.IsMatch(name, Settings.Patcher.InvalidMaterial, RegexOptions.IgnoreCase));
        }

        public static TArmorType GetArmorType(IArmorGetter armor)        {
            if (IsCloth(armor))
                return Regex.IsMatch(armor.EditorID, Settings.Patcher.ArmorTypeRegex["Wizard"], RegexOptions.IgnoreCase)
                    ? TArmorType.Wizard : TArmorType.Cloth;
            return armor.BodyTemplate.ArmorType == ArmorType.HeavyArmor ? TArmorType.Heavy : TArmorType.Light;
        }

        public static TGender GetGender(IArmorGetter armor)
        {
            if (armor.Armature != null && armor.Armature.Count > 0 
                && SynPatch.Settings.Cache.TryResolve<IArmorAddonGetter>(armor.Armature.FirstOrDefault().FormKey, out var addon))
            {
                if (addon.WorldModel == null) return TGender.Unknown;
                if (addon.WorldModel.Male != null && addon.WorldModel.Female != null)
                    return TGender.Common;
                if (addon.WorldModel.Male == null)
                    return TGender.Female;
                if (addon.WorldModel.Female == null)
                    return TGender.Male;
            }
            return TGender.Unknown;
        }

        public static IEnumerable<TBodySlot> GetBodySlots(IArmorGetter armor)
        {
            var flags = armor.BodyTemplate.FirstPersonFlags;
            return ArmorSlots.Where(x => flags.HasFlag((BipedObjectFlag)x));
        }

        public static IEnumerable<TBodySlot> GetBodySlots(IArmorAddonGetter addon)
        {
            var flags = addon.BodyTemplate.FirstPersonFlags;
            return ArmorSlots.Where(x => flags.HasFlag((BipedObjectFlag)x));
        }

        public static bool IsUpperArmor(IArmorGetter x)
        {
            var addons = x.Armature.EmptyIfNull().Select(x => x.Resolve(SynPatch.Settings.Cache));
            return addons.EmptyIfNull().Any(addon => addon.BodyTemplate.FirstPersonFlags.HasFlag(BipedObjectFlag.Body)
                || addon.BodyTemplate.FirstPersonFlags.HasFlag((BipedObjectFlag)TBodySlot.Chest)
                || addon.BodyTemplate.FirstPersonFlags.HasFlag((BipedObjectFlag)TBodySlot.ChestUnder));
        }

        public static bool IsUpperArmor(TBodySlot x)
        {
            return x.Equals(BipedObjectFlag.Body)
            || x.Equals(TBodySlot.Chest)
            || x.Equals(TBodySlot.ChestUnder);
        }

        public static bool IsBodyArmor(IArmorGetter x)
        {
            var slots = GetBodySlots(x);
            var status =  slots.Contains(TBodySlot.Body) 
            && !(slots.Contains(TBodySlot.Back)
             || slots.Contains(TBodySlot.Decapitate)
             || slots.Contains(TBodySlot.DecapitateHead));
            return status;
        }

        public static bool IsFeetArmor(IArmorGetter x)
        {
            var slots = GetBodySlots(x);
            return slots.Contains(TBodySlot.Feet);
        }

        public static bool IsLowerArmor(IArmorGetter x)
        {
            var addons = x.Armature.EmptyIfNull().Select(x => x.Resolve(SynPatch.Settings.Cache));
            return addons.EmptyIfNull().Any(addon => addon.BodyTemplate.FirstPersonFlags.HasFlag((BipedObjectFlag)TBodySlot.Pelvis)
            || addon.BodyTemplate.FirstPersonFlags.HasFlag((BipedObjectFlag)TBodySlot.PelvisUnder));
        }

        public static bool IsJewelry(IArmorGetter x) {
            return x.HasKeyword(Skyrim.Keyword.ArmorJewelry)
                || x.HasKeyword(Skyrim.Keyword.ClothingNecklace);
        }

        
        public static Comparer<TArmor> GetArmorComparer(string fullName)
        {
            return Comparer<TArmor>.Create((a, b) =>
            {
                return HelperUtils.GetMatchingWordCount(fullName, a.Name)
                    .CompareTo(HelperUtils.GetMatchingWordCount(fullName, b.Name));
            });
        }

        public static string ResolveItemName(IArmorGetter item) {
            return item.Name == null || item.Name.String.Length < 1 ? item.EditorID : item.Name.ToString();
        }

        public static string GetFullName(IArmorGetter item)
        {
            var words = HelperUtils.SplitString(item.EditorID + " " + (item.Name == null ? "" : item.Name.String)).Split(' ');
            return string.Join(" ", new HashSet<string>(words));
        }

        public static bool IsEligibleForMeshMapping(IArmorGetter armor)
        {
            return IsValidArmor(armor)
                    && !Regex.IsMatch(GetFullName(armor), "Shield", RegexOptions.IgnoreCase)
                    && armor.Armature != null
                    && armor.Armature.Count > 0
                    && !armor.HasKeyword(Skyrim.Keyword.ArmorShield)
                    && !GetBodySlots(armor).Contains(TBodySlot.Shield);
        }

        public static List<TArmorSet> GetMatchingArmorSets(TArmor Body, String Material, IEnumerable<TArmor> others, bool addAll, int commonName)
        {
            List<TArmorSet> sets = new();
            TArmorSet bodySet = new(Body, Material);
            sets.Add(bodySet);

            if (!others.Any())
            {
                Console.WriteLine("No matching armor found for {0}: {1}", Body.EditorID, Body.FormKey);
                return sets;
            }
            if (!addAll)
            {
                Dictionary<TBodySlot, Dictionary<int, List<TArmor>>> armors = new();
                var bname = Body.Name;
                foreach (var a in others)
                {
                    // Name based matching
                    var aname = a.Name;
                    int c = HelperUtils.GetMatchingWordCount(bname, aname, false) - commonName;
                    if (c > 0) a.BodySlots.ForEach(flag => armors.GetOrAdd(flag).GetOrAdd(c).Add(a));
                }
                int i = 0;
                var armorSets = armors
                    .Select(x => x.Value.OrderBy(k => k.Key).Last().Value)
                    .CartesianProduct()
                    .Select(x =>
                    {
                        TArmorSet set = new(Body, Material);
                        set.AddArmors(x.Distinct());
                        set.EditorID += "_" + i++;
                        return set;
                    });

            }
            else {
                bodySet.AddArmors(others);
            }
            return sets;
        }
    }
}
