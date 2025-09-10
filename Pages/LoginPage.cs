using System.Threading.Tasks;
using Microsoft.Playwright;

namespace WebTests.Pages
{
    /// <summary>
    /// Page object for the Login flow.
    /// Responsible for navigating to the login screen and submitting credentials.
    /// </summary>
    public class LoginPage
    {
        private readonly IPage _page;

        // --- Locators -------------------------------------------------------
        // Header menu → "My Account" (opens the account/login area)
        private readonly ILocator _myAccountMenu;
        // Account submenu → "Log in"
        private readonly ILocator _loginLink;

        // Login form controls
        private readonly ILocator _emailInput;
        private readonly ILocator _passwordInput;
        private readonly ILocator _loginButton;

        // Post-login references (kept for future use; intentionally unused)
        private readonly ILocator _accountOpener; // a.ico-account.opener
        private readonly ILocator _welcomeText;   // "Welcome, ..."

        public LoginPage(IPage page)
        {
            _page = page;

            // Header entry points
            _myAccountMenu = _page.Locator("a.ico-account");
            _loginLink     = _page.Locator("a.ico-login");

            // Login form fields
            _emailInput    = _page.Locator("#Email");
            _passwordInput = _page.Locator("#Password");
            _loginButton   = _page.Locator("button.login-button");
        }

        /// <summary>
        /// Navigates to the login screen by opening: My Account → Log in.
        /// </summary>
        public async Task GoToLoginAsync()
        {
            await _myAccountMenu.ClickAsync();
            await _loginLink.ClickAsync();
        }

        /// <summary>
        /// Submits the login form with the provided credentials.
        /// </summary>
        public async Task LoginAsync(string email, string password)
        {
            await _emailInput.FillAsync(email);
            await _passwordInput.FillAsync(password);

            // Give any client-side validation/async hooks a brief moment.
            await _page.WaitForTimeoutAsync(3000);

            await _loginButton.ClickAsync();
        }

        /// <summary>
        /// Verifies that login has succeeded by asserting the account opener
        /// element is visible in the header within a short timeout.
        /// </summary>
        public async Task<bool> IsLoginSuccessfulAsync()
        {
            try
            {
                // Wait only for the header account opener to become visible.
                await _page.WaitForSelectorAsync(
                    "a.ico-account.opener",
                    new() { State = WaitForSelectorState.Visible, Timeout = 5000 }
                );
                return true; // Visible → login is considered successful.
            }
            catch (TimeoutException)
            {
                return false; // Not visible in time → treat as login failure.
            }
        }
    }
}