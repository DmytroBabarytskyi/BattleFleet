using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BattleFleet.Models
{
    public enum CellState
    {
        Ocean,
        Ship,
        Hit,
        Miss,
        Sunk
    }

    public class Cell
    {
        public Point Position { get; set; }
        public CellState State { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

}
