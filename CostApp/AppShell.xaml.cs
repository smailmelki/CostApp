namespace CostApp
{
    public partial class AppShell : Shell
    {
        public bool IsDarkMode { get; set; }
        public AppShell()
        {
            InitializeComponent();
            IsDarkMode = Application.Current?.UserAppTheme == AppTheme.Dark;
            BindingContext = this;
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
