using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda;
using Noggog;
using log4net;
using Code.OutfitPatcher.Managers;


namespace Code.OutfitPatcher.Utils
{
    public class LeveledListUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LeveledListUtils));

        public static LeveledItem CreateLeveledList(ISkyrimMod Patch, IEnumerable<IItemGetter> items, string editorID, short level, LeveledItem.Flag flag)
        {
            if (SynPoint.Settings.Cache.TryResolve<ILeveledItemGetter>(editorID, out var ll))
            {
                editorID += "_dup";
            }
            Patch = FileUtils.GetIncrementedMod(Patch);
            LeveledItem lvli = Patch.LeveledItems.AddNew(editorID);
            lvli.Entries = new ExtendedList<LeveledItemEntry>();
            lvli.Flags = flag;

            AddItemsToLeveledList(Patch, lvli, items, 1);
            return lvli;
        }

        public static LeveledNpc CreateLeveledList(ISkyrimMod PatchMod, IEnumerable<ILeveledNpcGetter> items, string editorID, short level, LeveledNpc.Flag flag)
        {
            if (SynPoint.Settings.Cache.TryResolve<ILeveledNpcGetter>(editorID, out var ll))
            {
                editorID += "_dup";
            }


            LeveledNpc lvli = PatchMod.LeveledNpcs.AddNew(editorID);
            lvli.Entries = new();
            lvli.Flags = flag;

            AddItemsToLeveledList(PatchMod, lvli, items, 1);
            return lvli;
        }

        public static LeveledItem CreateLeveledList(ISkyrimMod PatchMod, IEnumerable<IFormLink<IItemGetter>> items, string editorID, short level, LeveledItem.Flag flag)
        {
            if (SynPoint.Settings.Cache.TryResolve<ILeveledItemGetter>(editorID, out var ll))
            {
                editorID += "_dup";
            }

            LeveledItem lvli = PatchMod.LeveledItems.AddNew(editorID);
            lvli.Entries = new ExtendedList<LeveledItemEntry>();
            lvli.Flags = flag;

            AddItemsToLeveledList(PatchMod, lvli, items, 1);
            return lvli;
        }

        internal static void FixLeveledList(ILeveledItemGetter lvli, HashSet<FormKey> set, Dictionary<FormKey, List<FormKey>> parentChildLL, ILinkCache mCache)
        {
            set.Add(lvli.FormKey);
            if (lvli.Entries == null && !lvli.Entries.Any()) return;
            var itms = lvli.Entries.Select(i => mCache.Resolve<IItemGetter>(i.Data.Reference.FormKey));
            foreach (var itm in itms)
            {
                if (set.Contains(lvli.FormKey))
                {
                    parentChildLL.GetOrAdd(lvli.FormKey).Add(itm.FormKey);
                }
                else
                {
                    if (itm is ILeveledItemGetter)
                        FixLeveledList((ILeveledItemGetter)itm, set, parentChildLL, mCache);
                }
            }
        }

        public static void AddItemsToLeveledList(ISkyrimMod patch, LeveledItem lvli, IEnumerable<IItemGetter> items, short level)
        {
            LeveledItem? sLL = null;
            bool hasMultiItems = items.Count() + lvli.Entries.EmptyIfNull().Count() > 250;
            if (!hasMultiItems) sLL = lvli;

            for (int i = 0, j = 0; i < items.Count(); i++)
            {
                if (hasMultiItems && i % 250 == 0)
                {
                    sLL = CreateLeveledList(patch, new List<IItemGetter>(), lvli.EditorID + (++j), 1, SynPoint.Settings.LeveledListFlag);
                    AddItemToLeveledList(lvli, sLL, 1);
                }
                AddItemToLeveledList(sLL, items.ElementAtOrDefault(i), 1);
            }
        }

        public static void AddItemsToLeveledList(ISkyrimMod patch, LeveledNpc lvli, IEnumerable<ILeveledNpcGetter> items, short level)
        {
            LeveledNpc? sLL = null;
            bool hasMultiItems = items.Count() + lvli.Entries.Count > 250;
            if (!hasMultiItems) sLL = lvli;

            for (int i = 0, j = 0; i < items.Count(); i++)
            {
                if (hasMultiItems && i % 250 == 0)
                {
                    sLL = CreateLeveledList(patch, new List<ILeveledNpcGetter>(), lvli.EditorID + (++j), 1, SynPoint.Settings.LeveledNpcFlag);
                    AddItemToLeveledList(lvli, sLL, 1);
                }
                AddItemToLeveledList(sLL, items.ElementAtOrDefault(i), 1);
            }
        }

        public static ISkyrimMod AddItemsToLeveledList(ISkyrimMod patch, LeveledItem lvli, IEnumerable<IFormLink<IItemGetter>> items, short level)
        {
            LeveledItem? sLL = null;
            bool hasMultiItems = items.Count() + lvli.Entries.Count > 250;
            if (!hasMultiItems) sLL = lvli;
            patch = FileUtils.GetIncrementedMod(patch);

            for (int i = 0, j = 0; i < items.Count(); i++)
            {
                patch = FileUtils.GetIncrementedMod(patch);
                if (hasMultiItems && i % 250 == 0)
                {
                    sLL = CreateLeveledList(patch, new List<IItemGetter>(), lvli.EditorID + (++j), 1, SynPoint.Settings.LeveledListFlag);
                    AddItemToLeveledList(lvli, sLL, 1);
                }
                AddItemToLeveledList(sLL, items.ElementAtOrDefault(i), 1);
            }
            return patch;
        }

        internal static ISkyrimMod AddItemsToLeveledList(ILinkCache<ISkyrimMod, ISkyrimModGetter> cache, ISkyrimMod patch, IOutfitGetter ot, IEnumerable<FormLink<IItemGetter>> set, short level)
        {
            patch = FileUtils.GetIncrementedMod(patch);
            var lls = GetLeveledLists(ot, cache);
            var ll = cache.Resolve<ILeveledItemGetter>(lls.First());
            var LL = patch.LeveledItems.GetOrAddAsOverride(ll);
            patch = AddItemsToLeveledList(patch, LL, set, level);
            return patch;
        }

        public static void AddItemToLeveledList(LeveledItem lvli, IItemGetter item, short level)
        {
            LeveledItemEntry entry = new LeveledItemEntry();
            LeveledItemEntryData data = new LeveledItemEntryData();
            data.Reference = item.ToLink();
            data.Level = level;
            data.Count = 1;
            entry.Data = data;
            if (lvli.Entries == null)
                lvli.Entries = new();
            lvli.Entries.Add(entry);
        }

        public static void AddItemToLeveledList(LeveledNpc lvli, ILeveledNpcGetter item, short level)
        {
            LeveledNpcEntry entry = new();
            LeveledNpcEntryData data = new();
            data.Reference = item.ToLink();
            data.Level = level;
            data.Count = 1;
            entry.Data = data;
            lvli.Entries.Add(entry);
        }

        public static void AddItemToLeveledList(LeveledItem lvli, IFormLink<IItemGetter> item, short level)
        {
            LeveledItemEntry entry = new LeveledItemEntry();
            LeveledItemEntryData data = new LeveledItemEntryData();
            data.Reference = item;
            data.Level = level;
            data.Count = 1;
            entry.Data = data;
            lvli.Entries.Add(entry);
        }

        public static void AddEntriesToLeveledList(ISkyrimMod patch, LeveledItem lvli, IEnumerable<LeveledItemEntry> items)
        {
            if (items.Count() < 250) items.ForEach(item => lvli.Entries.Add(item));
            else
            {
                LeveledItem? sLL = null;
                for (int i = 0; i < items.Count(); i++)
                {
                    if (i % 250 == 0)
                    {
                        sLL = CreateLeveledList(patch, new List<IItemGetter>(), lvli.EditorID + i, 1, lvli.Flags);
                        AddItemToLeveledList(lvli, sLL, 1);
                    }
                    sLL.Entries.Add(items.ElementAtOrDefault(i));
                }
            }

        }

        public static void AddEntriesToLeveledList(ISkyrimMod patch, LeveledNpc lvli, IEnumerable<LeveledNpcEntry> items)
        {
            if (items.Count() < 250) items.ForEach(item => lvli.Entries.Add(item));
            else
            {
                LeveledNpc? sLL = null;
                for (int i = 0; i < items.Count(); i++)
                {
                    if (i % 250 == 0)
                    {
                        sLL = CreateLeveledList(patch, new List<ILeveledNpcGetter>(), lvli.EditorID + i, 1, SynPoint.Settings.LeveledNpcFlag);
                        AddItemToLeveledList(lvli, sLL, 1);
                    }
                    sLL.Entries.Add(items.ElementAtOrDefault(i));
                }
            }

        }

        public static HashSet<FormKey> GetLeveledLists(IOutfitGetter otft, ILinkCache cache)
        {
            if (otft.Items.Any(x => x is IArmorGetter))
            {
                return new();
            }
            else
            {
                HashSet<FormKey> processed = new();
                otft.Items.Select(i => cache.Resolve<IItemGetter>(i.FormKey))
                    .ForEach(i => GetSubLeveledLists(i, processed));
                return processed;
            }
        }

        private static void GetSubLeveledLists(IItemGetter ll, HashSet<FormKey> LLs)
        {
            ll.EnumerateFormLinks().ForEach(i =>
            {
                if (!LLs.Contains(i.FormKey))
                {
                    LLs.Add(i.FormKey);
                    if (SynPoint.Settings.Cache.TryResolve<ILeveledItem>(i.FormKey, out var itm))
                    {
                        LLs.Add(itm.FormKey);
                        GetSubLeveledLists(itm, LLs);
                    }
                }
            });
        }

        public static void GetArmorList(ILinkCache cache, IItemGetter ll, ICollection<IArmorGetter> armors, List<FormKey> processed)
        {
            ll.EnumerateFormLinks().ForEach(i =>
            {
                if (!processed.Contains(i.FormKey))
                {
                    processed.Add(i.FormKey);
                    if (cache.TryResolve<IArmorGetter>(i.FormKey, out var itm))
                        armors.Add(itm);
                    if (cache.TryResolve<ILeveledItemGetter>(i.FormKey, out var lv))
                        GetArmorList(cache, lv, armors, processed);
                }
            });
        }

        public static List<IArmorGetter> GetArmorList(ILinkCache cache, ILeveledItemGetter lls)
        {
            List<IArmorGetter> armors = new();
            List<FormKey> ArmorsFormKey = new();
            lls.EnumerateFormLinks().Where(x => !x.IsNull)
                .Select(l => {
                    cache.TryResolve<IItemGetter>(l.FormKey, out var t);
                    return t;
                }).Where(x => x != null)
                .ForEach(i => {
                    if (i is IArmorGetter)
                        armors.Add((IArmorGetter)i);
                    if (i is ILeveledItemGetter)
                        GetArmorList(cache, i, armors, ArmorsFormKey);
                });
            return armors.Distinct().ToList();
        }
    }
}
