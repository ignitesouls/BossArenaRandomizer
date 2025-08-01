﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BossArenaRandomizer
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

            //Line Cool Stuff
            sb.AppendLine("ArenaBossName,ArenaBossID,ArenaBitmap,BossBitmap,ArenaSizeBitmap,BossSizeBitmap,ArenaDifficultyBitmap,BossDifficultBitmap");

            foreach (var arenaEntry in arenas)
            {
                string name = arenaEntry.Key;
                ArenaInfo arena = arenaEntry.Value;
                string arenaBitmap = GetArenaBitmap(arena);
                string arenaSizeBitmap = GetArenaSizeBitmap(arena.arenaSize);
                string arenaDifficultyBitmap = GetArenaDifficultyBitmap(arena.hardNotAllowed);

                if (bosses.TryGetValue(name, out BossInfo? boss))
                {
                    string bossBitmap = GetBossBitmap(boss);
                    string bossSizeBitmap = GetBossSizeBitmap(boss.bossSize);
                    string bossDifficultyBitmap = GetBossDifficultyBitmap(boss.isHard);
                    sb.AppendLine($"{name},{arena.id},{arenaBitmap},{bossBitmap},{arenaSizeBitmap},{bossSizeBitmap},{arenaDifficultyBitmap},{bossDifficultyBitmap}");
                }
            }

            File.WriteAllText(outputPath, sb.ToString());
        }

        private static string GetArenaBitmap(ArenaInfo arena)
        {
            return $"{arena.twoPhaseNotAllowed}{arena.dragonNotAllowed}{arena.npcNotAllowed}{arena.isEscapable}";
        }

        private static string GetBossBitmap(BossInfo boss)
        {
            return $"{boss.isTwoPhase}{boss.isDragon}{boss.isNPC}{boss.canEscape}";
        }

        private static string GetArenaSizeBitmap (int size)
        {
            return size switch
            {
                4 => "0000",
                3 => "1000",
                2 => "1100",
                1 => "1110",
                _ => "1111" //default
            };
        }

        private static string GetBossSizeBitmap ( int size)
        {
            return size switch
            {
                4 => "1000",
                3 => "0100",
                2 => "0010",
                1 => "0001",
                _ => "0000" //default
            };
        }

        private static string GetArenaDifficultyBitmap (int difficulty)
        {
            return difficulty switch
            {
                1=> "1",
                0=> "0",
                _=> "0" //default
            };
        }


        private static string GetBossDifficultyBitmap(int difficulty)
        {
            return difficulty switch
            {
                1 => "1",
                0 => "0",
                _ => "0" //default
            };
        }   
    }
}
