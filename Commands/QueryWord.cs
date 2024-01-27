using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Graphics = GameOverlay.Drawing.Graphics;

namespace QuickOper.Commands
{
    internal class QueryWord: Command
    {
    public class DictEntry
    {
        public string word { get; set; }
        public string phonetic { get; set; }
        public string definition { get; set; }
        public string translation { get; set; }
        public string pos { get; set; }
        public string collins { get; set; }
        public string oxford { get; set; }
        public string tag { get; set; }
        public int bnc { get; set; }
        public int frq { get; set; }
        public string exchange { get; set; }
        public string detail { get; set; }
        public string audio { get; set; }
    }

    private bool nodict = false;
    private static List<DictEntry> entries = new();

        /// https://stackoverflow.com/questions/9453731/how-to-calculate-distance-similarity-measure-of-given-2-strings
        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// </summary>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        public static int DamerauLevenshteinDistance(string source, string target, int threshold)
        {
            if (source == target) return -1;
            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length 
            if (length1 > length2)
            {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++)
            {

                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++)
                {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }

        static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }
        
        public QueryWord()
    {
        if (entries.Count == 0)
        {
            var dictPath = "J:/ecdict.csv";
            if (!System.IO.File.Exists(dictPath))
            {
                dictPath = "./ecdict.csv";
            }
            if (!System.IO.File.Exists(dictPath))
            {
                nodict = true;
            }
            else
            {
                using (var reader = new StreamReader(dictPath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<DictEntry>();
                    entries.AddRange(records.Where(v => v.word.Length < 25 && !v.word.Contains(" ")));
                }
                }
            
            }
    }
        static List<T> GetTopAscendingElements<T, TKey>(List<T> list, Func<T, TKey> keySelector, int count)
        {
            if (count <= 0 || list.Count == 0)
                return new List<T>();

            int left = 0;
            int right = list.Count - 1;

            while (true)
            {
                int pivotIndex = Partition(list, left, right, keySelector);

                if (pivotIndex == count - 1)
                    return list.Take(count).ToList();
                else if (pivotIndex > count - 1)
                    right = pivotIndex - 1;
                else
                    left = pivotIndex + 1;
            }
        }

        static int Partition<T, TKey>(List<T> list, int left, int right, Func<T, TKey> keySelector)
        {
            int pivotIndex = left;
            TKey pivotValue = keySelector(list[pivotIndex]);

            Swap(list, pivotIndex, right);

            int storeIndex = left;

            for (int i = left; i < right; i++)
            {
                if (Comparer<TKey>.Default.Compare(keySelector(list[i]), pivotValue) <= 0)
                {
                    Swap(list, i, storeIndex);
                    storeIndex++;
                }
            }

            Swap(list, storeIndex, right);

            return storeIndex;
        }

        static void Swap<T>(List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        private List<DictEntry> currentQueryResults = new();
        private string currentKeyword = null;
        public override void InputUpdated(ref string cmd)
        {
            
            var queryWord = string.Join(" ", cmd.Split(" ").Skip(1).Where(v=>!v.StartsWith('-')));
            if (queryWord.Length > 0)
            {
                if (currentKeyword == queryWord) return;
                Func<DictEntry, int> keySel = word => DamerauLevenshteinDistance(queryWord, word.word, 10);
                currentKeyword = queryWord;

                lock (currentQueryResults)
                {
                    Task.Run(() =>
                    {
                        if(currentKeyword != queryWord) return;
                        currentQueryResults
                            = GetTopAscendingElements(entries,
                                keySel,
                                10).OrderBy(keySel).ToList();
                    });
                }
            }

        }

        static string getDefinitionOfRes(string cmd, DictEntry entry)
        {
            var definition = entry.translation;
            if (cmd.Contains(" -e") || definition.Length == 0)
            {
                definition = entry.definition;
            }

            return definition.Replace("\\n","\n");
        }
        
        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {
            if (nodict)
            {
                gfx.DrawText("** 词典未加载，请将词典 ecdict.csv 放于同路径下 **", 15, "white", 0, ref startY, "red", paddingX: 10, paddingY: 4, marginX: 10);
                return;
            }

            gfx.DrawText("-e: 优先英文 Enter：复制本单词 Shift：复制释义", 10, "white", 0, ref startY, "blue", paddingX: 10, paddingY: 4, marginX: 10);
            foreach (var currentQueryResult in currentQueryResults)
            {
                var x = 10f;
                var yLeft = startY;
                var word = gfx.DrawText(currentQueryResult.word, 17, "white", x, ref yLeft, brushBg:"black", paddingX: 10);

                var def = gfx.DrawText(getDefinitionOfRes(cmd, currentQueryResult), 12, "black", x + word.X, startY, brushBg: "white", paddingX: 5, paddingY:5, maxWidth: 400);

                if (currentQueryResult.tag.Length != 0)
                {
                    var tag = gfx.DrawText(currentQueryResult.tag.Replace("zk", "中考").Replace("gk", "高考").ToUpper(), 10, "white", x,
                        ref yLeft, brushBg: "dark-purple", paddingX: 2, maxWidth: word.X - 4);
                    startY += Math.Max(tag.Y + word.Y, def.Y) + 10;
                    continue;
                }

                startY += Math.Max(word.Y, def.Y)  + 10;
            }
        }
        public override string GetDescription()
        {
            return "英语查词";
        }
        public override string[] GetPrefix()
        {
            return new[]
            {
                "queryword", "word", "q", "dict"
            };
        }

        static void Copy(string str)
        {
            Thread t = new Thread((ThreadStart)(() =>
                    {
                        Clipboard.SetText(str);
                    }

                ));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {
            if (currentQueryResults.Count == 0) return;
            if (Keys.LShiftKey == key || Keys.RShiftKey == key)
            {
                var def = getDefinitionOfRes(cmd, currentQueryResults[0]);
                showing = false;
                Copy(def);
            }
        }
        public override void Submit(ref string cmd, ref bool showing)
        
        {
            if (currentQueryResults.Count == 0) return;
            showing = false;
            Copy(currentQueryResults[0].word);
           
        }
    }
}
