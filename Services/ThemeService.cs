using Microsoft.Maui.ApplicationModel;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    public class ThemeService
    {
        public bool IsDarkMode { get; private set; }

        public void ApplyTheme()
        {
            Application.Current!.UserAppTheme =
                IsDarkMode ? AppTheme.Dark : AppTheme.Light;
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme();
        }
    }
}
