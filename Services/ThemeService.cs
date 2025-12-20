namespace SecureJournalapp_Paruhang_Khapung.Services
{
    public class ThemeService
    {
        public bool IsDarkMode { get; private set; }

        public event Action? OnThemeChanged;

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            OnThemeChanged?.Invoke();
        }
    }
}
