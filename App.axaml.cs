using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace Todo
{
    public partial class App : Avalonia.Application, IStorageService
    {
        private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
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

#if !ANDROID
                // Create and show MainWindow
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                desktop.MainWindow = mainWindow;
#endif

                // Load database & tasks in the background so desktop app opens instantly
                if (mainViewModel.InitializeCommand is System.Windows.Input.ICommand initCommand)
                {
                    initCommand.Execute(null);
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                // Instantiate services
                var todoService = new TodoService();
                var pdfReportService = new PdfReportService();
                var excelReportService = new ExcelReportService();

                // Instantiate viewmodel (App implements IStorageService)
                var mainViewModel = new MainViewModel(todoService, pdfReportService, excelReportService, this);

                // Load the mobile-optimized View immediately (MUST be synchronous on mobile)
                singleView.MainView = new MobileView
                {
                    DataContext = mainViewModel
                };

                // Load database & tasks in the background
                if (mainViewModel.InitializeCommand is System.Windows.Input.ICommand initCommand)
                {
                    initCommand.Execute(null);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Helper to retrieve the current active TopLevel for cross-platform dialogs
        private Avalonia.Controls.TopLevel? GetTopLevel()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                return Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView && singleView.MainView != null)
            {
                return Avalonia.Controls.TopLevel.GetTopLevel(singleView.MainView);
            }
            return null;
        }

        // Implementation of IStorageService for cross-platform file saving
        public async System.Threading.Tasks.Task<string?> SaveFileDialogAsync(string defaultFileName, string extension, string filterName)
        {
            var storageProvider = GetTopLevel()?.StorageProvider;
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
            var storageProvider = GetTopLevel()?.StorageProvider;
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
