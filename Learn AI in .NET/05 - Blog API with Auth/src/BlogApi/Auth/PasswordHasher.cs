using System.Security.Cryptography;

namespace BlogApi.Auth;

/// <summary>
/// Hashes and verifies passwords with PBKDF2 — no external dependencies.
///
/// LESSON — never store passwords, store slow salted hashes.
/// * A per-user random <b>salt</b> means two users with the same password get
///   different hashes (defeats rainbow tables).
/// * A high <b>iteration count</b> makes each guess expensive, slowing brute force.
/// * Verification uses a <b>fixed-time</b> comparison to avoid timing attacks.
///
/// (In a full app you'd typically use ASP.NET Core Identity's <c>PasswordHasher</c>,
/// which does all this for you. We hand-roll it here to make the mechanics visible.)
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;      // 128-bit salt
    private const int KeySize = 32;       // 256-bit derived key
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>Produces a self-describing hash string: "iterations.salt.key" (Base64).</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    /// <summary>Re-derives the key from the candidate password and compares in fixed time.</summary>
    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedKey = Convert.FromBase64String(parts[2]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);

        // CryptographicOperations.FixedTimeEquals prevents leaking info via timing.
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
