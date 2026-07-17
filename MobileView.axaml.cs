using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Todo
{
    public partial class MobileView : UserControl
    {
        public MobileView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
