using System.IO;
using System.Windows;
using System.Windows.Input;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem;

public partial class AuthWindow : Window
{
    private string connStr = "Server=stud-mssql.sttec.yar.ru,38325; Database=user95_db; User ID = user95_db; Password = user95; ;TrustServerCertificate=True;";

    private static string RememberedLoginFile =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "AuctionSystem", "remembered-login.txt");

    public AuthWindow()
    {
        InitializeComponent();
        Loaded += AuthWindow_Loaded;
    }

    private void AuthWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (TryReadRememberedLogin(out var login))
        {
            LoginBox.Text = login;
            PasswordBox.Focus();
        }
        else
        {
            LoginBox.Focus();
        }
    }

    private void Field_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AttemptLogin();
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => AttemptLogin();

    private void AttemptLogin()
    {
        MessageText.Text = string.Empty;

        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;

        if (!AuthService.TryLogin(connStr, login, password, out var user, out var error))
        {
            MessageText.Text = error;
            PasswordBox.Clear();
            PasswordBox.Focus();
            return;
        }

        Session.SignIn(user!);

        SaveRememberedLogin(RememberBox.IsChecked == true ? login : null);

        Close();
    }

    private static void SaveRememberedLogin(string? login)
    {
        try
        {
            if (login is null)
            {
                if (File.Exists(RememberedLoginFile))
                {
                    File.Delete(RememberedLoginFile);
                }

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RememberedLoginFile)!);
            File.WriteAllText(RememberedLoginFile, login);
        }
        catch (IOException)
        {

        }
    }

    private static bool TryReadRememberedLogin(out string login)
    {
        login = string.Empty;

        try
        {
            if (!File.Exists(RememberedLoginFile))
            {
                return false;
            }

            login = File.ReadAllText(RememberedLoginFile).Trim();
            return login.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
