#if ANDROID
using Android.Content.Res;
#endif
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace CostApp
{
    public partial class AppShell : Shell
    {
        public bool IsLightMode { get; set; }
        public AppShell()
        {
            InitializeComponent();
            mode();
            BindingContext = this;
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            Application.Current.UserAppTheme = e.Value ? AppTheme.Light : AppTheme.Dark;
        }

        void mode()
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
