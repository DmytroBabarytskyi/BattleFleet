using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Text.Json;
using Windows.Storage;

namespace BattleFleet.Models
{
    public static class StatsUpdater
    {
        private static string connectionString;

        static StatsUpdater()
        {
            connectionString = LoadConnectionString();
        }

        private static string LoadConnectionString()
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
            catch (Exception)
            {
                return "Server=localhost;Database=battlefleet;Uid=root;Pwd=10082006;";
            }
        }

        public static async Task UpdateStats(bool isWin, int shipsDestroyed, int shipsLost)
        {
            if (!CurrentPlayer.IsLoggedIn) return;

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE playerstats SET " +
                             "GamesWon = GamesWon + @GamesWon, " +
                             "GamesLost = GamesLost + @GamesLost, " +
                             "ShipsDestroyed = ShipsDestroyed + @ShipsDestroyed, " +
                             "ShipsLost = ShipsLost + @ShipsLost, " +
                             "LastPlayed = @LastPlayed " +
                             "WHERE Id = @PlayerId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GamesWon", isWin ? 1 : 0);
                    command.Parameters.AddWithValue("@GamesLost", isWin ? 0 : 1);
                    command.Parameters.AddWithValue("@ShipsDestroyed", shipsDestroyed);
                    command.Parameters.AddWithValue("@ShipsLost", shipsLost);
                    command.Parameters.AddWithValue("@LastPlayed", DateTime.Now);
                    command.Parameters.AddWithValue("@PlayerId", CurrentPlayer.Id);
                    
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
} 