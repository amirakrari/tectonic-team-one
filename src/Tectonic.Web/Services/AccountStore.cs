using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Tectonic.Web.Services;

public record Account(string Name, string Email);

public record SampleAccount(string Name, string Email, string Password);

// DEMO ONLY: in-memory accounts so the login/sign-up flow works before the API exists.
// Seeded from "SampleAccounts" in appsettings.Development.json; sign-ups live until the app restarts.
// Passwords are kept as salted hashes, never in plain text. Replace with the API's auth endpoints.
public class AccountStore
{
    private readonly ConcurrentDictionary<string, (Account Account, byte[] Salt, byte[] Hash)> _accounts =
        new(StringComparer.OrdinalIgnoreCase);

    public AccountStore(IConfiguration config)
    {
        var seeds = config.GetSection("SampleAccounts").Get<List<SampleAccount>>() ?? [];
        foreach (var seed in seeds)
        {
            Register(seed.Name, seed.Email, seed.Password);
        }
        Demo = seeds.FirstOrDefault();
    }

    // First sample account, for the "Use demo account" button (development only).
    public SampleAccount? Demo { get; }

    public Account? Find(string email) => _accounts.TryGetValue(email.Trim(), out var entry) ? entry.Account : null;

    public Account? SignIn(string email, string password)
    {
        if (!_accounts.TryGetValue(email.Trim(), out var entry)) return null;
        return CryptographicOperations.FixedTimeEquals(Hash(password, entry.Salt), entry.Hash) ? entry.Account : null;
    }

    // Returns null when the email is already taken.
    public Account? Register(string name, string email, string password)
    {
        var account = new Account(name.Trim(), email.Trim());
        var salt = RandomNumberGenerator.GetBytes(16);
        return _accounts.TryAdd(account.Email, (account, salt, Hash(password, salt))) ? account : null;
    }

    private static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
}
