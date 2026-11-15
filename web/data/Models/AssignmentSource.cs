namespace web.Data.Models;

/// <summary>Hvordan brugeren kom på vagten. Gemmes som tekst.</summary>
public enum AssignmentSource
{
    /// <summary>Tildelt af en administrator (også i det skjulte, før frigivelse).</summary>
    Admin,

    /// <summary>Brugeren tilmeldte sig selv (først-til-mølle).</summary>
    Self
}
