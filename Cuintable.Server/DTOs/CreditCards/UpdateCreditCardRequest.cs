namespace Cuintable.Server.DTOs.CreditCards;

public class UpdateCreditCardRequest
{
    public string Bank { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public int? CutoffDay { get; set; }
    public int? PaymentDueDay { get; set; }
    public bool IsActive { get; set; } = true;
}
