namespace web.Repositories.Profiles.Dtos;

/// <summary>De trøjestørrelser, en forælder kan vælge. Rækkefølgen er visningsrækkefølgen.</summary>
public static class ShirtSizes
{
    public static readonly IReadOnlyList<string> All = ["XS", "S", "M", "L", "XL", "XXL", "3XL"];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
