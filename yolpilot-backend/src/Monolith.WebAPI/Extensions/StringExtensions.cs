using System.Text;

namespace Monolith.WebAPI.Extensions;

public static class StringExtensions
{
    public static string NormalizeText(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            switch (c)
            {
                case 'ı':
                    sb.Append('i');
                    break;
                case 'İ':
                    sb.Append('I');
                    break;
                case 'ş':
                    sb.Append('s');
                    break;
                case 'Ş':
                    sb.Append('S');
                    break;
                case 'ç':
                    sb.Append('c');
                    break;
                case 'Ç':
                    sb.Append('C');
                    break;
                case 'ğ':
                    sb.Append('g');
                    break;
                case 'Ğ':
                    sb.Append('G');
                    break;
                case 'ü':
                    sb.Append('u');
                    break;
                case 'Ü':
                    sb.Append('U');
                    break;
                case 'ö':
                    sb.Append('o');
                    break;
                case 'Ö':
                    sb.Append('O');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}