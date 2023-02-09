using Code.OutfitPatcher.Config;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using Code.OutfitPatcher.Managers;

namespace Code.OutfitPatcher.Utils
{

    public static class HelperUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(HelperUtils));
        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static T ToEnum<T>(this string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }

        public static List<string> GetRegexBasedGroup(Dictionary<string, string> regx, string str, string? optionalStr = null)
        {
            List<string> group = new();
            regx.ForEach(pair =>
             {
                 string[] regex = pair.Value.Split("::");
                 if ((regex.Length == 1 ? Regex.Match(str ?? "", regex[0], RegexOptions.IgnoreCase).Success
                         : Regex.Match(str ?? "", regex[0], RegexOptions.IgnoreCase).Success
                         && !Regex.Match(str ?? "", regex[1], RegexOptions.IgnoreCase).Success))
                 {
                     group.Add(pair.Key);
                 }
                 if (optionalStr != null && (regex.Length == 1 ? Regex.Match(optionalStr ?? "", regex[0], RegexOptions.IgnoreCase).Success
                         : Regex.Match(optionalStr ?? "", regex[0], RegexOptions.IgnoreCase).Success
                         && !Regex.Match(optionalStr ?? "", regex[1], RegexOptions.IgnoreCase).Success))
                 {
                     group.Add(pair.Key);
                 }
             });
            return group.Distinct().ToList();
        }

        
        public static string ToCamelCase(this string text)
        {
            return string.Join(" ", text
                                .Split()
                                .Select(i => char.ToUpper(i[0]) + i.Substring(1)));
        }
         
        public static string GetLongestCommonSubstring(string s1, string s2)
        {
            int[,] a = new int[s1.Length + 1, s2.Length + 1];
            int row = 0;    // s1 index
            int col = 0;    // s2 index

            for (var i = 0; i < s1.Length; i++)
                for (var j = 0; j < s2.Length; j++)
                    if (s1[i] == s2[j])
                    {
                        int len = a[i + 1, j + 1] = a[i, j] + 1;
                        if (len > a[row, col])
                        {
                            row = i + 1;
                            col = j + 1;
                        }
                    }

            return s1.Substring(row - a[row, col], a[row, col]);
        }

        public static HashSet<string> GetCommonItems(List<string> list)
        {
            if (!list.Any()) return new();
            var hash = new HashSet<string>(list.First().Split(' '));
            for (int i = 1; i < list.Count; i++) {
                hash.IntersectWith(list[i].Split(' '));
                if (!hash.Any())
                    break;
            }       
            return hash;
        }

        public static int GetMatchingWordCount(string strOne, string strTwo, bool split=true)
        {
            if (split) {
                strOne = SplitString(strOne);
                strTwo = SplitString(strTwo);
            }           

            var tokensOne = strOne.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var list = tokensOne.Where(x => strTwo.Contains(x));
            return list.Count();
        }
        
        public static string SplitString(string input)
        {
            var underscore = Regex.Replace(input, "[_/-]", " ", RegexOptions.Compiled).Trim();
            return Regex.Replace(underscore, "([a-z0-9])([A-Z])", "$1 $2", RegexOptions.Compiled).Trim();
        }
        
        


        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }
    }
}
