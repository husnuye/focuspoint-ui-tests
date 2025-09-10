using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages;

/// <summary>
/// Home (landing) page object.
/// Responsible for interacting with the header search box and the search submit control.
/// Notes:
/// - Uses stable selectors with sensible fallbacks to tolerate minor UI/theme changes.
/// - Submit tries Enter first, then falls back to clicking the button if navigation doesn’t occur.
/// </summary>
public class HomePage
{
    private readonly IPage _page;
    public HomePage(IPage page) => _page = page;

    // Header search input.
    // Primary: stable id '#small-searchterms'
    // Fallbacks: name="q" or type="search" (covers common storefront themes)
    private ILocator SearchInput =>
        _page.Locator("#small-searchterms, input[name='q'], input[type='search']").First;

    // Header search button.
    // Primary: 'button.button-1.search-box-button'
    // Fallbacks: any submit button inside a search form
    private ILocator SearchButton =>
        _page.Locator("button.button-1.search-box-button, form .search-box button[type='submit'], button[type='submit']").First;

    /// <summary>
    /// Navigates to the given base URL.
    /// </summary>
    public Task GoToAsync(string baseUrl) => _page.GotoAsync(baseUrl);

    /// <summary>
    /// Focuses the header search input to make it ready for typing.
    /// </summary>
    public Task FocusSearchAsync() => SearchInput.ClickAsync();

    /// <summary>
    /// Types the given search text (overwrites the existing value).
    /// </summary>
    public Task TypeSearchAsync(string text) => SearchInput.FillAsync(text);

    /// <summary>
    /// Clears the search input and yields briefly to allow any on-change handlers.
    /// </summary>
    public async Task ClearSearchAsync()
    {
        await SearchInput.FillAsync(string.Empty);
        await _page.WaitForTimeoutAsync(100);
    }

    /// <summary>
    /// Submits the search:
    /// 1) Presses Enter (most themes submit on Enter).
    /// 2) Waits shortly for scripts/autocomplete.
    /// 3) If no results/navigation are observed, clicks the search button as a fallback.
    /// </summary>
    public async Task SubmitSearchAsync()
    {
        await SearchInput.PressAsync("Enter");

        // Short pause to allow client-side scripts to react (autocomplete or form submit handlers).
        await _page.WaitForTimeoutAsync(250);

        // Fallback: if Enter did not yield visible results or navigation, click the button.
        var resultsMaybe = _page.Locator(":is(.search-results,.products-wrapper,.product-item)");
        if (!await resultsMaybe.First.IsVisibleAsync())
        {
            await SearchButton.ClickAsync();
        }
    }
}