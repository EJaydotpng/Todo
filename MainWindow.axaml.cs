using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TodoApp.Models;
using TodoApp.ViewModels;

namespace Todo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Register global Drag-and-Drop event handlers for the Kanban columns
            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragEnterEvent, DragEnterHandler);
            AddHandler(DragDrop.DragLeaveEvent, DragLeaveHandler);
        }

        // Highlight column zones with a soft, semi-transparent blue tint when a task enters
        private void DragEnterHandler(object? sender, DragEventArgs e)
        {
            if (e.Source is Avalonia.Visual visual)
            {
                var current = visual;
                while (current != null)
                {
                    if (current is ListBox listBox && (listBox.Name == "TodoDropZone" || listBox.Name == "InProgressDropZone" || listBox.Name == "DoneDropZone"))
                    {
                        listBox.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0C3B82F6"));
                        break;
                    }
                    current = current.GetVisualParent();
                }
            }
        }

        // Reset backgrounds to fully transparent when dragging leaves or cancels
        private void DragLeaveHandler(object? sender, DragEventArgs e)
        {
            var todoBox = this.FindControl<ListBox>("TodoDropZone");
            if (todoBox != null) todoBox.Background = Avalonia.Media.Brushes.Transparent;

            var inProgressBox = this.FindControl<ListBox>("InProgressDropZone");
            if (inProgressBox != null) inProgressBox.Background = Avalonia.Media.Brushes.Transparent;

            var doneBox = this.FindControl<ListBox>("DoneDropZone");
            if (doneBox != null) doneBox.Background = Avalonia.Media.Brushes.Transparent;
        }

        // Triggered when a task card is clicked and dragged by the mouse
        private void TaskCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is TaskItem task)
            {
                var data = new DataObject();
                data.Set("TaskItem", task);

                // Create a gorgeous floating ghost card representing the task (Jira-style preview)
                var ghost = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EBF5FF")), // Soft translucent blue-white
                    BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6")), // Accent Blue border
                    BorderThickness = new Avalonia.Thickness(1.5),
                    CornerRadius = new Avalonia.CornerRadius(10),
                    Padding = new Avalonia.Thickness(15, 10),
                    Width = 220,
                    Height = 65,
                    Opacity = 0.9,
                    IsHitTestVisible = false,
                    [AdornerLayer.AdornedElementProperty] = border,
                    Child = new TextBlock
                    {
                        Text = task.Title,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E40AF")), // Strong deep blue
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        FontSize = 12,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                };

                // Add ghost to the window's AdornerLayer (renders on top of everything else)
                var layer = AdornerLayer.GetAdornerLayer(border);
                if (layer != null)
                {
                    layer.Children.Add(ghost);
                }

                // DragOver handler to move the ghost dynamically with the mouse cursor
                void OnDragOver(object? s, DragEventArgs args)
                {
                    // Calculate mouse position relative to the clicked task card (border)
                    var pos = args.GetPosition(border);
                    ghost.RenderTransform = new Avalonia.Media.TranslateTransform(pos.X - 110, pos.Y - 32);
                }

                // Add the handler globally to the window
                var window = this;
                window.AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, true);

                try
                {
                    // Fade the original card on the board to signify it's "lifted" in flight
                    border.Opacity = 0.3;

                    // Initiate drag-and-drop
                    DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
                }
                finally
                {
                    // Cleanup resources, restore card opacity, and unregister handler
                    border.Opacity = 1.0;
                    if (layer != null)
                    {
                        layer.Children.Remove(ghost);
                    }
                    window.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
                }
            }
        }

        // Triggered when a task card is dropped onto one of the columns
        private async void Drop(object? sender, DragEventArgs e)
        {
            // Instantly clear out all drag highlights upon dropping
            DragLeaveHandler(this, e);

            var task = e.Data.Get("TaskItem") as TaskItem;
            if (task == null) return;

            // Resolve the drop visual target
            var visual = e.Source as Avalonia.Visual;
            if (visual == null) return;

            // Traverse the visual tree upwards from the drop target to find which Column ListBox it landed inside
            string? destinationStatus = null;
            var current = visual;
            while (current != null)
            {
                if (current is ListBox listBox)
                {
                    if (listBox.Name == "TodoDropZone")
                    {
                        destinationStatus = "To Do";
                        break;
                    }
                    else if (listBox.Name == "InProgressDropZone")
                    {
                        destinationStatus = "In Progress";
                        break;
                    }
                    else if (listBox.Name == "DoneDropZone")
                    {
                        destinationStatus = "Done";
                        break;
                    }
                }
                current = current.GetVisualParent();
            }

            // If a valid column drop occurred and it's a status transition
            if (destinationStatus != null && task.Status != destinationStatus)
            {
                if (destinationStatus == "Done")
                {
                    // Dragging to Done: trigger the ViewModel status prompt modal!
                    if (DataContext is MainViewModel vm)
                    {
                        if (vm.ToggleTaskStatusCommand is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand asyncCommand)
                        {
                            await asyncCommand.ExecuteAsync(task);
                        }
                    }
                }
                else
                {
                    task.Status = destinationStatus;
                    task.IsFinished = false;
                    task.DateFinished = null;

                    // Update the SQLite database
                    using (var db = new TodoApp.Data.TodoDbContext())
                    {
                        db.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        await db.SaveChangesAsync();
                    }

                    // Force ViewModel collections to reload
                    if (DataContext is MainViewModel vm)
                    {
                        await vm.LoadTasksAsync();
                    }
                }
            }
        }
    }
}
