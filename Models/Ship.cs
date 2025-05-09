using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace BattleFleet.Models
{
    public class Ship
    {
        public List<Point> Coordinates { get; protected set; }
        public HashSet<Point> HitPoints { get; private set; }
        public List<Point> Hits { get; private set; }
        public bool IsSunk => HitPoints.Count == Coordinates.Count;
        public bool IsHorizontal { get; protected set; }
        public int Size { get; protected set; }

        public Ship(List<Point> coordinates)
        {
            Coordinates = coordinates;
            HitPoints = new HashSet<Point>();
            Hits = new List<Point>();
            // Визначаємо орієнтацію корабля
            if (coordinates.Count > 1)
            {
                // Якщо Y-координати однакові - корабель горизонтальний
                IsHorizontal = coordinates[0].Y == coordinates[1].Y;
            }
            else
            {
                // Для однопалубного корабля орієнтація не важлива
                IsHorizontal = true;
            }
            Size = coordinates.Count;
        }

        protected Ship(List<Point> coordinates, bool isHorizontal)
        {
            Coordinates = coordinates;
            HitPoints = new HashSet<Point>();
            Hits = new List<Point>();
            IsHorizontal = isHorizontal;
            Size = coordinates.Count;
        }

        // Регістрація попадання
        public void RegisterHit(Point hit)
        {
            if (Coordinates.Contains(hit) && !HitPoints.Contains(hit))
            {
                HitPoints.Add(hit);
                Hits.Add(hit);
            }
        }

        // Оновлення статусу потопленого корабля
        public void UpdateSunkStatus()
        {
            if (IsSunk)
            {
                // Додаткові дії, якщо корабель потоплений, наприклад, зміна вигляду
            }
        }
    }

    public class SingleDeckShip : Ship
    {
        public SingleDeckShip(List<Point> coordinates) : base(coordinates) { }
        public SingleDeckShip(List<Point> coordinates, bool isHorizontal) : base(coordinates, isHorizontal) { }
    }

    public class DoubleDeckShip : Ship
    {
        public DoubleDeckShip(List<Point> coordinates) : base(coordinates) { }
        public DoubleDeckShip(List<Point> coordinates, bool isHorizontal) : base(coordinates, isHorizontal) { }
    }

    public class TripleDeckShip : Ship
    {
        public TripleDeckShip(List<Point> coordinates) : base(coordinates) { }
        public TripleDeckShip(List<Point> coordinates, bool isHorizontal) : base(coordinates, isHorizontal) { }
    }

    public class QuadDeckShip : Ship
    {
        public QuadDeckShip(List<Point> coordinates) : base(coordinates) { }
        public QuadDeckShip(List<Point> coordinates, bool isHorizontal) : base(coordinates, isHorizontal) { }
    }
}
