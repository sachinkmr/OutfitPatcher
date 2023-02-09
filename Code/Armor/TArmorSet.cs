
using log4net;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Code.OutfitPatcher.Config;
using Code.OutfitPatcher.Utils;

namespace Code.OutfitPatcher.Armor
{
    public class TArmorSet
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TArmorSet));
        public HashSet<TArmor> Armors { get; }
        public HashSet<FormKey> Weapons { get; }
        public TArmor Body { get; }
        public TGender Gender { get; set; }
        public string Material { get; set; }
        public TArmorType Type { get; }
        public string EditorID { get; set; }
        public string LoadOrder { get; set; }
        public FormKey LLFormKey;

        public bool hasShield;
        public bool hasHalmet;

        

        public TArmorSet(TArmor body, string material)
        {
            Body = body;
            Armors = new();
            Weapons = new();
            Material = material;
            Type = body.Type;
            Gender = body.Gender;
            EditorID = Settings.Patcher.LeveledListPrefix + Body.Gender + "_" + Material + "_"+ Body.EditorID;
            LoadOrder = "";
            LLFormKey = FormKey.Null;
            Armors.Add(body);
        }

        public static TArmorSet GetArmorSet(ILeveledItemGetter LL, string material) {
            var armors = LeveledListUtils.GetArmorList(SynPoint.Settings.Cache, LL)
            .Select(x => new TArmor(x, material)).ToHashSet();
            var body = armors.Where(x => x.IsBody()).First();

            var set = new TArmorSet(body, material);
            set.AddArmors(armors);
            return set;
        }

        public void AddArmor(TArmor armor)
        {
            Armors.Add(armor);
        }

        public void AddArmors(IEnumerable<TArmor> armors)
        {
            armors.ForEach(a => Armors.Add(a));
        }

        public void AddWeapons(IEnumerable<IWeaponGetter> weapons)
        {
            weapons.ForEach(a => Weapons.Add(a.FormKey));
        }

        public void AddWeapons(IEnumerable<TWeapon> weapons)
        {
            weapons.ForEach(a => Weapons.Add(a.FormKey));
        }

        public void AddWeapon(IWeaponGetter weapon)
        {
            Weapons.Add(weapon.FormKey);
        }

        public ISkyrimMod CreateLeveledList(ISkyrimMod Patch)
        {
            if(!LLFormKey.IsNull) return Patch;
            LeveledItem ll = null;
            Patch = FileUtils.GetIncrementedMod(Patch);
            var items = Armors.Select(a => a.FormKey.ToLink<IItemGetter>())
                    .Union(Weapons.Select(a => a.ToLink<IItemGetter>()).EmptyIfNull());
            ll = LeveledListUtils.CreateLeveledList(Patch, items, EditorID, 1, LeveledItem.Flag.UseAll);
            LLFormKey = ll.FormKey;
            return Patch;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}:{3}:{4}", Body.Name, Material, Type, Gender, Armors.Count());
        }

        public override bool Equals(object? obj)
        {
            return obj is TArmorSet set &&
                   EditorID == set.EditorID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EditorID);
        }

        public void CreateMatchingSetFrom(IEnumerable<IWeaponGetter> weapons, int bodyCounts, bool addAll = false)
        {
            CreateMatchingSetFrom(weapons.Select(w => new TWeapon(w)).ToHashSet(), bodyCounts, addAll);
        }

        public void CreateMatchingSetFrom(HashSet<TWeapon> weapons, int bodyCounts, bool addAll = false)
        {
            Dictionary<string, Dictionary<int, TWeapon>> matchedMap = new();
            bool matched = false;
            if (!addAll)
            {
                foreach (var weapon in weapons)
                {
                    int matchingWords = HelperUtils.GetMatchingWordCount(Body.Name, weapon.Name, false);
                    if (matchingWords > 0)
                    {
                        matched = true;
                        matchedMap.GetOrAdd(weapon.Type)
                         .GetOrAdd(matchingWords, () => weapon);
                    }
                }
                var weaps = matchedMap.Values.Select(x => x.OrderBy(k => k.Key).Last().Value);
                this.AddWeapons(weaps);
            }
            else if (addAll || (!matched && bodyCounts < 5)) this.AddWeapons(weapons);
        }

        public void CreateMatchingSetFrom(IEnumerable<TArmor> others, bool addAll, int commonName)
        {
            if (!others.Any())
            {
                Console.WriteLine("No matching armor found for {0}: {1}", Body.EditorID, Body.FormKey);
                return;
            }
            if (!addAll)
            {
                Dictionary<TBodySlot, Dictionary<int, TArmor>> armors = new();                
                var bname = Body.Name;
                foreach (var a in others) {
                    // Name based matching
                    var aname = a.Name;
                    int c = HelperUtils.GetMatchingWordCount(bname, aname, false) - commonName;
                    if (c > 0) a.BodySlots.ForEach(flag => armors.GetOrAdd(flag).TryAdd(c, a));
                }

                var marmors = armors.Values.Select(x => x.OrderBy(k => k.Key).Last().Value).Distinct();
                this.AddArmors(marmors);
            }
            else this.AddArmors(others);
        }
    }
}
