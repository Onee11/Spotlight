using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace SpotlightWin.UI
{
    public partial class ScreenPointerWindow : Window
    {
        public ScreenPointerWindow(double targetX, double targetY, string labelText = "Pointing Here")
        {
            InitializeComponent();

            Left = targetX - (Width / 2);
            Top = targetY - (Height / 2);
            LabelText.Text = $"🎯 {labelText}";

            Loaded += (s, e) => AnimatePointer();
        }

        private async void AnimatePointer()
        {
            var sb = new Storyboard();

            var scaleX = new DoubleAnimation(0.5, 1.4, TimeSpan.FromMilliseconds(600))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            var scaleY = new DoubleAnimation(0.5, 1.4, TimeSpan.FromMilliseconds(600))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            Storyboard.SetTarget(scaleX, RingScale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
            Storyboard.SetTarget(scaleY, RingScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));

            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Begin();

            await Task.Delay(3600);
            Close();
        }

        public static void ShowAt(double screenX, double screenY, string label)
        {
            var pointer = new ScreenPointerWindow(screenX, screenY, label);
            pointer.Show();
        }
    }
}