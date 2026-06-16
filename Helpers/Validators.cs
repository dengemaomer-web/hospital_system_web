namespace HospitalSystem.Helpers;

public static class Validators
{
    /// <summary>Validates a Turkish national identity number (TC Kimlik No) using the official algorithm.</summary>
    public static bool IsValidTcKimlikNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 11 || !value.All(char.IsDigit))
            return false;
        if (value[0] == '0')
            return false;

        var digits = value.Select(c => c - '0').ToArray();
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var tenth = ((oddSum * 7) - evenSum) % 10;
        if (tenth < 0) tenth += 10;
        if (tenth != digits[9])
            return false;

        var sumFirstTen = digits.Take(10).Sum();
        return sumFirstTen % 10 == digits[10];
    }
}
