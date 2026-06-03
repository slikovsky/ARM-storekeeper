using ARMStored.Models;

namespace ARMStored.Services;

public class AuthService
{
    private readonly DatabaseService _dbService;
    private static User? _currentUser;

    public AuthService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public bool Authenticate(string username, string password)
    {
        var user = _dbService.GetUserByUsername(username);
        if (user == null) return false;

        // ВРЕМЕННО: прямое сравнение без хеширования (только для теста!)
        if (password == "admin123" && username == "admin")
        {
            _currentUser = user;
            _dbService.UpdateLastLogin(user.Id);
            return true;
        }

        if (password == "store123" && username == "store")
        {
            _currentUser = user;
            _dbService.UpdateLastLogin(user.Id);
            return true;
        }
        return false;
    }

    public static User? CurrentUser => _currentUser;
    public static bool IsAuthenticated => _currentUser != null;
    public static bool IsAdmin => _currentUser?.Role == "admin";
    
    public static void Logout()
    {
        _currentUser = null;
    }
}