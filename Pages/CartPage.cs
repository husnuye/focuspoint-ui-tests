using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebTests.Pages;

/// <summary>
/// Page object for the Cart page.
/// Encapsulates robust locators and high-level actions/assertions used by the E2E flow:
/// - Open cart via header
/// - Validate name/price/quantity for the first cart line
/// - Change quantity and wait for refresh
/// - Clear the cart and assert empty state
///
/// Notes:
/// - All waits are explicit to reduce UI race conditions.
/// - Price normalization removes currency symbols and unifies decimal separators.
/// </summary>
public class CartPage
{
    private readonly IPage _page;
    public CartPage(IPage page) => _page = page;

    // ---- Locators (sturdy) -------------------------------------------------

    // Header cart icon/link.
    private ILocator HeaderCartLink => _page.Locator("a.ico-cart");

    // Main cart table and first line item.
    private ILocator CartTable      => _page.Locator("table.cart");
    private ILocator FirstRow       => CartTable.Locator("tbody tr.cart-item-row").First;

    // Elements inside the first cart row.
    private ILocator NameLink       => FirstRow.Locator("td.product a.product-name");
    private ILocator UnitPriceSpan  => FirstRow.Locator("td.unit-price span.product-unit-price");

    // Quantity is a text input in this theme (id starts with 'itemquantity').
    private ILocator QtyInput       => FirstRow.Locator("td.quantity input[id^='itemquantity']");
    private ILocator UpdateBtn      => _page.Locator("button#updatecart.button-2.update-cart-button");
    private ILocator ClearBtn       => _page.Locator("button.button-2.clear-cart-button");

    // Empty-cart message element.
    private ILocator EmptyMsg       => _page.Locator("div.no-data:has-text('Your Shopping Cart is empty')");

    // ---- Navigation / Waits -----------------------------------------------

    /// <summary>
    /// Opens the cart page via the header cart icon and waits for the table to be visible.
    /// This ensures the page is fully navigated and the first cart row is interactable.
    /// </summary>
    public async Task OpenCartViaHeaderAsync()
    {
        await HeaderCartLink.ClickAsync();

        // Wait for navigation and DOM readiness.
        await _page.WaitForURLAsync(url => url.Contains("/cart", StringComparison.OrdinalIgnoreCase));
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Ensure cart table and first row are visible before proceeding.
        await CartTable.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await FirstRow.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    // ---- Assertions --------------------------------------------------------

    /// <summary>
    /// Asserts that the product name on the first cart line contains the expected substring (case-insensitive).
    /// Useful to confirm the correct item was added to the cart.
    /// </summary>
    public async Task AssertProductNameContainsAsync(string expectedPart)
    {
        await NameLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var name = (await NameLink.InnerTextAsync()).Trim();

        if (!name.Contains(expectedPart, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Cart product name mismatch. Expected contain '{expectedPart}', actual '{name}'.");
    }

    /// <summary>
    /// Asserts that the unit price equals the expected price string, ignoring currency symbols and spacing.
    /// Example: "$12.90" should match "12.90".
    /// </summary>
    public async Task AssertUnitPriceEqualsAsync(string expectedPrice)
    {
        await UnitPriceSpan.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var actual = (await UnitPriceSpan.InnerTextAsync()).Trim();

        if (NormalizePrice(actual) != NormalizePrice(expectedPrice))
            throw new Exception($"Cart unit price mismatch. Expected '{expectedPrice}', actual '{actual}'.");
    }

    /// <summary>
    /// Asserts that the quantity input of the first cart line matches the expected integer.
    /// </summary>
    public async Task AssertQuantityAsync(int expected)
    {
        await QtyInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var val = await QtyInput.InputValueAsync(); // reads current value attribute

        if (!int.TryParse(val, out var got) || got != expected)
            throw new Exception($"Quantity mismatch. Expected {expected}, actual '{val}'.");
    }

    /// <summary>
    /// Asserts the visible empty-cart message to confirm the cart has no items.
    /// </summary>
    public async Task AssertCartEmptyAsync()
    {
        await EmptyMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    // ---- Actions -----------------------------------------------------------

    /// <summary>
    /// Changes the quantity for the first cart line and clicks Update.
    /// Then waits for network to go idle and the first row to be visible again,
    /// ensuring the UI has re-rendered before subsequent assertions.
    /// </summary>
    public async Task ChangeQuantityAsync(int qty)
    {
        await QtyInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await QtyInput.FillAsync(qty.ToString());
        await UpdateBtn.ClickAsync();

        // Wait for the cart to refresh after update so the row becomes stable again.
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await FirstRow.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    /// <summary>
    /// Clicks the "Clear shopping cart" button and waits for network idle
    /// so that the empty state becomes stable for the next assertion.
    /// </summary>
    public async Task ClearCartAsync()
    {
        await ClearBtn.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // ---- Helpers -----------------------------------------------------------

    /// <summary>
    /// Normalizes price strings by stripping currency symbols/whitespace and unifying decimal separators.
    /// Keeps digits, dot, and comma; converts comma to dot; collapses duplicated dots.
    /// </summary>
    private static string NormalizePrice(string s)
    {
        var cleaned = Regex.Replace(s ?? "", @"[^\d\.,]", "");
        cleaned = cleaned.Replace(",", ".");
        cleaned = Regex.Replace(cleaned, @"\.(?=.*\.)", "");
        return cleaned.Trim();
    }
}