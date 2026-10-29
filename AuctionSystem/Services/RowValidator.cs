using System.Collections.Generic;

namespace AuctionSystem.Services;

public static class RowValidator
{
    public static string? Validate(Models.SectionDescriptor section, IDictionary<string, object?> values, bool isNew)
    {
        switch (section.Table)
        {
            case "dbo.UP03_Loty":
                if (values.TryGetValue("NomerLota", out var number) && number is int n && n <= 0)
                {
                    return "Номер лота должен быть положительным числом.";
                }

                if (values.TryGetValue("StartovayaCena", out var price) && price is decimal p && p < 0)
                {
                    return "Стартовая цена не может быть отрицательной.";
                }

                break;

            case "dbo.UP03_RezultatyTorgov":
            {
                var sold = values.TryGetValue("PriznakProdazhi", out var flag) && flag is true;
                if (sold)
                {
                    if (!values.TryGetValue("IDPokupatelya", out var buyer) || buyer is null)
                    {
                        return "Для проданного лота укажите покупателя.";
                    }

                    if (!values.TryGetValue("DataProdazhi", out var date) || date is null)
                    {
                        return "Для проданного лота укажите дату продажи.";
                    }
                }
                else
                {
                    values["IDPokupatelya"] = null;
                    values["FinalnayaCena"] = null;
                    values["SummaKommissii"] = null;
                    values["DataProdazhi"] = null;
                }

                break;
            }

            case "dbo.UP03_Sotrudniki":
            case "dbo.UP03_Prodavtsy":
            case "dbo.UP03_Pokupateli":
                if (isNew && (!values.TryGetValue("Parol", out var password) || string.IsNullOrEmpty(password as string)))
                {
                    return "Для новой учётной записи задайте пароль.";
                }

                break;
        }

        return null;
    }
}
