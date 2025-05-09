using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BattleFleet.Models;
using BattleFleet.Views;
using System;
using Windows.UI.Popups;

namespace BattleFleet
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            CheckForSavedGame();
        }

        private async void CheckForSavedGame()
        {
            if (await GameStateSerializer.SaveExistsAsync())
            {
                LoadGameButton.IsEnabled = true;
            }
            else
            {
                LoadGameButton.IsEnabled = false;
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GamePage));
        }

        private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gameState = await GameStateSerializer.LoadGameStateAsync();
                if (gameState != null)
                {
                    Frame.Navigate(typeof(GamePage), gameState);
                }
                else
                {
                    var dialog = new MessageDialog("Не вдалося завантажити збережену гру.", "Помилка");
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Помилка при завантаженні гри: {ex.Message}", "Помилка");
                await dialog.ShowAsync();
            }
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StatsPage));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}