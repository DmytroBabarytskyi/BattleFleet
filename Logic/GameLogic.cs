using BattleFleet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace BattleFleet.Logic
{
    public class GameLogic
    {
        private List<Ship> computerFleet;
        private HashSet<Point> computerOccupied = new HashSet<Point>();
        private List<Ship> playerFleet = new List<Ship>();
        private HashSet<Point> playerOccupied = new HashSet<Point>();
        private List<Point> playerShots = new List<Point>();
        private List<Point> computerShots = new List<Point>();
        private bool isPlayerTurn = true;
        private Difficulty currentDifficulty;
        private List<Point> potentialTargets = new List<Point>();
        private Point? lastHit = null;
        private bool isHunting = false;
        private bool? isHorizontalShip = null;
        private List<Point> currentShipHits = new List<Point>();
        private Point? shootingDirection = null;
        private bool isDestroyingShip = false;

        private readonly Dictionary<int, int> maxShipsPerSize = new Dictionary<int, int>
        {
            { 1, 4 },
            { 2, 3 },
            { 3, 2 },
            { 4, 1 }
        };

        public event Action PlayerWon;
        public event Action PlayerLost;

        public GameLogic(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
            computerFleet = new List<Ship>();
            GenerateComputerFleet();
        }

        public void GenerateComputerFleet()
        {
            try
            {
                computerFleet = new List<Ship>();
                computerOccupied.Clear();

                for (int size = 4; size >= 1; size--)
                {
                    int count = maxShipsPerSize[size];
                    for (int i = 0; i < count; i++)
                    {
                        bool isPlaced = false;
                        int attempts = 0;
                        const int maxAttempts = 100;

                        while (!isPlaced && attempts < maxAttempts)
                        {
                            attempts++;
                            var coordinates = GenerateShipCoordinates(size);
                            if (IsValidPlacement(coordinates))
                            {
                                Ship ship = CreateShipBySize(coordinates, size);
                                if (ship != null)
                                {
                                    computerFleet.Add(ship);
                                    foreach (var coord in coordinates)
                                    {
                                        computerOccupied.Add(coord);
                                    }
                                    isPlaced = true;
                                }
                            }
                        }

                        if (!isPlaced)
                        {
                            throw new Exception($"Не вдалося розмістити {size}-палубний корабель");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Помилка при генерації флоту комп'ютера", ex);
            }
        }

        private List<Point> GenerateShipCoordinates(int size)
        {
            Random random = new Random();
            bool isHorizontal = random.Next(2) == 0;
            List<Point> coordinates = new List<Point>();

            int maxX = isHorizontal ? 10 - size : 10;
            int maxY = isHorizontal ? 10 : 10 - size;

            int startX = random.Next(maxX);
            int startY = random.Next(maxY);

            for (int i = 0; i < size; i++)
            {
                coordinates.Add(new Point(
                    isHorizontal ? startX + i : startX,
                    isHorizontal ? startY : startY + i
                ));
            }

            return coordinates;
        }

        private bool IsValidPlacement(List<Point> coordinates)
        {
            foreach (var point in coordinates)
            {
                if (computerOccupied.Contains(point))
                {
                    return false;
                }

                foreach (var surroundingPoint in GetSurroundingPoints(point))
                {
                    if (computerOccupied.Contains(surroundingPoint))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool IsValidPlayerPlacement(List<Point> coordinates)
        {
            foreach (var point in coordinates)
            {
                if (playerOccupied.Contains(point))
                {
                    return false;
                }

                foreach (var surroundingPoint in GetSurroundingPoints(point))
                {
                    if (playerOccupied.Contains(surroundingPoint))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private List<Point> GetSurroundingPoints(Point point)
        {
            var directions = new[]
            {
                new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1),
                new Point(-1, -1), new Point(-1, 1), new Point(1, -1), new Point(1, 1)
            };

            return directions
                .Select(d => new Point(point.X + d.X, point.Y + d.Y))
                .Where(p => p.X >= 0 && p.X < 10 && p.Y >= 0 && p.Y < 10)
                .ToList();
        }

        public Ship CreateShipBySize(List<Point> coordinates, int size)
        {
            bool isHorizontal = coordinates.Count > 1 && coordinates[0].Y == coordinates[1].Y;
            
            return size switch
            {
                1 => new SingleDeckShip(coordinates, isHorizontal),
                2 => new DoubleDeckShip(coordinates, isHorizontal),
                3 => new TripleDeckShip(coordinates, isHorizontal),
                4 => new QuadDeckShip(coordinates, isHorizontal),
                _ => null
            };
        }

        public bool ProcessPlayerShot(Point target)
        {
            if (playerShots.Contains(target)) return false;

            playerShots.Add(target);

            if (computerOccupied.Contains(target))
            {
                var hitShip = computerFleet.FirstOrDefault(ship => ship.Coordinates.Contains(target));
                if (hitShip != null)
                {
                    hitShip.RegisterHit(target);

                    if (hitShip.IsSunk)
                    {
                        if (computerFleet.All(ship => ship.IsSunk))
                        {
                            PlayerWon?.Invoke();
                            return true;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public Point? GetComputerShot()
        {
            if (currentDifficulty == Difficulty.Easy)
            {
                return GetEasyComputerShot();
            }
            else
            {
                return GetHardComputerShot();
            }
        }

        private Point? GetEasyComputerShot()
        {
            var availablePoints = new List<Point>();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var point = new Point(x, y);
                    if (!computerShots.Contains(point))
                    {
                        availablePoints.Add(point);
                    }
                }
            }

            if (availablePoints.Count == 0) return null;

            Random rand = new Random();
            return availablePoints[rand.Next(availablePoints.Count)];
        }

        private Point? GetHardComputerShot()
        {
            if (isDestroyingShip && lastHit.HasValue)
            {
                var nextTarget = FindNextTarget(lastHit.Value);
                if (nextTarget != null)
                {
                    return nextTarget;
                }

                if (shootingDirection.HasValue)
                {
                    var oppositeDirection = new Point(-shootingDirection.Value.X, -shootingDirection.Value.Y);
                    var oppositePoint = new Point(lastHit.Value.X + oppositeDirection.X, lastHit.Value.Y + oppositeDirection.Y);
                    
                    if (IsValidTarget(oppositePoint))
                    {
                        shootingDirection = oppositeDirection;
                        return oppositePoint;
                    }
                }

                var surroundingPoints = GetSurroundingPoints(lastHit.Value);
                foreach (var point in surroundingPoints)
                {
                    if (IsValidTarget(point))
                    {
                        return point;
                    }
                }

                isDestroyingShip = false;
                lastHit = null;
                isHorizontalShip = null;
                currentShipHits.Clear();
                shootingDirection = null;
            }

            if (potentialTargets.Count > 0)
            {
                var target = potentialTargets[0];
                potentialTargets.RemoveAt(0);
                return target;
            }

            return GetEasyComputerShot();
        }

        private Point? FindNextTarget(Point lastHit)
        {
            if (shootingDirection.HasValue)
            {
                var nextPoint = new Point(
                    lastHit.X + shootingDirection.Value.X,
                    lastHit.Y + shootingDirection.Value.Y
                );
                
                if (IsValidTarget(nextPoint))
                {
                    return nextPoint;
                }
                return null;
            }

            if (isHorizontalShip.HasValue)
            {
                var directions = isHorizontalShip.Value
                    ? new[] { new Point(-1, 0), new Point(1, 0) }
                    : new[] { new Point(0, -1), new Point(0, 1) };

                foreach (var dir in directions)
                {
                    var nextPoint = new Point(lastHit.X + dir.X, lastHit.Y + dir.Y);
                    if (IsValidTarget(nextPoint))
                    {
                        return nextPoint;
                    }
                }
            }
            else
            {
                var directions = new[]
                {
                    new Point(-1, 0),
                    new Point(1, 0),
                    new Point(0, -1),
                    new Point(0, 1)
                };

                foreach (var dir in directions)
                {
                    var nextPoint = new Point(lastHit.X + dir.X, lastHit.Y + dir.Y);
                    if (IsValidTarget(nextPoint))
                    {
                        return nextPoint;
                    }
                }
            }

            return null;
        }

        private bool IsValidTarget(Point point)
        {
            if (point.X < 0 || point.X >= 10 || point.Y < 0 || point.Y >= 10)
                return false;

            if (computerShots.Contains(point))
                return false;

            return true;
        }

        public void ProcessComputerShot(Point target)
        {
            computerShots.Add(target);

            if (playerOccupied.Contains(target))
            {
                var hitShip = playerFleet.FirstOrDefault(ship => ship.Coordinates.Contains(target));
                if (hitShip != null)
                {
                    hitShip.RegisterHit(target);
                    currentShipHits.Add(target);
                    isDestroyingShip = true;

                    if (hitShip.IsSunk)
                    {
                        isDestroyingShip = false;
                        isHunting = false;
                        lastHit = null;
                        isHorizontalShip = null;
                        currentShipHits.Clear();
                        shootingDirection = null;

                        if (playerFleet.All(ship => ship.IsSunk))
                        {
                            PlayerLost?.Invoke();
                        }
                    }
                    else
                    {
                        if (!lastHit.HasValue)
                        {
                            isHunting = true;
                            lastHit = target;
                        }
                        else if (currentShipHits.Count == 2)
                        {
                            var firstHit = currentShipHits[0];
                            var secondHit = currentShipHits[1];
                            
                            if (firstHit.X == secondHit.X)
                            {
                                isHorizontalShip = false;
                                shootingDirection = new Point(0, secondHit.Y > firstHit.Y ? 1 : -1);
                            }
                            else if (firstHit.Y == secondHit.Y)
                            {
                                isHorizontalShip = true;
                                shootingDirection = new Point(secondHit.X > firstHit.X ? 1 : -1, 0);
                            }
                        }
                    }
                }
            }
        }

        public void AddPlayerShip(Ship ship)
        {
            playerFleet.Add(ship);
            foreach (var coord in ship.Coordinates)
            {
                playerOccupied.Add(coord);
            }
        }

        public bool IsPlayerTurn => isPlayerTurn;
        public void SetPlayerTurn(bool value) => isPlayerTurn = value;
        public List<Ship> PlayerFleet => playerFleet;
        public List<Ship> ComputerFleet => computerFleet;
        public HashSet<Point> PlayerOccupied => playerOccupied;
        public HashSet<Point> ComputerOccupied => computerOccupied;
        public List<Point> PlayerShots => playerShots;
        public List<Point> ComputerShots => computerShots;
    }
} 