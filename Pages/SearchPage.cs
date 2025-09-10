// Pages/SearchPage.cs
using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages;

/// <summary>
/// Search results page object.
/// - Waits for the search results grid to become stable/visible
/// - Adds the first product to the cart
/// - Provides a helper to open the cart via the header icon
/// 
/// Notes:
/// * Locators are intentionally conservative and scoped to the results container
///   to avoid picking up layout/marketing blocks elsewhere on the page.
/// * Tiny waits are used only to let client-side scripts settle after visibility.
/// </summary>
public class SearchPage
{
    private readonly IPage _page;
    public SearchPage(IPage page) => _page = page;

    // ----- Results area -----------------------------------------------------
    // Container wrapping the list of product cards.
    private ILocator ResultsContainer => _page.Locator(".products-wrapper .product-list");

    // Individual product cards under the container.
    private ILocator ResultCards => ResultsContainer.Locator(".item-box");

    // ----- Elements inside a product card ----------------------------------
    // Product title link (used for optional logging/verification).
    private static string TitleSel => "h2.product-title a";

    // "Add to cart" button in the product card (list view).
    private static string AddSel => "button.button-2.product-box-add-to-cart-button";

    // ----- Header controls --------------------------------------------------
    // Header cart icon (opens the cart page).
    private ILocator HeaderCartIcon => _page.Locator("a.ico-cart");

    /// <summary>
    /// Waits until the search results container is attached and at least one
    /// product card is visible. Adds a short extra pause to let dynamic scripts
    /// (e.g., ribbons, lazy images) finish.
    /// </summary>
    public async Task WaitResultsAsync()
    {
        await ResultsContainer.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Attached,
            Timeout = 15000
        });

        await ResultCards.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15000
        });

        // Small grace period for front-end scripts that run right after first paint.
        await _page.WaitForTimeoutAsync(200);
    }

    /// <summary>
    /// Reads the first product name from the results grid.
    /// Useful for logging or soft assertions prior to cart navigation.
    /// </summary>
    public async Task<string> GetFirstProductNameAsync()
        => (await ResultCards.Nth(0).Locator(TitleSel).InnerTextAsync()).Trim();

    /// <summary>
    /// Clicks "Add to cart" on the first product card.
    /// Scrolls into view to avoid click interception by sticky headers/overlays.
    /// </summary>
    public async Task AddFirstProductToCartAsync()
    {
        var addBtn = ResultCards.Nth(0).Locator(AddSel);
        await addBtn.ScrollIntoViewIfNeededAsync();
        await addBtn.ClickAsync();
    }

    /// <summary>
    /// Opens the cart via the header cart icon.
    /// </summary>
    public Task OpenCartFromHeaderAsync() => HeaderCartIcon.ClickAsync();
}