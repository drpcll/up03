using System.Data;
using System.Windows;
using System.Windows.Controls;
using AuctionSystem.Data;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem;

public partial class RegWindow : Window
{
    private string connStr = "Server=stud-mssql.sttec.yar.ru,38325; Database=user95_db; User ID = user95_db; Password = user95; ;TrustServerCertificate=True;";

    public RegWindow()
    {
        InitializeComponent();

        TypeEmployee.Checked += Type_Changed;
        TypeSeller.Checked += Type_Changed;
        TypeBuyer.Checked += Type_Changed;
        TypeEmployee.IsChecked = true;

        Loaded += RegWindow_Loaded;
    }

    private void RegWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RoleBox.ItemsSource = DbHelper
                .Query(connStr, "SELECT IDRole, Tip FROM dbo.UP03_Role ORDER BY IDRole")
                .DefaultView;

            if (RoleBox.Items.Count > 0)
            {
                RoleBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageText.Text = $"Не удалось загрузить справочник ролей: {ex.Message}";
        }

        ApplyType();
        FamiliyaBox.Focus();
    }

    private AccountType SelectedType =>
        TypeSeller?.IsChecked == true ? AccountType.Seller
        : TypeBuyer?.IsChecked == true ? AccountType.Buyer
        : AccountType.Employee;

    private void Type_Changed(object sender, RoutedEventArgs e) => ApplyType();

    private void ApplyType()
    {
        var isEmployee = SelectedType == AccountType.Employee;

        AdresLabel.Visibility = isEmployee ? Visibility.Collapsed : Visibility.Visible;
        AdresBox.Visibility = isEmployee ? Visibility.Collapsed : Visibility.Visible;

        RoleLabel.Visibility = isEmployee ? Visibility.Visible : Visibility.Collapsed;
        RoleBox.Visibility = isEmployee ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        MessageText.Text = string.Empty;

        var familiya = FamiliyaBox.Text.Trim();
        var imya = ImyaBox.Text.Trim();
        var otchestvo = NullIfEmpty(OtchestvoBox.Text);
        var telefon = NullIfEmpty(TelefonBox.Text);
        var email = NullIfEmpty(EmailBox.Text);
        var adres = NullIfEmpty(AdresBox.Text);
        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;

        if (familiya.Length == 0 || imya.Length == 0)
        {
            MessageText.Text = "Укажите фамилию и имя.";
            return;
        }

        if (login.Length == 0)
        {
            MessageText.Text = "Укажите логин.";
            return;
        }

        if (password.Length < 4)
        {
            MessageText.Text = "Пароль должен содержать не менее 4 символов.";
            return;
        }

        var type = SelectedType;
        var roleId = 0;

        if (type == AccountType.Employee)
        {
            if (RoleBox.SelectedItem is not DataRowView roleRow)
            {
                MessageText.Text = "Выберите роль сотрудника.";
                return;
            }

            roleId = Convert.ToInt32(roleRow["IDRole"]);
        }

        try
        {
            if (!AuthService.IsLoginFree(connStr, login))
            {
                MessageText.Text = "Такой логин уже занят.";
                return;
            }

            switch (type)
            {
                case AccountType.Employee:
                    AuthService.RegisterEmployee(connStr, familiya, imya, otchestvo,
                        login, password, roleId, telefon, email);
                    break;
                case AccountType.Seller:
                    AuthService.RegisterSeller(connStr, familiya, imya, otchestvo,
                        login, password, telefon, email, adres);
                    break;
                default:
                    AuthService.RegisterBuyer(connStr, familiya, imya, otchestvo,
                        login, password, telefon, email, adres);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageText.Text = $"Ошибка при регистрации: {ex.Message}";
            return;
        }

        MessageBox.Show(
            $"Регистрация выполнена успешно.\nЛогин: {login}",
            "Регистрация",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Close();
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
