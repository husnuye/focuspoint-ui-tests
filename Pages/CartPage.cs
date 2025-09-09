using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebTests.Pages;

public class CartPage
{
    private readonly IPage _page;
    public CartPage(IPage page) => _page = page;

    // ---- Locators (sturdy) -------------------------------------------------

    // Header cart icon/link
    private ILocator HeaderCartLink => _page.Locator("a.ico-cart");

    // Main cart table and first line
    private ILocator CartTable      => _page.Locator("table.cart");
    private ILocator FirstRow       => CartTable.Locator("tbody tr.cart-item-row").First;

    // Inside first row
    private ILocator NameLink       => FirstRow.Locator("td.product a.product-name");
    private ILocator UnitPriceSpan  => FirstRow.Locator("td.unit-price span.product-unit-price");
    // Qty input is TEXT in this theme (id starts with itemquantity)
    private ILocator QtyInput       => FirstRow.Locator("td.quantity input[id^='itemquantity']");
    private ILocator UpdateBtn      => _page.Locator("button#updatecart.button-2.update-cart-button");
    private ILocator ClearBtn       => _page.Locator("button.button-2.clear-cart-button");

    // Empty-cart message
    private ILocator EmptyMsg       => _page.Locator("div.no-data:has-text('Your Shopping Cart is empty')");

    // ---- Navigation / Waits -----------------------------------------------

    public async Task OpenCartViaHeaderAsync()
    {
        await HeaderCartLink.ClickAsync();
        // navigate & settle
        await _page.WaitForURLAsync(url => url.Contains("/cart", StringComparison.OrdinalIgnoreCase));
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await CartTable.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await FirstRow.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    // ---- Assertions --------------------------------------------------------

    public async Task AssertProductNameContainsAsync(string expectedPart)
    {
        await NameLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var name = (await NameLink.InnerTextAsync()).Trim();
        if (!name.Contains(expectedPart, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Cart product name mismatch. Expected contain '{expectedPart}', actual '{name}'.");
    }

    public async Task AssertUnitPriceEqualsAsync(string expectedPrice)
    {
        await UnitPriceSpan.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var actual = (await UnitPriceSpan.InnerTextAsync()).Trim();
        if (NormalizePrice(actual) != NormalizePrice(expectedPrice))
            throw new Exception($"Cart unit price mismatch. Expected '{expectedPrice}', actual '{actual}'.");
    }

    public async Task AssertQuantityAsync(int expected)
    {
        await QtyInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var val = await QtyInput.InputValueAsync(); // value attribute
        if (!int.TryParse(val, out var got) || got != expected)
            throw new Exception($"Quantity mismatch. Expected {expected}, actual '{val}'.");
    }

    public async Task AssertCartEmptyAsync()
    {
        await EmptyMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    // ---- Actions -----------------------------------------------------------

    public async Task ChangeQuantityAsync(int qty)
    {
        await QtyInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await QtyInput.FillAsync(qty.ToString());
        await UpdateBtn.ClickAsync();

        // Bekle: güncelleme sonrası tablo/row tekrar görünür olsun
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await FirstRow.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    public async Task ClearCartAsync()
    {
        await ClearBtn.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // ---- helpers -----------------------------------------------------------

    private static string NormalizePrice(string s)
    {
        var cleaned = Regex.Replace(s ?? "", @"[^\d\.,]", "");
        cleaned = cleaned.Replace(",", ".");
        cleaned = Regex.Replace(cleaned, @"\.(?=.*\.)", "");
        return cleaned.Trim();
    }
}