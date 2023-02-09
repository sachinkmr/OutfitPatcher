using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Noggog;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Code.OutfitPatcher.Utils;
using OutfitPatcher;
using Code.OutfitPatcher.Config;
using log4net;
using Code.OutfitPatcher.Managers;


namespace Code.OutfitPatcher.Utils
{
    public class CustomHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CustomHelper));

        public static string GetCategories()
        {
            var i = 0;
            var list = new List<string>()
            .Concat(Settings.Patcher.OutfitRegex.Keys.OrderBy(x => x))
            .Select(x => x + " = " + (i++));
            return string.Join(",\n", list);
        }

        public static void GiftsOfAkatoshPatcher()
        {
            var patchName = "GiftsOfAkatoshPatcher.esp";
            //var espName = "gifts of akatosh.esp";
            //ModKey modKey = ModKey.FromNameAndExtension(espName);
            //var patch = Program.Settings.State.LoadOrder[modKey].Mod;
            var patch = FileUtils.GetOrAddPatch(patchName);
            var eff = SynPoint.Settings.State.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides()
                .Where(x => x.FormKey.ToString().Contains("gifts of akatosh.esp")
                        && x.Name.ToString().Contains("Akatosh"));

            eff.GroupBy(x => x.TargetType).ForEach(t =>
            {
                var r = patch.ObjectEffects.DuplicateInAsNewRecord(t.First(), t.First().EditorID + "GOD");
                r.Effects.Clear();
                var e = t.SelectMany(a => a.Effects).Distinct();
                e.ForEach(e => r.Effects.Add(e.DeepCopy()));
            });
        }

        internal static void getESLifiableMods(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var cache = state.LinkCache;
            var esls = state.LoadOrder.PriorityOrder
                .Where(x => !x.Mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.LightMaster) && FileUtils.CanESLify(x.Mod, 2047))
                .Select(x => x.Mod)
                .ToList();
            foreach (var esl in esls)
            {
                var fileName = esl.ModKey.FileName.ToString();
                var elsifiable = false;
                foreach (var r in esl.EnumerateMajorRecords())
                {
                    var contexts = cache.ResolveAllContexts(r.FormKey).Select(c => c.ModKey.FileName.ToString()).ToList();
                    if (!contexts.IndexOf(fileName).Equals(contexts.Count - 1))
                    {
                        elsifiable = false;
                        break;
                    }
                    else
                    {
                        elsifiable = true;
                    }
                }
                if (elsifiable) Console.WriteLine("{0} can be ESL", fileName);
            }
        }

        public static void MoveMods(string modNames, string src, string dest)
        {
            var lines = File.ReadAllLines(modNames).Select(x => x[1..]);
            foreach (var line in lines)
            {
                CopyDirectory(Path.Combine(src, line), Path.Combine(dest, line));
            }
        }

        public static void ShowOptionalPlugins(string modNames, string modsDir)
        {
            var lines = File.ReadAllLines(modNames).Select(x => x[1..].Trim());
            foreach (var line in lines)
            {
                string src = Path.Combine(modsDir, line, "optional");
                string des = Path.Combine(modsDir, line);
                if (Directory.Exists(src)) {
                    var allFiles = Directory.GetFiles(src, "*.es*", SearchOption.TopDirectoryOnly);
                    foreach (string newPath in allFiles)
                    {
                        File.Copy(newPath, newPath.Replace(src, des), true);
                    }
                    Console.WriteLine("Processed: " + line);
                }                
            }
        }

        public static void HideOptionalPlugins(string modNames, string modsDir)
        {
            var lines = File.ReadAllLines(modNames).Select(x => x[1..].Trim());
            foreach (var line in lines)
            {
                string des = Path.Combine(modsDir, line, "optional");
                string src = Path.Combine(modsDir, line);
                if (Directory.Exists(src))
                {
                    var allFiles = Directory.GetFiles(src, "*.es*", SearchOption.TopDirectoryOnly);
                    foreach (string newPath in allFiles)
                    {
                        File.Copy(newPath, newPath.Replace(src, des), true);
                    }
                    Console.WriteLine("Processed: " + line);
                }
            }
        }

        public static void MergeMods(string modNames, string src, string mergeName = "All Armor Mods")
        {
            var lines = File.ReadAllLines(modNames).Select(x => x[1..]).Reverse();
            var dest = Path.Combine(src, mergeName);
            foreach (var line in lines)
            {
                var mod = Path.Combine(src, line);
                CopyDirectory(mod, dest);
                Console.WriteLine("Copied: " + mod);
            }
        }

        public static void GetSovnNPCsWithOutfits(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string outFile)
        {
            // Getting NPC using Sovn armors
            var sovnOTFTs = state.LoadOrder.PriorityOrder
                .Where(x => x.ModKey.FileName.String.Equals("Sovn's Armor and Weapons Merge.esp"))
                .Outfit().WinningOverrides().Where(x => x.FormKey.ToString().Contains("Sovn"))
                .Select(x => x.FormKey);


            var skippableNPC = state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>()
                 .Where(n => NPCUtils.IsUnique(n)
                             && n.DefaultOutfit != null
                             && sovnOTFTs.Contains(n.DefaultOutfit.FormKey))
                 .ToDictionary(x => x.FormKey, x => x.EditorID);
            FileUtils.WriteJson(outFile, skippableNPC);
        }

        public static void MergePlugins(string loc, bool show)
        {
            JObject data = JObject.Parse(File.ReadAllText(loc));
            var plugins = data.GetValue("plugins");
            foreach (var plugin in plugins)
            {
                var filename = plugin.Value<string>("filename");
                var fileLoc = plugin.Value<string>("dataFolder");

                var src = show ? Path.Combine(fileLoc, "optional", filename) : Path.Combine(fileLoc, filename);
                var dest = show ? Path.Combine(fileLoc, filename) : Path.Combine(fileLoc, "optional", filename);
                if (!File.Exists(Path.GetDirectoryName(dest))) Directory.CreateDirectory(Path.GetDirectoryName(dest));
                try
                {
                    if (File.Exists(src)) File.Move(src, dest, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void UpdateSPIDFile(string mergeMap, List<string> spidINIs, string mergedMod)
        {
            var trimmer = new char[] { '0' };
            JObject data = JObject.Parse(File.ReadAllText(mergeMap));
            string spid = "";

            foreach (var v2 in spidINIs)
            {
                var spidLines = File.ReadAllLines(v2);
                var spidTxt = string.Join("\n", spidLines);
                if (!Regex.IsMatch(spidTxt, "\\.es[plm]\\|",RegexOptions.IgnoreCase)) continue;
                spid = spid + "\n;Merging SPID Records for " + Path.GetFileName(v2) + "\n" + spidTxt;
                var esps = spidLines.Where(l => l.Contains(".esp|")).Select(line =>
                {
                    int start = line.IndexOf('~') + 1;
                    return line[start..line.IndexOf('|')];
                }).Distinct();
                esps.ToList().ForEach(esp =>
                {
                    data.GetValue(esp).Select(v => v.ToString().Replace("\"", "").Replace(" ", "").Split(':').Select(a => a.TrimStart(trimmer))).ToList().ForEach(v =>
                    {
                        var str1 = "0x" + v.ElementAt(0) + "~" + esp;
                        var str2 = "0x" + v.ElementAt(1) + "~" + mergedMod;
                        //spid = spid.Replace(str1, str2);
                        spid = Regex.Replace(spid, str1, str2, RegexOptions.IgnoreCase);
                    });
                });
                File.Move(v2, v2+".mohidden");
            }
            var file = Path.Combine(Directory.GetParent(Directory.GetParent(mergeMap).FullName).FullName, Path.GetFileNameWithoutExtension(mergedMod) + "_DISTR.ini");
            File.WriteAllText(file, spid);
        }

        public static void CopyDirectory(string sourcePath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                Console.WriteLine("Skipping: " + targetPath);
                return;
            }

            //Now Create all of the directories
            Directory.CreateDirectory(targetPath);
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}