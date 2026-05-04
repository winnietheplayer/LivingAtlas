using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LivingAtlas.Desktop.Controls;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Editor.Tools;

namespace LivingAtlas.Desktop.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType LivingAtlasProjectFileType = new("Living Atlas Project JSON")
    {
        Patterns = new[] { "*.json" },
        MimeTypes = new[] { "application/json" }
    };

    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_closeConfirmed)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel && viewModel.IsDirty)
        {
            e.Cancel = true;

            bool canClose = await HandleUnsavedChangesAsync(viewModel);
            if (canClose)
            {
                _closeConfirmed = true;
                Close();
            }
        }
    }

    private async Task<bool> HandleUnsavedChangesAsync(MainWindowViewModel viewModel)
    {
        if (!viewModel.IsDirty)
        {
            return true;
        }

        var dialog = new UnsavedChangesDialog();
        var result = await dialog.ShowDialog<UnsavedChangesDialogResult>(this);

        if (result == UnsavedChangesDialogResult.Cancel)
        {
            viewModel.SetStatusMessage("Action cancelled");
            return false;
        }

        if (result == UnsavedChangesDialogResult.DontSave)
        {
            return true;
        }

        if (result == UnsavedChangesDialogResult.Save)
        {
            if (viewModel.HasProjectPath)
            {
                return await viewModel.SaveProjectAsync();
            }
            
            return await SaveProjectAsCoreAsync(viewModel);
        }

        return false;
    }

    private async void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!await HandleUnsavedChangesAsync(viewModel))
        {
            return;
        }

        viewModel.NewProject();
        RefreshProjectVisuals();
    }

    private async void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!await HandleUnsavedChangesAsync(viewModel))
        {
            return;
        }

        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Living Atlas Project",
                AllowMultiple = false,
                FileTypeFilter = new[] { LivingAtlasProjectFileType }
            });

            if (files.Count == 0)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                viewModel.SetStatusMessage("Open failed: selected file has no local path.");
                return;
            }

            if (await viewModel.OpenProjectAsync(path))
            {
                RefreshProjectVisuals();
            }
        }
        catch (Exception exception)
        {
            viewModel.SetStatusMessage($"Open failed: {exception.Message}");
        }
    }

    private async void SaveProject_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.HasProjectPath)
        {
            await viewModel.SaveProjectAsync();
            return;
        }

        await SaveProjectAsCoreAsync(viewModel);
    }

    private async void SaveProjectAs_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await SaveProjectAsCoreAsync(viewModel);
        }
    }

    private void ToggleSnapToGrid_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleSnapToGrid();
        }
    }

    private void SelectionChildMapAction_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.CreateOrOpenChildMapFromSelection())
        {
            RefreshProjectVisuals();
        }
    }

    private void CreateChildMapFromSelection_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.CreateChildMapFromSelection())
        {
            RefreshProjectVisuals();
        }
    }

    private void OpenSelectedChildMap_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OpenSelectedChildMap())
        {
            RefreshProjectVisuals();
        }
    }

    private void BackToParentMap_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OpenParentMap())
        {
            RefreshProjectVisuals();
        }
    }

    private void OpenRootMap_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OpenRootMap())
        {
            RefreshProjectVisuals();
        }
    }

    private void ApplyInspector_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.ApplyInspectorChanges())
        {
            RefreshProjectVisuals();
        }
    }

    private void Breadcrumb_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel
            || sender is not Button { CommandParameter: Guid mapId })
        {
            return;
        }

        if (viewModel.OpenMap(mapId))
        {
            RefreshProjectVisuals();
        }
    }

    private void ProjectTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel
            || sender is not TreeView { SelectedItem: ProjectTreeItemViewModel item }
            || item.MapId == null
            || item.IsLayer) // Skip map switching if a layer is selected to avoid UI refresh during double-clicks
        {
            return;
        }

        if (viewModel.OpenMap(item.MapId.Value))
        {
            RefreshProjectVisuals();
        }
    }

    private async void RenameLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        
        ProjectTreeItemViewModel? item = null;
        if (sender is MenuItem menuItem)
        {
            item = menuItem.DataContext as ProjectTreeItemViewModel;
        }

        if (item is null || !item.IsLayer || item.MapId == null || item.LayerId == null)
        {
            return;
        }

        await ExecuteRenameAsync(viewModel, item.MapId.Value, item.LayerId.Value, item.Name);
    }

    private async void LayerName_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        
        ProjectTreeItemViewModel? item = null;
        if (sender is Control control)
        {
            item = control.DataContext as ProjectTreeItemViewModel;
        }

        if (item is null || !item.IsLayer || item.MapId == null || item.LayerId == null)
        {
            return;
        }

        await ExecuteRenameAsync(viewModel, item.MapId.Value, item.LayerId.Value, item.Name);
        e.Handled = true;
    }

    private async Task ExecuteRenameAsync(MainWindowViewModel viewModel, Guid mapId, Guid layerId, string currentName)
    {
        var dialog = new RenameLayerDialog(currentName);
        var newName = await dialog.ShowDialog<string?>(this);

        if (!string.IsNullOrWhiteSpace(newName))
        {
            viewModel.RenameLayer(mapId, layerId, newName);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (TryHandleEditorKey(e))
        {
            return;
        }

        base.OnKeyDown(e);
    }

    private void SelectMoveTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.SelectMove);
    }

    private void PanTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.Pan);
    }

    private void DistrictTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.District);
    }

    private void RoadTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.Road);
    }

    private void PointOfInterestTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.PointOfInterest);
    }

    private void LabelTool_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveTool(EditorToolType.Label);
    }

    private bool TryHandleEditorKey(KeyEventArgs e)
    {
        if (EditorHotkeyGuard.ShouldIgnoreEditorHotkeys(e))
        {
            return false;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }

        if (e.KeyModifiers == KeyModifiers.None
            && e.Key == Key.Enter
            && viewModel.MapViewport.ActiveTool == EditorToolType.Road)
        {
            viewModel.MapViewport.TryFinishRoadDrawing();
            MapViewport.InvalidateVisual();
            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers == KeyModifiers.None
            && e.Key == Key.Enter
            && viewModel.MapViewport.ActiveTool == EditorToolType.District)
        {
            viewModel.MapViewport.TryFinishDistrictDrawing();
            MapViewport.InvalidateVisual();
            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers == KeyModifiers.None
            && e.Key == Key.Escape
            && viewModel.MapViewport.ActiveTool == EditorToolType.Road)
        {
            viewModel.MapViewport.CancelRoadDrawing();
            MapViewport.InvalidateVisual();
            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers == KeyModifiers.None
            && e.Key == Key.Escape
            && viewModel.MapViewport.ActiveTool == EditorToolType.District)
        {
            viewModel.MapViewport.CancelDistrictDrawing();
            MapViewport.InvalidateVisual();
            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers == KeyModifiers.None
            && (e.Key == Key.Delete || e.Key == Key.Back))
        {
            if (viewModel.MapViewport.DeleteSelectedObject())
            {
                MapViewport.InvalidateVisual();
            }

            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.G)
        {
            viewModel.ToggleSnapToGrid();
            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            if (viewModel.MapViewport.Undo())
            {
                MapViewport.InvalidateVisual();
            }

            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            if (viewModel.MapViewport.Redo())
            {
                MapViewport.InvalidateVisual();
            }

            e.Handled = true;
            return true;
        }

        if (e.KeyModifiers != KeyModifiers.None)
        {
            return false;
        }

        var tool = e.Key switch
        {
            Key.V or Key.S => EditorToolType.SelectMove,
            Key.H or Key.Space => EditorToolType.Pan,
            Key.D => EditorToolType.District,
            Key.R => EditorToolType.Road,
            Key.P => EditorToolType.PointOfInterest,
            Key.T => EditorToolType.Label,
            _ => (EditorToolType?)null
        };

        if (tool is null)
        {
            return false;
        }

        SetActiveTool(tool.Value);
        e.Handled = true;
        return true;
    }

    private void SetActiveTool(EditorToolType tool)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.MapViewport.SetActiveTool(tool);
        MapViewport.InvalidateVisual();
        MapViewport.Focus();
    }

    private async Task<bool> SaveProjectAsCoreAsync(MainWindowViewModel viewModel)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Living Atlas Project",
                SuggestedFileName = GetSuggestedProjectFileName(viewModel),
                DefaultExtension = "json",
                ShowOverwritePrompt = true,
                FileTypeChoices = new[] { LivingAtlasProjectFileType }
            });

            var path = file?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return await viewModel.SaveProjectAsAsync(path);
        }
        catch (Exception exception)
        {
            viewModel.SetStatusMessage($"Save failed: {exception.Message}");
            return false;
        }
    }

    private void RefreshProjectVisuals()
    {
        MapViewport.InvalidateVisual();
        MapViewport.Focus();
    }

    private static string GetSuggestedProjectFileName(MainWindowViewModel viewModel)
    {
        var fileName = viewModel.Project.Name.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(fileName)
            ? "LivingAtlasProject.json"
            : $"{fileName}.json";
    }
}
