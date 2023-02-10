

using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache.Internals.Implementations;
using System.Data;
using log4net;
using Code.OutfitPatcher;
using Code.OutfitPatcher.Utils;

namespace Code.OutfitPatcher.Armor
{
    public class TArmorGroup
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TArmorGroup));
        public string Name { get; }
        
        public Dictionary<TGender, FormKey> GenderOutfit = new();

        public List<TArmorSet> Armorsets { get; }

        public Dictionary<FormKey, string> Outfits { get; }

        public Dictionary<FormKey, string> NPCs;
        
        public Dictionary<string, Dictionary<FormKey, string>> Identifiers;

        public TArmorGroup(string name)
        {
            Name = name;
            NPCs = new();
            Armorsets = new();
            Outfits = new();
            Identifiers = new();
        }

        public void AddArmorSet(TArmorSet set)
        {
            if (!Armorsets.Contains(set))
                Armorsets.Add(set);
        }

        public void AddArmorSets(IEnumerable<TArmorSet> sets)
        {
            sets.ForEach(s => AddArmorSet(s));
        }

        public void AddOutfit(IOutfitGetter outfit)
        {
            if (!Outfits.ContainsKey(outfit.FormKey))
                Outfits.TryAdd(outfit.FormKey, outfit.EditorID);
        }

        public void AddOutfits(IEnumerable<IOutfitGetter> outfits)
        {
            outfits.ForEach(o => AddOutfit(o));
        }

        public void AddOutfits(MutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> cache, IEnumerable<KeyValuePair<FormKey, string>> outfits)
        {
            outfits.ForEach(o =>
            {
                var ot = cache.Resolve<IOutfitGetter>(o.Key);
                AddOutfit(ot);
            });
        }

        public ISkyrimMod CreateOutfits(ISkyrimMod Patch)
        {
            var cache = SynPatch.Settings.State.LoadOrder.ToMutableLinkCache();
            var gArmors = Armorsets.GroupBy(x => x.Gender).ToDictionary(x => x.Key, x => x.Select(a => a));
            foreach (var g in gArmors.Keys)
            {
                if (!gArmors[g].Any()) continue;
                string eid = Name + "_" + g + "_";
                Patch = FileUtils.GetIncrementedMod(Patch);
                var set = gArmors[g].Select(a => a.LLFormKey.ToLink<IItemGetter>()).ToList();
                
                Outfit newOutfit = null;
                if (SynPatch.Settings.Cache.TryResolve<IOutfitGetter>(OutfitUtils.GetOutfitName(eid), out var ot))
                {
                    newOutfit = Patch.Outfits.GetOrAddAsOverride(ot);
                    LeveledItem mLL = LeveledListUtils.CreateLeveledList(Patch, set, "mLL_" + eid, 1, SynPatch.Settings.LeveledListFlag);
                    newOutfit.Items = new(mLL.ToLink().AsEnumerable());
                }
                else {
                    newOutfit = OutfitUtils.CreateOutfit(Patch, eid, set);
                }
                GenderOutfit.Add(g, newOutfit.FormKey);
            }
            return Patch;
        }

        public override bool Equals(object? obj)
        {
            return obj is TArmorGroup category &&
                   Name == category.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
