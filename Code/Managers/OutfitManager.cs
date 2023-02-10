using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Text.RegularExpressions;
using Code.OutfitPatcher.Utils;
using Code.OutfitPatcher.Armor;
using Code.OutfitPatcher.Config;
using Mutagen.Bethesda.Plugins;
using System.Text;
using log4net;

namespace Code.OutfitPatcher.Managers
{
    public class OutfitManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OutfitManager));

        private ISkyrimMod? Patch;
        readonly private string LoadOrderFile;
        readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> State;        
        readonly private SortedDictionary<string, TArmorGroup> GrouppedArmorSets = new();

        

        public OutfitManager(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            State = state;
            LoadOrderFile = Path.Combine(Settings.LogsDirectory, "ArmorModsLoadOrder.txt");
        }

        public ISkyrimMod Process(ISkyrimMod patch)
        {
            Patch = FileUtils.GetIncrementedMod(patch);

            // Hadling armor mods
            CreateArmorsSets();

            // Creating new Outfits
            MergePatchedOutfits();
            FilterGaurdOutfits();
            CreateMannequinOutfits();
            CreateNewOutfits();
            ResolveOutfitOverrides();

            // Generating SPID File
            CreateSPID();
            DumpDebugLogs();
            GrouppedArmorSets.Clear();
            return Patch;
        }

        /**
         * Fetches outfits created earlier by patcher (in case of esps are merged)
         */
        private void MergePatchedOutfits()
        {
            Console.WriteLine("Merging previously created outfits...");
            var cache = State.LoadOrder.ToMutableLinkCache();
            var outfits = State.LoadOrder.PriorityOrder
                .Where(l => !SynPatch.Settings.Patches.Contains(l.Mod))
                .WinningOverrides<IOutfitGetter>()
                .Where(o => !SynPatch.Settings.User.ModsToSkip.Contains(o.FormKey.ModKey)
                    && o.EditorID.EndsWith(Settings.Patcher.OutfitSuffix)
                    && o.EditorID.StartsWith(Settings.Patcher.OutfitPrefix));

            foreach (var otft in outfits)
            {
                // Merging outfits created by patcher in previous run
                var tokens = Regex.Replace(otft.EditorID, Settings.Patcher.OutfitSuffix + "|" + Settings.Patcher.OutfitPrefix, "").Split('_');
                var category = tokens[0];
                var gender = tokens[1].ToEnum<TGender>();

                var armorGroup = GrouppedArmorSets.GetOrAdd(category, () => new TArmorGroup(category));
                var lls = LeveledListUtils.GetLeveledLists(otft, cache)
                        .Select(l => cache.Resolve<ILeveledItemGetter>(l))
                        .Where(l => l.Flags.HasFlag(LeveledItem.Flag.UseAll));

                lls.ForEach(ll =>
                {
                    var set = TArmorSet.GetArmorSet(ll, category);
                    armorGroup.AddArmorSet(set);
                });
            }
        }

        /* 
         * Creates armor sets based on material and provided keywords
         */
        private void CreateArmorsSets()
        {
            Console.WriteLine("Creating matching armor sets for armor mods...");
            var modlists = State.LoadOrder.PriorityOrder
                .Where(x => (SynPatch.Settings.User.ArmorMods.ContainsKey(x.ModKey.FileName)
                    && x.Mod.Armors.Count > 0)
                    && !(x.Mod.Outfits != null
                    && x.Mod.Outfits.Any(o => o.EditorID.EndsWith(Settings.Patcher.OutfitSuffix)
                    && o.EditorID.StartsWith(Settings.Patcher.OutfitPrefix))));

            var totalSets = 0;
            var loadOrderList = new List<string>();
            var skyrimFeets = State.LoadOrder.PriorityOrder
                .Where(x => Settings.Patcher.Masters.Contains(x.ModKey.FileName))
                .WinningOverrides<IArmorGetter>()
                .Where(x => ArmorUtils.IsFeetArmor(x))
                .Select(x => new TArmor(x, ArmorUtils.GetMaterial(x).First()))
                .GroupBy(x => x.Type)
                .ToDictionary(x => x.Key, x => x.Select(a => a));

            for (int l = 0; l < modlists.Count(); l++)
            {
                var modLoadOrder = "_LO-" + l;
                var mod = modlists.ElementAt(l).Mod;
                List<IArmorGetter> bodies = new();
                List<TArmor> others = new();
                List<TArmor> jewelries = new();
                loadOrderList.Add(mod.ModKey.FileName + " = " + modLoadOrder);

                var modsCategories = SynPatch.Settings.User.ArmorMods[mod.ModKey.FileName].Distinct().ToList();
                modsCategories.Remove("Generic");

                mod.Armors
                    .Where(x => ArmorUtils.IsValidArmor(x)
                        && x.Keywords != null && x.Armature != null && x.Armature.Any())
                    .ForEach(armor =>
                    {
                        Patch = FileUtils.GetIncrementedMod(Patch);
                        armor = State.LinkCache.Resolve<IArmorGetter>(armor.FormKey);
                        if (ArmorUtils.IsBodyArmor(armor)) { bodies.Add(armor); }
                        else
                        {
                            var mats = ArmorUtils.GetMaterial(armor);
                            if (mats.Count > 1 && mats.Contains("Unknown"))
                                mats.Remove("Unknown");

                            mats.Concat(modsCategories).Distinct()
                            .ForEach(m =>
                            {
                                TArmor ar = new(armor, m);
                                others.Add(ar);
                                if (ArmorUtils.IsJewelry(armor))
                                    jewelries.Add(ar);
                            });

                        }
                    });

                int bodyCount = bodies.Count;
                var tt = 0;

                var commanName = 0;
                if (bodyCount > 5 && SynPatch.Settings.User.ArmorMods[mod.ModKey.FileName].Contains("Generic"))
                    commanName = HelperUtils.GetCommonItems(others.Select(x => HelperUtils.SplitString(x.Name)).ToList())
                               .Where(x => !x.IsNullOrEmpty()).Count();


                var weapons = mod.Weapons.EmptyIfNull().Select(w => new TWeapon(w)).ToHashSet();
                Dictionary<TArmorType, Dictionary<string, List<TArmor>>> armorGroups = new();
                others.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Select(a => a)).ForEach(x =>
                {
                    Dictionary<string, List<TArmor>> d1 = new();
                    x.Value.ForEach(a => d1.GetOrAdd(a.Material).Add(a));
                    armorGroups.Add(x.Key, d1);
                });

                for (int i = 0; i < bodyCount; i++)
                {
                    // Creating armor sets and LLs
                    var body = bodies.ElementAt(i);
                    var fullname = ArmorUtils.GetFullName(body);
                    var bMats = ArmorUtils.GetMaterial(body).Concat(modsCategories).Distinct().ToList();
                    if (bMats.Contains("Unknown"))
                        bMats.Remove("Unknown");

                    bMats.ForEach(bMat =>
                    {
                        var bArmor = new TArmor(body, bMat);
                        var jwels = jewelries.Where(z => HelperUtils.GetMatchingWordCount(bArmor.Name, z.Name, false) - commanName > 0);
                        List<TArmor> armors = armorGroups.GetOrAdd(bArmor.Type)
                            .GetOrDefault(bMat).EmptyIfNull()
                            .Union(jwels).ToList();

                        // Creating weapons sets
                        IEnumerable<TWeapon> matchingWeapons = null;
                        if (SynPatch.Settings.User.DistributeWeapons && weapons.Any())
                            matchingWeapons = WeaponUtils.GetMatchingWeapons(bArmor, weapons);

                        // Creating armor sets
                        ArmorUtils.GetMatchingArmorSets(bArmor, bMat, armors, bodyCount == 1, commanName)
                        .ForEach(armorSet =>
                        {
                            armorSet.LoadOrder = modLoadOrder;

                            //Distributing weapons as well
                            if (matchingWeapons != null)
                                armorSet.AddWeapons(matchingWeapons);

                            // Checking for Boots
                            //if (!armorSet.Armors.Where(x => x.BodySlots.Contains(TBodySlot.Feet)).Any())
                            //{
                            //    var type = armorSet.Type;
                            //    var feets = others.Where(x => x.BodySlots.Contains(TBodySlot.Feet));
                            //    var gps = feets.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Select(a => a));
                            //    if (gps.ContainsKey(type))
                            //        armorSet.AddArmor(gps[type].OrderBy(i => Random.Next()).First());
                            //    else
                            //        armorSet.AddArmor(skyrimFeets[type].OrderBy(i => Random.Next()).First());
                            //}

                            // Creating Leveled List
                            Patch = armorSet.CreateLeveledList(Patch);

                            // Add armor set to category list
                            AddArmorSetToGroup(bMat, armorSet);
                            if (tt++ > 0 && tt % 100 == 0)
                                Console.WriteLine("Created {0} armor-set for: {1}", tt, mod.ModKey.FileName);

                        });
                    });
                }
                Console.WriteLine("Created {0} armor-set for: {1}", tt, mod.ModKey.FileName);
                totalSets += tt;
            }
            Console.WriteLine("Created {0} matching armor sets from armor mods...\n", totalSets);

            File.WriteAllLines(LoadOrderFile, loadOrderList);
        }

        private void CreateNewOutfits()
        {
            Console.WriteLine("Creating New Outfits...");
            if (GrouppedArmorSets.Remove("Unknown", out var temp))
            {
                var types = temp.Armorsets.GroupBy(a => a.Type).ToDictionary(x => x.Key, x => x.Select(b => b));
                foreach (var t in types)
                {
                    if (t.Key == TArmorType.Cloth)
                    {
                        GrouppedArmorSets.GetOrAdd("Merchant", () => new TArmorGroup("Merchant")).AddArmorSets(temp.Armorsets);
                        GrouppedArmorSets.GetOrAdd("Citizen", () => new TArmorGroup("Citizen")).AddArmorSets(temp.Armorsets);
                    }
                    else
                    {
                        GrouppedArmorSets.GetOrAdd("Bandit", () => new TArmorGroup("Bandit")).AddArmorSets(t.Value);
                        GrouppedArmorSets.GetOrAdd("Warrior", () => new TArmorGroup("Warrior")).AddArmorSets(t.Value);
                    }
                }
            }
            var bandit = GrouppedArmorSets.GetOrAdd("Bandit", () => new TArmorGroup("Bandit"));
            bandit.AddArmorSets(GrouppedArmorSets.GetOrAdd("Warrior", () => new TArmorGroup("Warrior")).Armorsets);
            bandit.AddArmorSets(GrouppedArmorSets.GetOrAdd("Knight", () => new TArmorGroup("Knight")).Armorsets);

            GrouppedArmorSets.ForEach(rec =>
            {
                Patch = FileUtils.GetIncrementedMod(Patch);
                rec.Value.CreateOutfits(Patch);
                Console.WriteLine("Created new outfit record for: " + rec.Key);
            });
        }

        private void ResolveOutfitOverrides()
        {
            if (!SynPatch.Settings.User.ResolveOutfitConflicts) return;
            Console.WriteLine("\nResolving outfit armor mods conflicts...");

            Patch = FileUtils.GetIncrementedMod(Patch, true);
            var outfitContext = State.LoadOrder.PriorityOrder.Outfit()
               .WinningContextOverrides();
            foreach (var outfit in State.LoadOrder.PriorityOrder
                .Where(x => SynPatch.Settings.User.ArmorMods.ContainsKey(x.ModKey.FileName.String))
                .WinningOverrides<IOutfitGetter>())
            {
                var winningOtfts = outfitContext.Where(c => c.Record.FormKey == outfit.FormKey).EmptyIfNull();
                if (winningOtfts.Any())
                {
                    List<IItemGetter> oLLs = new();
                    var winningOtft = winningOtfts.First().Record;

                    // Merging outfit's lvls from the armor mods together
                    var context = SynPatch.Settings.Cache.ResolveAllContexts<IOutfit, IOutfitGetter>(winningOtft.FormKey).ToList();
                    var overridenOtfts = context.Where(c => SynPatch.Settings.User.ArmorMods.ContainsKey(c.ModKey.FileName.String)).ToList();
                    var lastNonModOutfit = context.Where(c => !SynPatch.Settings.User.ArmorMods.ContainsKey(c.ModKey.FileName.String)).ToList();

                    if (overridenOtfts.Count > 0 && lastNonModOutfit.Count > 1)
                    {
                        // Reverting Overridden outfit by armor mods added in the patcher
                        overridenOtfts.ForEach(r =>
                        {
                            var items = r.Record.Items.Select(x => SynPatch.Settings.Cache.Resolve<IItemGetter>(x.FormKey));
                            if (items.Count() == 1)
                            {
                                oLLs.Add(items.First());
                            }
                            else
                            {
                                Patch = FileUtils.GetIncrementedMod(Patch);
                                var ll = LeveledListUtils.CreateLeveledList(Patch, items, "ll_" + r.Record.EditorID + 0, 1, LeveledItem.Flag.UseAll);
                                oLLs.Add(ll);
                            }
                        });

                        // Getting outfit records form armor mods added in the patcher and patching those
                        Patch = FileUtils.GetIncrementedMod(Patch);
                        Outfit nOutfit = Patch.Outfits.GetOrAddAsOverride(lastNonModOutfit.First().Record);
                        var items = nOutfit.Items.Select(x => SynPatch.Settings.Cache.Resolve<IItemGetter>(x.FormKey));
                        if (items.Count() == 1)
                        {
                            oLLs.Add(items.First());
                        }
                        else
                        {
                            Patch = FileUtils.GetIncrementedMod(Patch);
                            var ll = LeveledListUtils.CreateLeveledList(Patch, items, "ll_" + nOutfit.EditorID + 0, 1, LeveledItem.Flag.UseAll);
                            oLLs.Add(ll);
                        }

                        // Creating patched outfit
                        Patch = FileUtils.GetIncrementedMod(Patch);
                        LeveledItem sLL = LeveledListUtils.CreateLeveledList(Patch, oLLs.Distinct(), "sll_" + outfit.EditorID, 1, SynPatch.Settings.LeveledListFlag);
                        nOutfit.Items = new();
                        nOutfit.Items.Add(sLL);
                    }
                }
            }
        }

        private void CreateMannequinOutfits()
        {
            if (SynPatch.Settings.User.AssignMannequinOutfits)
            {
                var category = GrouppedArmorSets.GetOrAdd("Mannequins", () => new TArmorGroup("Mannequins"));
                GrouppedArmorSets.Where(x => !Regex.IsMatch(x.Key, "Children|Guards", RegexOptions.IgnoreCase))
                    .ForEach(pair => category.AddArmorSets(pair.Value.Armorsets));
            }
        }

        private void CreateSPID()
        {
            Console.WriteLine("Creating SPID ini file for outfits...");
            var percentage = SynPatch.Settings.User.OutfitDistributionPercentage;
            var cache = State.LoadOrder.ToMutableLinkCache();
            var SPID = new List<string>();

            foreach (var pair in GrouppedArmorSets)
                pair.Value.GenderOutfit
                .ForEach(x =>
                {
                    var gender = x.Key.Equals(TGender.Male) ? "M" : x.Key.Equals(TGender.Common) ? "NONE" : "F";
                    var outfit = x.Value;
                    var keywords = Settings.Patcher.KeywordPrefix + pair.Key;
                    var armorSets = OutfitUtils.GetArmorList(cache, cache.Resolve<IOutfitGetter>(outfit))
                        .Where(x => ArmorUtils.IsBodyArmor(x))
                        .Count();
                    string line1 = string.Format("\n;Outfits for {0}/{1} [{2} armor sets]", pair.Key,
                        gender.Replace("NONE", "M+F").Replace("/U", ""), armorSets);
                    string line2 = String.Format("Outfit = 0x{0}~{1}|{2}|{3}|NONE|{4}|NONE|{5}",
                        outfit.ID.ToString("X"), outfit.ModKey.FileName, keywords, "NONE", gender, percentage);
                    if (SynPatch.Settings.User.SkipGuardDistribution && pair.Key.StartsWith("Guards")) line2 = ";" + line2;
                    if (armorSets < 1) line2 = ";" + line2;
                    if (SynPatch.Settings.User.FilterUniqueNPC) SPID.Add(line2.Replace("/U|NONE", "|NONE"));
                    SPID.Add(line1);
                    SPID.Add(line2);
                    SPID.Add(Environment.NewLine);
                });

            SPID = SPID.Where(x => x.Trim().Any()).ToList();
            File.WriteAllLines(Settings.Patcher.SPIDFile, SPID);
        }

        private void DumpDebugLogs()
        {
            // Writing Debug logs
            Console.WriteLine("Writing Debug logs...");
            var outputfile = Path.Combine(Settings.LogsDirectory, "Categories.json");
            FileUtils.WriteJson(outputfile, GrouppedArmorSets);
            Console.WriteLine("Data Written...");
        }

        private void AddArmorSetToGroup(string group, TArmorSet armorSet)
        {
            GrouppedArmorSets.GetOrAdd(group, () => new TArmorGroup(group)).AddArmorSet(armorSet);
        }

        private void FilterGaurdOutfits()
        {
            if (!SynPatch.Settings.User.SkipGuardDistribution) return;

            Dictionary<FormKey, string> list = new();
            //GrouppedArmorSets.Where(x => x.Key.EndsWith("Armor") || x.Key.StartsWith("Guards") || x.Key.EndsWith("Race"))
            GrouppedArmorSets.Where(x => x.Key.StartsWith("Guards"))
                .Select(x => x.Value.Outfits)
                .ForEach(x => x.ForEach(o =>
                {
                    if (Settings.Patcher.Masters.Contains(o.Key.ModKey.FileName))
                        list.TryAdd(o.Key, o.Value);
                }));

            list.ForEach(o => GrouppedArmorSets
                // .Where(x => !x.Key.EndsWith("Armor") && !x.Key.EndsWith("Guards") && !x.Key.EndsWith("Race"))
                .Where(x => !x.Key.EndsWith("Guards"))
                .ToDictionary()
                .Values.ForEach(x =>
                {
                    var common = x.Outfits.Where(x => list.ContainsKey(x.Key))
                             .ToDictionary(x => x.Key, x => x.Value);
                    common.ForEach(c => x.Outfits.Remove(c.Key));
                })); ;
        }


        //public static void CategorizeNPCs()
        //{
        //    Console.WriteLine("\nCategorizing NPCs...");
        //    Dictionary<FormKey, Dictionary<string, HashSet<string>>> IdentifierGroups = new();
        //    Dictionary<string, Dictionary<FormKey, string>> map = new();

        //    var keywordFile = Settings.Patcher.KeywordFile;
        //    var SPID1 = File.ReadAllLines(keywordFile).ToList();
        //    var order = Program.Settings.State.LoadOrder.PriorityOrder
        //        .Where(x => Program.Settings.User.ModsToPatch.Contains(x.ModKey)
        //            && !Program.Settings.User.ModsToSkip.Contains(x.ModKey));

        //    //Class Based
        //    Console.WriteLine("Parsing NPC Classes...");
        //    order.WinningOverrides<IClassGetter>()
        //        .Where(c => NPCUtils.IsValidClass(c))
        //        .ForEach(c =>
        //        {
        //            var id = Regex.Replace(c.EditorID, "class|combat", "", RegexOptions.IgnoreCase);
        //            var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, id);
        //            if (!groups.Any()) groups.Add("Unknown");
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Categories").UnionWith(groups);
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Identifier").Add(id);
        //            groups.ForEach(group => map.GetOrAdd(group).Add(c.FormKey, c.EditorID));
        //        });

        //    //Outfit Based
        //    Console.WriteLine("Parsing NPC Outfits...");
        //    order.WinningOverrides<IOutfitGetter>()
        //        .Where(o => OutfitUtils.IsValidOutfit(o))
        //        .ForEach(c =>
        //        {
        //            var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, c.EditorID);
        //            if (!groups.Any()) groups.Add("Unknown");
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Categories").UnionWith(groups);
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Identifier").Add(c.EditorID);
        //            groups.ForEach(group => map.GetOrAdd(group).Add(c.FormKey, c.EditorID));
        //        });

        //    // Race Based
        //    Console.WriteLine("Parsing NPC Races...");
        //    order.WinningOverrides<IRaceGetter>()
        //        .Where(r => NPCUtils.IsValidRace(r))
        //        .ForEach(c =>
        //        {
        //            var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, c.EditorID);
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Categories").UnionWith(groups);
        //            IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Identifier").Add(c.EditorID);
        //            groups.ForEach(group => map.GetOrAdd(group).Add(c.FormKey, c.EditorID));
        //        });

        //    // Faction Based Distribution
        //    Console.WriteLine("Parsing NPC Factions...");
        //    order.WinningOverrides<IFactionGetter>()
        //    .Where(r => NPCUtils.IsValidFaction(r))
        //    .ForEach(c =>
        //    {
        //        var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, c.EditorID);
        //        if (!groups.Any()) groups.Add("Unknown");
        //        IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Categories").UnionWith(groups);
        //        IdentifierGroups.GetOrAdd(c.FormKey).GetOrAdd("Identifier").Add(c.EditorID);
        //        groups.ForEach(group => map.GetOrAdd(group).Add(c.FormKey, c.EditorID));
        //    });

        //    // Skipping specific NPCs
        //    var idx = SPID1.FindIndex(0, (x) => x.Contains("OPTypeInvalid") && !x.Contains("OPTypeValid"));
        //    var npcs = Program.Settings.User.NPCToSkip
        //            .Select(v => "0x" + v.ToString().Replace(":", "~").Replace("~Skyrim.esm", ""))
        //            .ToList();
        //    npcs.Add("Player");
        //    var npcsFilter = string.Join(",", npcs);
        //    SPID1.Insert(idx, "Keyword = OPTypeInvalid|NONE|" + string.Join(",", npcsFilter));
        //    SPID1.RemoveAt(idx + 1);

        //    if (Program.Settings.User.PatchNpcUsingSPID)
        //    {
        //        // Creating keywords
        //        Console.WriteLine("Writing dynamically generated SPID keywords...");
        //        map.OrderBy(x => x.Key).ForEach(r =>
        //        {
        //            string name = Settings.Patcher.KeywordPrefix + r.Key;
        //            var keyword = Settings.Patcher.KeywordPrefix + "Valid";
        //            if (r.Key.StartsWith("Guards")) keyword = Settings.Patcher.KeywordPrefix + "Guards";
        //            if (r.Key.StartsWith("Child")) keyword = Settings.Patcher.KeywordPrefix + "Children";

        //            var filters = r.Value.Select(v => "0x" + v.Key.ToString().Replace(":", "~").Replace("~Skyrim.esm", ""));
        //            string line = "Keyword = " + name + "|" + keyword + "|" + string.Join(",", filters);

        //            if (r.Key.StartsWith("Unknown"))
        //            {
        //                filters = r.Value.Select(v => v.Value);
        //                line = ";Keyword = " + name + "|" + keyword + "|" + string.Join(",", filters);
        //            }
        //            SPID1.Add(line);

        //            var filters1 = r.Value.Select(v => v.Value);
        //            string line1 = ";Keyword = " + name + "|" + keyword + "|" + string.Join(",", filters1);
        //            Console.WriteLine(line1);
        //        });

        //        //SPID1.Add("\n\n\n; Unknown ------------");
        //        //SPID1.Add(string.Join("\n", map.GetOrAdd("Unknown").Select(v=>v.Value).OrderBy(x=>x)));
        //        File.WriteAllLines(keywordFile, SPID1, Encoding.UTF8);
        //    }
        //    //else
        //    //    PatchNPCs(IdentifierGroups);
        //}


        ///**
        //* Returns a dictionary with outfit and number of times those are used.
        //*/
        //private void GetPatchableOutfits()
        //{
        //    Console.WriteLine("Fetching outfit records...");
        //    State.LoadOrder.PriorityOrder
        //        .WinningOverrides<INpcGetter>()
        //        .Where(x => !Program.Settings.User.ModsToSkip.Contains(x.FormKey.ModKey) 
        //                && !Program.Settings.User.NPCToSkip.Contains(x.FormKey))
        //        .Where(x => NPCUtils.IsValidActorType(x))
        //        .ForEach(npc =>
        //        {
        //            var npcs = Program.Settings.Cache.ResolveAllContexts<INpc, INpcGetter>(npc.FormKey);
        //            npcs.Where(pc=> Program.Settings.User.ModsToPatch.Contains(pc.ModKey)).ForEach(n =>
        //            {
        //                var record = n.Record;
        //                if (record.DefaultOutfit.TryResolve<IOutfitGetter>(Program.Settings.Cache, out IOutfitGetter otft))
        //                {
        //                    string otfteid = otft.EditorID;
        //                    OutfitsWithNPC.GetOrAdd(otfteid).Add(npc.FormKey);
        //                }
        //            });
        //        });

        //    // Adding valid outfits to Armor Category group
        //    var outfitNpcCount = Program.Settings.User.MinimumNpcForOutfit;
        //    var otfts = State.LoadOrder.PriorityOrder
        //        .WinningOverrides<IOutfitGetter>()
        //        .Where(x => !Program.Settings.User.ModsToSkip.Contains(x.FormKey.ModKey));
        //    foreach (IOutfitGetter outfit in otfts)
        //    {
        //        if (OutfitUtils.IsValidOutfit(outfit))
        //        {
        //            if (!OutfitsWithNPC.ContainsKey(outfit.EditorID))
        //            {
        //                OutfitsWithNPC.GetOrAdd(outfit.EditorID);
        //                AddOutfitToGroup(outfit);
        //            }
        //            else if (!OutfitsWithNPC[outfit.EditorID].Any() || OutfitsWithNPC[outfit.EditorID].Count >= outfitNpcCount)
        //                AddOutfitToGroup(outfit);
        //        }
        //        else Console.WriteLine("Skipping Invalid OTFT: " + outfit.EditorID);
        //    }
        //}

        ///*
        // * Filter outfits based on the its uses count
        // */
        //private void GroupOutfits()
        //{
        //    Console.WriteLine("Getting outfits to be patched....");
        //    foreach (IOutfitGetter outfit in State.LoadOrder.PriorityOrder
        //        .WinningOverrides<IOutfitGetter>()
        //        .Where(x => Program.Settings.User.ModsToPatch.Contains(x.FormKey.ModKey))
        //        .Where(x => OutfitUtils.IsValidOutfit(x)))
        //    {
        //        AddOutfitToGroup(outfit);
        //    }
        //    Console.WriteLine("Outfits are categorized for patching....\n\n");
        //}

        //private void PatchSPIDOutfits()
        //{
        //    // Getting SPID outfits to patch....
        //    foreach (IOutfitGetter outfit in State.LoadOrder.PriorityOrder
        //        .Where(x => Program.Settings.User.SPIDOutfitMods.Contains(x.ModKey))
        //        .WinningOverrides<IOutfitGetter>())
        //    {
        //        Patch = FileUtils.GetIncrementedMod(Patch);
        //        var lls = OutfitUtils.GetLeveledLists(Patch, outfit)
        //            //.Select(x => State.LinkCache.Resolve<ILeveledItemGetter>(x))
        //            .Where(x => x.Flags.HasFlag(LeveledItem.Flag.UseAll));

        //        var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, outfit.EditorID);
        //        groups.ForEach(group => lls.ForEach(l => AddArmorSetToGroup(group, TArmorSet.GetArmorSet(l, group))));
        //    }
        //    Console.WriteLine("SPID outfits patched....");
        //}


        //private void AddOutfitToGroup(IOutfitGetter outfit)
        //{
        //    // Handling already created outfits by patcher
        //    string eid = outfit.EditorID.StartsWith(Settings.Patcher.OutfitPrefix)
        //        && outfit.EditorID.EndsWith(Settings.Patcher.OutfitSuffix)
        //        ? outfit.EditorID.Split("_")[2] : outfit.EditorID;
        //    var groups = HelperUtils.GetRegexBasedGroup(Settings.Patcher.OutfitRegex, eid);
        //    groups.ForEach(group => GrouppedArmorSets.GetOrAdd(group, () => new TArmorGroup(group)).AddOutfit(outfit));
        //    if (!groups.Any())
        //        Console.WriteLine("Outfit Missed: {0}[{1}]=> {2}", eid, OutfitsWithNPC.GetOrAdd(eid).Count, outfit.FormKey);
        //}

    }
}
