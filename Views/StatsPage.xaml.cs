using BattleFleet.Models;
using BattleFleet.Services;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text.Json;
using Windows.Storage;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;

namespace BattleFleet.Views
{
    public sealed partial class StatsPage : Page
    {
        private readonly string connectionString;
        private List<PlayerRanking> leaderboard;

        public StatsPage()
        {
            this.InitializeComponent();
            connectionString = LoadConnectionString();
            leaderboard = new List<PlayerRanking>();
            UpdateLoginStatus();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadLeaderboard();
        }

        private void UpdateLoginStatus()
        {
            if (CurrentPlayer.IsLoggedIn)
            {
                LoginStatusText.Text = $"Ввійшли як: {CurrentPlayer.Name}";
                LogoutButton.Visibility = Visibility.Visible;
                PlayerNameTextBox.Text = CurrentPlayer.Name;
                PlayerNameTextBox.IsEnabled = false;
                LoginButton.Content = "Оновити статистику";
            }
            else
            {
                LoginStatusText.Text = "Не ввійшли в систему";
                LogoutButton.Visibility = Visibility.Collapsed;
                PlayerNameTextBox.Text = "";
                PlayerNameTextBox.IsEnabled = true;
                LoginButton.Content = "Увійти";
            }
        }

        private string LoadConnectionString()
        {
            try
            {
                var configFile = StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///appsettings.json")).GetAwaiter().GetResult();
                
                var json = FileIO.ReadTextAsync(configFile).GetAwaiter().GetResult();
                var config = JsonSerializer.Deserialize<JsonElement>(json);
                
                var dbSettings = config.GetProperty("DatabaseSettings");
                return $"Server={dbSettings.GetProperty("Server").GetString()};" +
                       $"Database={dbSettings.GetProperty("Database").GetString()};" +
                       $"Uid={dbSettings.GetProperty("UserId").GetString()};" +
                       $"Pwd={dbSettings.GetProperty("Password").GetString()};";
            }
            catch (Exception ex)
            {
                // Якщо не вдалося завантажити конфігурацію, використовуємо значення за замовчуванням
                return "Server=localhost;Database=battlefleet;Uid=root;Pwd=10082006;";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string playerName = PlayerNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                await ShowMessage("Будь ласка, введіть ім'я гравця");
                return;
            }

            try
            {
                PlayerStats stats = await GetPlayerStats(playerName);
                if (stats != null)
                {
                    DisplayStats(stats);
                }
                else
                {
                    var dialog = new MessageDialog(
                        "Гравець не знайдений. Бажаєте створити нового гравця?",
                        "Новий гравець");
                    dialog.Commands.Add(new UICommand("Так", async (command) =>
                    {
                        await CreateNewPlayer(playerName);
                        stats = await GetPlayerStats(playerName);
                        if (stats != null)
                        {
                            DisplayStats(stats);
                        }
                    }));
                    dialog.Commands.Add(new UICommand("Ні"));
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"Помилка при отриманні статистики: {ex.Message}");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentPlayer.Logout();
            UpdateLoginStatus();
            StatsPanel.Visibility = Visibility.Collapsed;
        }

        private async Task CreateNewPlayer(string playerName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO playerstats (PlayerName, GamesWon, GamesLost, ShipsDestroyed, ShipsLost, LastPlayed) " +
                             "VALUES (@PlayerName, 0, 0, 0, 0, @LastPlayed)";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName);
                    command.Parameters.AddWithValue("@LastPlayed", DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<PlayerStats> GetPlayerStats(string playerName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM playerstats WHERE PlayerName = @PlayerName";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new PlayerStats
                            {
                                Id = reader.GetInt32("Id"),
                                PlayerName = reader.GetString("PlayerName"),
                                GamesWon = reader.GetInt32("GamesWon"),
                                GamesLost = reader.GetInt32("GamesLost"),
                                ShipsDestroyed = reader.GetInt32("ShipsDestroyed"),
                                ShipsLost = reader.GetInt32("ShipsLost"),
                                LastPlayed = reader.GetDateTime("LastPlayed")
                            };
                        }
                    }
                }
            }
            return null;
        }

        private async void LoadLeaderboard()
        {
            try
            {
                var databaseService = new DatabaseService(connectionString);
                var topPlayers = await databaseService.GetTopPlayersAsync(10);
                
                LeaderboardListView.Items.Clear();
                int rank = 1;
                foreach (var player in topPlayers)
                {
                    var grid = new Grid();
                    grid.Margin = new Thickness(0, 5, 0, 5);
                    
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });

                    var rankText = new TextBlock
                    {
                        Text = rank++.ToString(),
                        Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(rankText, 0);

                    var nameText = new TextBlock
                    {
                        Text = player.PlayerName,
                        Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                    };
                    Grid.SetColumn(nameText, 1);

                    var winsText = new TextBlock
                    {
                        Text = player.GamesWon.ToString(),
                        Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                    };
                    Grid.SetColumn(winsText, 2);

                    grid.Children.Add(rankText);
                    grid.Children.Add(nameText);
                    grid.Children.Add(winsText);

                    LeaderboardListView.Items.Add(grid);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"Помилка при завантаженні таблиці лідерів: {ex.Message}");
            }
        }

        private void DisplayStats(PlayerStats stats)
        {
            StatsPanel.Visibility = Visibility.Visible;
            PlayerNameText.Text = stats.PlayerName;
            GamesWonText.Text = stats.GamesWon.ToString();
            GamesLostText.Text = stats.GamesLost.ToString();
            WinRateText.Text = $"{stats.WinRate}%";
            ShipsDestroyedText.Text = stats.ShipsDestroyed.ToString();
            ShipsLostText.Text = stats.ShipsLost.ToString();
            LastPlayedText.Text = stats.LastPlayed.ToString("dd.MM.yyyy HH:mm");

            // Зберігаємо інформацію про поточного гравця
            CurrentPlayer.Login(stats);
            UpdateLoginStatus();
            
            // Оновлюємо таблицю лідерів
            LoadLeaderboard();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        private async Task ShowMessage(string message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }
    }

    public class PlayerRanking
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int GamesWon { get; set; }
    }
} 