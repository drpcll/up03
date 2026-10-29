using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AuctionSystem.Models;
using AuctionSystem.Services;
using Microsoft.Data.SqlClient;

namespace AuctionSystem.Views;

public partial class AuctionCardWindow : Window
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly SolidColorBrush Green = MakeBrush(0x2E, 0x7D, 0x32);
    private static readonly SolidColorBrush GreenDark = MakeBrush(0x1B, 0x5E, 0x20);
    private static readonly SolidColorBrush GreenLight = MakeBrush(0xE8, 0xF5, 0xE9);
    private static readonly SolidColorBrush Amber = MakeBrush(0xEF, 0x8A, 0x00);
    private static readonly SolidColorBrush AmberDark = MakeBrush(0x8A, 0x4B, 0x00);
    private static readonly SolidColorBrush AmberLight = MakeBrush(0xFF, 0xF3, 0xE0);
    private static readonly SolidColorBrush Blue = MakeBrush(0x1F, 0x4E, 0x79);
    private static readonly SolidColorBrush BlueDark = MakeBrush(0x0D, 0x47, 0xA1);
    private static readonly SolidColorBrush BlueLight = MakeBrush(0xE3, 0xF2, 0xFD);
    private static readonly SolidColorBrush Gray = MakeBrush(0x6B, 0x7C, 0x93);
    private static readonly SolidColorBrush GrayLight = MakeBrush(0xEE, 0xF2, 0xF7);

    private readonly int _auctionId;
    private DataRow? _auction;

    public AuctionCardWindow(int auctionId)
    {
        InitializeComponent();
        _auctionId = auctionId;
        Loaded += AuctionCardWindow_Loaded;
    }

    private void AuctionCardWindow_Loaded(object sender, RoutedEventArgs e) => LoadData();

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

    private void LoadData()
    {
        try
        {
            var table = DataService.Query(
                """
                SELECT a.*,
                       s.Familiya + ' ' + s.Imya + ISNULL(' ' + s.Otchestvo, '') AS OrganizerName
                FROM dbo.UP03_Auktsiony a
                LEFT JOIN dbo.UP03_Sotrudniki s ON s.IDSotrudnika = a.IDSotrudnika
                WHERE a.IDAukciona = @id
                """,
                new SqlParameter("@id", _auctionId));

            if (table.Rows.Count == 0)
            {
                MessageBox.Show("Аукцион не найден.", "Карточка аукциона",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            _auction = table.Rows[0];
            if (!CanView(_auction))
            {
                MessageBox.Show("У вас нет доступа к этому аукциону.", "Карточка аукциона",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            var lots = DataService.Query(
                """
                SELECT l.IDLota, l.NomerLota, l.Naimenovanie, l.Opisanie,
                       l.StartovayaCena, l.Sostoyanie, l.Status, l.Foto,
                       l.IDAukciona, l.IDProdavca,
                       p.Familiya + ' ' + p.Imya + ISNULL(' ' + p.Otchestvo, '') AS SellerName,
                       r.PriznakProdazhi, r.FinalnayaCena, r.SummaKommissii,
                       r.IDPokupatelya,
                       b.Familiya + ' ' + b.Imya + ISNULL(' ' + b.Otchestvo, '') AS BuyerName
                FROM dbo.UP03_Loty l
                JOIN dbo.UP03_Prodavtsy p ON p.IDProdavca = l.IDProdavca
                LEFT JOIN dbo.UP03_RezultatyTorgov r ON r.IDLota = l.IDLota
                LEFT JOIN dbo.UP03_Pokupateli b ON b.IDPokupatelya = r.IDPokupatelya
                WHERE l.IDAukciona = @id
                ORDER BY l.NomerLota
                """,
                new SqlParameter("@id", _auctionId));

            FillHeader(_auction);
            FillLots(lots);
            FillSummary(lots);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось открыть карточку аукциона.\n\n" + ex.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanView(DataRow auction)
    {
        if (Session.CurrentUser is null)
        {
            return false;
        }

        if (Session.IsSeller)
        {
            var ownLots = DataService.Scalar(
                "SELECT COUNT(*) FROM dbo.UP03_Loty WHERE IDAukciona = @id AND IDProdavca = @uid",
                new SqlParameter("@id", _auctionId),
                new SqlParameter("@uid", Session.CurrentUser.Id));
            return Convert.ToInt32(ownLots) > 0;
        }

        if (Session.IsAuctionAdministrator)
        {
            return Int(auction, "IDSotrudnika") == Session.CurrentUser.Id;
        }

        return true;
    }

    private void FillHeader(DataRow auction)
    {
        TitleText.Text = Value(auction, "Nazvanie", "Аукцион");
        DateText.Text = FormatDate(auction);
        DateTimeText.Text = DateText.Text;
        PlaceText.Text = Value(auction, "MestoProvedeniya", "Место не указано");
        PlaceInfoText.Text = PlaceText.Text;
        SpecifikaText.Text = Value(auction, "Specifika", "—");
        OrganizerInfoText.Text = Value(auction, "OrganizerName", "Не указан");
        IdInfoText.Text = "№ " + _auctionId;
        AuctionIdText.Text = "ID " + _auctionId;

        var organizer = Value(auction, "OrganizerName", "не указан");
        OrganizerText.Text = "Организатор: " + organizer;

        CoverImage.Source = PhotoService.LoadAuction(Text(auction, "Foto"));

        var status = Text(auction, "Status");
        StatusText.Text = string.IsNullOrWhiteSpace(status) ? "Статус не указан" : status;
        switch (status)
        {
            case "Завершён":
                StatusBadge.Background = GreenLight;
                StatusText.Foreground = GreenDark;
                StatusDot.Fill = Green;
                break;
            case "Идёт":
                StatusBadge.Background = AmberLight;
                StatusText.Foreground = AmberDark;
                StatusDot.Fill = Amber;
                break;
            case "Запланирован":
                StatusBadge.Background = BlueLight;
                StatusText.Foreground = BlueDark;
                StatusDot.Fill = Blue;
                break;
            default:
                StatusBadge.Background = GrayLight;
                StatusText.Foreground = Gray;
                StatusDot.Fill = Gray;
                break;
        }

        RoleNoteText.Text = Session.IsBuyer
            ? "Вы вошли как покупатель. В карточке отмечены лоты, которые вы приобрели."
            : Session.IsSeller
                ? "Вы вошли как продавец. В карточке отмечены ваши лоты."
                : Session.IsAuctionAdministrator
                    ? "Вы вошли как администратор аукциона. Карточка доступна только для просмотра."
                    : "Вы вошли как администратор. Карточка доступна только для просмотра; данные изменяются в разделе «Аукционы».";

        FooterText.Text = "Данные: SQL Server 2022  ·  фотографии: локальная папка Images  ·  карточка только для просмотра";
    }

    private void FillLots(DataTable lots)
    {
        var items = new List<LotCardItem>();
        foreach (DataRow row in lots.Rows)
        {
            var status = Text(row, "Status");
            var sold = Bool(row, "PriznakProdazhi");
            var finalPrice = Decimal(row, "FinalnayaCena");
            var startPrice = Decimal(row, "StartovayaCena");
            var seller = Text(row, "SellerName");
            var buyer = Text(row, "BuyerName");
            var number = Int(row, "NomerLota") ?? 0;

            var isBuyerLot = Session.IsBuyer &&
                             Int(row, "IDPokupatelya") == Session.CurrentUser?.Id;
            var isSellerLot = Session.IsSeller &&
                              Int(row, "IDProdavca") == Session.CurrentUser?.Id;

            var badge = isBuyerLot ? "Ваша покупка" : isSellerLot ? "Ваш лот" : string.Empty;
            var badgeBrush = isBuyerLot ? Green : Blue;

            var details = new List<string>
            {
                $"Лот № {number}: {Value(row, "Naimenovanie", "без названия")}",
                $"Стартовая цена: {Money(startPrice ?? 0)}",
                sold && finalPrice.HasValue ? $"Продано за: {Money(finalPrice.Value)}" : "Лот не продан",
                $"Продавец: {Value(row, "SellerName", "не указан")}"
            };
            if (sold && !string.IsNullOrWhiteSpace(buyer))
            {
                details.Add("Покупатель: " + buyer);
            }
            var condition = Text(row, "Sostoyanie");
            if (!string.IsNullOrWhiteSpace(condition))
            {
                details.Add("Состояние: " + condition);
            }
            var description = Text(row, "Opisanie");
            if (!string.IsNullOrWhiteSpace(description))
            {
                details.Add(string.Empty);
                details.Add(description);
            }

            items.Add(new LotCardItem
            {
                Photo = PhotoService.LoadLot(Text(row, "Foto")),
                NumberText = "ЛОТ № " + number,
                Title = Value(row, "Naimenovanie", "Без названия"),
                PriceText = sold && finalPrice.HasValue
                    ? "Продано за " + Money(finalPrice.Value)
                    : "Старт: " + Money(startPrice ?? 0),
                SellerText = "Продавец: " + (string.IsNullOrWhiteSpace(seller) ? "не указан" : seller),
                StatusText = string.IsNullOrWhiteSpace(status) ? "—" : status,
                StatusBrush = StatusBrush(status),
                Badge = badge,
                BadgeBrush = badgeBrush,
                BadgeVisibility = string.IsNullOrWhiteSpace(badge)
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                Details = string.Join(Environment.NewLine, details)
            });
        }

        LotsList.ItemsSource = items;
        LotsList.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        EmptyLotsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LotsHeaderText.Text = items.Count == 0
            ? "нет лотов"
            : $"{items.Count} {Plural(items.Count, "лот", "лота", "лотов")}  ·  " +
              $"{items.Count(i => i.StatusText == "Продан")} продано";
    }

    private void FillSummary(DataTable lots)
    {
        var soldRows = lots.Rows.Cast<DataRow>().Where(r => Bool(r, "PriznakProdazhi")).ToList();
        var total = soldRows.Sum(r => Decimal(r, "FinalnayaCena") ?? 0);
        var sellers = lots.Rows.Cast<DataRow>()
            .Select(r => Int(r, "IDProdavca"))
            .Where(id => id.HasValue)
            .Distinct()
            .Count();
        var buyers = lots.Rows.Cast<DataRow>()
            .Select(r => Int(r, "IDPokupatelya"))
            .Where(id => id.HasValue)
            .Distinct()
            .Count();

        LotsCountText.Text = lots.Rows.Count.ToString(Ru);
        SoldCountText.Text = soldRows.Count.ToString(Ru);
        TotalText.Text = Money(total);
        CompositionText.Text = $"Продавцов: {sellers}   ·   Покупателей: {buyers}";
    }

    private static Brush StatusBrush(string status) => status switch
    {
        "Продан" => Green,
        "Не продан" => Gray,
        "Выставлен" => Blue,
        "Запланирован" => BlueDark,
        _ => Gray
    };

    private static string FormatDate(DataRow row)
    {
        var date = Date(row, "DataProvedeniya");
        if (date is null)
        {
            return "Дата не указана";
        }

        var time = Time(row, "VremyaProvedeniya");
        return time is null
            ? date.Value.ToString("dd.MM.yyyy", Ru)
            : date.Value.ToString("dd.MM.yyyy", Ru) + " в " + time.Value.ToString(@"hh\:mm", Ru);
    }

    private static string Money(decimal value) => value.ToString("N0", Ru) + " ₽";

    private static string Plural(int count, string one, string few, string many)
    {
        var mod100 = count % 100;
        var mod10 = count % 10;
        if (mod100 is >= 11 and <= 14)
        {
            return many;
        }

        return mod10 switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many
        };
    }

    private static string Text(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return string.Empty;
        }

        var value = row[column];
        return value is null || value == DBNull.Value
            ? string.Empty
            : Convert.ToString(value, Ru)?.Trim() ?? string.Empty;
    }

    private static string Value(DataRow row, string column, string fallback)
    {
        var text = Text(row, column);
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static int? Int(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        var value = row[column];
        return value is null || value == DBNull.Value
            ? null
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static decimal? Decimal(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        var value = row[column];
        return value is null || value == DBNull.Value
            ? null
            : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static bool Bool(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return false;
        }

        var value = row[column];
        return value is not null && value != DBNull.Value && Convert.ToBoolean(value);
    }

    private static DateTime? Date(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        var value = row[column];
        return value is DateTime date ? date : null;
    }

    private static TimeSpan? Time(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        return row[column] switch
        {
            TimeSpan time => time,
            DateTime date => date.TimeOfDay,
            _ => null
        };
    }

    private static SolidColorBrush MakeBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public sealed class LotCardItem
    {
        public BitmapImage? Photo { get; init; }
        public string NumberText { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string PriceText { get; init; } = string.Empty;
        public string SellerText { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public Brush StatusBrush { get; init; } = Brushes.Gray;
        public string Badge { get; init; } = string.Empty;
        public Brush BadgeBrush { get; init; } = Brushes.Transparent;
        public Visibility BadgeVisibility { get; init; } = Visibility.Collapsed;
        public string Details { get; init; } = string.Empty;
    }
}
