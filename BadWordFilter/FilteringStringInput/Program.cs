using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FilteringStringInput
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string> badWords = new Dictionary<string, string>()
            {
                {"poop*", "p**phead"},
                {"*HB", "boss"},
                {"gotten", "become"},
                {"*crap*", "taco supreme"},
                {"Fun", "!in"}
            };
            string input = "My PHB is such a poophead. It's gotten worse since his promotion. Fun fact you might call him a supercraphead.";          
            Console.WriteLine($"Original Input: {input}\n");

            string filteredInput = BadWordFilter.Replace(input, badWords);
            Console.WriteLine($"Filter #1: {filteredInput}\n");

            //Filter #2 can't handle conditional caps
            badWords.Remove("Fun");
            BadWordFilter2.Replace(ref input, badWords);
            Console.WriteLine($"Filter #2: {input}");

            Console.ReadKey();
        }
    }

    public class BadWordFilter
    {
        public static string Replace(string input, IDictionary<string, string> badWordMap)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input), "String cannot be null, empty, or whitespace.");

            Dictionary<string, string> idMap = new Dictionary<string, string>();
            StringBuilder pattern = new StringBuilder();
            int idCounter = 0;

            //Map replacement value to an ID, use ID and key to construct compound match pattern
            foreach (KeyValuePair<string, string> badWord in badWordMap)
            {
                string id = "ID" + idCounter++;
                idMap.Add(id, badWord.Value);
                ConstructMatchPattern(badWord.Key, id, pattern);
            }
            //Remove unnecessary first | in pattern
            pattern.Remove(0, 1);

            Regex filter = new Regex(pattern.ToString(), RegexOptions.IgnoreCase);
            string[] groupNames = filter.GetGroupNames();
            MatchEvaluator evaluator = match =>
            {
                string replacement = "";
                //Determine which group was matched, retrieve replacement value
                for (int i = 1; i < groupNames.Length; i++)
                    if (match.Groups[groupNames[i]].Success)
                    {
                        replacement = idMap[groupNames[i]];
                        break;
                    }

                //Handle caps as necessary
                if (replacement.StartsWith("!"))
                {
                    replacement = replacement.Remove(0, 1);
                    //All caps
                    if (match.Value == match.Value.ToUpper())
                        replacement = replacement.ToUpper();
                    //First letter cap
                    else if (match.Value[0] == char.ToUpper(match.Value[0]))
                        replacement = char.ToUpper(replacement[0]) + replacement.Substring(1);
                }
                return replacement;
            };

            return filter.Replace(input, evaluator);
        }

        private static void ConstructMatchPattern(string badWord, string id, StringBuilder pattern)
        {
            if (string.IsNullOrWhiteSpace(badWord))
                return;
            int patternLength = pattern.Length;
            pattern.Append($@"|(?<{id}>(?:\b){badWord.Trim('*')}");
            //Wildcard at start
            if (badWord.StartsWith("*"))
                pattern.Insert(patternLength + id.Length + 11, @"\w*", 1);
            //Wildcard at end
            if (badWord.EndsWith("*"))
                pattern.Append(@"\w*");
            pattern.Append(')');
        }
    }

    public class BadWordFilter2
    {
        public static void Replace(ref string input,
            IDictionary<string, string> badWordMap)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input),
                    "String cannot be null, empty, or whitespace.");

            foreach (KeyValuePair<string, string> badWord in badWordMap)
                ReplaceBadWord(ref input, badWord);
        }

        private static void ReplaceBadWord(ref string input,
            KeyValuePair<string, string> badWord)
        {
            if (string.IsNullOrWhiteSpace(badWord.Key))
                throw new ArgumentException(nameof(badWord.Key),
                    "Key cannot be null, empty, or whitespace.");

            string pattern = GetReplacementPattern(badWord.Key);
            MatchEvaluator evaluator = match =>
            {
                string replacement = badWord.Value;
                if (match.Value == match.Value.ToUpper())
                {
                    if (badWord.Key != badWord.Key.ToUpper())
                        replacement = badWord.Value.ToUpper();
                }
                else if (match.Value[0] == char.ToUpper(match.Value[0]))
                    replacement = char.ToUpper(badWord.Value[0]) + badWord.Value.Substring(1);
                return replacement;
            };
            input = Regex.Replace(input, pattern, evaluator, RegexOptions.IgnoreCase);
        }

        private static string GetReplacementPattern(string badWordKey)
        {
            StringBuilder pattern = new StringBuilder(
                $@"(?:\b){badWordKey.Trim('*')}"
            );
            if (badWordKey.StartsWith("*"))
                pattern.Insert(6, @"\w*", 1);
            if (badWordKey.EndsWith("*"))
                pattern.Append(@"\w*");
            return pattern.ToString();
        }
    }
}
