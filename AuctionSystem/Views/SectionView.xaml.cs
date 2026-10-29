using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AuctionSystem.Models;
using AuctionSystem.Services;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Views;

public partial class SectionView : UserControl
{
    private readonly SectionDescriptor _section;
    private readonly bool _isAuctions;

    public SectionView(SectionDescriptor section)
    {
        InitializeComponent();
        _section = section;
        _isAuctions = section.Title == "Аукционы";
        AddButton.IsEnabled = section.CanAdd;
        EditButton.IsEnabled = section.CanEdit;
        DeleteButton.IsEnabled = section.CanDelete;
        CardButton.Visibility = _isAuctions ? Visibility.Visible : Visibility.Collapsed;
        CardButton.IsEnabled = false;
        BuildColumns();
        Load();
    }

    private DataRowView? CurrentRow => Grid.SelectedItem as DataRowView;

    private void BuildColumns()
    {
        Grid.Columns.Clear();
        foreach (var field in _section.Fields.Where(f => f.ShowInGrid && !f.Auto))
        {
            DataGridColumn column;
            if (field.ComboSql is not null)
            {
                column = new DataGridTextColumn
                {
                    Header = field.Header,
                    Binding = new Binding(field.Column + "__Display")
                };
            }
            else
            {
                column = field.Kind switch
                {
                    FieldKind.Bool => new DataGridCheckBoxColumn
                    {
                        Header = field.Header,
                        Binding = new Binding(field.Column),
                        IsReadOnly = true
                    },
                    FieldKind.Date => new DataGridTextColumn
                    {
                        Header = field.Header,
                        Binding = new Binding(field.Column) { StringFormat = "{0:d}" }
                    },
                    FieldKind.Time => new DataGridTextColumn
                    {
                        Header = field.Header,
                        Binding = new Binding(field.Column) { StringFormat = "{0:hh\\:mm}" }
                    },
                    FieldKind.Decimal => new DataGridTextColumn
                    {
                        Header = field.Header,
                        Binding = new Binding(field.Column) { StringFormat = "{0:N2}" }
                    },
                    _ => new DataGridTextColumn
                    {
                        Header = field.Header,
                        Binding = new Binding(field.Column)
                    }
                };
            }

            column.Width = new DataGridLength(field.GridWidth);
            Grid.Columns.Add(column);
        }
    }

    private void Load()
    {
        try
        {
            var table = DataService.LoadSection(_section, SearchBox.Text ?? string.Empty);
            AddDisplayColumns(table);
            Grid.ItemsSource = table.DefaultView;
            StatusText.Text = $"Записей: {table.Rows.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка загрузки данных: " + ex.Message;
        }
    }

    private void AddDisplayColumns(DataTable table)
    {
        foreach (var field in _section.Fields.Where(f => f.ComboSql is not null && f.ShowInGrid))
        {
            var displayColumn = field.Column + "__Display";
            if (!table.Columns.Contains(displayColumn))
            {
                table.Columns.Add(displayColumn, typeof(string));
            }

            var map = DataService.LoadCombo(field.ComboSql!)
                .GroupBy(i => Convert.ToString(i.Value) ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.First().Display);

            foreach (DataRow row in table.Rows)
            {
                var key = Convert.ToString(row[field.Column]) ?? string.Empty;
                row[displayColumn] = map.TryGetValue(key, out var display) ? display : string.Empty;
            }
        }
    }

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isAuctions)
        {
            CardButton.IsEnabled = CurrentRow is not null;
        }
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!_isAuctions || CurrentRow is null)
        {
            return;
        }

        e.Handled = true;
        OpenCard();
    }

    private void Card_Click(object sender, RoutedEventArgs e) => OpenCard();

    private void OpenCard()
    {
        var row = CurrentRow;
        if (row is null)
        {
            StatusText.Text = "Выберите аукцион для просмотра карточки.";
            return;
        }

        try
        {
            var id = Convert.ToInt32(row.Row[_section.IdColumn]);
            var card = new AuctionCardWindow(id) { Owner = Window.GetWindow(this) };
            card.ShowDialog();
            Load();
        }
        catch (Exception ex)
        {
            ShowError("Не удалось открыть карточку", ex);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditDialog(_section, null, true) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            DataService.Insert(_section, dialog.Values);
            Load();
            StatusText.Text = "Запись добавлена.";
        }
        catch (Exception ex)
        {
            ShowError("Ошибка добавления", ex);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var row = CurrentRow;
        if (row is null)
        {
            StatusText.Text = "Выберите запись для изменения.";
            return;
        }

        var id = row.Row[_section.IdColumn];
        var values = DataService.GetRow(_section, id);
        var dialog = new EditDialog(_section, values, false) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            DataService.Update(_section, id, dialog.Values);
            Load();
            StatusText.Text = "Запись изменена.";
        }
        catch (Exception ex)
        {
            ShowError("Ошибка изменения", ex);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = CurrentRow;
        if (row is null)
        {
            StatusText.Text = "Выберите запись для удаления.";
            return;
        }

        var id = row.Row[_section.IdColumn];
        if (MessageBox.Show($"Удалить выбранную запись (ID = {id})?", "Удаление",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            DataService.Delete(_section, id);
            Load();
            StatusText.Text = "Запись удалена.";
        }
        catch (SqlException)
        {
            ShowError("Ошибка удаления",
                new InvalidOperationException("Невозможно удалить запись: с ней связаны другие данные."));
        }
        catch (Exception ex)
        {
            ShowError("Ошибка удаления", ex);
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => Load();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        Load();
    }

    private void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Load();
        }
    }

    private void ShowError(string title, Exception ex)
    {
        StatusText.Text = title + ": " + ex.Message;
        MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
