using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AuctionSystem.Models;
using AuctionSystem.Services;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Views;

public partial class ReportsView : UserControl
{
    private const string AuctionReport = "Лоты аукциона";
    private const string SellerReport = "Лоты продавца";
    private const string RegistryReport = "Реестр участников";

    private DataTable? _current;

    public ReportsView()
    {
        InitializeComponent();
        ReportBox.Items.Add(AuctionReport);
        ReportBox.Items.Add(SellerReport);
        ReportBox.Items.Add(RegistryReport);

        if (Session.IsSeller)
        {
            ReportBox.SelectedItem = SellerReport;
        }
        else
        {
            ReportBox.SelectedItem = AuctionReport;
        }
    }

    private void Report_Changed(object sender, SelectionChangedEventArgs e)
    {
        var selected = ReportBox.SelectedItem as string;

        if (selected == AuctionReport)
        {
            ParamLabel.Visibility = Visibility.Visible;
            ParamBox.Visibility = Visibility.Visible;
            ParamBox.IsEnabled = !Session.IsSeller;
            var filter = Session.IsAuctionAdministrator
                ? " WHERE IDSotrudnika = " + (Session.CurrentUser?.Id ?? 0)
                : string.Empty;
            ParamBox.ItemsSource = DataService.LoadCombo(
                "SELECT IDAukciona AS Value, Nazvanie AS Display FROM dbo.UP03_Auktsiony" +
                filter + " ORDER BY DataProvedeniya DESC");
        }
        else if (selected == SellerReport)
        {
            ParamLabel.Visibility = Visibility.Visible;
            ParamBox.Visibility = Visibility.Visible;
            ParamBox.IsEnabled = !Session.IsSeller;
            if (Session.IsSeller)
            {
                ParamBox.ItemsSource = DataService.LoadCombo(
                    "SELECT IDProdavca AS Value, Familiya + ' ' + Imya AS Display FROM dbo.UP03_Prodavtsy " +
                    "WHERE IDProdavca = " + (Session.CurrentUser?.Id ?? 0));
            }
            else
            {
                ParamBox.ItemsSource = DataService.LoadCombo(
                    "SELECT IDProdavca AS Value, Familiya + ' ' + Imya AS Display FROM dbo.UP03_Prodavtsy ORDER BY Familiya");
            }
        }
        else
        {
            ParamLabel.Visibility = Visibility.Collapsed;
            ParamBox.Visibility = Visibility.Collapsed;
            ParamBox.ItemsSource = null;
        }

        if (ParamBox.Items.Count > 0)
        {
            ParamBox.SelectedIndex = 0;
        }

        Build();
    }

    private void Build_Click(object sender, RoutedEventArgs e) => Build();

    private void Build()
    {
        var selected = ReportBox.SelectedItem as string;
        try
        {
            _current = selected switch
            {
                AuctionReport => BuildAuctionReport(),
                SellerReport => BuildSellerReport(),
                RegistryReport => BuildRegistry(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            SummaryText.Text = "Ошибка построения отчёта: " + ex.Message;
            return;
        }

        Grid.ItemsSource = _current?.DefaultView;
    }

    private DataTable BuildAuctionReport()
    {
        if (ParamBox.SelectedValue is null)
        {
            SummaryText.Text = "Выберите аукцион.";
            return new DataTable();
        }

        var id = Convert.ToInt32(ParamBox.SelectedValue);
        var table = DataService.Query(
            """
            SELECT l.NomerLota AS [№], l.Naimenovanie AS [Лот],
                   p.Familiya + ' ' + p.Imya AS [Продавец],
                   l.StartovayaCena AS [Старт. цена],
                   pok.Familiya + ' ' + pok.Imya AS [Покупатель],
                   r.FinalnayaCena AS [Фин. цена],
                   l.Status AS [Статус]
            FROM dbo.UP03_Loty l
            JOIN dbo.UP03_Prodavtsy p ON p.IDProdavca = l.IDProdavca
            LEFT JOIN dbo.UP03_RezultatyTorgov r ON r.IDLota = l.IDLota
            LEFT JOIN dbo.UP03_Pokupateli pok ON pok.IDPokupatelya = r.IDPokupatelya
            WHERE l.IDAukciona = @id
            ORDER BY l.NomerLota
            """,
            new SqlParameter("@id", id));

        var total = DataService.Scalar(
            "SELECT ISNULL(SUM(FinalnayaCena), 0) FROM dbo.UP03_RezultatyTorgov r " +
            "JOIN dbo.UP03_Loty l ON l.IDLota = r.IDLota " +
            "WHERE l.IDAukciona = @id AND r.PriznakProdazhi = 1",
            new SqlParameter("@id", id));

        var avg = table.Rows.Count > 0 ? "  |  Лотов: " + table.Rows.Count : string.Empty;
        SummaryText.Text = $"Итоговая сумма продаж: {Convert.ToDecimal(total):N2} руб.{avg}";
        return table;
    }

    private DataTable BuildSellerReport()
    {
        if (ParamBox.SelectedValue is null)
        {
            SummaryText.Text = "Выберите продавца.";
            return new DataTable();
        }

        var id = Convert.ToInt32(ParamBox.SelectedValue);
        var table = DataService.Query(
            """
            SELECT l.NomerLota AS [№], l.Naimenovanie AS [Лот],
                   a.Nazvanie AS [Аукцион], l.StartovayaCena AS [Старт. цена],
                   r.FinalnayaCena AS [Фин. цена], l.Status AS [Статус]
            FROM dbo.UP03_Loty l
            JOIN dbo.UP03_Auktsiony a ON a.IDAukciona = l.IDAukciona
            LEFT JOIN dbo.UP03_RezultatyTorgov r ON r.IDLota = l.IDLota
            WHERE l.IDProdavca = @id
            ORDER BY l.IDAukciona, l.NomerLota
            """,
            new SqlParameter("@id", id));

        var total = DataService.Scalar(
            "SELECT ISNULL(SUM(r.FinalnayaCena), 0) FROM dbo.UP03_RezultatyTorgov r " +
            "JOIN dbo.UP03_Loty l ON l.IDLota = r.IDLota " +
            "WHERE l.IDProdavca = @id AND r.PriznakProdazhi = 1",
            new SqlParameter("@id", id));

        SummaryText.Text = $"Лотов у продавца: {table.Rows.Count}  |  Сумма продаж: {Convert.ToDecimal(total):N2} руб.";
        return table;
    }

    private DataTable BuildRegistry()
    {
        var table = DataService.Query(
            """
            SELECT Familiya + ' ' + Imya + ISNULL(' ' + Otchestvo, '') AS [ФИО],
                   Telefon AS [Телефон], Email AS [Email], Adres AS [Адрес],
                   N'Продавец' AS [Тип участника]
            FROM dbo.UP03_Prodavtsy
            UNION ALL
            SELECT Familiya + ' ' + Imya + ISNULL(' ' + Otchestvo, ''),
                   Telefon, Email, Adres, N'Покупатель'
            FROM dbo.UP03_Pokupateli
            ORDER BY [Тип участника], [ФИО]
            """);

        SummaryText.Text = "Всего участников: " + table.Rows.Count;
        return table;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || _current.Rows.Count == 0)
        {
            SummaryText.Text = "Нет данных для печати.";
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildDocument(_current, SummaryText.Text);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.ColumnWidth = dialog.PrintableAreaWidth;
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Отчёт");
    }

    private static FlowDocument BuildDocument(DataTable table, string summary)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            PagePadding = new Thickness(40)
        };

        document.Blocks.Add(new Paragraph(new Run("Аукционная фирма — отчёт"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });

        var flowTable = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        foreach (DataColumn column in table.Columns)
        {
            flowTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        }

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = Brushes.LightGray };
        foreach (DataColumn column in table.Columns)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(column.ColumnName)))
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(4),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            });
        }

        headerGroup.Rows.Add(headerRow);
        flowTable.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        foreach (DataRow row in table.Rows)
        {
            var tableRow = new TableRow();
            foreach (DataColumn column in table.Columns)
            {
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(row[column]?.ToString() ?? string.Empty)))
                {
                    Padding = new Thickness(4)
                });
            }

            bodyGroup.Rows.Add(tableRow);
        }

        flowTable.RowGroups.Add(bodyGroup);
        document.Blocks.Add(flowTable);
        document.Blocks.Add(new Paragraph(new Run(summary))
        {
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 12, 0, 0)
        });

        return document;
    }
}
