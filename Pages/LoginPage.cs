using System.Threading.Tasks;
using Microsoft.Playwright;

namespace WebTests.Pages
{
    public class LoginPage
    {
        private readonly IPage _page;

        // Locators
        private readonly ILocator _myAccountMenu;
        private readonly ILocator _loginLink;
        private readonly ILocator _emailInput;
        private readonly ILocator _passwordInput;
        private readonly ILocator _loginButton;

        // After-login account opener and welcome text
        private readonly ILocator _accountOpener; // a.ico-account.opener
        private readonly ILocator _welcomeText;   // "Welcome, ..."

        public LoginPage(IPage page)
        {
            _page = page;

            // Header menu → My account
            _myAccountMenu = _page.Locator("a.ico-account");

            // Submenu → Log in
            _loginLink = _page.Locator("a.ico-login");

            // Login form
            _emailInput = _page.Locator("#Email");
            _passwordInput = _page.Locator("#Password");
            _loginButton = _page.Locator("button.login-button");
        }

        /// <summary>
        /// Navigates to login screen by opening My Account > Log in.
        /// </summary>
        public async Task GoToLoginAsync()
        {
            await _myAccountMenu.ClickAsync();
            await _loginLink.ClickAsync();
        }

        /// <summary>
        /// Performs login with provided credentials.
        /// </summary>
        public async Task LoginAsync(string email, string password)
        {
            await _emailInput.FillAsync(email);
            await _passwordInput.FillAsync(password);
            // wait
            await _page.WaitForTimeoutAsync(3000);
            await _loginButton.ClickAsync();
        }

        /// <summary>
        /// Verifies login success 
        /// </summary>
        public async Task<bool> IsLoginSuccessfulAsync()
        {
            try
            {
                // Sadece header’daki "a.ico-account.opener" elementini bekle
                await _page.WaitForSelectorAsync(
                    "a.ico-account.opener",
                    new() { State = WaitForSelectorState.Visible, Timeout = 5000 }
                );
                return true;  // Göründü → login başarılı
            }
            catch (TimeoutException)
            {
                return false; // Süresinde görünmedi → login başarısız
            }
        }
    }
}