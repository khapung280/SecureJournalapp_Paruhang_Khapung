using Microsoft.JSInterop;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _js;

        public bool IsDarkMode { get; private set; }

        public ThemeService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task ApplyThemeAsync()
        {
            var theme = IsDarkMode ? "dark" : "light";
            await _js.InvokeVoidAsync("document.body.className", theme);
        }

        public async Task ToggleThemeAsync()
        {
            IsDarkMode = !IsDarkMode;
            await ApplyThemeAsync();
        }
    }
}
