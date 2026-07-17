using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Todo.Android
{
    [Activity(
        Theme = "@style/MyTheme.Splash",
        MainLauncher = true,
        NoHistory = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish();
        }
    }
}
