namespace Cuintable.Server.DTOs.CreditCards;

public class CreditCardResponse
{
    public Guid Id { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
