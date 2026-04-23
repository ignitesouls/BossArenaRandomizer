using System;
using System.Windows;
using System.Windows.Media;
using BossArenaRandomizer.Services;
using BossArenaRandomizer.ViewModels;

namespace BossArenaRandomizer
{
    public partial class MainWindow : Window
    {
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;

        public MainWindow()
        {
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            InitializeComponent();

            var appStateService = new AppStateService(_basePath);
            DataContext = new MainViewModel(_basePath, appStateService);
        }
    }
}