using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;

namespace SpotlightWin.Infrastructure.Services
{
    public class UiAutomationService
    {
        public static (double PercentX, double PercentY, string FoundName, Rect AbsoluteBounds)? FindNativeUiElement(string targetText)
        {
            if (string.IsNullOrWhiteSpace(targetText)) return null;

            string searchKey = targetText.ToLowerInvariant()
                .Replace("point to", "")
                .Replace("click on", "")
                .Replace("click", "")
                .Replace("locate", "")
                .Replace("the", "")
                .Trim();

            if (searchKey.Length < 2) return null;

            AutomationElement root = AutomationElement.RootElement;
            if (root == null) return null;

            // 🔧 FIX: Fully qualify System.Windows.Automation.Condition to avoid collision with System.Windows.Condition
            System.Windows.Automation.Condition controlCondition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem)
            );

            AutomationElementCollection elements = root.FindAll(TreeScope.Descendants, controlCondition);
            AutomationElement? bestMatch = null;

            foreach (AutomationElement element in elements)
            {
                try
                {
                    if (element.Current.IsOffscreen) continue;

                    string name = element.Current.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string lowerName = name.ToLowerInvariant();

                    if (lowerName.Equals(searchKey, StringComparison.OrdinalIgnoreCase))
                    {
                        bestMatch = element;
                        break;
                    }

                    if (lowerName.Contains(searchKey) || searchKey.Contains(lowerName))
                    {
                        bestMatch = element;
                    }
                }
                catch
                {
                    // Skip stale elements
                }
            }

            if (bestMatch != null)
            {
                try
                {
                    Rect bounds = bestMatch.Current.BoundingRectangle;
                    if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                    {
                        var primaryScreen = Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);

                        double centerX = bounds.X + (bounds.Width / 2.0);
                        double centerY = bounds.Y + (bounds.Height / 2.0);

                        double percentX = Math.Round((centerX / primaryScreen.Width) * 100.0, 1);
                        double percentY = Math.Round((centerY / primaryScreen.Height) * 100.0, 1);

                        return (percentX, percentY, bestMatch.Current.Name, bounds);
                    }
                }
                catch { }
            }

            return null;
        }
    }
}