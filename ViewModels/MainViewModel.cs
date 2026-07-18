using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Data;

namespace TodoApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ITodoService _todoService;
        private readonly IPdfReportService _pdfReportService;
        private readonly IExcelReportService _excelReportService;
        private readonly IStorageService _storageService;

        // Application Navigation State
        private string _currentScreen = "Home"; // "Home", "TaskManagement", "MoneyManager"
        public string CurrentScreen
        {
            get => _currentScreen;
            set
            {
                if (SetProperty(ref _currentScreen, value))
                {
                    OnPropertyChanged(nameof(IsHomeScreenActive));
                    OnPropertyChanged(nameof(IsTaskScreenActive));
                    OnPropertyChanged(nameof(IsMoneyScreenActive));
                }
            }
        }

        public bool IsHomeScreenActive => CurrentScreen == "Home";
        public bool IsTaskScreenActive => CurrentScreen == "TaskManagement";
        public bool IsMoneyScreenActive => CurrentScreen == "MoneyManager";

        private readonly IFinanceService _financeService = new FinanceService();
        private bool _isLoadingFinanceData;

        // Finance Collections
        public ObservableCollection<FinanceAccount> FinanceAccounts { get; } = new ObservableCollection<FinanceAccount>();
        public ObservableCollection<FinanceCategory> FinanceCategories { get; } = new ObservableCollection<FinanceCategory>();
        public ObservableCollection<FinanceTransaction> FinanceTransactions { get; } = new ObservableCollection<FinanceTransaction>();
        public Dictionary<Guid, decimal> AccountBalances { get; } = new Dictionary<Guid, decimal>();

        // Finance View Selection & Navigation
        private FinanceAccount? _selectedFinanceAccount;
        public FinanceAccount? SelectedFinanceAccount
        {
            get => _selectedFinanceAccount;
            set
            {
                if (SetProperty(ref _selectedFinanceAccount, value))
                {
                    if (value != null)
                    {
                        IsAllTransactionsViewActive = false;
                    }
                    _ = LoadFinanceDataAsync(reloadAccounts: false);
                }
            }
        }

        private bool _isAllTransactionsViewActive = true;
        public bool IsAllTransactionsViewActive
        {
            get => _isAllTransactionsViewActive;
            set
            {
                if (SetProperty(ref _isAllTransactionsViewActive, value))
                {
                    if (value)
                    {
                        _selectedFinanceAccount = null;
                        OnPropertyChanged(nameof(SelectedFinanceAccount));
                    }
                    _ = LoadFinanceDataAsync(reloadAccounts: false);
                }
            }
        }

        public string FinanceViewTitle
        {
            get
            {
                if (IsAllTransactionsViewActive) return "All Transactions";
                if (SelectedFinanceAccount != null) return $"{SelectedFinanceAccount.Name} ({SelectedFinanceAccount.CurrencyCode})";
                return "Money Manager";
            }
        }

        // Add Account Dialog state
        private bool _isAddAccountDialogVisible;
        public bool IsAddAccountDialogVisible
        {
            get => _isAddAccountDialogVisible;
            set => SetProperty(ref _isAddAccountDialogVisible, value);
        }

        private string _newAccountName = string.Empty;
        public string NewAccountName
        {
            get => _newAccountName;
            set => SetProperty(ref _newAccountName, value);
        }

        private FinanceAccountType _newAccountType = FinanceAccountType.CASH;
        public FinanceAccountType NewAccountType
        {
            get => _newAccountType;
            set
            {
                if (SetProperty(ref _newAccountType, value))
                {
                    OnPropertyChanged(nameof(NewAccountTypeIndex));
                }
            }
        }

        public int NewAccountTypeIndex
        {
            get => (int)NewAccountType;
            set => NewAccountType = (FinanceAccountType)value;
        }

        public List<FinanceAccountType> AccountTypesList { get; } = Enum.GetValues(typeof(FinanceAccountType)).Cast<FinanceAccountType>().ToList();

        // Add Transaction Dialog state
        private bool _isAddTransactionDialogVisible;
        public bool IsAddTransactionDialogVisible
        {
            get => _isAddTransactionDialogVisible;
            set => SetProperty(ref _isAddTransactionDialogVisible, value);
        }

        private DateTimeOffset _newTransactionDate = DateTimeOffset.Now;
        public DateTimeOffset NewTransactionDate
        {
            get => _newTransactionDate;
            set => SetProperty(ref _newTransactionDate, value);
        }

        private string _newTransactionDescription = string.Empty;
        public string NewTransactionDescription
        {
            get => _newTransactionDescription;
            set => SetProperty(ref _newTransactionDescription, value);
        }

        private decimal _newTransactionAmount;
        public decimal NewTransactionAmount
        {
            get => _newTransactionAmount;
            set => SetProperty(ref _newTransactionAmount, value);
        }

        private FinanceAccount? _newTransactionAccount;
        public FinanceAccount? NewTransactionAccount
        {
            get => _newTransactionAccount;
            set => SetProperty(ref _newTransactionAccount, value);
        }

        private FinanceCategory? _newTransactionCategory;
        public FinanceCategory? NewTransactionCategory
        {
            get => _newTransactionCategory;
            set => SetProperty(ref _newTransactionCategory, value);
        }

        private string _newTransactionMemo = string.Empty;
        public string NewTransactionMemo
        {
            get => _newTransactionMemo;
            set => SetProperty(ref _newTransactionMemo, value);
        }

        private string _newTransactionTags = string.Empty;
        public string NewTransactionTags
        {
            get => _newTransactionTags;
            set => SetProperty(ref _newTransactionTags, value);
        }

        private string _financeSearchText = string.Empty;
        public string FinanceSearchText
        {
            get => _financeSearchText;
            set
            {
                if (SetProperty(ref _financeSearchText, value))
                {
                    _ = LoadFinanceDataAsync(reloadAccounts: false);
                }
            }
        }

        // Collections
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();

        // Navigation & Selection
        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    if (value != null)
                    {
                        IsFinishedViewActive = false;
                        IsAllTasksViewActive = false;
                    }
                    _searchText = string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(ViewTitle));
                    _ = LoadTasksAsync();
                }
            }
        }

        private bool _isFinishedViewActive;
        public bool IsFinishedViewActive
        {
            get => _isFinishedViewActive;
            set
            {
                if (SetProperty(ref _isFinishedViewActive, value))
                {
                    if (value)
                    {
                        _selectedCategory = null;
                        OnPropertyChanged(nameof(SelectedCategory));
                        _isAllTasksViewActive = false;
                        OnPropertyChanged(nameof(IsAllTasksViewActive));
                    }
                    _searchText = string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(ViewTitle));
                    _ = LoadTasksAsync();
                }
            }
        }

        private bool _isAllTasksViewActive = true;
        public bool IsAllTasksViewActive
        {
            get => _isAllTasksViewActive;
            set
            {
                if (SetProperty(ref _isAllTasksViewActive, value))
                {
                    if (value)
                    {
                        _selectedCategory = null;
                        OnPropertyChanged(nameof(SelectedCategory));
                        _isFinishedViewActive = false;
                        OnPropertyChanged(nameof(IsFinishedViewActive));
                    }
                    _searchText = string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(ViewTitle));
                    _ = LoadTasksAsync();
                }
            }
        }

        public string ViewTitle
        {
            get
            {
                if (IsFinishedViewActive) return "Completed Tasks";
                if (IsAllTasksViewActive) return "All Active Tasks";
                if (SelectedCategory != null) return SelectedCategory.Name;
                return "My Tasks";
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadTasksAsync();
                }
            }
        }

        private TaskItem? _selectedTask;
        public TaskItem? SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        // Form Fields (Inline Adding)
        private string _newCategoryName = string.Empty;
        public string NewCategoryName
        {
            get => _newCategoryName;
            set => SetProperty(ref _newCategoryName, value);
        }

        // Modal Dialog State for Add/Edit Task
        private bool _isTaskDialogVisible;
        public bool IsTaskDialogVisible
        {
            get => _isTaskDialogVisible;
            set => SetProperty(ref _isTaskDialogVisible, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _dialogTaskTitle = string.Empty;
        public string DialogTaskTitle
        {
            get => _dialogTaskTitle;
            set => SetProperty(ref _dialogTaskTitle, value);
        }

        private string _dialogTaskDescription = string.Empty;
        public string DialogTaskDescription
        {
            get => _dialogTaskDescription;
            set => SetProperty(ref _dialogTaskDescription, value);
        }

        private Category? _dialogTaskCategory;
        public Category? DialogTaskCategory
        {
            get => _dialogTaskCategory;
            set => SetProperty(ref _dialogTaskCategory, value);
        }

        private string _dialogTaskSubCategory = string.Empty;
        public string DialogTaskSubCategory
        {
            get => _dialogTaskSubCategory;
            set => SetProperty(ref _dialogTaskSubCategory, value);
        }

        private int? _editingTaskId;

        private bool _isCategoryDialogVisible;
        public bool IsCategoryDialogVisible
        {
            get => _isCategoryDialogVisible;
            set => SetProperty(ref _isCategoryDialogVisible, value);
        }

        private bool _isSubtaskDialogVisible;
        public bool IsSubtaskDialogVisible
        {
            get => _isSubtaskDialogVisible;
            set => SetProperty(ref _isSubtaskDialogVisible, value);
        }

        private bool _isDataModalVisible;
        public bool IsDataModalVisible
        {
            get => _isDataModalVisible;
            set => SetProperty(ref _isDataModalVisible, value);
        }

        private bool _isReportModalVisible;
        public bool IsReportModalVisible
        {
            get => _isReportModalVisible;
            set => SetProperty(ref _isReportModalVisible, value);
        }

        // Complete Task Dialog properties for Sub-Category prompt
        private bool _isCompleteTaskDialogVisible;
        public bool IsCompleteTaskDialogVisible
        {
            get => _isCompleteTaskDialogVisible;
            set => SetProperty(ref _isCompleteTaskDialogVisible, value);
        }

        private string _completeTaskSubCategory = string.Empty;
        public string CompleteTaskSubCategory
        {
            get => _completeTaskSubCategory;
            set => SetProperty(ref _completeTaskSubCategory, value);
        }

        private TaskItem? _taskBeingCompleted;
        public TaskItem? TaskBeingCompleted
        {
            get => _taskBeingCompleted;
            set => SetProperty(ref _taskBeingCompleted, value);
        }

        // Subcategories filtering in Completed tasks list
        public ObservableCollection<string> CompletedSubCategories { get; } = new ObservableCollection<string>();

        private string _selectedCompletedSubCategory = "All Subcategories";
        public string SelectedCompletedSubCategory
        {
            get => _selectedCompletedSubCategory;
            set
            {
                if (SetProperty(ref _selectedCompletedSubCategory, value))
                {
                    _ = LoadTasksAsync();
                }
            }
        }

        // Subtask Form Fields
        private string _newSubtaskTitle = string.Empty;
        public string NewSubtaskTitle
        {
            get => _newSubtaskTitle;
            set => SetProperty(ref _newSubtaskTitle, value);
        }

        // --- Custom Dialogs State (Replacements for Windows MessageBox) ---
        private bool _isConfirmDialogVisible;
        public bool IsConfirmDialogVisible
        {
            get => _isConfirmDialogVisible;
            set => SetProperty(ref _isConfirmDialogVisible, value);
        }

        private string _confirmTitle = string.Empty;
        public string ConfirmTitle
        {
            get => _confirmTitle;
            set => SetProperty(ref _confirmTitle, value);
        }

        private string _confirmMessage = string.Empty;
        public string ConfirmMessage
        {
            get => _confirmMessage;
            set => SetProperty(ref _confirmMessage, value);
        }

        private Func<Task>? _confirmCallback;

        private bool _isAlertDialogVisible;
        public bool IsAlertDialogVisible
        {
            get => _isAlertDialogVisible;
            set => SetProperty(ref _isAlertDialogVisible, value);
        }

        private string _alertMessage = string.Empty;
        public string AlertMessage
        {
            get => _alertMessage;
            set => SetProperty(ref _alertMessage, value);
        }

        // Commands
        public ICommand InitializeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SelectAllTasksCommand { get; }
        public ICommand SelectFinishedTasksCommand { get; }
        
        // Category Commands
        public ICommand OpenAddCategoryDialogCommand { get; }
        public ICommand CloseCategoryDialogCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }

        // Task Commands
        public ICommand OpenAddTaskDialogCommand { get; }
        public ICommand OpenEditTaskDialogCommand { get; }
        public ICommand CloseTaskDialogCommand { get; }
        public ICommand SaveTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleTaskStatusCommand { get; }
        public ICommand CloseCompleteTaskDialogCommand { get; }
        public ICommand SaveCompleteTaskCommand { get; }
        public ICommand CloseTaskDetailsCommand { get; }

        // Subtask Commands
        public ICommand OpenAddSubtaskDialogCommand { get; }
        public ICommand CloseSubtaskDialogCommand { get; }
        public ICommand AddSubtaskCommand { get; }
        public ICommand ToggleSubtaskStatusCommand { get; }
        public ICommand DeleteSubtaskCommand { get; }

        // Report Command
        public ICommand GenerateReportCommand { get; }
        public ICommand GenerateExcelReportCommand { get; }

        // Database Backup & Restore Commands
        public ICommand ExportDatabaseCommand { get; }
        public ICommand ImportDatabaseCommand { get; }
        public ICommand OpenDataModalCommand { get; }
        public ICommand CloseDataModalCommand { get; }
        public ICommand OpenReportModalCommand { get; }
        public ICommand CloseReportModalCommand { get; }

        // Confirm Overlay Dialog Commands
        public ICommand ConfirmCommand { get; }
        public ICommand CancelConfirmCommand { get; }
        public ICommand CloseAlertDialogCommand { get; }

        // Navigation Commands
        public ICommand GoToTaskManagementCommand { get; }
        public ICommand GoToMoneyManagerCommand { get; }
        public ICommand GoToHomeCommand { get; }

        // Finance Commands
        public ICommand OpenAddAccountDialogCommand { get; }
        public ICommand CloseAddAccountDialogCommand { get; }
        public ICommand SaveAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        public ICommand OpenAddTransactionDialogCommand { get; }
        public ICommand CloseAddTransactionDialogCommand { get; }
        public ICommand SaveTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand ExportFinanceCsvCommand { get; }

        // Constructor
        public MainViewModel(ITodoService todoService, IPdfReportService pdfReportService, IExcelReportService excelReportService, IStorageService storageService)
        {
            _todoService = todoService;
            _pdfReportService = pdfReportService;
            _excelReportService = excelReportService;
            _storageService = storageService;

            // Initialize commands
            InitializeCommand = new AsyncRelayCommand(InitializeAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);

            // Initialize navigation commands
            GoToTaskManagementCommand = new RelayCommand(() => CurrentScreen = "TaskManagement");
            GoToMoneyManagerCommand = new RelayCommand(() => { CurrentScreen = "MoneyManager"; _ = LoadFinanceDataAsync(reloadAccounts: true); });
            GoToHomeCommand = new RelayCommand(() => CurrentScreen = "Home");

            // Initialize finance commands
            OpenAddAccountDialogCommand = new RelayCommand(() => { NewAccountName = string.Empty; IsAddAccountDialogVisible = true; });
            CloseAddAccountDialogCommand = new RelayCommand(() => IsAddAccountDialogVisible = false);
            SaveAccountCommand = new AsyncRelayCommand(SaveAccountAsync);
            DeleteAccountCommand = new AsyncRelayCommand<FinanceAccount>(DeleteAccountAsync);

            OpenAddTransactionDialogCommand = new RelayCommand(() => {
                NewTransactionDescription = string.Empty;
                NewTransactionAmount = 0;
                NewTransactionMemo = string.Empty;
                NewTransactionTags = string.Empty;
                NewTransactionDate = DateTimeOffset.Now;
                NewTransactionAccount = FinanceAccounts.FirstOrDefault();
                NewTransactionCategory = FinanceCategories.FirstOrDefault();
                IsAddTransactionDialogVisible = true;
            });
            CloseAddTransactionDialogCommand = new RelayCommand(() => IsAddTransactionDialogVisible = false);
            SaveTransactionCommand = new AsyncRelayCommand(SaveTransactionAsync);
            DeleteTransactionCommand = new AsyncRelayCommand<FinanceTransaction>(DeleteTransactionAsync);
            ExportFinanceCsvCommand = new AsyncRelayCommand(ExportFinanceCsvAsync);
            SelectAllTasksCommand = new RelayCommand(() => IsAllTasksViewActive = true);
            SelectFinishedTasksCommand = new RelayCommand(() => IsFinishedViewActive = true);

            OpenAddCategoryDialogCommand = new RelayCommand(() => { NewCategoryName = string.Empty; IsCategoryDialogVisible = true; });
            CloseCategoryDialogCommand = new RelayCommand(() => IsCategoryDialogVisible = false);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync);
            DeleteCategoryCommand = new AsyncRelayCommand<Category>(DeleteCategoryAsync);

            OpenAddTaskDialogCommand = new RelayCommand(OpenAddTaskDialog);
            OpenEditTaskDialogCommand = new RelayCommand<TaskItem>(OpenEditTaskDialog);
            CloseTaskDialogCommand = new RelayCommand(CloseTaskDialog);
            SaveTaskCommand = new AsyncRelayCommand(SaveTaskAsync);
            DeleteTaskCommand = new AsyncRelayCommand<TaskItem>(DeleteTaskAsync);
            ToggleTaskStatusCommand = new AsyncRelayCommand<TaskItem>(ToggleTaskStatusAsync);
            CloseCompleteTaskDialogCommand = new AsyncRelayCommand(CloseCompleteTaskDialogAsync);
            SaveCompleteTaskCommand = new AsyncRelayCommand(SaveCompleteTaskAsync);
            CloseTaskDetailsCommand = new RelayCommand(() => SelectedTask = null);

            OpenAddSubtaskDialogCommand = new RelayCommand(() => { NewSubtaskTitle = string.Empty; IsSubtaskDialogVisible = true; });
            CloseSubtaskDialogCommand = new RelayCommand(() => IsSubtaskDialogVisible = false);
            AddSubtaskCommand = new AsyncRelayCommand<TaskItem>(AddSubtaskAsync);
            ToggleSubtaskStatusCommand = new AsyncRelayCommand<TaskItem>(ToggleSubtaskStatusAsync);
            DeleteSubtaskCommand = new AsyncRelayCommand<TaskItem>(DeleteSubtaskAsync);

            GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
            GenerateExcelReportCommand = new AsyncRelayCommand(GenerateExcelReportAsync);
            ExportDatabaseCommand = new AsyncRelayCommand(ExportDatabaseAsync);
            ImportDatabaseCommand = new AsyncRelayCommand(ImportDatabaseAsync);

            // Overlay controls commands
            ConfirmCommand = new AsyncRelayCommand(async () =>
            {
                IsConfirmDialogVisible = false;
                if (_confirmCallback != null)
                {
                    await _confirmCallback.Invoke();
                    _confirmCallback = null;
                }
            });

            CancelConfirmCommand = new RelayCommand(() =>
            {
                IsConfirmDialogVisible = false;
                _confirmCallback = null;
            });

            CloseAlertDialogCommand = new RelayCommand(() => IsAlertDialogVisible = false);

            OpenDataModalCommand = new RelayCommand(() => IsDataModalVisible = true);
            CloseDataModalCommand = new RelayCommand(() => IsDataModalVisible = false);
            OpenReportModalCommand = new RelayCommand(() => IsReportModalVisible = true);
            CloseReportModalCommand = new RelayCommand(() => IsReportModalVisible = false);
        }

        // Lifecycle Actions
        private async Task InitializeAsync()
        {
            await _todoService.EnsureDatabaseCreatedAsync();
            await LoadCategoriesAsync();
            await LoadCompletedSubCategoriesAsync();
            await LoadTasksAsync();
            await LoadFinanceDataAsync(reloadAccounts: true);
        }

        private async Task RefreshAsync()
        {
            await LoadCategoriesAsync();
            await LoadCompletedSubCategoriesAsync();
            await LoadTasksAsync();
            await LoadFinanceDataAsync(reloadAccounts: true);
        }

        // ================= FINANCE MODULE ACTIONS =================

        public async Task LoadFinanceDataAsync(bool reloadAccounts = false)
        {
            if (_isLoadingFinanceData) return;
            _isLoadingFinanceData = true;

            try
            {
                using var db = new TodoDbContext();
                if (!await db.FinanceCategories.AnyAsync())
                {
                    db.FinanceCategories.AddRange(
                        new FinanceCategory { Name = "Salary", Type = FinanceCategoryType.INCOME },
                        new FinanceCategory { Name = "Investments", Type = FinanceCategoryType.INCOME },
                        new FinanceCategory { Name = "Groceries", Type = FinanceCategoryType.EXPENSE },
                        new FinanceCategory { Name = "Dining Out", Type = FinanceCategoryType.EXPENSE },
                        new FinanceCategory { Name = "Rent / Mortgage", Type = FinanceCategoryType.EXPENSE },
                        new FinanceCategory { Name = "Utilities", Type = FinanceCategoryType.EXPENSE },
                        new FinanceCategory { Name = "Entertainment", Type = FinanceCategoryType.EXPENSE },
                        new FinanceCategory { Name = "Transport", Type = FinanceCategoryType.EXPENSE }
                    );
                    await db.SaveChangesAsync();
                }

                if (!await db.FinanceAccounts.AnyAsync())
                {
                    db.FinanceAccounts.Add(new FinanceAccount { Name = "Checking Account", Type = FinanceAccountType.CASH, CurrencyCode = "USD" });
                    await db.SaveChangesAsync();
                }

                var accounts = await _financeService.GetAllAccountsAsync();
                FinanceAccounts.Clear();
                AccountBalances.Clear();
                foreach (var account in accounts)
                {
                    var bal = await _financeService.GetAccountBalanceAsync(account.Id);
                    AccountBalances[account.Id] = bal;
                    FinanceAccounts.Add(account);
                }
                OnPropertyChanged(nameof(AccountBalances));

                var categories = await _financeService.GetAllCategoriesFlatAsync();
                FinanceCategories.Clear();
                foreach (var cat in categories)
                {
                    FinanceCategories.Add(cat);
                }

                var txs = await _financeService.GetTransactionsAsync(
                    accountId: SelectedFinanceAccount?.Id,
                    startDate: null,
                    endDate: null
                );

                if (!string.IsNullOrWhiteSpace(FinanceSearchText))
                {
                    string search = FinanceSearchText.ToLower();
                    txs = txs.Where(t => 
                        t.Description.ToLower().Contains(search) || 
                        t.LedgerEntries.Any(le => 
                            le.Memo.ToLower().Contains(search) || 
                            (le.Category != null && le.Category.Name.ToLower().Contains(search)) ||
                            le.Tags.Any(tg => tg.Name.ToLower().Contains(search))
                        )
                    ).ToList();
                }

                FinanceTransactions.Clear();
                foreach (var tx in txs)
                {
                    FinanceTransactions.Add(tx);
                }

                OnPropertyChanged(nameof(FinanceViewTitle));
            }
            catch (Exception ex)
            {
                AlertMessage = $"Error loading finance data: {ex.Message}";
                IsAlertDialogVisible = true;
            }
            finally
            {
                _isLoadingFinanceData = false;
            }
        }

        private async Task SaveAccountAsync()
        {
            if (string.IsNullOrWhiteSpace(NewAccountName))
            {
                AlertMessage = "Account name cannot be empty.";
                IsAlertDialogVisible = true;
                return;
            }

            try
            {
                await _financeService.CreateAccountAsync(NewAccountName, NewAccountType);
                IsAddAccountDialogVisible = false;
                await LoadFinanceDataAsync(reloadAccounts: true);
            }
            catch (Exception ex)
            {
                AlertMessage = $"Error creating account: {ex.Message}";
                IsAlertDialogVisible = true;
            }
        }

        private async Task DeleteAccountAsync(FinanceAccount? account)
        {
            if (account == null) return;

            using var db = new TodoDbContext();
            bool hasTransactions = await db.LedgerEntries.AnyAsync(le => le.AccountId == account.Id);

            if (hasTransactions)
            {
                ConfirmTitle = "Archive Account";
                ConfirmMessage = $"This account '{account.Name}' has historical transactions and cannot be deleted permanently without breaking your ledger history.\n\nWould you like to archive/deactivate it instead? It will be hidden from your active list.";
                _confirmCallback = async () =>
                {
                    try
                    {
                        var acc = await db.FinanceAccounts.FindAsync(account.Id);
                        if (acc != null)
                        {
                            acc.IsActive = false;
                            await db.SaveChangesAsync();
                        }
                        await LoadFinanceDataAsync(reloadAccounts: true);
                    }
                    catch (Exception ex)
                    {
                        AlertMessage = $"Error deactivating account: {ex.Message}";
                        IsAlertDialogVisible = true;
                    }
                };
                IsConfirmDialogVisible = true;
            }
            else
            {
                ConfirmTitle = "Delete Account";
                ConfirmMessage = $"Are you sure you want to permanently delete the empty account '{account.Name}'?";
                _confirmCallback = async () =>
                {
                    try
                    {
                        var acc = await db.FinanceAccounts.FindAsync(account.Id);
                        if (acc != null)
                        {
                            db.FinanceAccounts.Remove(acc);
                            await db.SaveChangesAsync();
                        }
                        await LoadFinanceDataAsync(reloadAccounts: true);
                    }
                    catch (Exception ex)
                    {
                        AlertMessage = $"Error deleting account: {ex.Message}";
                        IsAlertDialogVisible = true;
                    }
                };
                IsConfirmDialogVisible = true;
            }
        }

        private async Task SaveTransactionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTransactionDescription))
            {
                AlertMessage = "Transaction description cannot be empty.";
                IsAlertDialogVisible = true;
                return;
            }

            if (NewTransactionAmount <= 0)
            {
                AlertMessage = "Transaction amount must be greater than zero.";
                IsAlertDialogVisible = true;
                return;
            }

            if (NewTransactionAccount == null)
            {
                AlertMessage = "Please select a source account.";
                IsAlertDialogVisible = true;
                return;
            }

            if (NewTransactionCategory == null)
            {
                AlertMessage = "Please select a destination category.";
                IsAlertDialogVisible = true;
                return;
            }

            try
            {
                var entries = new List<LedgerEntryDto>
                {
                    new LedgerEntryDto
                    {
                        AccountId = NewTransactionAccount.Id,
                        Amount = -NewTransactionAmount,
                        Memo = NewTransactionMemo,
                        TagNames = NewTransactionTags.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    },
                    new LedgerEntryDto
                    {
                        CategoryId = NewTransactionCategory.Id,
                        Amount = NewTransactionAmount,
                        Memo = NewTransactionMemo,
                        TagNames = NewTransactionTags.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    }
                };

                await _financeService.RecordTransactionAsync(
                    NewTransactionDate.DateTime,
                    NewTransactionDescription,
                    NewTransactionAmount,
                    NewTransactionAccount.CurrencyCode,
                    entries
                );

                IsAddTransactionDialogVisible = false;
                await LoadFinanceDataAsync(reloadAccounts: true);
            }
            catch (Exception ex)
            {
                AlertMessage = $"Error saving transaction: {ex.Message}";
                IsAlertDialogVisible = true;
            }
        }

        private async Task DeleteTransactionAsync(FinanceTransaction? transaction)
        {
            if (transaction == null) return;
            ConfirmTitle = "Delete Transaction";
            ConfirmMessage = $"Are you sure you want to delete this transaction '{transaction.Description}'? This will reverse all balanced ledger splits.";
            _confirmCallback = async () =>
            {
                try
                {
                    using var db = new TodoDbContext();
                    var tx = await db.FinanceTransactions.FindAsync(transaction.Id);
                    if (tx != null)
                    {
                        db.FinanceTransactions.Remove(tx);
                        await db.SaveChangesAsync();
                    }
                    await LoadFinanceDataAsync(reloadAccounts: true);
                }
                catch (Exception ex)
                {
                    AlertMessage = $"Error deleting transaction: {ex.Message}";
                    IsAlertDialogVisible = true;
                }
            };
            IsConfirmDialogVisible = true;
        }

        private async Task ExportFinanceCsvAsync()
        {
            var txs = await _financeService.GetTransactionsAsync();
            if (!txs.Any())
            {
                AlertMessage = "No transactions available to export.";
                IsAlertDialogVisible = true;
                return;
            }

            var path = await _storageService.SaveFileDialogAsync("transactions_export.csv", ".csv", "CSV Files");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("TransactionID,Date,Description,TotalAmount,Currency,SplitID,Account,Category,SplitAmount,Memo,Tags");

                foreach (var tx in txs)
                {
                    foreach (var le in tx.LedgerEntries)
                    {
                        var accountName = le.Account != null ? le.Account.Name : "";
                        var categoryName = le.Category != null ? le.Category.Name : "";
                        var tags = string.Join(" ", le.Tags.Select(t => t.Name));
                        
                        sb.AppendLine($"\"{tx.Id}\",\"{tx.TransactionDate:yyyy-MM-dd}\",\"{tx.Description.Replace("\"", "\"\"")}\",{tx.TotalAmount},\"{tx.BaseCurrency}\",\"{le.Id}\",\"{accountName.Replace("\"", "\"\"")}\",\"{categoryName.Replace("\"", "\"\"")}\",{le.Amount},\"{le.Memo.Replace("\"", "\"\"")}\",\"{tags.Replace("\"", "\"\"")}\"");
                    }
                }

                await System.IO.File.WriteAllTextAsync(path, sb.ToString());
                AlertMessage = $"Successfully exported transactions to {System.IO.Path.GetFileName(path)}!";
                IsAlertDialogVisible = true;
            }
            catch (Exception ex)
            {
                AlertMessage = $"Error exporting CSV: {ex.Message}";
                IsAlertDialogVisible = true;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _todoService.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        public async Task LoadTasksAsync()
        {
            Tasks.Clear();
            SelectedTask = null;

            if (IsFinishedViewActive)
            {
                // Load all finished tasks
                var finishedTasks = await _todoService.GetRootTasksAsync(isFinished: true);

                // Filter by subcategory if a specific one is selected (case-insensitive)
                if (SelectedCompletedSubCategory != null && SelectedCompletedSubCategory != "All Subcategories")
                {
                    finishedTasks = finishedTasks
                        .Where(t => t.SubCategory != null && t.SubCategory.Equals(SelectedCompletedSubCategory, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Filter by search query (case-insensitive contains)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var query = SearchText.Trim();
                    finishedTasks = finishedTasks.Where(t => 
                        t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                        (t.Description != null && t.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (t.SubCategory != null && t.SubCategory.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                foreach (var task in finishedTasks)
                {
                    Tasks.Add(task);
                }
            }
            else if (IsAllTasksViewActive)
            {
                // Load all unfinished tasks
                var unfinishedTasks = await _todoService.GetRootTasksAsync(isFinished: false);

                // Filter by search query (case-insensitive contains)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var query = SearchText.Trim();
                    unfinishedTasks = unfinishedTasks.Where(t => 
                        t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                        (t.Description != null && t.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (t.SubCategory != null && t.SubCategory.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                foreach (var task in unfinishedTasks)
                {
                    Tasks.Add(task);
                }
            }
            else if (SelectedCategory != null)
            {
                // Load tasks by selected category
                var catTasks = await _todoService.GetRootTasksByCategoryAsync(SelectedCategory.Id, isFinished: false);

                // Filter by search query (case-insensitive contains)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var query = SearchText.Trim();
                    catTasks = catTasks.Where(t => 
                        t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                        (t.Description != null && t.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (t.SubCategory != null && t.SubCategory.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                foreach (var task in catTasks)
                {
                    Tasks.Add(task);
                }
            }
        }

        // Category Actions
        private async Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                return;

            await _todoService.AddCategoryAsync(NewCategoryName);
            NewCategoryName = string.Empty;
            IsCategoryDialogVisible = false;
            await LoadCategoriesAsync();
        }

        private async Task DeleteCategoryAsync(Category? category)
        {
            if (category == null) return;

            ConfirmTitle = "Confirm Delete";
            ConfirmMessage = $"Are you sure you want to delete the category '{category.Name}'? Tasks in this category will become Uncategorized.";
            _confirmCallback = async () =>
            {
                await _todoService.DeleteCategoryAsync(category.Id);
                var deletedSelected = SelectedCategory?.Id == category.Id;
                await LoadCategoriesAsync();
                if (deletedSelected)
                {
                    IsAllTasksViewActive = true;
                }
                else
                {
                    await LoadTasksAsync();
                }
            };
            IsConfirmDialogVisible = true;
        }

        // Task Dialog Actions
        private void OpenAddTaskDialog()
        {
            IsEditMode = false;
            DialogTaskTitle = string.Empty;
            DialogTaskDescription = string.Empty;
            DialogTaskCategory = SelectedCategory; // Auto-select current category
            DialogTaskSubCategory = string.Empty;
            _editingTaskId = null;
            IsTaskDialogVisible = true;
        }

        private void OpenEditTaskDialog(TaskItem? task)
        {
            if (task == null) return;

            IsEditMode = true;
            DialogTaskTitle = task.Title;
            DialogTaskDescription = task.Description ?? string.Empty;
            DialogTaskCategory = Categories.FirstOrDefault(c => c.Id == task.CategoryId);
            DialogTaskSubCategory = task.SubCategory ?? string.Empty;
            _editingTaskId = task.Id;
            IsTaskDialogVisible = true;
        }

        private void CloseTaskDialog()
        {
            IsTaskDialogVisible = false;
        }

        private async Task SaveTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(DialogTaskTitle))
            {
                AlertMessage = "Task Title is required.";
                IsAlertDialogVisible = true;
                return;
            }

            if (IsEditMode && _editingTaskId.HasValue)
            {
                // Update existing
                var task = await _todoService.GetTaskByIdAsync(_editingTaskId.Value);
                if (task != null)
                {
                    task.Title = DialogTaskTitle;
                    task.Description = DialogTaskDescription;
                    task.CategoryId = DialogTaskCategory?.Id;
                    task.SubCategory = string.IsNullOrWhiteSpace(DialogTaskSubCategory) ? null : DialogTaskSubCategory;
                    await _todoService.UpdateTaskAsync(task);
                }
            }
            else
            {
                // Create new
                var task = new TaskItem
                {
                    Title = DialogTaskTitle,
                    Description = DialogTaskDescription,
                    CategoryId = DialogTaskCategory?.Id,
                    SubCategory = string.IsNullOrWhiteSpace(DialogTaskSubCategory) ? null : DialogTaskSubCategory,
                    DateStarted = DateTime.Now,
                    IsFinished = false
                };
                await _todoService.AddTaskAsync(task);
            }

            IsTaskDialogVisible = false;
            await LoadCompletedSubCategoriesAsync();
            await LoadTasksAsync();
        }

        private async Task DeleteTaskAsync(TaskItem? task)
        {
            if (task == null) return;

            ConfirmTitle = "Confirm Delete";
            ConfirmMessage = $"Are you sure you want to delete the task '{task.Title}'?";
            _confirmCallback = async () =>
            {
                await _todoService.DeleteTaskAsync(task.Id);
                await LoadTasksAsync();
            };
            IsConfirmDialogVisible = true;
        }

        private async Task ToggleTaskStatusAsync(TaskItem? task)
        {
            if (task == null) return;

            if (!task.IsFinished)
            {
                // We are completing the task! Open the custom completion overlay modal
                TaskBeingCompleted = task;
                CompleteTaskSubCategory = string.Empty;
                IsCompleteTaskDialogVisible = true;
            }
            else
            {
                // We are unchecking/reactivating the task! Mark incomplete directly
                task.IsFinished = false;
                task.DateFinished = null;
                task.SubCategory = null; // Clear subcategory on reactivation

                await _todoService.UpdateTaskAsync(task);
                await LoadCompletedSubCategoriesAsync();
                await LoadTasksAsync();
            }
        }

        private async Task SaveCompleteTaskAsync()
        {
            if (TaskBeingCompleted == null) return;

            TaskBeingCompleted.IsFinished = true;
            TaskBeingCompleted.DateFinished = DateTime.Now;
            TaskBeingCompleted.SubCategory = string.IsNullOrWhiteSpace(CompleteTaskSubCategory) ? null : CompleteTaskSubCategory;

            // Optional polish: If main task is completed, mark all its subtasks as completed
            if (TaskBeingCompleted.Subtasks != null)
            {
                foreach (var subtask in TaskBeingCompleted.Subtasks)
                {
                    subtask.IsFinished = true;
                    subtask.DateFinished = DateTime.Now;
                }
            }

            await _todoService.UpdateTaskAsync(TaskBeingCompleted);

            IsCompleteTaskDialogVisible = false;
            TaskBeingCompleted = null;
            CompleteTaskSubCategory = string.Empty;

            await LoadCompletedSubCategoriesAsync();
            await LoadTasksAsync();
        }

        private async Task CloseCompleteTaskDialogAsync()
        {
            IsCompleteTaskDialogVisible = false;
            TaskBeingCompleted = null;
            CompleteTaskSubCategory = string.Empty;
            await LoadTasksAsync(); // Force-reload to reset the visual checked state of the CheckBox in UI!
        }

        public async Task LoadCompletedSubCategoriesAsync()
        {
            try
            {
                using (var db = new TodoApp.Data.TodoDbContext())
                {
                    var rawSubcats = await db.TaskItems
                        .Where(t => t.IsFinished && t.SubCategory != null && t.SubCategory != "")
                        .Select(t => t.SubCategory!)
                        .ToListAsync<string>();

                    // Normalize and group case-insensitively in-memory
                    var subcats = rawSubcats
                        .GroupBy(s => s.Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First()) // Keep original casing of the first encountered
                        .OrderBy(s => s)
                        .ToList();

                    CompletedSubCategories.Clear();
                    CompletedSubCategories.Add("All Subcategories");
                    foreach (var subcat in subcats)
                    {
                        CompletedSubCategories.Add(subcat);
                    }
                }
            }
            catch
            {
                // Database fallback/startup safely
                CompletedSubCategories.Clear();
                CompletedSubCategories.Add("All Subcategories");
            }
        }

        // Subtask Actions
        private async Task AddSubtaskAsync(TaskItem? parentTask)
        {
            if (parentTask == null || string.IsNullOrWhiteSpace(NewSubtaskTitle))
                return;

            var subtask = new TaskItem
            {
                Title = NewSubtaskTitle,
                ParentTaskId = parentTask.Id,
                DateStarted = DateTime.Now,
                IsFinished = false
            };

            await _todoService.AddTaskAsync(subtask);
            NewSubtaskTitle = string.Empty;
            IsSubtaskDialogVisible = false;

            // Reload this parent task to update local subtasks
            var updatedParent = await _todoService.GetTaskByIdAsync(parentTask.Id);
            if (updatedParent != null)
            {
                // Update in the ObservableCollection
                var index = Tasks.IndexOf(parentTask);
                if (index >= 0)
                {
                    Tasks[index] = updatedParent;
                    SelectedTask = updatedParent;
                }
            }
        }

        private async Task ToggleSubtaskStatusAsync(TaskItem? subtask)
        {
            if (subtask == null || !subtask.ParentTaskId.HasValue) return;

            subtask.IsFinished = !subtask.IsFinished;
            subtask.DateFinished = subtask.IsFinished ? DateTime.Now : null;
            await _todoService.UpdateTaskAsync(subtask);

            // Fetch parent to refresh UI
            var parentTask = await _todoService.GetTaskByIdAsync(subtask.ParentTaskId.Value);
            if (parentTask != null)
            {
                var existingParent = Tasks.FirstOrDefault(t => t.Id == parentTask.Id);
                if (existingParent != null)
                {
                    var index = Tasks.IndexOf(existingParent);
                    if (index >= 0)
                    {
                        Tasks[index] = parentTask;
                        SelectedTask = parentTask;
                    }
                }
            }
        }

        private async Task DeleteSubtaskAsync(TaskItem? subtask)
        {
            if (subtask == null || !subtask.ParentTaskId.HasValue) return;

            ConfirmTitle = "Confirm Delete";
            ConfirmMessage = $"Are you sure you want to delete the subtask '{subtask.Title}'?";
            _confirmCallback = async () =>
            {
                await _todoService.DeleteTaskAsync(subtask.Id);

                // Fetch parent task to refresh view
                var parentTask = await _todoService.GetTaskByIdAsync(subtask.ParentTaskId.Value);
                if (parentTask != null)
                {
                    var existingParent = Tasks.FirstOrDefault(t => t.Id == parentTask.Id);
                    if (existingParent != null)
                    {
                        var index = Tasks.IndexOf(existingParent);
                        if (index >= 0)
                        {
                            Tasks[index] = parentTask;
                            SelectedTask = parentTask;
                        }
                    }
                }
            };
            IsConfirmDialogVisible = true;
        }

        // PDF Report Generation Action (Uses abstracted cross-platform dialog)
        private async Task GenerateReportAsync()
        {
            IsReportModalVisible = false; // Hide mobile selection overlay
            var fileName = $"TodoReport_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = await _storageService.SaveFileDialogAsync(fileName, "pdf", "PDF files (*.pdf)");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    await _pdfReportService.GenerateReportAsync(filePath);
                    AlertMessage = "Report generated and saved successfully!";
                    IsAlertDialogVisible = true;
                }
                catch (Exception ex)
                {
                    AlertMessage = $"Failed to generate report: {ex.Message}";
                    IsAlertDialogVisible = true;
                }
            }
        }

        // Excel Report Generation Action (Uses abstracted cross-platform dialog)
        private async Task GenerateExcelReportAsync()
        {
            IsReportModalVisible = false; // Hide mobile selection overlay
            var fileName = $"TodoReport_{DateTime.Now:yyyyMMdd}.xlsx";
            var filePath = await _storageService.SaveFileDialogAsync(fileName, "xlsx", "Excel files (*.xlsx)");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    await _excelReportService.GenerateReportAsync(filePath);
                    AlertMessage = "Excel report generated and saved successfully!";
                    IsAlertDialogVisible = true;
                }
                catch (Exception ex)
                {
                    AlertMessage = $"Failed to generate Excel report: {ex.Message}";
                    IsAlertDialogVisible = true;
                }
            }
        }

        // Database Backup (Export) Action
        private async Task ExportDatabaseAsync()
        {
            IsDataModalVisible = false; // Hide mobile selection overlay
            var fileName = $"TodoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var filePath = await _storageService.SaveFileDialogAsync(fileName, "db", "SQLite Database (*.db)");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    await _todoService.ExportDatabaseAsync(filePath);
                    AlertMessage = "Database backed up successfully!";
                    IsAlertDialogVisible = true;
                }
                catch (Exception ex)
                {
                    AlertMessage = $"Failed to back up database: {ex.Message}";
                    IsAlertDialogVisible = true;
                }
            }
        }

        // Database Restore (Import) Action
        private async Task ImportDatabaseAsync()
        {
            IsDataModalVisible = false; // Hide mobile selection overlay
            ConfirmTitle = "Restore Database";
            ConfirmMessage = "Are you sure you want to restore? This will overwrite your current tasks and categories with the selected backup file.";
            _confirmCallback = async () =>
            {
                var filePath = await _storageService.OpenFileDialogAsync("db", "SQLite Database (*.db)");

                if (!string.IsNullOrEmpty(filePath))
                {
                    try
                    {
                        await _todoService.ImportDatabaseAsync(filePath);
                        
                        // Force UI to reload completely from the new database
                        await RefreshAsync();
                        
                        AlertMessage = "Database restored and loaded successfully!";
                        IsAlertDialogVisible = true;
                    }
                    catch (Exception ex)
                    {
                        AlertMessage = $"Failed to restore database: {ex.Message}";
                        IsAlertDialogVisible = true;
                    }
                }
            };
            IsConfirmDialogVisible = true;
        }
    }
}
