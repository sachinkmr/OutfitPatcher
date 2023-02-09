using log4net;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Code.OutfitPatcher.Utils;
using System.Collections.Generic;
using System.IO;
using Mutagen.Bethesda.Synthesis.Settings;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System;
using System.Reflection;
using log4net.Config;

namespace Code.OutfitPatcher.Config
{
    public class Settings
    {
        [Ignore]
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Settings));
        
        [Ignore]
        public static readonly string EXE_LOC;

        [Ignore]
        public static Patcher Patcher;

        [SynthesisOrder]
        [JsonDiskName("UserSettings")]
        [SettingName("Patcher Settings: ")]
        public User User = new();

        // Properties
        [Ignore]
        public IPatcherState<ISkyrimMod, ISkyrimModGetter>? State;

        [Ignore]
        public ILinkCache<ISkyrimMod, ISkyrimModGetter>? Cache;
        
        [Ignore]
        internal LeveledItem.Flag LeveledListFlag;
        
        [Ignore]
        internal LeveledNpc.Flag LeveledNpcFlag;

        [Ignore]
        internal List<ISkyrimMod> Patches = new();        

        [Ignore]
        public static string LogsDirectory;

        static Settings() {
            
            EXE_LOC = Directory.GetParent(Assembly.GetAssembly(typeof(Settings)).Location).FullName;
            string ConfigFile = Path.Combine(EXE_LOC, "data", "config", "PatcherSettings.json");

            Console.WriteLine("\n\n**********************************");
            Console.WriteLine("Starting Outfit Patcher...");
            Patcher = FileUtils.ReadJson<Patcher>(ConfigFile);
        }

        internal void Init(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            State = state;
            Cache = State.LinkCache;

            // Logs
            LogsDirectory = Path.Combine(State.DataFolderPath, "OutfitPatcherLogs", DateTime.Now.ToString("F").Replace(":", "-"));
            Directory.CreateDirectory(LogsDirectory);

            var appender = (log4net.Appender.FileAppender)LogManager.GetRepository().GetAppenders()[0];
            appender.File = Path.Combine(LogsDirectory, "debug-");
            appender.ActivateOptions();
            Console.WriteLine("Logs Directory: " + LogsDirectory);

            LeveledListFlag = LeveledItem.Flag.CalculateForEachItemInCount.Or(LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer);
            LeveledNpcFlag = LeveledNpc.Flag.CalculateForEachItemInCount.Or(LeveledNpc.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer);
            
            Patcher.Init(state);
            Console.WriteLine("Settings are loaded...\n\n");
        }
    }
}
