namespace web.Data.Models;

/// <summary>Forælderens stamdata (1:1 med User). E-mail er brugernavnet og står ikke her.</summary>
public class Profile : EntityBase
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>En af værdierne i ShirtSizes.All.</summary>
    public string ShirtSize { get; set; } = string.Empty;

    /// <summary>Antal væskebilletter (stk.), 0–50.</summary>
    public int DrinkTickets { get; set; }

    /// <summary>Antal ark tivolibilletter, 0–2.</summary>
    public int TivoliSheets { get; set; }

    public string ChildName { get; set; } = string.Empty;
    public string ChildClass { get; set; } = string.Empty;
}
