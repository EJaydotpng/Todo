using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
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
            
            // Register global Drop event handler for the Kanban columns
            AddHandler(DragDrop.DropEvent, Drop);
        }

        // Triggered when a task card is clicked and dragged by the mouse
        private void TaskCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is TaskItem task)
            {
                var data = new DataObject();
                data.Set("TaskItem", task);
                
                // Initiate the native drag-and-drop operation
                DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }

        // Triggered when a task card is dropped onto one of the columns
        private async void Drop(object? sender, DragEventArgs e)
        {
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
