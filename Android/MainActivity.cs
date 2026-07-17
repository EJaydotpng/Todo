using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Todo.Android
{
    [Activity(
        Label = "Todo Studio",
        Theme = "@style/MyTheme.NoActionBar",
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            // Initialize SQLite raw provider for .NET Android
            SQLitePCL.Batteries_V2.Init();

            return base.CustomizeAppBuilder(builder);
        }
    }
}
