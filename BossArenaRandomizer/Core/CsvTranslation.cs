using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BossArenaRandomizer.Core
{
    public static class CsvTranslation
    {
        public static void WriteArenaBossCsv(
            Dictionary<string, ArenaInfo> arenas,
            Dictionary<string, BossInfo> bosses,
            string outputPath
            )
        {
            var sb = new StringBuilder();

            //Line Cool Stuff Current Bitmap Single = 19/32
            sb.AppendLine("ArenaBossName,ArenaBossID,ArenaBitmap,BossBitmap,ArenaSizeBitmap,BossSizeBitmap,ArenaDifficultyBitmap,BossDifficultBitmap,arenaDifficultyPassThroughBitmap,bossBaseDifficulty");

            foreach (var arenaEntry in arenas)
            {
                string name = arenaEntry.Key;
                ArenaInfo arena = arenaEntry.Value;
                string arenaTypeEvergaol = GetArenaType(arena.arenaType); //Checks for arenaType 7 which is Evergaol See HCData.cs for categories
                string arenaBitmap = GetArenaBitmap(arena, arenaTypeEvergaol);
                string arenaSizeBitmap = GetArenaSizeBitmap(arena.arenaSize);
                string arenaBossRushDifficulty = GetArenaBoshRushDifficultyBitmap(arena.hardNotAllowed);
                string arenaLooseDifficultyCurve = GetArenaLooseDifficultyCurveBitmap(arena.difficultyPassThrough);

                if (bosses.TryGetValue(name, out BossInfo? boss))
                {
                    string bossBitmap = GetBossBitmap(boss);
                    string bossSizeBitmap = GetBossSizeBitmap(boss.bossSize);
                    string bossBossRushDifficulty = GetBossRushDifficultyBitmap(boss.isHard);
                    string bossBaseDifficulty = GetBossBaseDifficultyBitap(boss.baseDifficulty);
                    sb.AppendLine($"{name},{arena.id},{arenaBitmap},{bossBitmap},{arenaSizeBitmap},{bossSizeBitmap},{arenaBossRushDifficulty},{bossBossRushDifficulty},{arenaLooseDifficultyCurve},{bossBaseDifficulty}");
                }
            }

            File.WriteAllText(outputPath, sb.ToString());
        }

        private static string GetArenaBitmap(ArenaInfo arena, string arenaTypeEvergaol)
        {
            return $"{arena.twoPhaseNotAllowed}{arena.dragonNotAllowed}{arena.npcNotAllowed}{arena.isEscapable}{arena.messmerNotAllowed}{arena.malikethNotAllowed}{arenaTypeEvergaol}{arena.godskinduoNotAllowed}";
        }

        private static string GetBossBitmap(BossInfo boss)
        {
            return $"{boss.isTwoPhase}{boss.isDragon}{boss.isNPC}{boss.canEscape}{boss.isMessmer}{boss.isMaliketh}{boss.isEvergaolIncompatible}{boss.isGodskinDuo}";
        }

        private static string GetArenaSizeBitmap(int size)
        {
            return size switch
            {
                5 => "00000",
                4 => "10000",
                3 => "11000",
                2 => "11100",
                1 => "11110",
                _ => "11111" //default
            };
        }

        private static string GetBossSizeBitmap(int size)
        {
            return size switch
            {
                5 => "10000",
                4 => "01000",
                3 => "00100",
                2 => "00010",
                1 => "00001",
                _ => "00000" //default
            };
        }

        private static string GetArenaBoshRushDifficultyBitmap(int difficulty)
        {
            return difficulty switch
            {
                1 => "1",
                0 => "0",
                _ => "0" //default
            };
        }

        private static string GetBossRushDifficultyBitmap(int difficulty)
        {
            return difficulty switch
            {
                1 => "1",
                0 => "0",
                _ => "0" //default
            };
        }

        private static string GetArenaType(int arenaType)
        {
            return arenaType switch
            {
                7 => "1",
                _ => "0" //default
            };
        }

        private static string GetArenaLooseDifficultyCurveBitmap(int passDifficulty)
        {
            return passDifficulty switch
            {
                5 => "00000",
                4 => "10000",
                3 => "11000",
                2 => "11100",
                1 => "11110",
                _ => "11111" //default
            };
        }

        private static readonly Random _rng = new Random();

        private static string GetBossBaseDifficultyBitap(int baseDifficulty)
        {
            // clamp to [1..5] (or default to 1 if you prefer)
            int randomizeDifficulty = baseDifficulty;
            if (randomizeDifficulty < 1) randomizeDifficulty = 1;
            if (randomizeDifficulty > 5) randomizeDifficulty = 5;

            // 20% chance to DOWNGRADE to any lower value (never up)
            if (randomizeDifficulty > 1 && _rng.NextDouble() < 0.20)
            {
                // random lower tier: [1 .. d-1]
                randomizeDifficulty = _rng.Next(1, randomizeDifficulty);
            }

            return randomizeDifficulty switch
            {
                5 => "10000",
                4 => "01000",
                3 => "00100",
                2 => "00010",
                1 => "00001",
                _ => "00001"
            };
        }
    }
}
