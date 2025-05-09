using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace BattleFleet.Models
{
    public class GameState
    {
        public List<Ship> PlayerShips { get; set; } = new List<Ship>();
        public List<Ship> ComputerShips { get; set; } = new List<Ship>();
        public List<Point> PlayerShots { get; set; } = new List<Point>();
        public List<Point> ComputerShots { get; set; } = new List<Point>();
        public bool IsPlayerTurn { get; set; }
        public DateTime LastSaved { get; set; }
    }
} 