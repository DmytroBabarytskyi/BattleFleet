using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BattleFleet.Models;

namespace BattleFleet.Views
{
    public sealed partial class DifficultySelectionPage : Page
    {
        public DifficultySelectionPage()
        {
            this.InitializeComponent();
        }

        private void EasyButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GamePage), Difficulty.Easy);
        }

        private void HardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GamePage), Difficulty.Hard);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
} 