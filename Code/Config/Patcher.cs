
using log4net;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Code.OutfitPatcher.Armor;
using Code.OutfitPatcher.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Code.OutfitPatcher.Config
{
    public class Patcher
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Patcher));
        
        
        // Settings.json properties
        public string? InvalidArmorsRegex;
        public string? InvalidFactionRegex;
        public string? ValidFactionRegex;
        public string? ValidArmorsRegex;
        public string? ValidNpcRegex;
        public string? InvalidNpcRegex;
        public string? SluttyRegex;
        public string? SleepOutfitRegex;
        public string? ValidMaterial;
        public string? InvalidMaterial;

        public string? KeywordFile;
        public string? SPIDFile;

        // Prefixes and suffixes
        public string? PatcherPrefix;
        public string? LeveledListPrefix;
        public string? OutfitPrefix;
        public string? OutfitSuffix;
        public string? KeywordPrefix;

        public List<string> Masters = new();
        public List<string> Categories = new() { "Generic"};

        public Dictionary<string, string> OutfitRegex = new();
        public Dictionary<string, string> ArmorTypeRegex = new();

        public Patcher Init(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {
            // SPID Files
            SPIDFile = Path.Combine(state.DataFolderPath, PatcherPrefix + "_DISTR.ini");
            KeywordFile = Path.Combine(state.DataFolderPath,  PatcherPrefix + "Keywords_DISTR.ini");
            File.Copy(Path.Combine(Settings.EXE_LOC, "data", "config", "Keywords.ini"), KeywordFile, true);

            // Parsing SPID Keywords
            Console.WriteLine("Parsing SPID Keywords... ");
            Categories.AddRange(FileUtils.ParseSPIDKeywords(state.DataFolderPath));
            Categories.AddRange(OutfitRegex.Keys);
            Categories = Categories.Distinct().ToList();
            return this;
        }
    }
}
