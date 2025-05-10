using BattleFleet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace BattleFleet.Logic
{
    public class UIManager
    {
        private readonly AnimationManager animationManager;
        private readonly Grid playerGrid;
        private readonly Grid computerGrid;

        public UIManager(Grid playerGrid, Grid computerGrid, AnimationManager animationManager)
        {
            this.playerGrid = playerGrid;
            this.computerGrid = computerGrid;
            this.animationManager = animationManager;
        }

        public void GenerateGrid()
        {
            GenerateField(playerGrid);
            GenerateField(computerGrid);
        }

        private void GenerateField(Grid grid)
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            for (int i = 0; i < 10; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Button cell = new Button
                    {
                        Tag = new Point(row, col),
                        Background = new ImageBrush
                        {
                            ImageSource = animationManager.HitFrames[0],
                            Stretch = Stretch.UniformToFill
                        },
                        Margin = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    animationManager.AddButtonToOceanAnimation(cell);
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }
        }

        public Button GetButtonAtPoint(Grid grid, Point point)
        {
            foreach (var child in grid.Children)
            {
                if (child is Button btn && (Point)btn.Tag == point)
                {
                    return btn;
                }
            }
            return null;
        }

        public void UpdatePlayerGrid(List<Ship> playerFleet, List<Point> computerShots)
        {
            foreach (var child in playerGrid.Children)
            {
                if (child is Button button)
                {
                    button.Background = new ImageBrush
                    {
                        ImageSource = animationManager.HitFrames[0],
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            foreach (var ship in playerFleet)
            {
                var coords = ship.Coordinates.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                bool isHorizontal = coords.Count > 1 && coords[0].Y == coords[1].Y;

                for (int i = 0; i < coords.Count; i++)
                {
                    var coord = coords[i];
                    var button = GetButtonAtPoint(playerGrid, coord);
                    if (button != null)
                    {
                        string imageName = GetShipSegmentImageName(coord, coords, isHorizontal);
                        if (animationManager.ShipImages.TryGetValue(imageName, out var image))
                        {
                            animationManager.SetStaticImageOnButton(button, image);
                        }
                    }
                }
            }

            foreach (var shot in computerShots)
            {
                var button = GetButtonAtPoint(playerGrid, shot);
                if (button != null)
                {
                    if (playerFleet.Any(ship => ship.Coordinates.Contains(shot)))
                    {
                        animationManager.StartLoopingAnimation(button, animationManager.HitFrames);
                    }
                    else
                    {
                        animationManager.StartLoopingAnimation(button, animationManager.MissFrames);
                    }
                }
            }
        }

        public void UpdateComputerGrid(List<Ship> computerFleet, List<Point> playerShots)
        {
            foreach (var child in computerGrid.Children)
            {
                if (child is Button button)
                {
                    button.Background = new ImageBrush
                    {
                        ImageSource = animationManager.HitFrames[0],
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            foreach (var ship in computerFleet)
            {
                if (ship.IsSunk)
                {
                    var coords = ship.Coordinates.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                    bool isHorizontal = coords.Count > 1 && coords[0].Y == coords[1].Y;

                    for (int i = 0; i < coords.Count; i++)
                    {
                        var coord = coords[i];
                        var button = GetButtonAtPoint(computerGrid, coord);
                        if (button != null)
                        {
                            string imageName = GetShipSegmentImageName(coord, coords, isHorizontal);
                            if (animationManager.ShipImages.TryGetValue(imageName, out var image))
                            {
                                animationManager.SetStaticImageOnButton(button, image);
                            }
                        }
                    }
                }
            }

            foreach (var shot in playerShots)
            {
                var button = GetButtonAtPoint(computerGrid, shot);
                if (button != null)
                {
                    if (computerFleet.Any(ship => ship.Coordinates.Contains(shot)))
                    {
                        animationManager.StartLoopingAnimation(button, animationManager.HitFrames);
                    }
                    else
                    {
                        animationManager.StartLoopingAnimation(button, animationManager.MissFrames);
                    }
                }
            }
        }

        public void MarkSurroundingCells(List<Point> shipCoordinates, Grid grid)
        {
            var allSurrounding = new HashSet<Point>();

            foreach (var point in shipCoordinates)
            {
                foreach (var surrounding in GetSurroundingPoints(point))
                {
                    allSurrounding.Add(surrounding);
                }
            }

            foreach (var point in allSurrounding)
            {
                MarkCellIfValid(point, grid);
            }
        }

        private void MarkCellIfValid(Point point, Grid grid)
        {
            if (point.X >= 0 && point.X < 10 && point.Y >= 0 && point.Y < 10)
            {
                Button cell = GetButtonAtPoint(grid, point);
                if (cell != null && cell.IsEnabled)
                {
                    animationManager.StartLoopingAnimation(cell, animationManager.MissFrames);
                    cell.IsEnabled = false;
                }
            }
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

        private string GetShipSegmentImageName(Point point, List<Point> coords, bool isHorizontal)
        {
            if (coords.Count == 1)
                return "single_horizontal";

            int index = coords.IndexOf(point);
            
            if (isHorizontal)
            {
                if (index == 0) return "start_left";
                if (index == coords.Count - 1) return "end_right";
                return "middle_horizontal";
            }
            else
            {
                if (index == 0) return "start_top";
                if (index == coords.Count - 1) return "end_bottom";
                return "middle_vertical";
            }
        }

        public void EnableComputerGrid(bool enable)
        {
            foreach (var child in computerGrid.Children)
            {
                if (child is Button btn)
                {
                    btn.IsEnabled = enable;
                }
            }
        }
    }
} 