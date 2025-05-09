// GamePage.xaml.cs
using BattleFleet.Models;
using BattleFleet.Logic;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace BattleFleet
{
    public sealed partial class GamePage : Page
    {
        private List<Ship> computerFleet;
        private HashSet<Point> computerOccupied = new HashSet<Point>();
        private Grid playerGrid;
        private Grid computerGrid;

        private enum PlacementState { Placing, Ready }
        private PlacementState currentState = PlacementState.Placing;

        private List<Ship> playerFleet = new List<Ship>();
        private HashSet<Point> playerOccupied = new HashSet<Point>();
        private int placingShipSize = 4; // Починаємо з найбільшого
        private int shipsPlacedCount = 0;
        private bool isHorizontal = true;

        private readonly Dictionary<int, int> maxShipsPerSize = new Dictionary<int, int>
        {
            { 1, 4 },
            { 2, 3 },
            { 3, 2 },
            { 4, 1 }
        };

        public event Action PlayerWon;
        public event Action PlayerLost;

        private List<BitmapImage> oceanFrames = new List<BitmapImage>();
        private List<BitmapImage> hitFrames = new List<BitmapImage>();
        private List<BitmapImage> missFrames = new List<BitmapImage>();

        private Dictionary<string, BitmapImage> shipImages = new Dictionary<string, BitmapImage>();

        private int currentFrame = 0;
        private DispatcherTimer frameTimer;
        private Dictionary<Button, int> buttonFrameIndex = new Dictionary<Button, int>();

        private HashSet<Button> animatedButtons = new HashSet<Button>();
        private Dictionary<Button, DispatcherTimer> perButtonTimers = new Dictionary<Button, DispatcherTimer>();

        private bool isPlayerTurn = true;
        private List<Point> playerShots = new List<Point>();
        private List<Point> computerShots = new List<Point>();

        public GamePage()
        {
            this.InitializeComponent();
            // Завантаження кадрів та запуск анімації
            LoadOceanFrames();
            LoadShipImages();

            StartOceanAnimation();

            PlayerWon += OnPlayerWon;
            PlayerLost += OnPlayerLost;

            playerGrid = PlayerGrid;
            computerGrid = ComputerGrid;
            GenerateGrid();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is GameState savedState)
            {
                // Ініціалізуємо computerFleet перед завантаженням стану
                computerFleet = new List<Ship>();
                computerOccupied = new HashSet<Point>();
                
                LoadGameState(savedState);
                // Show save button and hide placement controls for loaded game
                SaveGameButton.Opacity = 1.0;
                ShipSelector.Visibility = Visibility.Collapsed;
                SetHorizontalButton.Visibility = Visibility.Collapsed;
                SetVerticalButton.Visibility = Visibility.Collapsed;
                FinishPlacementButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Ініціалізуємо computerFleet для нової гри
                computerFleet = new List<Ship>();
                computerOccupied = new HashSet<Point>();
                
                // Start new game
                GenerateComputerFleet();
                // Hide save button and show placement controls for new game
                SaveGameButton.Opacity = 0.3;
                ShipSelector.Visibility = Visibility.Visible;
                SetHorizontalButton.Visibility = Visibility.Visible;
                SetVerticalButton.Visibility = Visibility.Visible;
                FinishPlacementButton.Visibility = Visibility.Visible;
            }
        }

        private void LoadGameState(GameState gameState)
        {
            // Clear existing state
            playerFleet.Clear();
            computerFleet.Clear();
            playerOccupied.Clear();
            computerOccupied.Clear();
            playerShots.Clear();
            computerShots.Clear();

            // Load player ships
            foreach (var ship in gameState.PlayerShips)
            {
                playerFleet.Add(ship);
                foreach (var coord in ship.Coordinates)
                {
                    playerOccupied.Add(coord);
                }
            }

            // Load computer ships
            foreach (var ship in gameState.ComputerShips)
            {
                computerFleet.Add(ship);
                foreach (var coord in ship.Coordinates)
                {
                    computerOccupied.Add(coord);
                }
            }

            // Load shots
            playerShots = new List<Point>(gameState.PlayerShots);
            computerShots = new List<Point>(gameState.ComputerShots);

            // Set turn state
            isPlayerTurn = gameState.IsPlayerTurn;
            currentState = PlacementState.Ready;

            // Update UI
            UpdatePlayerGrid();
            UpdateComputerGrid();

            // Відновлюємо стан клітинок для кораблів комп'ютера
            foreach (var ship in computerFleet)
            {
                // Позначаємо всі влучання
                foreach (var hit in ship.Hits)
                {
                    Button cell = GetButtonAtPoint(computerGrid, hit);
                    if (cell != null)
                    {
                        StartLoopingAnimation(cell, hitFrames);
                        cell.IsEnabled = false;
                    }
                }

                // Якщо корабель потоплений, позначаємо клітинки навколо
                if (ship.IsSunk)
                {
                    MarkSurroundingCells(ship.Coordinates, computerGrid);
                }
            }

            // Відновлюємо стан клітинок для кораблів гравця
            foreach (var ship in playerFleet)
            {
                // Позначаємо всі влучання
                foreach (var hit in ship.Hits)
                {
                    Button cell = GetButtonAtPoint(playerGrid, hit);
                    if (cell != null)
                    {
                        StartLoopingAnimation(cell, hitFrames);
                        cell.IsEnabled = false;
                    }
                }

                // Якщо корабель потоплений, позначаємо клітинки навколо
                if (ship.IsSunk)
                {
                    MarkSurroundingCells(ship.Coordinates, playerGrid);
                }
            }

            // Відновлюємо стан промахів
            foreach (var shot in playerShots)
            {
                if (!computerOccupied.Contains(shot))
                {
                    Button cell = GetButtonAtPoint(computerGrid, shot);
                    if (cell != null)
                    {
                        StartLoopingAnimation(cell, missFrames);
                        cell.IsEnabled = false;
                    }
                }
            }

            foreach (var shot in computerShots)
            {
                if (!playerOccupied.Contains(shot))
                {
                    Button cell = GetButtonAtPoint(playerGrid, shot);
                    if (cell != null)
                    {
                        StartLoopingAnimation(cell, missFrames);
                        cell.IsEnabled = false;
                    }
                }
            }

            // Enable/disable grids based on turn and shots
            foreach (var child in computerGrid.Children)
            {
                if (child is Button button)
                {
                    button.IsEnabled = isPlayerTurn && !playerShots.Contains((Point)button.Tag);
                }
            }

            foreach (var child in playerGrid.Children)
            {
                if (child is Button button)
                {
                    button.IsEnabled = false;
                }
            }

            // If it's computer's turn, make it take its turn
            if (!isPlayerTurn)
            {
                System.Diagnostics.Debug.WriteLine("Computer's turn after loading. Starting ComputerTurn...");
                ComputerTurn();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Player's turn after loading.");
            }
        }

        private void UpdatePlayerGrid()
        {
            // Clear existing images
            foreach (var child in playerGrid.Children)
            {
                if (child is Button button)
                {
                    button.Background = new ImageBrush
                    {
                        ImageSource = oceanFrames[0],
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            // Draw player ships
            foreach (var ship in playerFleet)
            {
                var coords = ship.Coordinates.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                bool isHorizontal;
                
                if (coords.Count > 1)
                {
                    var firstPoint = coords[0];
                    var lastPoint = coords[coords.Count - 1];
                    isHorizontal = Math.Abs(lastPoint.X - firstPoint.X) > Math.Abs(lastPoint.Y - firstPoint.Y);
                }
                else
                {
                    isHorizontal = true;
                }

                for (int i = 0; i < coords.Count; i++)
                {
                    var coord = coords[i];
                    var button = GetButtonAtPoint(playerGrid, coord);
                    if (button != null)
                    {
                        string imageName;
                        // Використовуємо різні методи для початкової розстановки і завантаженої гри
                        if (currentState == PlacementState.Placing)
                        {
                            imageName = GetShipSegmentImageName(coord, coords, isHorizontal);
                        }
                        else
                        {
                            imageName = GetLoadedShipSegmentImageName(coord, coords, isHorizontal);
                        }
                        
                        if (shipImages.TryGetValue(imageName, out var image))
                        {
                            SetStaticImageOnButton(button, image);
                        }
                    }
                }
            }

            // Draw computer shots
            foreach (var shot in computerShots)
            {
                var button = GetButtonAtPoint(playerGrid, shot);
                if (button != null)
                {
                    if (playerFleet.Any(ship => ship.Coordinates.Contains(shot)))
                    {
                        StartLoopingAnimation(button, hitFrames);
                    }
                    else
                    {
                        StartLoopingAnimation(button, missFrames);
                    }
                }
            }
        }

        private void UpdateComputerGrid()
        {
            // Clear existing images
            foreach (var child in computerGrid.Children)
            {
                if (child is Button button)
                {
                    button.Background = new ImageBrush
                    {
                        ImageSource = oceanFrames[0],
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            // Draw computer ships (тільки якщо вони потоплені)
            foreach (var ship in computerFleet)
            {
                if (ship.IsSunk)
                {
                    var coords = ship.Coordinates.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                    bool isHorizontal;
                    
                    if (coords.Count > 1)
                    {
                        var firstPoint = coords[0];
                        var lastPoint = coords[coords.Count - 1];
                        isHorizontal = Math.Abs(lastPoint.X - firstPoint.X) > Math.Abs(lastPoint.Y - firstPoint.Y);
                    }
                    else
                    {
                        isHorizontal = true;
                    }

                    for (int i = 0; i < coords.Count; i++)
                    {
                        var coord = coords[i];
                        var button = GetButtonAtPoint(computerGrid, coord);
                        if (button != null)
                        {
                            string imageName = GetLoadedShipSegmentImageName(coord, coords, isHorizontal);
                            if (shipImages.TryGetValue(imageName, out var image))
                            {
                                SetStaticImageOnButton(button, image);
                            }
                        }
                    }
                }
            }

            // Draw player shots
            foreach (var shot in playerShots)
            {
                var button = GetButtonAtPoint(computerGrid, shot);
                if (button != null)
                {
                    if (computerFleet.Any(ship => ship.Coordinates.Contains(shot)))
                    {
                        StartLoopingAnimation(button, hitFrames);
                    }
                    else
                    {
                        StartLoopingAnimation(button, missFrames);
                    }
                }
            }
        }

        private async void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gameState = new GameState
                {
                    PlayerShips = playerFleet,
                    ComputerShips = computerFleet,
                    PlayerShots = playerShots,
                    ComputerShots = computerShots,
                    IsPlayerTurn = isPlayerTurn,
                    LastSaved = DateTime.Now
                };

                await GameStateSerializer.SaveGameStateAsync(gameState);
                
                var dialog = new MessageDialog("Гра успішно збережена!", "Успіх");
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Помилка при збереженні гри: {ex.Message}", "Помилка");
                await dialog.ShowAsync();
            }
        }

        private void GenerateGrid()
        {
            // Генерація клітинок для поля гравця
            GenerateField(playerGrid);

            // Генерація клітинок для поля комп'ютера (схоже на поле гравця)
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
                    var initialFrame = 0;
                    Button cell = new Button
                    {
                        Tag = new Point(row, col),
                        Background = new ImageBrush
                        {
                            ImageSource = oceanFrames[initialFrame],
                            Stretch = Stretch.UniformToFill
                        },
                        Margin = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    buttonFrameIndex[cell] = initialFrame;

                    cell.Click += Cell_Click;
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }
        }

        private void LoadOceanFrames()
        {
            for (int i = 0; i < 3; i++)
            {
                oceanFrames.Add(new BitmapImage(new Uri($"ms-appx:///Assets/Ocean/OceanTile_{i}.png")));
                hitFrames.Add(new BitmapImage(new Uri($"ms-appx:///Assets/ShipStates/hit_{i}.png")));
                missFrames.Add(new BitmapImage(new Uri($"ms-appx:///Assets/Ocean/miss_{i}.png")));
            }
        }

        private void LoadShipImages()
        {
            string basePath = "ms-appx:///Assets/ShipStates/Ships/";
            string[] parts = {
        "single_horizontal", "single_vertical",
        "start_left", "start_top", "end_right", "end_bottom",
        "middle_horizontal", "middle_vertical"
    };

            foreach (var part in parts)
            {
                shipImages[part] = new BitmapImage(new Uri(basePath + part + ".png"));
            }
        }

        private void StartOceanAnimation()
        {
            frameTimer = new DispatcherTimer();
            frameTimer.Interval = TimeSpan.FromMilliseconds(100);
            frameTimer.Tick += (s, e) =>
            {
                if (oceanFrames.Count == 0) return;

                PlayerOceanBackground.Source = oceanFrames[currentFrame];
                ComputerOceanBackground.Source = oceanFrames[currentFrame];

                foreach (var entry in buttonFrameIndex.ToList())
                {
                    var button = entry.Key;

                    // Пропускаємо кнопки, які наразі анімуються
                    if (animatedButtons.Contains(button)) continue;

                    button.Background = new ImageBrush
                    {
                        ImageSource = oceanFrames[currentFrame],
                        Stretch = Stretch.UniformToFill
                    };

                    buttonFrameIndex[button] = currentFrame;
                }

                currentFrame = (currentFrame + 1) % oceanFrames.Count;
            };

            frameTimer.Start();
        }

        private void StartLoopingAnimation(Button button, List<BitmapImage> frames)
        {
            int frameIndex = 0;

            if (perButtonTimers.TryGetValue(button, out var existingTimer))
            {
                existingTimer.Stop();
                perButtonTimers.Remove(button);
            }

            animatedButtons.Add(button);

            DispatcherTimer animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(100);
            animationTimer.Tick += (s, e) =>
            {
                button.Background = new ImageBrush
                {
                    ImageSource = frames[frameIndex],
                    Stretch = Stretch.UniformToFill
                };

                frameIndex = (frameIndex + 1) % frames.Count;
            };

            animationTimer.Start();
            perButtonTimers[button] = animationTimer;
        }

        private void SetStaticImageOnButton(Button button, BitmapImage image)
        {
            animatedButtons.Add(button);

            DispatcherTimer animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(100);
            animationTimer.Tick += (s, e) =>
            {
                button.Background = new ImageBrush
                {
                    ImageSource = image,
                    Stretch = Stretch.UniformToFill
                };
            };

            animationTimer.Start();
            perButtonTimers[button] = animationTimer;
        }

        private void GenerateComputerFleet()
        {
            try
        {
            computerFleet = new List<Ship>();
                computerOccupied.Clear();

                // Generate ships in order from largest to smallest
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
                // Перевірка, чи не перекриваються з іншими кораблями
                if (computerOccupied.Contains(point))
                {
                    return false;
                }

                // Перевірка, чи є достатньо відстані навколо кожної клітинки
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

        private bool IsValidPlayerPlacement(List<Point> coordinates)
        {
            foreach (var point in coordinates)
            {

                // Перевірка, чи не перекриваються з іншими кораблями
                if (playerOccupied.Contains(point))
                {
                    return false;
                }

                // Перевірка, чи є достатньо відстані навколо кожної клітинки
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

        private Button GetButtonAtPoint(Grid grid, Point point)
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

        private void MarkSurroundingCells(List<Point> shipCoordinates, Grid grid)
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

        private async void MarkCellIfValid(Point point, Grid grid)
        {
            if (point.X >= 0 && point.X < 10 && point.Y >= 0 && point.Y < 10)
            {
                Button cell = GetButtonAtPoint(grid, point);
                if (cell != null && cell.IsEnabled) // Перевірка, що клітинка ще активна
                {
                    StartLoopingAnimation(cell, missFrames);
                    cell.IsEnabled = false;
                    
                    // Додаємо клітинку до списку пострілів
                    if (grid == computerGrid)
                    {
                        playerShots.Add(point);
                    }
                    else if (grid == playerGrid)
                    {
                        computerShots.Add(point);
                }
            }
        }
        }

        private async void Cell_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var point = (Point)button.Tag;

            if (currentState == PlacementState.Placing)
            {
                if (button != null && button.Tag is Point startPoint)
                {
                    List<Point> newShipCoords = new List<Point>();
                    int dx = isHorizontal ? 0 : 1;
                    int dy = isHorizontal ? 1 : 0;

                    for (int i = 0; i < placingShipSize; i++)
                    {
                        var p = new Point(startPoint.X + i * dx, startPoint.Y + i * dy);
                        if (p.X >= 10 || p.Y >= 10)
                        {
                            ShowMessage("Корабель виходить за межі!");
                            return;
                        }
                        newShipCoords.Add(p);
                    }

                    if (IsValidPlayerPlacement(newShipCoords))
                    {
                        int currentCount = playerFleet.Count(s => s.Size == placingShipSize);
                        if (currentCount >= maxShipsPerSize[placingShipSize])
                        {
                            ShowMessage($"Максимальна кількість кораблів розміру {placingShipSize} вже розміщена!");
                            return;
                        }

                        Ship ship = CreateShipBySize(newShipCoords, placingShipSize);
                      
                        if (ship == null)
                        {
                            ShowMessage("Помилка створення корабля!");
                            return;
                        }
                        
                        playerFleet.Add(ship);

                        foreach (var p in newShipCoords)
                        {
                            playerOccupied.Add(p);
                            Button cell = GetButtonAtPoint(playerGrid, p);
                            if (cell != null)
                            {
                                string imageName = GetShipSegmentImageName(p, newShipCoords, isHorizontal);
                                if (shipImages.TryGetValue(imageName, out var image))
                                {
                                    SetStaticImageOnButton(cell, image);
                                }
                                else
                                {
                                    cell.Background = new SolidColorBrush(Colors.Gray);
                                }
                            }
                        }

                        shipsPlacedCount++;

                        if (shipsPlacedCount >= 10)
                        {
                            currentState = PlacementState.Ready;
                            ShowMessage("Всі кораблі розміщені!");
                        }
                    }
                    else
                    {
                        ShowMessage("Некоректне розміщення!");
                    }
                }
            }
            else if (currentState == PlacementState.Ready)
            {
                if (!isPlayerTurn) return;

                if (playerShots.Contains(point)) return;

                playerShots.Add(point);

                if (button != null && button.Tag is Point target)
                {
                    if (!computerGrid.Children.Contains(button))
                    {
                        ShowMessage("Стріляти можна тільки по полі супротивника!");
                        return;
                    }

                    if (!button.IsEnabled) return;

                    button.IsEnabled = false;
                    isPlayerTurn = false;

                    if (computerOccupied.Contains(target))
                    {
                        StartLoopingAnimation(button, hitFrames);

                        var hitShip = computerFleet.FirstOrDefault(ship => ship.Coordinates.Contains(target));
                        if (hitShip != null)
                        {
                            hitShip.RegisterHit(target);

                            if (hitShip.IsSunk)
                            {
                                // Оновлюємо всі клітинки корабля одночасно
                                foreach (var coord in hitShip.Coordinates)
                                {
                                    var cell = GetButtonAtPoint(computerGrid, coord);
                                    if (cell != null)
                                    {
                                        StartLoopingAnimation(cell, hitFrames);
                                        cell.IsEnabled = false;
                                    }
                                }
                                MarkSurroundingCells(hitShip.Coordinates, computerGrid);
                            }
                        }

                        if (computerFleet.All(ship => ship.IsSunk))
                        {
                            currentState = PlacementState.Placing;
                            PlayerWon?.Invoke();
                            return;
                        }

                        isPlayerTurn = true;
                    }
                    else
                    {
                        StartLoopingAnimation(button, missFrames);
                        ComputerTurn();
                    }

                    foreach (var child in computerGrid.Children)
                    {
                        if (child is Button btn)
                        {
                            btn.IsEnabled = isPlayerTurn;
                        }
                    }
                }
            }
        }

        private void SetHorizontal_Click(object sender, RoutedEventArgs e) => isHorizontal = true;
        private void SetVertical_Click(object sender, RoutedEventArgs e) => isHorizontal = false;

        private void ShipSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShipSelector.SelectedItem is ComboBoxItem item)
            {
                placingShipSize = int.Parse(item.Tag.ToString());
            }
        }

        private Ship CreateShipBySize(List<Point> coordinates, int size)
        {
            // Визначаємо орієнтацію на основі координат
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

        private async void ComputerTurn()
        {
            System.Diagnostics.Debug.WriteLine("ComputerTurn started");
            
            // Знаходимо всі доступні клітинки на полі гравця
            var availableButtons = playerGrid.Children.OfType<Button>()
                .Where(b => !computerShots.Contains((Point)b.Tag)).ToList();

            System.Diagnostics.Debug.WriteLine($"Available buttons: {availableButtons.Count}");

            if (availableButtons.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No available buttons, ending computer turn");
                isPlayerTurn = true;
                EnableComputerGrid();
                return;
            }

            Random rand = new Random();
            var btn = availableButtons[rand.Next(availableButtons.Count)];
            var target = (Point)btn.Tag;

            System.Diagnostics.Debug.WriteLine($"Computer shooting at ({target.X}, {target.Y})");

            btn.IsEnabled = false;
            computerShots.Add(target);

            if (playerOccupied.Contains(target))
            {
                System.Diagnostics.Debug.WriteLine("Computer hit!");
                StartLoopingAnimation(btn, hitFrames);

                var hitShip = playerFleet.FirstOrDefault(ship => ship.Coordinates.Contains(target));
                if (hitShip != null)
                {
                    hitShip.RegisterHit(target);

                    if (hitShip.IsSunk)
                    {
                        System.Diagnostics.Debug.WriteLine("Computer sunk a ship!");
                        // Оновлюємо всі клітинки корабля одночасно
                        foreach (var coord in hitShip.Coordinates)
                        {
                            var cell = GetButtonAtPoint(playerGrid, coord);
                            if (cell != null)
                            {
                                StartLoopingAnimation(cell, hitFrames);
                                cell.IsEnabled = false;
                            }
                        }
                        MarkSurroundingCells(hitShip.Coordinates, playerGrid);
                        ShowMessage("Комп'ютер потопив ваш корабель!");
                    }
                }

                if (playerFleet.All(ship => ship.IsSunk))
                {
                    System.Diagnostics.Debug.WriteLine("All player ships sunk, game over!");
                    currentState = PlacementState.Placing;
                    PlayerLost?.Invoke();
                    return;
                }

                // Якщо влучив - ходить ще раз
                ComputerTurn();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Computer missed");
                StartLoopingAnimation(btn, missFrames);
                isPlayerTurn = true;
                EnableComputerGrid();
            }
        }

        private void EnableComputerGrid()
        {
            foreach (var child in computerGrid.Children)
            {
                if (child is Button btn && !playerShots.Contains((Point)btn.Tag))
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private async void OnPlayerWon()
        {
            if (CurrentPlayer.IsLoggedIn)
            {
                await StatsUpdater.UpdateStats(true, 
                    computerFleet.Count(s => s.IsSunk), 
                    playerFleet.Count(s => s.IsSunk));
            }
            ShowEndGameDialog("Перемога! Ви потопили всі кораблі супротивника!");
        }

        private async void OnPlayerLost()
        {
            if (CurrentPlayer.IsLoggedIn)
            {
                await StatsUpdater.UpdateStats(false, 
                    computerFleet.Count(s => s.IsSunk), 
                    playerFleet.Count(s => s.IsSunk));
            }
            ShowEndGameDialog("Поразка! Всі ваші кораблі були потоплені.");
        }

        private string GetShipSegmentImageName(Point point, List<Point> coords, bool isHorizontal)
        {
            if (coords.Count == 1)
                return "single_horizontal";

            int index = coords.IndexOf(point);
            
            // Для горизонтальних кораблів
            if (isHorizontal)
            {
                if (index == 0) return "start_left";
                if (index == coords.Count - 1) return "end_right";
                return "middle_horizontal";
            }
            // Для вертикальних кораблів
            else
            {
                if (index == 0) return "start_top";
                if (index == coords.Count - 1) return "end_bottom";
                return "middle_vertical";
            }
        }

        private string GetLoadedShipSegmentImageName(Point point, List<Point> coords, bool isHorizontal)
        {
            if (coords.Count == 1)
                return "single_horizontal";

            int index = coords.IndexOf(point);
            
            // Для вертикальних кораблів
            if (!isHorizontal)
            {
                if (index == 0) return "start_left";
                if (index == coords.Count - 1) return "end_right";
                return "middle_horizontal";
            }
            // Для горизонтальних кораблів
            else
            {
                if (index == 0) return "start_top";
                if (index == coords.Count - 1) return "end_bottom";
                return "middle_vertical";
            }
        }

        private async void FinishPlacement_Click(object sender, RoutedEventArgs e)
        {
            if (shipsPlacedCount < 10)
            {
                ShowMessage("Спочатку розташуйте всі кораблі!");
                return;
            }

            if (currentState == PlacementState.Placing)
            {
                try
                {
                    // Clear any existing computer fleet
                    computerFleet.Clear();
                    computerOccupied.Clear();

                    // Generate new computer fleet
                    GenerateComputerFleet();
                    
                    // Update UI to show computer ships (for debugging)
                    foreach (var ship in computerFleet)
                    {
                        foreach (var coord in ship.Coordinates)
                        {
                            computerOccupied.Add(coord);
                        }
                    }

                    // Update game state
            currentState = PlacementState.Ready;
                    isPlayerTurn = true;

                    // Update UI visibility
                    SaveGameButton.Opacity = 1.0;
                    ShipSelector.Visibility = Visibility.Collapsed;
                    SetHorizontalButton.Visibility = Visibility.Collapsed;
                    SetVerticalButton.Visibility = Visibility.Collapsed;
                    FinishPlacementButton.Visibility = Visibility.Collapsed;

                    // Enable computer grid for shooting
                    foreach (var child in computerGrid.Children)
                    {
                        if (child is Button button)
                        {
                            button.IsEnabled = true;
                        }
                    }

                    // Disable player grid for shooting
                    foreach (var child in playerGrid.Children)
                    {
                        if (child is Button button)
                        {
                            button.IsEnabled = false;
                        }
                    }

                    // Show success message
                    var dialog = new MessageDialog("Гра почалась! Оберіть клітинку на полі противника.", "Готово");
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new MessageDialog($"Помилка при запуску гри: {ex.Message}", "Помилка");
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void ShowMessage(string message)
        {
            // Виведення повідомлення через MessageDialog
            var messageDialog = new MessageDialog(message);
            messageDialog.ShowAsync();
        }

        private async void ShowEndGameDialog(string resultMessage)
        {
            ContentDialog endGameDialog = new ContentDialog
            {
                Title = resultMessage,
                Content = "Виберіть дію:",
                PrimaryButtonText = "Головне меню",
                SecondaryButtonText = "Повторити гру",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await endGameDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Перехід до головного меню
                Frame.Navigate(typeof(MainPage));
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Перезапустити гру (перезавантажити GamePage)
                Frame.Navigate(typeof(GamePage));
            }
        }

        private void BackToMainButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевіряємо, чи гра в процесі
            if (currentState == PlacementState.Ready)
            {
                // Показуємо діалог підтвердження
                var dialog = new MessageDialog("Ви впевнені, що хочете вийти? Прогрес гри буде втрачено.", "Підтвердження");
                dialog.Commands.Add(new UICommand("Так", command => Frame.Navigate(typeof(MainPage))));
                dialog.Commands.Add(new UICommand("Ні"));
                dialog.DefaultCommandIndex = 1;
                dialog.CancelCommandIndex = 1;
                dialog.ShowAsync();
            }
            else
            {
                // Якщо гра ще не почалась, просто переходимо на головну
                Frame.Navigate(typeof(MainPage));
            }
        }
    }
}
