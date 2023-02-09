
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Code.OutfitPatcher.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Code.OutfitPatcher.Armor;
using System.Security.Cryptography;
using log4net;
using Code.OutfitPatcher.Managers;


namespace Code.OutfitPatcher.Utils
{
    public class OutfitUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OutfitUtils));
        //public static bool IsValidOutfit(IOutfitGetter outfit)
        //{
        //    return IsValidOutfit(outfit.EditorID);
        //}

        //public static bool IsValidOutfit(string outfit)
        //{
        //    return Regex.Match(outfit, Settings.Patcher.ValidOutfitRegex, RegexOptions.IgnoreCase).Success
        //            || !Regex.Match(outfit, Settings.Patcher.InvalidOutfitRegex, RegexOptions.IgnoreCase).Success;
        //}
        
        internal static Outfit CreateOutfit(ISkyrimMod? PatchedMod, string eid, IEnumerable<IFormLink<IItemGetter>> set)
        {
            LeveledItem mLL = LeveledListUtils.CreateLeveledList(PatchedMod, set, "mLL_" + eid, 1, SynPoint.Settings.LeveledListFlag);
            Outfit newOutfit = PatchedMod.Outfits.AddNew(GetOutfitName(eid));
            newOutfit.Items = new(mLL.ToLink().AsEnumerable());
            return newOutfit;
        }

        public static List<IArmorGetter> GetArmorList(ILinkCache cache, IOutfitGetter outfit)
        {
            List<IArmorGetter> armors = new();
            List<FormKey> ArmorsFormKey = new();
            outfit.EnumerateFormLinks().Where(x => !x.IsNull)
                .Select(l => {
                    cache.TryResolve<IItemGetter>(l.FormKey, out var t);
                    return t;
                }).Where(x => x != null)
                .ForEach(i => {
                    if (i is IArmorGetter)
                        armors.Add((IArmorGetter)i);
                    if (i is ILeveledItemGetter)
                        LeveledListUtils.GetArmorList(cache, i, armors, ArmorsFormKey);
                });
            return armors.Distinct().ToList();
        }

        public static string GetOutfitName(string eid)
        {
            return Settings.Patcher.OutfitPrefix + eid + Settings.Patcher.OutfitSuffix;
        }

             
    }
}
