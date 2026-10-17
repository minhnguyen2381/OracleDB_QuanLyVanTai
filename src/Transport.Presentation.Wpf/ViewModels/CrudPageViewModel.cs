using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;

namespace Transport.Presentation.Wpf.ViewModels;

// Lớp nền cho trang danh mục: danh sách + tìm kiếm + form thêm/sửa + xóa + xuất Excel
public abstract partial class CrudPageViewModel<TEntity> : PageViewModel
    where TEntity : class
{
    private List<TEntity> _all = [];

    protected CrudPageViewModel(PageServices services)
        : base(services) => SearchText = string.Empty;

    public ObservableCollection<TEntity> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial TEntity? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial bool IsFormOpen { get; set; }

    // true = form đang thêm mới, false = đang sửa
    [ObservableProperty]
    public partial bool IsNewItem { get; set; }

    // Mặc định chỉ điều hành được thêm/sửa/xóa (seller chỉ xem)
    public virtual bool CanCreate => IsOperator;

    public virtual bool CanEditItems => IsOperator;

    public virtual bool CanDeleteItems => IsOperator;

    protected abstract string ExportTitleKey { get; }

    protected abstract IReadOnlyList<ExportColumn<TEntity>> ExportColumns { get; }

    public override Task LoadAsync() => RunBusyAsync(ReloadAsync);

    protected abstract Task<IReadOnlyList<TEntity>> LoadItemsAsync();

    protected abstract bool Matches(TEntity item, string text);

    // item = null: form thêm mới
    protected abstract void FillForm(TEntity? item);

    protected abstract Task SaveFormAsync(bool isNew);

    protected abstract Task DeleteItemAsync(TEntity item);

    // Tên hiển thị trong câu hỏi xác nhận xóa
    protected abstract string Describe(TEntity item);

    protected virtual bool CanEditItem(TEntity item) => true;

    protected virtual bool CanDeleteItem(TEntity item) => true;

    protected virtual void OnSelectionChanged(TEntity? item)
    {
    }

    protected virtual void OnItemsLoaded(IReadOnlyList<TEntity> items)
    {
    }

    protected static bool Contains(string? source, string text) =>
        source?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true;

    protected static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected async Task ReloadAsync()
    {
        _all = [.. await LoadItemsAsync()];
        ApplyFilter();
        OnItemsLoaded(_all);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedItemChanged(TEntity? value) => OnSelectionChanged(value);

    private void ApplyFilter()
    {
        var text = SearchText.Trim();
        Items.Clear();
        foreach (var item in _all.Where(i => text.Length == 0 || Matches(i, text)))
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(ReloadAsync);

    [RelayCommand]
    private void New()
    {
        if (CanCreate)
        {
            OpenForm(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private void Edit()
    {
        if (SelectedItem is { } item && CanEditSelected())
        {
            OpenForm(item);
        }
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormOpen = false;
        ClearErrors();
    }

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            Notify.ShowWarning(FirstValidationError());
            return;
        }

        await SaveFormAsync(IsNewItem);
        IsFormOpen = false;
        Notify.ShowSuccess(Loc["Msg_Saved"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private Task DeleteAsync() => RunBusyAsync(async () =>
    {
        if (SelectedItem is not { } item || !Dialogs.Confirm(Loc.Format("Msg_ConfirmDelete", Describe(item))))
        {
            return;
        }

        await DeleteItemAsync(item);
        Notify.ShowSuccess(Loc["Msg_Deleted"]);
        await ReloadAsync();
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Items.ToList(), ExportColumns, ExportTitleKey);

    private void OpenForm(TEntity? item)
    {
        IsNewItem = item is null;
        FillForm(item);
        // Gán giá trị form xong mới xóa lỗi, để form mới không đỏ sẵn
        ClearErrors();
        IsFormOpen = true;
    }

    private bool CanEditSelected() => CanEditItems && SelectedItem is { } item && CanEditItem(item);

    private bool CanDeleteSelected() => CanDeleteItems && SelectedItem is { } item && CanDeleteItem(item);
}
