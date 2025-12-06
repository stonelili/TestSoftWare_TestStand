using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.UserManagement
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _displayName = string.Empty;
        private UserRole _role = UserRole.Operator;
        private bool _isLoggedIn;
        private DateTime? _loginTime;
        private string _passwordHash = string.Empty;

        /// <summary>
        /// Username for login.
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Display name for the user.
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Role of the user.
        /// </summary>
        public UserRole Role
        {
            get => _role;
            set
            {
                if (_role != value)
                {
                    _role = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether user is currently logged in.
        /// </summary>
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn != value)
                {
                    _isLoggedIn = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Time of login.
        /// </summary>
        public DateTime? LoginTime
        {
            get => _loginTime;
            set
            {
                if (_loginTime != value)
                {
                    _loginTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Password hash (for security).
        /// </summary>
        public string PasswordHash
        {
            get => _passwordHash;
            set
            {
                if (_passwordHash != value)
                {
                    _passwordHash = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Creates a new user with username and role.
        /// </summary>
        public User(string username, UserRole role)
        {
            Username = username;
            DisplayName = username;
            Role = role;
        }

        /// <summary>
        /// Creates a new user with full details.
        /// </summary>
        public User(string username, string displayName, UserRole role)
        {
            Username = username;
            DisplayName = displayName;
            Role = role;
        }

        /// <summary>
        /// Checks if user has permission for an action.
        /// </summary>
        public bool HasPermission(Permission permission)
        {
            return Role switch
            {
                UserRole.Administrator => true, // Admin has all permissions
                UserRole.Developer => permission != Permission.UserManagement,
                UserRole.Technician => permission is Permission.ExecuteTests or Permission.ViewReports or Permission.ViewSequences,
                UserRole.Operator => permission is Permission.ExecuteTests or Permission.ViewReports,
                _ => false
            };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// User roles with different permission levels.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Can only run tests and view results.
        /// </summary>
        Operator,

        /// <summary>
        /// Can run tests, view/edit limits, view sequences.
        /// </summary>
        Technician,

        /// <summary>
        /// Can create/edit sequences, steps, and run tests.
        /// </summary>
        Developer,

        /// <summary>
        /// Full access to all features including user management.
        /// </summary>
        Administrator
    }

    /// <summary>
    /// Permissions for different actions.
    /// </summary>
    public enum Permission
    {
        ExecuteTests,
        ViewReports,
        GenerateReports,
        ViewSequences,
        EditSequences,
        CreateSequences,
        DeleteSequences,
        EditLimits,
        EditVariables,
        UserManagement,
        SystemConfiguration
    }
}
