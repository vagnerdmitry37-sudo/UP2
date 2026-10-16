namespace UP.Domain.Common;

public sealed record Money
{
    public const int CurrencyCodeLength = 3;
    public const int MaxDecimalPlaces = 2;
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxAmount);

        if (decimal.Round(amount, MaxDecimalPlaces) != amount)
        {
            throw new ArgumentException(
                $"Amount must have at most {MaxDecimalPlaces} decimal places.", nameof(amount));
        }

        ArgumentNullException.ThrowIfNull(currency);

        if (currency.Length != CurrencyCodeLength || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException(
                $"Currency must be a {CurrencyCodeLength}-letter upper-case ISO 4217 code.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }
}
