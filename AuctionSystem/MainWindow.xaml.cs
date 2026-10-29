using System.Windows;
using System.Windows.Controls;
using AuctionSystem.Data;
using AuctionSystem.Models;
using AuctionSystem.Services;
using AuctionSystem.Views;

namespace AuctionSystem;

public partial class MainWindow : Window
{
    private const string ConnStr =
        "Server=stud-mssql.sttec.yar.ru,38325;Database=user95_db;User ID=user95_db;Password=user95;TrustServerCertificate=True;";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    public string ConnectionString => ConnStr;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!DbHelper.TestConnection(ConnStr, out var error))
        {
            StatusText.Text = "Нет соединения с базой данных";
            MessageBox.Show(
                "Не удалось подключиться к базе данных.\n\n" + error,
                "Ошибка подключения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        StatusText.Text = "Соединение с базой данных установлено";
        RefreshUser();
        FillMenu();
    }

    private void FillMenu()
    {
        MenuList.Items.Clear();
        foreach (var title in Session.AvailableSections())
        {
            MenuList.Items.Add(title);
        }

        if (MenuList.Items.Count > 0)
        {
            MenuList.SelectedIndex = 0;
        }
        else
        {
            SectionTitle.Text = "Обзор";
            SectionHost.Content = new TextBlock
            {
                Text = "Нет доступных разделов.",
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private void RefreshUser()
    {
        UserText.Text = Session.CurrentUser?.ShortName ?? "Гость";
        SignInButton.Visibility = Session.IsSignedIn ? Visibility.Collapsed : Visibility.Visible;
        RegButton.Visibility = Session.IsSignedIn ? Visibility.Collapsed : Visibility.Visible;
        SignOutButton.Visibility = Session.IsSignedIn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        var auth = new AuthWindow { Owner = this };
        auth.Show();
        auth.Closed += (_, _) =>
        {
            RefreshUser();
            FillMenu();
            StatusText.Text = Session.IsSignedIn
                ? "Вход выполнен: " + Session.CurrentUser!.ShortName
                : "Пользователь не авторизован";
        };
    }

    private void RegButton_Click(object sender, RoutedEventArgs e)
    {
        var reg = new RegWindow { Owner = this };
        reg.Show();
        reg.Closed += (_, _) => StatusText.Text = "Регистрация завершена";
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Session.IsSignedIn)
        {
            StatusText.Text = "Пользователь не входит в систему";
            return;
        }

        Session.SignOut();
        RefreshUser();
        FillMenu();
        StatusText.Text = "Выполнен выход из системы";
    }

    private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MenuList.SelectedItem is not string section)
        {
            return;
        }

        SectionTitle.Text = section;

        if (!Session.IsSignedIn)
        {
            SectionHost.Content = new TextBlock
            {
                Text = "Выполните вход, чтобы работать с данными.",
                TextWrapping = TextWrapping.Wrap
            };
            return;
        }

        try
        {
            SectionHost.Content = CreateView(section);
        }
        catch (System.Exception ex)
        {
            SectionHost.Content = new TextBlock
            {
                Text = "Не удалось открыть раздел: " + ex.Message,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private object CreateView(string section) => section switch
    {
        "Аукционы" => new SectionView(SectionRegistry.Auctions()),
        "Лоты" => new SectionView(SectionRegistry.Lots()),
        "Участники" => new ParticipantsView(),
        "Результаты торгов" => new SectionView(SectionRegistry.Results()),
        "Отчёты" => new ReportsView(),
        "Справочники" => new SectionView(SectionRegistry.Directories()),
        "Пользователи" => new SectionView(SectionRegistry.Employees()),
        _ => new TextBlock { Text = "Раздел не найден." }
    };
}
