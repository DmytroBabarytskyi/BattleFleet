using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using BattleFleet.Models;

namespace BattleFleet.Logic
{
    public static class ShipGenerator
    {
        private static Random _rand = new Random();

        public static List<Ship> GenerateFleet()
        {
            var fleet = new List<Ship>();
            var occupied = new HashSet<Point>();

            // Формат: кількість -> генератор кораблів
            var shipTypes = new List<(int, Func<Point, bool, Ship>, int)>
            {
                (1, (p, h) => new QuadDeckShip(new List<Point> { p }, h), 4),
                (2, (p, h) => new TripleDeckShip(new List<Point> { p }, h), 3),
                (3, (p, h) => new DoubleDeckShip(new List<Point> { p }, h), 2),
                (4, (p, h) => new SingleDeckShip(new List<Point> { p }, h), 1)
            };

            foreach (var (count, factory, size) in shipTypes)
            {
                for (int i = 0; i < count; i++)
                {
                    Ship ship;
                    do
                    {
                        bool horizontal = _rand.Next(2) == 0;
                        int maxX = horizontal ? 10 - size : 9;
                        int maxY = horizontal ? 9 : 10 - size;
                        Point start = new Point(_rand.Next(0, maxX + 1), _rand.Next(0, maxY + 1));
                        ship = factory(start, horizontal);
                    }
                    while (ship.Coordinates.Any(p => occupied.Contains(p)));

                    foreach (var point in ship.Coordinates)
                        occupied.Add(point);

                    fleet.Add(ship);
                }
            }

            return fleet;
        }
    }
}

