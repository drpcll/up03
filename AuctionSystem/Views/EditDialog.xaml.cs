using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem.Views;

public partial class EditDialog : Window
{
    private readonly SectionDescriptor _section;
    private readonly bool _isNew;
    private readonly Dictionary<string, Func<object?>> _getters = new();

    public Dictionary<string, object?> Values { get; } = new();

    public EditDialog(SectionDescriptor section, Dictionary<string, object?>? row, bool isNew)
    {
        InitializeComponent();
        _section = section;
        _isNew = isNew;
        Title = isNew ? $"Добавление — {section.Title}" : $"Изменение — {section.Title}";
        Build(row, isNew);
    }

    private void Build(Dictionary<string, object?>? row, bool isNew)
    {
        foreach (var field in _section.Fields)
        {
            if (field.Identity || field.Auto || !field.Editable)
            {
                continue;
            }

            var label = new TextBlock
            {
                Text = field.Header + (field.Required ? " *" : string.Empty),
                Margin = new Thickness(0, 8, 0, 2)
            };
            FieldsPanel.Children.Add(label);

            var value = row is not null && row.TryGetValue(field.Column, out var v) ? v : null;
            FieldsPanel.Children.Add(BuildEditor(field, value, isNew));
        }
    }

    private FrameworkElement BuildEditor(FieldDescriptor field, object? value, bool isNew)
    {
        if (field.ComboSql is not null)
        {
            var combo = new ComboBox { DisplayMemberPath = "Display", SelectedValuePath = "Value" };
            combo.ItemsSource = DataService.LoadCombo(field.ComboSql);

            var selected = value ?? ResolveDefault(field, isNew);
            if (selected is not null)
            {
                combo.SelectedValue = selected;
            }

            _getters[field.Column] = () => combo.SelectedValue;
            return combo;
        }

        switch (field.Kind)
        {
            case FieldKind.Bool:
            {
                var check = new CheckBox { IsChecked = value is bool b && b };
                _getters[field.Column] = () => check.IsChecked == true;
                return check;
            }
            case FieldKind.Date:
            {
                var picker = new DatePicker();
                if (value is DateTime date)
                {
                    picker.SelectedDate = date;
                }
                else if (isNew && field.DefaultValue == "@today")
                {
                    picker.SelectedDate = DateTime.Today;
                }

                _getters[field.Column] = () => picker.SelectedDate;
                return picker;
            }
            case FieldKind.Time:
            {
                var text = new TextBox();
                if (value is TimeSpan time)
                {
                    text.Text = time.ToString(@"hh\:mm");
                }

                _getters[field.Column] = () => ParseTime(text.Text);
                return text;
            }
            case FieldKind.Multiline:
            {
                var text = new TextBox
                {
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 70,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Text = value?.ToString() ?? string.Empty
                };
                _getters[field.Column] = () => string.IsNullOrWhiteSpace(text.Text) ? null : text.Text.Trim();
                return text;
            }
            case FieldKind.Int:
            case FieldKind.Decimal:
            {
                var text = new TextBox { Text = FormatNumber(value) };
                _getters[field.Column] = () => ParseNumber(field, text.Text);
                return text;
            }
            case FieldKind.Text when field.IsPassword:
            {
                var box = new PasswordBox();
                _getters[field.Column] = () => string.IsNullOrEmpty(box.Password) ? null : box.Password;
                return box;
            }
            default:
            {
                var text = new TextBox { Text = value?.ToString() ?? string.Empty };
                if (!string.IsNullOrEmpty(field.DefaultValue) && value is null)
                {
                    text.Text = field.DefaultValue;
                }

                _getters[field.Column] = () => string.IsNullOrWhiteSpace(text.Text) ? null : text.Text.Trim();
                return text;
            }
        }
    }

    private object? ResolveDefault(FieldDescriptor field, bool isNew)
    {
        if (!isNew || string.IsNullOrEmpty(field.DefaultValue))
        {
            return null;
        }

        if (field.DefaultValue == "@current")
        {
            return Session.CurrentUser?.Id;
        }

        if (field.Kind is FieldKind.Int or FieldKind.Combo)
        {
            return int.TryParse(field.DefaultValue, out var number) ? number : null;
        }

        return field.DefaultValue;
    }

    private static string FormatNumber(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value is IFormattable formattable
            ? formattable.ToString("0.##", CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }

    private static object? ParseNumber(FieldDescriptor field, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim().Replace(',', '.');
        if (field.Kind == FieldKind.Int)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
        }

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static object? ParseTime(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var t) ? t : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        Values.Clear();

        foreach (var field in _section.Fields)
        {
            if (!_getters.TryGetValue(field.Column, out var getter))
            {
                continue;
            }

            var value = getter();

            if (field.Required && (value is null || (value is string s && s.Length == 0)))
            {
                ErrorText.Text = $"Поле «{field.Header}» обязательно для заполнения.";
                return;
            }

            if (field.IsPassword && field.Required && string.IsNullOrEmpty(value as string))
            {
                ErrorText.Text = "Укажите пароль.";
                return;
            }

            Values[field.Column] = value;
        }

        var error = RowValidator.Validate(_section, Values, _isNew);
        if (error is not null)
        {
            ErrorText.Text = error;
            return;
        }

        DialogResult = true;
    }
}
