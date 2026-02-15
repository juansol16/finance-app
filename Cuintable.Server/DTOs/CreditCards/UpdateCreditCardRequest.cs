namespace Cuintable.Server.DTOs.CreditCards;

public class UpdateCreditCardRequest
{
    public string Bank { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
