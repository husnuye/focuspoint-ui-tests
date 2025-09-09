using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages;

/// <summary>
/// Home (landing) page: controls the header search box and button.
/// </summary>
public class HomePage
{
    private readonly IPage _page;
    public HomePage(IPage page) => _page = page;

    // Search input (header)
    //   - Prefer stable id: #small-searchterms
    //   - Fallbacks: name="q" or type="search"
    private ILocator SearchInput =>
        _page.Locator("#small-searchterms, input[name='q'], input[type='search']").First;

    // Search button (header)
    //   - Usually: button.button-1.search-box-button
    //   - Fallback: any submit button inside the search form
    private ILocator SearchButton =>
        _page.Locator("button.button-1.search-box-button, form .search-box button[type='submit'], button[type='submit']").First;

    /// <summary>Navigates to the base URL.</summary>
    public Task GoToAsync(string baseUrl) => _page.GotoAsync(baseUrl);

    /// <summary>Focuses the header search input.</summary>
    public Task FocusSearchAsync() => SearchInput.ClickAsync();

    /// <summary>Types the search text (clears existing value first).</summary>
    public Task TypeSearchAsync(string text) => SearchInput.FillAsync(text);

    /// <summary>Clears the search input.</summary>
    public async Task ClearSearchAsync()
    {
        await SearchInput.FillAsync(string.Empty);
        await _page.WaitForTimeoutAsync(100);
    }

    /// <summary>
    /// Submits the search. Tries Enter first; if no results appear, clicks the button as a fallback.
    /// </summary>
    public async Task SubmitSearchAsync()
    {
        await SearchInput.PressAsync("Enter");

        // Small pause to allow autocomplete/submit scripts to run
        await _page.WaitForTimeoutAsync(250);

        // If Enter didn’t navigate/show results, click the button
        var resultsMaybe = _page.Locator(":is(.search-results,.products-wrapper,.product-item)");
        if (!await resultsMaybe.First.IsVisibleAsync())
        {
            await SearchButton.ClickAsync();
        }
    }
}