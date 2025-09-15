using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UniversalReplacementRandomizer;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.IO.Enumeration;


namespace BossArenaRandomizer
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, ArenaInfo> arenas;
        private Dictionary<string, BossInfo> bosses;
        private FilterArenas filterArenas;
        private FilterBosses filterBosses;
        private readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private string? selectedOptionsPreset = null;

        public MainWindow()
        {
            InitializeComponent();

            arenas = InitialDataRead.LoadArenas(System.IO.Path.Combine(basePath, "Data", "arenas.json"));
            bosses = InitialDataRead.LoadBosses(System.IO.Path.Combine(basePath, "Data", "bosses.json"));
            
            

            CsvTranslation.WriteArenaBossCsv(arenas, bosses, System.IO.Path.Combine(basePath, "ArenaBossData.csv"));
            

            //Changed var modules = new Modules()
            var modules = new Modules(arenas, bosses);
            this.DataContext = modules;
            filterArenas = modules.ArenaFilter;
            filterBosses = modules.BossesFilter;

            LoadCustomArenaPresetList();
            LoadCustomBossPresetList();
            LoadOptionsPresetList();

            string savedPath = GetOutputPathFromSettings();
            if (!string.IsNullOrEmpty(savedPath))
            {
                OutputPathDisplay.Text = $"Saving to: {savedPath}";
            }
            else
            {
                OutputPathDisplay.Text = "No output path selected.";
            }

            string lastArenaPreset = Properties.Settings.Default.LastUsedArenaPreset;
            if (!string.IsNullOrEmpty(lastArenaPreset) && ArenaPresetComboBox.Items.Contains(lastArenaPreset))
            {
                ArenaPresetComboBox.SelectedItem = lastArenaPreset;
            }

            string lastBossPreset = Properties.Settings.Default.LastUsedBossPreset;
            if (!string.IsNullOrEmpty(lastBossPreset) && BossPresetComboBox.Items.Contains(lastBossPreset))
            {
                BossPresetComboBox.SelectedItem = lastBossPreset;
            }

            // Load saved selected preset if available
            selectedOptionsPreset = Properties.Settings.Default.SelectedOptionsPreset;

            if (!string.IsNullOrEmpty(selectedOptionsPreset))
            {
                OptionsPresetComboBox.SelectedItem = selectedOptionsPreset;
            }

            // Restore checkbox states
            ClearArenasCheckbox.IsChecked = Properties.Settings.Default.UseClearArenas;
            ArenaSizeRestriction.IsChecked = Properties.Settings.Default.UseArenaSizeRestriction;
            ArenaDifficultyRestriction.IsChecked = Properties.Settings.Default.UseArenaDifficultyRestrict;
        }

        private string GetOutputPathFromSettings()
        {
            return Properties.Settings.Default.OutputFilePath;
        }

        private void SaveOutputPathToSettings(string path)
        {
            Properties.Settings.Default.OutputFilePath = path;
            Properties.Settings.Default.Save();
            OutputPathDisplay.Text = $"Saving to: {path}";
        }

        private void SetOutputPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "BAROptionsFile.randomizeopt",
                DefaultExt = ".randomizeopt",
                Filter = "Randomizer Options File (.randomizeopt)|*.randomizeopt"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveOutputPathToSettings(dialog.FileName);
            }
        }

        private void DiscordLink_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            { 
                FileName = "http://discord.gg/ignitesouls",
                UseShellExecute = true
            });
        }

        private List<string> GetSelectedArenaIds()
        {
            return filterArenas.ArenaSelections
                         .Where(a => a.IsSelected)
                         .Select(a => a.Id)
                         .ToList();
        }

        private List<string> GetSelectedBossesIds()
        {
            return filterBosses.BossSelections
                         .Where(b => b.IsSelected)
                         .Select(b => b.Id)
                         .ToList();
        }

        private void ResetAllArenas_Click(Object sender, RoutedEventArgs e)
        {
            foreach (var arena in filterArenas.ArenaSelections)
            {
                arena.IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(arena.Id);
            }
        }

        private void ResetAllBosses_Click(Object sender, RoutedEventArgs e)
        {
            foreach (var bosses in filterBosses.BossSelections)
            {
                bosses.IsSelected = !HCFilterIds.UncheckArenaBossIds.Contains(bosses.Id);
            }
        }

        private void SelectBaseGameArenas_Click(object sender, RoutedEventArgs e)
        {
            foreach (var arena in filterArenas.ArenaSelections)
            {
                arena.IsSelected = HCFilterIds.BaseGameArenaIds.Contains(arena.Id);
            }
        }


        private void SelectDLCArenas_Click(object sender, RoutedEventArgs e)
        {
            foreach (var arena in filterArenas.ArenaSelections)
            {
                arena.IsSelected = HCFilterIds.DLCArenaIds.Contains(arena.Id);
            }
        }

        private void SelectBaseGameBosses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var boss in filterBosses.BossSelections)
            {
                boss.IsSelected = HCFilterIds.BaseGameBossesIds.Contains(boss.Id);
            }
        }

        private void SelectDLCBosses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var boss in filterBosses.BossSelections)
            {
                boss.IsSelected = HCFilterIds.DLCBossesIds.Contains(boss.Id);
            }
        }

        private void SelectIgniteBosses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var boss in filterBosses.BossSelections)
            {
                boss.IsSelected = HCFilterIds.IgniteBossesIds.Contains(boss.Id);
            }
        }

        private void ClearArenasCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void ArenaSizeRestrictionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void ArenaDifficultyRestrictionCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SaveCustomArenaSelection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = System.IO.Path.Combine(basePath, "Presets", "Arenas"),
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "MyCustomArenaPreset"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedArenaIds = filterArenas.ArenaSelections
                    .Where(a => a.IsSelected)
                    .Select(a => a.Id)
                    .ToList();

                var json = JsonSerializer.Serialize(selectedArenaIds);
                File.WriteAllText(dialog.FileName, json);

                MessageBox.Show("Custom Arena Preset Saved!");
                LoadCustomArenaPresetList(); //Refresh List
            }
        }

        private void SaveCustomBossSelection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { 
                InitialDirectory = System.IO.Path.Combine(basePath, "Presets", "Bosses"),
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "MyCustomBossPreset"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedBossIds = filterBosses.BossSelections
                    .Where(b=>b.IsSelected)
                    .Select(b => b.Id)
                    .ToList();

                var json = JsonSerializer.Serialize(selectedBossIds);
                File.WriteAllText(dialog.FileName, json);

                MessageBox.Show("Custom Boss Preset Saved!");
                LoadCustomBossPresetList(); //Refresh List
            }
        }

        private void LoadCustomArenaPresetList()
        {
            string presetDir = System.IO.Path.Combine(basePath, "Presets", "Arenas");
            if (!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }

            var files = Directory.GetFiles(presetDir, "*.json");
            ArenaPresetComboBox.ItemsSource = files.Select(f=> System.IO.Path.GetFileName(f)).ToList();
        }

        private void LoadCustomBossPresetList()
        {
            string presetDir = System.IO.Path.Combine(basePath, "Presets", "Bosses");
            if(!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }

            var files = Directory.GetFiles(presetDir, "*.json");
            BossPresetComboBox.ItemsSource = files.Select(f => System.IO.Path.GetFileName(f)).ToList();
        }

        private void SelectArenaPreset_Click(object sender, RoutedEventArgs e)
        {
            string selectedPresetName = ArenaPresetComboBox.SelectedItem as string;

            Properties.Settings.Default.LastUsedArenaPreset = selectedPresetName;
            Properties.Settings.Default.Save();

            if (string.IsNullOrEmpty(selectedPresetName))
            {
                MessageBox.Show("Please Select a Preset.");
                return;
            }

            string presetPath = System.IO.Path.Combine(basePath, "Presets", "Arenas", selectedPresetName);

            if (!File.Exists(presetPath))
            {
                MessageBox.Show("Preset file not found");
                return;
            }

            var json = File.ReadAllText(presetPath);
            var loadedIds = JsonSerializer.Deserialize<List<string>>(json);

            HCFilterIds.CustomArenas = new HashSet<string>(loadedIds);
            
            foreach (var arena in filterArenas.ArenaSelections)
            {
                arena.IsSelected = HCFilterIds.CustomArenas.Contains(arena.Id);
            }
        }

        private void SelectBossPreset_Click(object sender, RoutedEventArgs e)
        {
            string selectedPresetName = BossPresetComboBox.SelectedItem as string;

            Properties.Settings.Default.LastUsedBossPreset = selectedPresetName;
            Properties.Settings.Default.Save();

            if (string.IsNullOrEmpty(selectedPresetName))
            {
                MessageBox.Show("Please select a preset.");
                return;
            }

            string presetPath = System.IO.Path.Combine(basePath, "Presets", "Bosses", selectedPresetName);

            if (!File.Exists(presetPath))
            {
                MessageBox.Show("Preset file not found");
                return;
            }

            var json = File.ReadAllText(presetPath);
            var loadedIds = JsonSerializer.Deserialize<List<string>>(json);

            HCFilterIds.CustomBosses = new HashSet<string>(loadedIds);

            foreach (var boss in filterBosses.BossSelections)
            {
                boss.IsSelected = HCFilterIds.CustomBosses.Contains(boss.Id);
            }
        }

        private void LoadOptionsPresetList()
        {
            string optionsDir = System.IO.Path.Combine(basePath, "Options");
            
            if (!Directory.Exists(optionsDir))
            {
                Directory.CreateDirectory(optionsDir);
            }

            var files = Directory.GetFiles(optionsDir, "*.randomizeopt");
            OptionsPresetComboBox.ItemsSource = files.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList();
        }

        private void SelectOptionsPreset_Click(object sender, RoutedEventArgs e)
        {
            selectedOptionsPreset = OptionsPresetComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedOptionsPreset))
            {
                MessageBox.Show("Please Select an Options Presets.");
                return;
            }

            string presetPath = System.IO.Path.Combine(basePath, "Options", selectedOptionsPreset + ".randomizeopt");

            if (!File.Exists(presetPath))
            {
                MessageBox.Show("Options Preset File not Found.");
                return;
            }

            MessageBox.Show("Options File Selected");
        }

        private void Randomize_Click(object sender, RoutedEventArgs e)
        {
            //Condition to Prevent Crash
            if (string.IsNullOrEmpty(selectedOptionsPreset)) 
            {
                MessageBox.Show("Please Load a Options Preset");
                return;
            }

            bool sizeRestriction = ArenaSizeRestriction.IsChecked == true;
            bool difficultyRestriction = ArenaDifficultyRestriction.IsChecked == true;
            var validator = Randomization.LoadBitmapsFromCsv(System.IO.Path.Combine(basePath, "ArenaBossData.csv"), sizeRestriction, difficultyRestriction);

            ResultList.Items.Clear();
            var random = new Random();

            Dictionary<string, string> finalAssignments = null;

            const int maxAttempts = 1500;
            int attempt = 0;

            var selectedArenaIds = GetSelectedArenaIds();



            while (attempt++ < maxAttempts)
            {
                var selectedBossIds = GetSelectedBossesIds();
                var selectedBossPool = bosses.Where(b => selectedBossIds.Contains(b.Value.id)).ToDictionary(b => b.Key, b => b.Value);

                var usedBosses = new HashSet<string>();

                var tempAssignments = new Dictionary<string, string>();

                bool allValid = true;

                foreach (var arenaEntry in arenas.Where(a => selectedArenaIds.Contains(a.Value.id)))
                {
                    string arenaName = arenaEntry.Key;
                    int arenaId = int.Parse(arenaEntry.Value.id);

                    string? selectedBossName = null;

                    // Get shuffled list of bosses
                    var shuffledBosses = selectedBossPool.OrderBy(_ => random.Next()).ToList();

                    if (selectedArenaIds.Count > selectedBossIds.Count)
                    {
                        MessageBox.Show("Not enough bosses selected for the number of arenas. Please select more bosses.");
                        return;
                    }


                    foreach (var bossEntry in shuffledBosses)
                    {
                        string bossName = bossEntry.Key;
                        int bossId = int.Parse(bossEntry.Value.id);

                        // No Dupes
                        if (usedBosses.Contains(bossName))
                            continue;

                        if (validator.Validate(arenaId, bossId))
                        {
                            selectedBossName = bossName;
                            usedBosses.Add(bossName);
                            break;
                        }
                    }

                    if (selectedBossName == null)
                    {
                        allValid = false;
                        break;
                    }

                    tempAssignments[arenaName] = selectedBossName;
                }

                if (allValid)
                {
                    Debug.WriteLine($"Number of iterations before success: {attempt}");

                    finalAssignments = tempAssignments;
                    break;
                }
            }

            if (finalAssignments == null)
            {
                MessageBox.Show("Failed to Randomizer due to constraints. Try Again");
                return;
            }

            //Get Base Seed and Display
            var randomizer = new UniversalReplacementRandomizer.SeedManager();
            int seed = randomizer.GetBaseSeed();

            //Set Region Grouping
            var groupedByRegion = finalAssignments
                .GroupBy(kvp => arenas[kvp.Key].region)
                .OrderBy(g => g.Key);


            foreach (var regionGroup in groupedByRegion)
            {
                //add Region Header
                string regionName = HCData.RegionNames.ContainsKey(regionGroup.Key)
                    ? HCData.RegionNames[regionGroup.Key]
                    : $"Region {regionGroup.Key}";


                var regionHeader = new TextBlock
                {
                    Text = $"{regionName}:",
                    Foreground = Brushes.Orange,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                };

                //ResultList.Items.Add(" ");
                ResultList.Items.Add(regionHeader);
                foreach (var kvp in regionGroup)
                {
                    string arenaName = kvp.Key;
                    string bossName = kvp.Value;

                    string arenaId = arenas[arenaName].id;
                    string bossId = bosses[bossName].id;

                    ResultList.Items.Add($"{arenaName} (ID: {arenaId}) -> {bossName} (ID: {bossId})");
                }
            }

            /* Display results
            foreach (var kvp in finalAssignments)
            {
                string arenaName = kvp.Key;
                string bossName = kvp.Value;

                string arenaId = arenas[arenaName].id;
                string bossId = bosses[bossName].id;


                ResultList.Items.Add($"{arenaName} (ID: {arenaId}) -> {bossName} (ID: {bossId}) (Valid)");
            }*/

            
            bool clearArenasEnabled = ClearArenasCheckbox.IsChecked == true;

            string outputPath = GetOutputPathFromSettings();
            string optionsFilePath = System.IO.Path.Combine(basePath, "Options", selectedOptionsPreset + ".randomizeopt");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show("Please select an output path first using the 'Set Output Path' button.");
                return;
            }


            FinalizeTextFile.WriteFinalAssignments(finalAssignments, arenas, bosses, outputPath, optionsFilePath, seed, clearArenasEnabled);

            //Save Settings for the User
            Properties.Settings.Default.SelectedOptionsPreset = selectedOptionsPreset;
            Properties.Settings.Default.UseClearArenas = ClearArenasCheckbox.IsChecked == true;
            Properties.Settings.Default.UseArenaSizeRestriction = ArenaSizeRestriction.IsChecked == true;
            Properties.Settings.Default.UseArenaDifficultyRestrict = ArenaDifficultyRestriction.IsChecked == true;
            Properties.Settings.Default.Save();
            
            SeedTextBlock.Text = $"Seed Used: {seed}";
        }
    }
}