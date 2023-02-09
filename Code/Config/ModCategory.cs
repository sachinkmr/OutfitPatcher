using log4net;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Code.OutfitPatcher.Armor;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;

namespace Code.OutfitPatcher.Config
{
    public class ModCategory
    {
        [MaintainOrder]
        [SettingName("Armor Mod: ")]
        [SynthesisTooltip("Select the armor mods to create outfits")]
        public ModKey ArmorMod = new();

        [MaintainOrder]
        [SettingName("Outfit Categories: ")]
        [SynthesisTooltip("Select the categories for the above selected armor mod to create outfits. " +
            "\nOutfits created by the mod will be distributes among these categories")]
        public List<string> Categories = new();

        [MaintainOrder]
        [SettingName("Outfit Categories1: ")]
        [SynthesisTooltip("Select the categories for the above selected armor mod to create outfits. " +
            "\nOutfits created by the mod will be distributes among these categories")]
        public List<CategoryEnum> CategoriesEnum = new();

        public ModCategory() { }

        public ModCategory(ModKey ArmorMod, List<string> Categories) { 
            this.ArmorMod = ArmorMod;
            this.Categories = Categories;
        }

        //private static void createEnum() {
        //    // Get the current application domain for the current thread.
        //    AppDomain currentDomain = AppDomain.CurrentDomain;

        //    // Create a dynamic assembly in the current application domain,
        //    // and allow it to be executed and saved to disk.
        //    AssemblyName aName = new AssemblyName("TempAssemblySynCatEnum");
        //    AssemblyBuilder ab = currentDomain.DefineDynamicAssembly(
        //        aName, AssemblyBuilderAccess.RunAndSave);

        //    // Define a dynamic module in "TempAssembly" assembly. For a single-
        //    // module assembly, the module has the same name as the assembly.
        //    ModuleBuilder mb = ab.DefineDynamicModule(aName.Name + ".dll");

        //    // Define a public enumeration with the name "Elevation" and an
        //    // underlying type of Integer.
        //    EnumBuilder eb = mb.DefineEnum("Elevation", TypeAttributes.Public, typeof(int));
        //}
    }
}
