using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace BattleFleet.Logic
{
    public class AnimationManager
    {
        private List<BitmapImage> oceanFrames = new List<BitmapImage>();
        private List<BitmapImage> hitFrames = new List<BitmapImage>();
        private List<BitmapImage> missFrames = new List<BitmapImage>();
        private Dictionary<string, BitmapImage> shipImages = new Dictionary<string, BitmapImage>();

        private int currentFrame = 0;
        private DispatcherTimer frameTimer;
        private Dictionary<Button, int> buttonFrameIndex = new Dictionary<Button, int>();
        private HashSet<Button> animatedButtons = new HashSet<Button>();
        private Dictionary<Button, DispatcherTimer> perButtonTimers = new Dictionary<Button, DispatcherTimer>();

        public AnimationManager()
        {
            LoadOceanFrames();
            LoadShipImages();
            StartOceanAnimation();
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

                foreach (var entry in buttonFrameIndex.ToList())
                {
                    var button = entry.Key;

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

        public void StartLoopingAnimation(Button button, List<BitmapImage> frames)
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

        public void SetStaticImageOnButton(Button button, BitmapImage image)
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

        public void AddButtonToOceanAnimation(Button button)
        {
            buttonFrameIndex[button] = 0;
        }

        public void RemoveButtonFromOceanAnimation(Button button)
        {
            buttonFrameIndex.Remove(button);
        }

        public List<BitmapImage> HitFrames => hitFrames;
        public List<BitmapImage> MissFrames => missFrames;
        public Dictionary<string, BitmapImage> ShipImages => shipImages;
    }
} 