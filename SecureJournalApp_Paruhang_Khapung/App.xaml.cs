namespace SecureJournalApp_Paruhang_Khapung
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "SecureJournalApp_Paruhang_Khapung" };
        }
    }
}
