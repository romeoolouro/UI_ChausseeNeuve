using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services
{
    public static class ToastService
    {
        private static System.Windows.Controls.Panel? _toastContainer;

        public static void Initialize(System.Windows.Controls.Panel container)
        {
            _toastContainer = container;
        }

        public static void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            if (_toastContainer == null) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = CreateToast(message, type);
                _toastContainer.Children.Add(toast);

                // Animation d'entrée
                var slideIn = new DoubleAnimation(300, 0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

                toast.BeginAnimation(Canvas.RightProperty, slideIn);
                toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // Auto-suppression après délai
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(durationMs)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    RemoveToast(toast);
                };
                timer.Start();
            });
        }

        private static Border CreateToast(string message, ToastType type)
        {
            var (icon, color, bgColor) = GetToastStyle(type);

            var toast = new Border
            {
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(0, 0, 4, 0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 20, 8),
                MaxWidth = 400,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 2,
                    Opacity = 0.25
                }
            };

            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // Icône
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 16,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Message
            var messageText = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.Black),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(messageText);
            toast.Child = stackPanel;

            // Positionnement
            Canvas.SetTop(toast, _toastContainer.Children.Count * 60 + 20);
            Canvas.SetRight(toast, 300); // Position initiale hors écran

            return toast;
        }

        private static (string icon, System.Windows.Media.Color color, System.Windows.Media.Color bgColor) GetToastStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => ("✅", System.Windows.Media.Color.FromRgb(34, 197, 94), System.Windows.Media.Color.FromRgb(240, 253, 244)),
                ToastType.Warning => ("⚠️", System.Windows.Media.Color.FromRgb(251, 146, 60), System.Windows.Media.Color.FromRgb(255, 251, 235)),
                ToastType.Error => ("❌", System.Windows.Media.Color.FromRgb(239, 68, 68), System.Windows.Media.Color.FromRgb(254, 242, 242)),
                ToastType.Info => ("ℹ️", System.Windows.Media.Color.FromRgb(59, 130, 246), System.Windows.Media.Color.FromRgb(239, 246, 255)),
                _ => ("ℹ️", System.Windows.Media.Color.FromRgb(59, 130, 246), System.Windows.Media.Color.FromRgb(239, 246, 255))
            };
        }

        private static void RemoveToast(Border toast)
        {
            var slideOut = new DoubleAnimation(0, 300, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));

            slideOut.Completed += (_, _) =>
            {
                _toastContainer?.Children.Remove(toast);
                ReorganizeToasts();
            };

            toast.BeginAnimation(Canvas.RightProperty, slideOut);
            toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private static void ReorganizeToasts()
        {
            if (_toastContainer == null) return;

            for (int i = 0; i < _toastContainer.Children.Count; i++)
            {
                if (_toastContainer.Children[i] is Border toast)
                {
                    var moveAnimation = new DoubleAnimation(Canvas.GetTop(toast), i * 60 + 20, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    toast.BeginAnimation(Canvas.TopProperty, moveAnimation);
                }
            }
        }
    }
}
