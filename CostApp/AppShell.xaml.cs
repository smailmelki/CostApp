#if ANDROID
using Android.Content.Res;
#endif

namespace CostApp
{
    public partial class AppShell : Shell
    {
        public bool IsLightMode { get; set; }

        public AppShell()
        {
            InitializeComponent();
            DetectInitialTheme();
            BindingContext = this;
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = e.Value ? AppTheme.Light : AppTheme.Dark;
            }
        }

        private void DetectInitialTheme()
        {
#if ANDROID
            var uiModeFlags = Android.App.Application.Context?.Resources?.Configuration?.UiMode & UiMode.NightMask;
            IsLightMode = uiModeFlags == UiMode.NightNo;
#else
            IsLightMode = Application.Current?.RequestedTheme == AppTheme.Light;
#endif
        }
    }
}
