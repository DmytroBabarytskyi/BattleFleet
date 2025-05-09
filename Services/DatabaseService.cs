using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using BattleFleet.Models;

namespace BattleFleet.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var createTableCommand = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS PlayerStats (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    PlayerName VARCHAR(100) NOT NULL,
                    GamesWon INT DEFAULT 0,
                    GamesLost INT DEFAULT 0,
                    ShipsDestroyed INT DEFAULT 0,
                    ShipsLost INT DEFAULT 0,
                    LastPlayed DATETIME DEFAULT CURRENT_TIMESTAMP
                )", connection);

            createTableCommand.ExecuteNonQuery();
        }

        public async Task<PlayerStats> GetPlayerStatsAsync(string playerName)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(
                "SELECT * FROM PlayerStats WHERE PlayerName = @playerName",
                connection);
            command.Parameters.AddWithValue("@playerName", playerName);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PlayerStats
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PlayerName = reader.GetString(reader.GetOrdinal("PlayerName")),
                    GamesWon = reader.GetInt32(reader.GetOrdinal("GamesWon")),
                    GamesLost = reader.GetInt32(reader.GetOrdinal("GamesLost")),
                    ShipsDestroyed = reader.GetInt32(reader.GetOrdinal("ShipsDestroyed")),
                    ShipsLost = reader.GetInt32(reader.GetOrdinal("ShipsLost")),
                    LastPlayed = reader.GetDateTime(reader.GetOrdinal("LastPlayed"))
                };
            }

            return null;
        }

        public async Task SavePlayerStatsAsync(PlayerStats stats)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(@"
                INSERT INTO PlayerStats (PlayerName, GamesWon, GamesLost, ShipsDestroyed, ShipsLost, LastPlayed)
                VALUES (@playerName, @gamesWon, @gamesLost, @shipsDestroyed, @shipsLost, @lastPlayed)
                ON DUPLICATE KEY UPDATE
                GamesWon = VALUES(GamesWon),
                GamesLost = VALUES(GamesLost),
                ShipsDestroyed = VALUES(ShipsDestroyed),
                ShipsLost = VALUES(ShipsLost),
                LastPlayed = VALUES(LastPlayed)", connection);

            command.Parameters.AddWithValue("@playerName", stats.PlayerName);
            command.Parameters.AddWithValue("@gamesWon", stats.GamesWon);
            command.Parameters.AddWithValue("@gamesLost", stats.GamesLost);
            command.Parameters.AddWithValue("@shipsDestroyed", stats.ShipsDestroyed);
            command.Parameters.AddWithValue("@shipsLost", stats.ShipsLost);
            command.Parameters.AddWithValue("@lastPlayed", stats.LastPlayed);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<PlayerStats>> GetTopPlayersAsync(int limit = 10)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(
                "SELECT * FROM PlayerStats ORDER BY GamesWon DESC LIMIT @limit",
                connection);
            command.Parameters.AddWithValue("@limit", limit);

            var players = new List<PlayerStats>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                players.Add(new PlayerStats
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PlayerName = reader.GetString(reader.GetOrdinal("PlayerName")),
                    GamesWon = reader.GetInt32(reader.GetOrdinal("GamesWon")),
                    GamesLost = reader.GetInt32(reader.GetOrdinal("GamesLost")),
                    ShipsDestroyed = reader.GetInt32(reader.GetOrdinal("ShipsDestroyed")),
                    ShipsLost = reader.GetInt32(reader.GetOrdinal("ShipsLost")),
                    LastPlayed = reader.GetDateTime(reader.GetOrdinal("LastPlayed"))
                });
            }

            return players;
        }
    }
} 