// Pages/SearchPage.cs
using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages;

/// <summary>
/// Search results page: waits for results and adds the first product to cart.
/// Also provides a helper to open the cart via the header cart icon.
/// </summary>
public class SearchPage
{
    private readonly IPage _page;
    public SearchPage(IPage page) => _page = page;

    // Results list container and items (based on your screenshots/DOM)
    private ILocator ResultsContainer => _page.Locator(".products-wrapper .product-list");
    private ILocator ResultCards      => ResultsContainer.Locator(".item-box");

    // Elements inside a result card
    private static string TitleSel => "h2.product-title a";
    private static string AddSel   => "button.button-2.product-box-add-to-cart-button";

    // Header cart icon (top-right)
    private ILocator HeaderCartIcon => _page.Locator("a.ico-cart");

    /// <summary>Waits until search results and at least one card are visible.</summary>
    public async Task WaitResultsAsync()
    {
        await ResultsContainer.First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
        await ResultCards.First.WaitForAsync(new()      { State = WaitForSelectorState.Visible,  Timeout = 15000 });
        await _page.WaitForTimeoutAsync(200); // tiny breath for dynamic scripts
    }

    /// <summary>Returns the first product's name (for optional logging/verification).</summary>
    public async Task<string> GetFirstProductNameAsync()
        => (await ResultCards.Nth(0).Locator(TitleSel).InnerTextAsync()).Trim();

    /// <summary>Clicks "Add to cart" on the first product card.</summary>
    public async Task AddFirstProductToCartAsync()
    {
        var addBtn = ResultCards.Nth(0).Locator(AddSel);
        await addBtn.ScrollIntoViewIfNeededAsync();
        await addBtn.ClickAsync();
    }

    /// <summary>Opens the Cart by clicking the header cart icon.</summary>
    public Task OpenCartFromHeaderAsync() => HeaderCartIcon.ClickAsync();
}