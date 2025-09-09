using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages;

public class ProductPage
{
    private readonly IPage _page;
    public ProductPage(IPage page) => _page = page;

    private ILocator AddToCart => _page.Locator("button#add-to-cart, [data-testid='add-to-cart'], button:has-text('Add to cart')").First;
    private ILocator Price => _page.Locator(":is(.price,[data-testid='price'])").First;

    public Task AddToCartAsync() => AddToCart.ClickAsync();
    public async Task<string> GetPriceAsync() => (await Price.InnerTextAsync()).Trim();
}