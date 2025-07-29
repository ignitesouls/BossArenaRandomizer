using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalReplacementRandomizer;
using System.IO;

namespace BossArenaRandomizer
{
    public static class Randomization
    {
        public static int ConvertBitmapToInt(string bitmap)
        {
            if( string.IsNullOrWhiteSpace(bitmap) || !IsBinary(bitmap))
                throw new ArgumentException("Input must be a non-empty binary string.", nameof(bitmap));


            return Convert.ToInt32(bitmap, 2);
        }

        private static bool IsBinary(string input)
        {
            foreach (char c in input)
            {
                if (c != '0' && c != '1')
                {
                    return false;
                }
            }
            return true;
        }

        public static EncodedBitmapValidator LoadBitmapsFromCsv(string csvPath, bool useSizeBitmaps = false, bool useDifficultyBitmap = false)
        {
            var targetArenas = new Dictionary<int, int>();
            var replacementBosses = new Dictionary<int, int>();

            var lines = File.ReadAllLines(csvPath);

            foreach (var line in lines.Skip(1)) // Skip header
            {
                var parts = line.Split(',');

                if (parts.Length < 8)
                    continue;

                string arenaName = parts[0];
                if (!int.TryParse(parts[1], out int arenaId)) continue;

                string arenaBitmapStr = parts[2];
                string bossBitmapStr = parts[3];
                string arenaSizeBitmapStr = parts[4]; 
                string bossSizeBitmapStr = parts[5];
                string arenaDifficultyBitmapStr = parts[6];
                string bossDifficultyBitmapStr = parts[7];

                string finalArenaBitmap = "";
                string finalBossBitmap = "";
                
                if (useSizeBitmaps)
                {
                    finalArenaBitmap += arenaSizeBitmapStr;
                    finalBossBitmap += bossSizeBitmapStr;
                }

                if (useDifficultyBitmap)
                {
                    finalArenaBitmap += arenaDifficultyBitmapStr;
                    finalBossBitmap += bossDifficultyBitmapStr;
                }

                finalArenaBitmap += arenaBitmapStr;
                finalBossBitmap += bossBitmapStr;

                int arenaInt = ConvertBitmapToInt(finalArenaBitmap);
                int bossInt = ConvertBitmapToInt(finalBossBitmap);

                targetArenas[arenaId] = arenaInt;
                replacementBosses[arenaId] = bossInt;
            }

            return new EncodedBitmapValidator(targetArenas, replacementBosses);
        }
    }
}
