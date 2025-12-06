using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace TestStandClone.Core.UserManagement
{
    /// <summary>
    /// Manages users and authentication.
    /// </summary>
    public class UserManager : INotifyPropertyChanged
    {
        private static UserManager? _instance;
        private User? _currentUser;
        private readonly ObservableCollection<User> _users = new ObservableCollection<User>();

        /// <summary>
        /// Singleton instance of UserManager.
        /// </summary>
        public static UserManager Instance
        {
            get
            {
                _instance ??= new UserManager();
                return _instance;
            }
        }

        /// <summary>
        /// Currently logged in user.
        /// </summary>
        public User? CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLoggedIn));
                    OnPropertyChanged(nameof(CurrentUserName));
                    OnPropertyChanged(nameof(CurrentUserRole));
                }
            }
        }

        /// <summary>
        /// Whether a user is currently logged in.
        /// </summary>
        public bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// Name of current user.
        /// </summary>
        public string CurrentUserName => CurrentUser?.DisplayName ?? "Not logged in";

        /// <summary>
        /// Role of current user.
        /// </summary>
        public UserRole? CurrentUserRole => CurrentUser?.Role;

        /// <summary>
        /// All registered users.
        /// </summary>
        public ObservableCollection<User> Users => _users;

        /// <summary>
        /// Event raised when user logs in.
        /// </summary>
        public event EventHandler<User>? UserLoggedIn;

        /// <summary>
        /// Event raised when user logs out.
        /// </summary>
        public event EventHandler<User>? UserLoggedOut;

        /// <summary>
        /// Creates a new UserManager with default admin user.
        /// </summary>
        public UserManager()
        {
            // Add default admin user
            _users.Add(new User("admin", "Administrator", UserRole.Administrator)
            {
                PasswordHash = HashPassword("admin")
            });

            // Add default operator user
            _users.Add(new User("operator", "Default Operator", UserRole.Operator)
            {
                PasswordHash = HashPassword("operator")
            });
        }

        /// <summary>
        /// Attempts to log in a user.
        /// </summary>
        public bool Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return false;
            }

            if (user.PasswordHash != HashPassword(password))
            {
                return false;
            }

            // Log out current user if any
            if (CurrentUser != null)
            {
                Logout();
            }

            user.IsLoggedIn = true;
            user.LoginTime = DateTime.Now;
            CurrentUser = user;
            UserLoggedIn?.Invoke(this, user);

            return true;
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            if (CurrentUser != null)
            {
                var user = CurrentUser;
                user.IsLoggedIn = false;
                user.LoginTime = null;
                CurrentUser = null;
                UserLoggedOut?.Invoke(this, user);
            }
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        public bool CreateUser(string username, string displayName, string password, UserRole role)
        {
            // Check if user already exists
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var user = new User(username, displayName, role)
            {
                PasswordHash = HashPassword(password)
            };

            _users.Add(user);
            return true;
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        public bool DeleteUser(string username)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null || user.Username == "admin")
            {
                return false; // Cannot delete admin
            }

            return _users.Remove(user);
        }

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null || user.PasswordHash != HashPassword(oldPassword))
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            return true;
        }

        /// <summary>
        /// Checks if current user has a permission.
        /// </summary>
        public bool HasPermission(Permission permission)
        {
            return CurrentUser?.HasPermission(permission) ?? false;
        }

        /// <summary>
        /// Gets a user by username.
        /// </summary>
        public User? GetUser(string username)
        {
            return _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Hashes a password for storage.
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
