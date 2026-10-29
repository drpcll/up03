using AuctionSystem.Models;

namespace AuctionSystem.Services;

public static class SectionRegistry
{
    private const string PersonName = "Familiya + ' ' + Imya + ISNULL(' ' + Otchestvo, '')";

    private const string EmployeeCombo =
        "SELECT IDSotrudnika AS Value, " + PersonName + " AS Display FROM dbo.UP03_Sotrudniki ORDER BY Familiya, Imya";

    private const string AuctionCombo =
        "SELECT IDAukciona AS Value, Nazvanie AS Display FROM dbo.UP03_Auktsiony ORDER BY DataProvedeniya DESC, IDAukciona DESC";

    private const string SellerCombo =
        "SELECT IDProdavca AS Value, " + PersonName + " AS Display FROM dbo.UP03_Prodavtsy ORDER BY Familiya, Imya";

    private const string BuyerCombo =
        "SELECT IDPokupatelya AS Value, " + PersonName + " AS Display FROM dbo.UP03_Pokupateli ORDER BY Familiya, Imya";

    private const string RoleCombo =
        "SELECT IDRole AS Value, Tip AS Display FROM dbo.UP03_Role ORDER BY IDRole";

    private const string AuctionStatusCombo =
        "SELECT N'Запланирован' AS Value, N'Запланирован' AS Display " +
        "UNION ALL SELECT N'Идёт', N'Идёт' UNION ALL SELECT N'Завершён', N'Завершён'";

    private const string LotStatusCombo =
        "SELECT N'Запланирован' AS Value, N'Запланирован' AS Display " +
        "UNION ALL SELECT N'Выставлен', N'Выставлен' " +
        "UNION ALL SELECT N'Продан', N'Продан' " +
        "UNION ALL SELECT N'Не продан', N'Не продан'";

    public static SectionDescriptor Auctions()
    {
        var admin = Session.IsAdministrator;
        var auctionAdmin = Session.IsAuctionAdministrator;
        var seller = Session.IsSeller;

        string? filter = null;
        if (auctionAdmin)
        {
            filter = "t.IDSotrudnika = @uid";
        }
        else if (seller)
        {
            filter = "t.IDAukciona IN (SELECT IDAukciona FROM dbo.UP03_Loty WHERE IDProdavca = @uid)";
        }

        return new SectionDescriptor
        {
            Title = "Аукционы",
            Table = "dbo.UP03_Auktsiony",
            IdColumn = "IDAukciona",
            OrderBy = "t.DataProvedeniya DESC, t.IDAukciona DESC",
            RowFilter = filter,
            CanAdd = admin || auctionAdmin,
            CanEdit = admin || auctionAdmin,
            CanDelete = admin || auctionAdmin,
            SearchColumns = new[] { "Nazvanie", "MestoProvedeniya", "Specifika" },
            Fields = new[]
            {
                new FieldDescriptor { Column = "IDAukciona", Header = "ID", Identity = true, Editable = false, GridWidth = 45 },
                new FieldDescriptor { Column = "Nazvanie", Header = "Название", Required = true, GridWidth = 220 },
                new FieldDescriptor { Column = "DataProvedeniya", Header = "Дата", Kind = FieldKind.Date, Required = true, GridWidth = 100 },
                new FieldDescriptor { Column = "VremyaProvedeniya", Header = "Время", Kind = FieldKind.Time, Required = true, GridWidth = 70 },
                new FieldDescriptor { Column = "MestoProvedeniya", Header = "Место", GridWidth = 170 },
                new FieldDescriptor { Column = "Specifika", Header = "Специфика", GridWidth = 170 },
                new FieldDescriptor { Column = "Status", Header = "Статус", Required = true, DefaultValue = "Запланирован", ComboSql = AuctionStatusCombo, GridWidth = 110 },
                new FieldDescriptor { Column = "IDSotrudnika", Header = "Организатор", Kind = FieldKind.Combo, Required = true, ComboSql = EmployeeCombo, GridWidth = 160, DefaultValue = "@current" },
                new FieldDescriptor { Column = "Foto", Header = "Фото", ShowInGrid = false, Editable = false },
            }
        };
    }

    public static SectionDescriptor Lots()
    {
        var admin = Session.IsAdministrator;
        var auctionAdmin = Session.IsAuctionAdministrator;
        var seller = Session.IsSeller;
        var buyer = Session.IsBuyer;

        string? filter = null;
        if (auctionAdmin)
        {
            filter = "t.IDAukciona IN (SELECT IDAukciona FROM dbo.UP03_Auktsiony WHERE IDSotrudnika = @uid)";
        }
        else if (seller)
        {
            filter = "t.IDProdavca = @uid";
        }

        return new SectionDescriptor
        {
            Title = "Лоты",
            Table = "dbo.UP03_Loty",
            IdColumn = "IDLota",
            OrderBy = "t.IDAukciona, t.NomerLota",
            RowFilter = filter,
            CanAdd = admin || auctionAdmin,
            CanEdit = admin || auctionAdmin,
            CanDelete = admin || auctionAdmin,
            SearchColumns = new[] { "Naimenovanie", "Opisanie", "Sostoyanie" },
            Fields = new[]
            {
                new FieldDescriptor { Column = "IDLota", Header = "ID", Identity = true, Editable = false, GridWidth = 45 },
                new FieldDescriptor { Column = "NomerLota", Header = "Номер", Kind = FieldKind.Int, Required = true, GridWidth = 60 },
                new FieldDescriptor { Column = "Naimenovanie", Header = "Наименование", Required = true, GridWidth = 230 },
                new FieldDescriptor { Column = "Opisanie", Header = "Описание", Kind = FieldKind.Multiline, ShowInGrid = false },
                new FieldDescriptor { Column = "StartovayaCena", Header = "Старт. цена", Kind = FieldKind.Decimal, Required = true, GridWidth = 100 },
                new FieldDescriptor { Column = "Sostoyanie", Header = "Состояние", GridWidth = 120 },
                new FieldDescriptor { Column = "Status", Header = "Статус", Required = true, DefaultValue = "Запланирован", ComboSql = LotStatusCombo, GridWidth = 100 },
                new FieldDescriptor { Column = "IDAukciona", Header = "Аукцион", Kind = FieldKind.Combo, Required = true, ComboSql = AuctionCombo, GridWidth = 170 },
                new FieldDescriptor { Column = "IDProdavca", Header = "Продавец", Kind = FieldKind.Combo, Required = true, ComboSql = SellerCombo, GridWidth = 160 },
                new FieldDescriptor { Column = "Foto", Header = "Фото", ShowInGrid = false, Editable = false },
            }
        };
    }

    public static SectionDescriptor Results()
    {
        var auctionAdmin = Session.IsAuctionAdministrator;
        var buyer = Session.IsBuyer;

        string? filter = null;
        if (auctionAdmin)
        {
            filter = "t.IDLota IN (SELECT l.IDLota FROM dbo.UP03_Loty l " +
                     "JOIN dbo.UP03_Auktsiony a ON a.IDAukciona = l.IDAukciona WHERE a.IDSotrudnika = @uid)";
        }
        else if (buyer)
        {
            filter = "t.IDPokupatelya = @uid";
        }

        return new SectionDescriptor
        {
            Title = "Результаты торгов",
            Table = "dbo.UP03_RezultatyTorgov",
            IdColumn = "IDRezultata",
            OrderBy = "t.IDLota",
            RowFilter = filter,
            CanAdd = !buyer,
            CanEdit = !buyer,
            CanDelete = !buyer,
            SearchColumns = System.Array.Empty<string>(),
            Fields = new[]
            {
                new FieldDescriptor { Column = "IDRezultata", Header = "ID", Identity = true, Editable = false, GridWidth = 45 },
                new FieldDescriptor { Column = "IDLota", Header = "Лот", Kind = FieldKind.Combo, Required = true, ComboSql = LotCombo(), GridWidth = 220 },
                new FieldDescriptor { Column = "PriznakProdazhi", Header = "Продан", Kind = FieldKind.Bool, Required = true, GridWidth = 70 },
                new FieldDescriptor { Column = "FinalnayaCena", Header = "Фин. цена", Kind = FieldKind.Decimal, GridWidth = 100 },
                new FieldDescriptor { Column = "SummaKommissii", Header = "Комиссия", Kind = FieldKind.Decimal, GridWidth = 100 },
                new FieldDescriptor { Column = "IDPokupatelya", Header = "Покупатель", Kind = FieldKind.Combo, ComboSql = BuyerCombo, GridWidth = 160 },
                new FieldDescriptor { Column = "DataProdazhi", Header = "Дата продажи", Kind = FieldKind.Date, GridWidth = 100 },
            }
        };
    }

    private static string LotCombo() =>
        "SELECT l.IDLota AS Value, CAST(l.NomerLota AS varchar(20)) + ' - ' + l.Naimenovanie AS Display " +
        "FROM dbo.UP03_Loty l ORDER BY l.IDAukciona, l.NomerLota";

    public static SectionDescriptor Sellers()
    {
        var admin = Session.IsAdministrator || Session.IsAuctionAdministrator;
        var seller = Session.IsSeller;
        return new SectionDescriptor
        {
            Title = "Продавцы",
            Table = "dbo.UP03_Prodavtsy",
            IdColumn = "IDProdavca",
            OrderBy = "t.Familiya, t.Imya",
            RowFilter = seller ? "t.IDProdavca = @uid" : null,
            CanAdd = admin,
            CanEdit = admin || seller,
            CanDelete = admin,
            SearchColumns = new[] { "Familiya", "Imya", "Otchestvo", "Telefon", "Email" },
            Fields = PersonFields("IDProdavca") 
        };
    }

    public static SectionDescriptor Buyers()
    {
        var admin = Session.IsAdministrator || Session.IsAuctionAdministrator;
        var buyer = Session.IsBuyer;
        return new SectionDescriptor
        {
            Title = "Покупатели",
            Table = "dbo.UP03_Pokupateli",
            IdColumn = "IDPokupatelya",
            OrderBy = "t.Familiya, t.Imya",
            RowFilter = buyer ? "t.IDPokupatelya = @uid" : null,
            CanAdd = admin,
            CanEdit = admin || buyer,
            CanDelete = admin,
            SearchColumns = new[] { "Familiya", "Imya", "Otchestvo", "Telefon", "Email" },
            Fields = PersonFields("IDPokupatelya")
        };
    }

    public static SectionDescriptor Employees()
    {
        return new SectionDescriptor
        {
            Title = "Пользователи",
            Table = "dbo.UP03_Sotrudniki",
            IdColumn = "IDSotrudnika",
            OrderBy = "t.Familiya, t.Imya",
            CanAdd = true,
            CanEdit = true,
            CanDelete = true,
            SearchColumns = new[] { "Familiya", "Imya", "Otchestvo", "Login", "Email" },
            Fields = new[]
            {
                new FieldDescriptor { Column = "IDSotrudnika", Header = "ID", Identity = true, Editable = false, GridWidth = 45 },
                new FieldDescriptor { Column = "Familiya", Header = "Фамилия", Required = true, GridWidth = 130 },
                new FieldDescriptor { Column = "Imya", Header = "Имя", Required = true, GridWidth = 120 },
                new FieldDescriptor { Column = "Otchestvo", Header = "Отчество", GridWidth = 130 },
                new FieldDescriptor { Column = "Login", Header = "Логин", Required = true, GridWidth = 120 },
                new FieldDescriptor { Column = "Parol", Header = "Пароль", IsPassword = true, ShowInGrid = false },
                new FieldDescriptor { Column = "Sol", Header = "Соль", Auto = true, ShowInGrid = false, Editable = true },
                new FieldDescriptor { Column = "IDRole", Header = "Роль", Kind = FieldKind.Combo, Required = true, ComboSql = RoleCombo, DefaultValue = "3", GridWidth = 150 },
                new FieldDescriptor { Column = "Telefon", Header = "Телефон", GridWidth = 110 },
                new FieldDescriptor { Column = "Email", Header = "Email", GridWidth = 150 },
                new FieldDescriptor { Column = "DataPrioma", Header = "Дата приёма", Kind = FieldKind.Date, GridWidth = 100 },
            }
        };
    }

    public static SectionDescriptor Directories()
    {
        return new SectionDescriptor
        {
            Title = "Справочники",
            Table = "dbo.UP03_Role",
            IdColumn = "IDRole",
            OrderBy = "t.IDRole",
            CanAdd = false,
            CanEdit = false,
            CanDelete = false,
            SearchColumns = new[] { "Tip" },
            Fields = new[]
            {
                new FieldDescriptor { Column = "IDRole", Header = "ID", Identity = true, Editable = false, GridWidth = 50 },
                new FieldDescriptor { Column = "Tip", Header = "Роль", GridWidth = 250 },
            }
        };
    }

    private static FieldDescriptor[] PersonFields(string idColumn) => new[]
    {
        new FieldDescriptor { Column = idColumn, Header = "ID", Identity = true, Editable = false, GridWidth = 45 },
        new FieldDescriptor { Column = "Familiya", Header = "Фамилия", Required = true, GridWidth = 130 },
        new FieldDescriptor { Column = "Imya", Header = "Имя", Required = true, GridWidth = 110 },
        new FieldDescriptor { Column = "Otchestvo", Header = "Отчество", GridWidth = 130 },
        new FieldDescriptor { Column = "Telefon", Header = "Телефон", GridWidth = 110 },
        new FieldDescriptor { Column = "Email", Header = "Email", GridWidth = 150 },
        new FieldDescriptor { Column = "Adres", Header = "Адрес", GridWidth = 180 },
        new FieldDescriptor { Column = "Login", Header = "Логин", GridWidth = 120 },
        new FieldDescriptor { Column = "Parol", Header = "Пароль", IsPassword = true, ShowInGrid = false },
        new FieldDescriptor { Column = "Sol", Header = "Соль", Auto = true, ShowInGrid = false, Editable = true },
        new FieldDescriptor { Column = "DataRegistracii", Header = "Дата регистрации", Kind = FieldKind.Date, GridWidth = 100 },
    };
}
