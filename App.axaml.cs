using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace Todo
{
    public partial class App : Application, IStorageService
    {
        private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktopLifetime = desktop;

                // Instantiate services
                var todoService = new TodoService();
                var pdfReportService = new PdfReportService();
                var excelReportService = new ExcelReportService();

                // Instantiate viewmodel (App implements IStorageService)
                var mainViewModel = new MainViewModel(todoService, pdfReportService, excelReportService, this);

                // Initialize database & load initial data
                if (mainViewModel.InitializeCommand is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand initCommand)
                {
                    await initCommand.ExecuteAsync(null);
                }

                // Create and show MainWindow
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Implementation of IStorageService for cross-platform file saving
        public async System.Threading.Tasks.Task<string?> SaveFileDialogAsync(string defaultFileName, string extension, string filterName)
        {
            if (_desktopLifetime?.MainWindow == null) return null;

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(_desktopLifetime.MainWindow)?.StorageProvider;
            if (storageProvider == null) return null;

            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export TODO Report",
                SuggestedFileName = defaultFileName,
                DefaultExtension = extension,
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(filterName)
                    {
                        Patterns = new[] { "*." + extension }
                    }
                }
            };

            var fileFile = await storageProvider.SaveFilePickerAsync(options);
            return fileFile?.Path.LocalPath;
        }

        // Implementation of IStorageService for cross-platform file opening
        public async System.Threading.Tasks.Task<string?> OpenFileDialogAsync(string extension, string filterName)
        {
            if (_desktopLifetime?.MainWindow == null) return null;

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(_desktopLifetime.MainWindow)?.StorageProvider;
            if (storageProvider == null) return null;

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import TODO Database",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(filterName)
                    {
                        Patterns = new[] { "*." + extension }
                    }
                }
            };

            var files = await storageProvider.OpenFilePickerAsync(options);
            return files != null && files.Count > 0 ? files[0].Path.LocalPath : null;
        }
    }
}
