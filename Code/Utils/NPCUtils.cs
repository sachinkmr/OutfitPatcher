using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Code.OutfitPatcher.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Noggog;
using log4net;
using Code.OutfitPatcher.Managers;

namespace Code.OutfitPatcher.Utils
{
    public class NPCUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NPCUtils));
        public static string GetName(INpcGetter npc)
        {
            return npc.Name == null || npc.Name.String.IsNullOrEmpty()
                ? HelperUtils.SplitString(npc.EditorID) : npc.Name.ToString();
        }

        public static bool IsValidFaction(IFactionGetter faction)
        {
            return IsValidFaction(faction.EditorID);
        }

        public static bool IsValidFaction(string faction)
        {
            return Regex.Match(faction, Settings.Patcher.ValidFactionRegex, RegexOptions.IgnoreCase).Success
                    || !Regex.Match(faction, Settings.Patcher.InvalidFactionRegex, RegexOptions.IgnoreCase).Success;
        }

        public static bool IsValidClass(IClassGetter classGetter)
        {
            return IsValidClass(classGetter.EditorID);
        }

        public static bool IsValidClass(string faction)
        {
            return Regex.Match(faction, Settings.Patcher.ValidFactionRegex, RegexOptions.IgnoreCase).Success
                    || !Regex.Match(faction, Settings.Patcher.InvalidFactionRegex, RegexOptions.IgnoreCase).Success;
        }

        public static bool IsValidNPCName(INpcGetter npc)
        {
            return IsValidNPCName(npc.EditorID);
        }

        public static bool IsValidNPCName(string npc)
        {
            return Regex.Match(npc, Settings.Patcher.ValidNpcRegex, RegexOptions.IgnoreCase).Success
                    || !Regex.Match(npc, Settings.Patcher.InvalidNpcRegex, RegexOptions.IgnoreCase).Success;
        }

        public static bool IsChild(INpcGetter npc) {
            return IsChild(SynPoint.Settings.Cache.Resolve<IRaceGetter>(npc.Race.FormKey))
                || IsChild(SynPoint.Settings.Cache.Resolve<IClassGetter>(npc.Class.FormKey).EditorID);
        }

        public static bool IsChild(IRaceGetter race)
        {
            return IsChild(race.EditorID);
        }

        public static bool IsChild(string race)
        {
            return Regex.IsMatch(race, "child", RegexOptions.IgnoreCase);
        }

        public static bool IsValidNPC(INpcGetter npc) {
            return !SynPoint.Settings.User.NPCToSkip.Contains(npc.FormKey.ToLinkGetter<INpcGetter>())
                && IsValidActorType(npc)
                && IsValidNPCName(npc.EditorID);
        }

        public static bool IsValidActorType(INpcGetter npc)
        {
            return IsValidActorType(npc, SynPoint.Settings.Cache);
        }

        public static bool IsValidActorType(INpcGetter npc, ILinkCache cache)
        {
            var r = cache.Resolve<IRaceGetter>(npc.Race.FormKey);
            return r.HasKeyword(Skyrim.Keyword.ActorTypeNPC)
                || npc.HasKeyword(Skyrim.Keyword.ActorTypeNPC);
        }

        //public static bool IsSkipable(INpcGetter npc) { 
        //    return HelperUtils.GetRegexBasedGroup(Settings.Patcher.SkippableRegex, GetName(npc)).Any();
        //}
            
        public static bool IsValidRace(IRaceGetter r) {
            return r.HasKeyword(Skyrim.Keyword.ActorTypeNPC);
            //return !(r.HasKeyword(Skyrim.Keyword.ActorTypeAnimal)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeCow)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeCreature)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeDragon)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeDwarven)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeFamiliar)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeGiant)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeHorse)
            //    || r.HasKeyword(Skyrim.Keyword.ActorTypeTroll));
        }

        public static bool IsGuard(INpcGetter npc) {
            return npc.Factions.Any(x => x.Faction.FormKey.Equals(Skyrim.Faction.GuardDialogueFaction.FormKey));
        }
            

        public static bool IsUnique(INpcGetter npc)
        {
            return npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique);
        }

        public static bool IsEssential(INpcGetter npc)
        {
            return npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Essential);
        }

        public static bool IsProtected(INpcGetter npc)
        {
            return npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Protected);
        }

        public static bool IsGhost(INpcGetter npc)
        {
            return npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsGhost);
        }

        public static bool IsFemale(INpcGetter npc)
        {
            return npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
        }

        public static bool IsFollower(INpcGetter npc)
        {
            return npc.Factions.Any(r => r.Faction.FormKey.Equals(Skyrim.Faction.CurrentFollowerFaction.FormKey)
                                    || r.Faction.FormKey.Equals(Skyrim.Faction.DismissedFollowerFaction.FormKey)
                                    || r.Faction.FormKey.Equals(Skyrim.Faction.PotentialFollowerFaction.FormKey)
                                    || r.Faction.FormKey.Equals(Skyrim.Faction.PlayerFollowerFaction.FormKey));
        }
    }
}
