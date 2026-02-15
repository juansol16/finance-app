namespace Cuintable.Server.DTOs.CreditCards;

public class CreateCreditCardRequest
{
    public string Bank { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
}
