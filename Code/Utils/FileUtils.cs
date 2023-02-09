using Code.OutfitPatcher.Config;
using Code.OutfitPatcher.Converters;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using log4net;
using Code.OutfitPatcher.Managers;


namespace Code.OutfitPatcher.Utils
{
    public static class FileUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileUtils));
        public static BinaryWriteParameters SafeBinaryWriteParameters => new()
        {
            MasterFlag = MasterFlagOption.ChangeToMatchModKey,
            ModKey = ModKeyOption.CorrectToPath,
            RecordCount = RecordCountOption.Iterate,
            LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
            MastersListContent = MastersListContentOption.Iterate,
            FormIDUniqueness = FormIDUniquenessOption.Iterate,
            NextFormID = NextFormIDOption.Iterate,
            CleanNulls = true,
            MastersListOrdering = MastersListOrderingByLoadOrder.Factory(SynPoint.Settings.State.LoadOrder.ListedOrder.Select(x=>x.ModKey))
        };

        public static ISkyrimMod GetIncrementedMod(ISkyrimMod mod, bool forceCreate=false)
        {
            if (!forceCreate && GetMasters(mod).Count < 250 && CanESLify(mod))
                return mod;

            string? name;
            try
            {
                var indx = Int32.Parse(mod.ModKey.Name.Last().ToString());
                name = mod.ModKey.Name.Replace(indx.ToString(), (indx + 1).ToString());
            }
            catch
            {
                name = mod.ModKey.Name + " 1";
            }
            SynPoint.Patch = GetOrAddPatch(name);
            return SynPoint.Patch;
        }

        public static List<string> GetMasters(ISkyrimModGetter mod) {
            return mod.EnumerateMajorRecords()
                .Where(x => !x.FormKey.ModKey.Equals(mod.ModKey))
                .Select(x=> x.FormKey.ModKey.FileName.String)
                .Distinct()
                .ToList();
        }

        public static bool CanESLify(ISkyrimModGetter mod, int count = 2000) {
            return mod.EnumerateMajorRecords().Where(x => x.FormKey.ModKey.Equals(mod.ModKey)).Count() < count;
        }

        public static T ReadJson<T>(string filePath)
        {
            using StreamReader r = new(filePath);
            string json = r.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void WriteJson(string filePath, Object classInfo)
        {
            JsonConverter[] converters = new JsonConverter[] {
                new FormKeyConverter(),
                new BodySlotConverter(),
                new DictionaryConverter()
            };
            //File.SetAttributes(filePath, FileAttributes.Normal);
            using (StreamWriter r = File.CreateText(filePath)) {
                r.Write(JsonConvert.SerializeObject(classInfo, Formatting.Indented, converters));
                r.Flush();
            }            
        }

        public static void SaveMod(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod patch)
        {
            var patchFile = patch.ModKey.FileName;
            var records = patch.EnumerateMajorRecords().Where(r=> r.FormKey.ModKey.Equals(patch.ModKey));
            if (CanESLify(patch, 2047))
                patch.ModHeader.Flags = SkyrimModHeader.HeaderFlag.LightMaster;
            string location = Path.Combine(state.DataFolderPath, patchFile);
            patch.WriteToBinary(location, FileUtils.SafeBinaryWriteParameters);
            Console.WriteLine("Saved Patch: {0} ", patchFile);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static ISkyrimMod GetOrAddPatch(string espName)
        {
            espName = espName.EndsWith(".esp") ? espName : espName + ".esp";
            espName = espName.StartsWith(Settings.Patcher.PatcherPrefix) ? espName : Settings.Patcher.PatcherPrefix + espName;
            ModKey modKey = ModKey.FromNameAndExtension(espName);

            if (SynPoint.Settings.State.LoadOrder.ContainsKey(modKey))
                return SynPoint.Settings.Patches.Find(x=>x.ModKey.Equals(modKey));

            ISkyrimMod patch = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);
            SynPoint.Settings.Patches.Add(patch);

            var listing = new ModListing<ISkyrimModGetter>(patch, true);
            SynPoint.Settings.State.LoadOrder.Add(listing);
            return patch;
        }

        public static void CopyDirectory(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            Directory.CreateDirectory(targetPath);
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }

        public static List<string> ParseSPIDKeywords(string path)
        {
            HashSet<string> lines = new();
            HashSet<string> keywrds = new();
            var regex = @"^Keyword\s*=\s*(.+?)\|";
            var filePaths = Directory.GetFiles(path, "*_DISTR.ini")
                .Where(x=> !Path.GetFileName(x).Contains(Settings.Patcher.PatcherPrefix));
            foreach (string f in filePaths) {
                File.ReadAllLines(f)
                    .ForEach(l =>
                    {
                        var m = Regex.Match(l.Trim(), regex);
                        var val = m.Groups[1].Value.Trim();
                        if (m.Success && !Regex.IsMatch(val, "\\.es[p|m|l]$", RegexOptions.IgnoreCase))
                            keywrds.Add(val);
                    });
                
                //lines.UnionWith(ls);    
            }
            return keywrds.ToList(); 
        }
    }
}
