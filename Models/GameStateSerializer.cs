using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;

namespace BattleFleet.Models
{
    [DataContract]
    public class SerializablePoint
    {
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }

        public SerializablePoint() { }

        public SerializablePoint(Point point)
        {
            X = point.X;
            Y = point.Y;
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }
    }

    [DataContract]
    public class SerializableShip
    {
        [DataMember]
        public List<SerializablePoint> Coordinates { get; set; }
        [DataMember]
        public List<SerializablePoint> Hits { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public bool IsHorizontal { get; set; }

        public SerializableShip() { }

        public SerializableShip(Ship ship)
        {
            Coordinates = ship.Coordinates.ConvertAll(p => new SerializablePoint(p));
            Hits = ship.Hits.ConvertAll(p => new SerializablePoint(p));
            Type = ship.GetType().Name;
            IsHorizontal = ship.IsHorizontal;
        }

        public Ship ToShip()
        {
            var coords = Coordinates.ConvertAll(p => p.ToPoint());
            return Type switch
            {
                "SingleDeckShip" => new SingleDeckShip(coords, IsHorizontal),
                "DoubleDeckShip" => new DoubleDeckShip(coords, IsHorizontal),
                "TripleDeckShip" => new TripleDeckShip(coords, IsHorizontal),
                "QuadDeckShip" => new QuadDeckShip(coords, IsHorizontal),
                _ => throw new ArgumentException($"Unknown ship type: {Type}")
            };
        }
    }

    [DataContract]
    public class SerializableGameState
    {
        [DataMember]
        public List<SerializableShip> PlayerShips { get; set; }
        [DataMember]
        public List<SerializableShip> ComputerShips { get; set; }
        [DataMember]
        public List<SerializablePoint> PlayerShots { get; set; }
        [DataMember]
        public List<SerializablePoint> ComputerShots { get; set; }
        [DataMember]
        public DateTime LastSaved { get; set; }
        [DataMember]
        public bool IsPlayerTurn { get; set; }

        public SerializableGameState() { }

        public SerializableGameState(GameState gameState)
        {
            PlayerShips = gameState.PlayerShips.ConvertAll(s => new SerializableShip(s));
            ComputerShips = gameState.ComputerShips.ConvertAll(s => new SerializableShip(s));
            PlayerShots = gameState.PlayerShots.ConvertAll(p => new SerializablePoint(p));
            ComputerShots = gameState.ComputerShots.ConvertAll(p => new SerializablePoint(p));
            LastSaved = gameState.LastSaved;
            IsPlayerTurn = gameState.IsPlayerTurn;
        }

        public GameState ToGameState()
        {
            return new GameState
            {
                PlayerShips = PlayerShips.ConvertAll(s => s.ToShip()),
                ComputerShips = ComputerShips.ConvertAll(s => s.ToShip()),
                PlayerShots = PlayerShots.ConvertAll(p => p.ToPoint()),
                ComputerShots = ComputerShots.ConvertAll(p => p.ToPoint()),
                LastSaved = LastSaved,
                IsPlayerTurn = IsPlayerTurn
            };
        }
    }

    public static class GameStateSerializer
    {
        private static readonly string SaveFileName = "battlefleet_save.json";

        public static async Task SaveGameStateAsync(GameState gameState)
        {
            try
            {
                gameState.LastSaved = DateTime.Now;
                var serializableState = new SerializableGameState(gameState);
                
                var serializer = new DataContractJsonSerializer(typeof(SerializableGameState));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, serializableState);
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        string jsonString = await reader.ReadToEndAsync();
                        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                        StorageFile saveFile = await localFolder.CreateFileAsync(SaveFileName, CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(saveFile, jsonString);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save game state: " + ex.Message, ex);
            }
        }

        public static async Task<GameState> LoadGameStateAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile saveFile = await localFolder.GetFileAsync(SaveFileName);
                string jsonString = await FileIO.ReadTextAsync(saveFile);

                var serializer = new DataContractJsonSerializer(typeof(SerializableGameState));
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                {
                    var serializableState = (SerializableGameState)serializer.ReadObject(stream);
                    return serializableState.ToGameState();
                }
            }
            catch (FileNotFoundException)
            {
                return null; // No save file exists
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load game state: " + ex.Message, ex);
            }
        }

        public static async Task<bool> SaveExistsAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile saveFile = await localFolder.GetFileAsync(SaveFileName);
                return saveFile != null;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }
} 