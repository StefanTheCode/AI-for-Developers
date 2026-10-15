using System.Security.Cryptography;

namespace UrlShortener.Services;

/// <summary>
/// Generates short, URL-safe codes like "aB3xK9q".
///
/// LESSON — use a cryptographic RNG for identifiers.
/// <see cref="RandomNumberGenerator"/> gives unpredictable codes, so nobody can
/// guess the next one and enumerate everyone's links. <see cref="System.Random"/>
/// would be faster but predictable — the wrong trade-off for anything public.
/// </summary>
public static class ShortCodeGenerator
{
    // Base62 alphabet: 0-9, a-z, A-Z. No ambiguous punctuation, URL-safe.
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string Generate(int length = 7)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            // RandomNumberGenerator.GetInt32 is unbiased over the range.
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
