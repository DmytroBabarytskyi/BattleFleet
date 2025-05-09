using System;

namespace BattleFleet.Models
{
    public class PlayerStats
    {
        public int Id { get; set; }
        public string PlayerName { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int ShipsDestroyed { get; set; }
        public int ShipsLost { get; set; }
        public DateTime LastPlayed { get; set; }

        public double WinRate 
        { 
            get 
            { 
                return GamesWon + GamesLost > 0 
                    ? Math.Round((double)GamesWon / (GamesWon + GamesLost) * 100, 2) 
                    : 0; 
            } 
        }
    }
} 