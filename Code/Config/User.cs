using Noggog;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Newtonsoft.Json.Linq;
using log4net;
using System.ComponentModel.DataAnnotations;
using Mutagen.Bethesda.Skyrim;


namespace Code.OutfitPatcher.Config
{
    public class User 
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(User));
        [MaintainOrder]
        [JsonDiskName("OutfitDistributionPercentage")]
        [SettingName("Distribute Outfits By: ")]
        [SynthesisTooltip("distribute modded outfits by mentioned percentage.")]
        public int OutfitDistributionPercentage = 70;

        [MaintainOrder]
        [JsonDiskName("FilterUniqueNPC")]
        [SettingName("Filter Unique NPC: ")]
        [SynthesisTooltip("Outfits will not be assigned to unique NPCs when selected")]
        public bool FilterUniqueNPC = false;

        [MaintainOrder]
        [JsonDiskName("SkipGuardDistribution")]
        [SettingName("Skip Guard Distribution: ")]
        [SynthesisTooltip("Distribute armors to the guards")]
        public bool SkipGuardDistribution = true;

        [MaintainOrder]
        [JsonDiskName("SkipSluttyOutfit")]
        [SettingName("Skip Slutty Outfits: ")]
        [SynthesisTooltip("When selected, Patcher will try to skip slutty armors")]
        public bool SkipSluttyOutfit = true;

        [MaintainOrder]
        [JsonDiskName("CreateBashPatch")]
        [SettingName("Bash Patch For Leveled Lists: ")]
        [SynthesisTooltip("Outfits will not be assigned to unique NPCs when selected")]
        public bool CreateBashPatch = true;

        [MaintainOrder]
        [JsonDiskName("ResolveOutfitConflicts")]
        [SettingName("Resolve Outfit Conflicts: ")]
        [SynthesisTooltip("Resolve outfit conflicts with armor mods.\nUsing this will make sure that outfits from both mods (armor mods and load order loosing mod) are shown.")]
        public bool ResolveOutfitConflicts = true;

        [MaintainOrder]
        [JsonDiskName("DistributeWeapons")]
        [SettingName("Distribute Weapons: ")]
        [SynthesisTooltip("Distribute Weapons along with outfit. Note:- This is very slow")]
        public bool DistributeWeapons = true;

        [MaintainOrder]
        [JsonDiskName("AssignMannequinOutfits")]
        [SettingName("Assign Mannequin Outfits: ")]
        [SynthesisTooltip("Assign Outfits to Mannequins")]
        public bool AssignMannequinOutfits = true;

        [MaintainOrder]
        [JsonDiskName("NPCToSkip")]
        [SettingName("Skip NPCs: ")]
        [SynthesisTooltip("These npcs will be skipped")]
        [FormLinkPickerCustomization(typeof(INpcGetter))]
        public HashSet<IFormLinkGetter<INpcGetter>> NPCToSkip = new();

        [MaintainOrder]
        [JsonDiskName("ModsToSkip")]
        [SettingName("Skip Mods: ")]
        [SynthesisTooltip("Select the mods which you don't want to use in patcher or any mods creating issues while patching")]
        public HashSet<ModKey> ModsToSkip = new();

        [MaintainOrder]
        [JsonDiskName("SleepingOutfit")]
        [SettingName("Sleeping Outfits: ")]
        [SynthesisTooltip("Select the mods from which NPCs will use new sleeping outfits. \nNote: Only one piece armor will be selected for now. \nHard Requirements: (Sleep Tight SE or Immersive Indoor Attire and Etiquette)")]
        public HashSet<ModKey> SleepingOutfit = new();

        //[Ignore]
        //[MaintainOrder]
        //[JsonDiskName("SPIDOutfitMods")]
        //[SettingName("SPID Outfit Mods: ")]
        //[SynthesisTooltip("Select the Outfit mods which distributes outfits using SPID. \nNote: Outfit Patcher will try to merge those outfits as well")]
        //public HashSet<ModKey> SPIDOutfitMods = new();

        [MaintainOrder]
        [SettingName("Armor Mods: ")]
        [SynthesisTooltip("Select the armor mods and the outfit category.\nIf category is not selected the mod will use Generic Category.\nFor Generic category, outfit will be created based on the armor material type.\n\n" +
            "Patcher will try to select some armor mods and assign those categories. \nYou should check this list and make sure everything is correct")]
        public List<ModCategory> PatchableArmorMods = new();

        [Ignore]
        [JsonDiskName("ArmorMods")]
        public Dictionary<string, List<string>>? ArmorMods = new();

        public User()
        {
            string exeLoc = Directory.GetParent(System.Reflection.Assembly.GetAssembly(typeof(User)).Location).FullName;
            string ConfigFile = Path.Combine(exeLoc, "data", "config", "UserSettings.json");
            JObject data = JObject.Parse(File.ReadAllText(ConfigFile));

            var order = SynPoint.PatcherEnv.LoadOrder;

            CreateBashPatch = bool.Parse(data["CreateBashPatch"].ToString());
            SkipSluttyOutfit = bool.Parse(data["SkipSluttyOutfit"].ToString());
            FilterUniqueNPC = bool.Parse(data["FilterUniqueNPC"].ToString());
            ResolveOutfitConflicts = bool.Parse(data["ResolveOutfitConflicts"].ToString());
            SkipGuardDistribution = bool.Parse(data["SkipGuardDistribution"].ToString());
            DistributeWeapons = bool.Parse(data["DistributeWeapons"].ToString());

            OutfitDistributionPercentage = int.Parse(data["OutfitDistributionPercentage"].ToString());

            // Mods to Skip
            if (ModsToSkip == null || !ModsToSkip.Any())
                ModsToSkip = data["ModsToSkip"]
                    .Select(x => ModKey.FromNameAndExtension(x.ToString()))
                    .Where(x => order.ContainsKey(x))
                    .ToHashSet();

            // SPID Outfits
            //if (SPIDOutfitMods == null || !SPIDOutfitMods.Any())
            //    SPIDOutfitMods = data["SPIDOutfitMods"]
            //        .Select(x => ModKey.FromNameAndExtension(x.ToString()))
            //        .Where(x => order.ContainsKey(x))
            //        .ToHashSet();

            // Sleeping Outfit
            if (SleepingOutfit == null || !SleepingOutfit.Any())
                SleepingOutfit = data["SleepingOutfit"]
                    .Select(x => ModKey.FromNameAndExtension(x.ToString()))
                    .Where(x => order.ContainsKey(x))
                    .ToHashSet();


            // NPCs to Skip
            if (NPCToSkip == null || !NPCToSkip.Any())
                NPCToSkip = data["NPCToSkip"]
                    .Select(x => FormKey.Factory(x.ToString()).ToLinkGetter<INpcGetter>()).ToHashSet();


            // Armor Mods
            if (PatchableArmorMods == null || !PatchableArmorMods.Any())
            {
                var mods = data["ArmorMods"].ToObject<Dictionary<string, List<string>>>();
                foreach (var pair in mods)
                {
                    if (ModKey.TryFromNameAndExtension(pair.Key, out var modKey) && SynPoint.PatcherEnv.LoadOrder.ContainsKey(modKey))
                    {
                        var item = new ModCategory(modKey, pair.Value);
                        PatchableArmorMods.Add(item);
                    }
                }
            }
            PatchableArmorMods.ForEach(mc =>
            {
                ArmorMods.GetOrAdd(mc.ArmorMod.FileName)
                .AddRange(mc.Categories.Select(x => x.ToString()).Distinct());
            });
        }

    }
}
