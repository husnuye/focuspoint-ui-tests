// OPTIONAL: only if you ever navigate to product details
using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WebTests.Pages
{
    /// <summary>
    /// Product details page (only needed if your flow opens a single product view).
    /// </summary>
    public class ProductPage
    {
        private readonly IPage _page;
        public ProductPage(IPage page) => _page = page;

        private ILocator Title      => _page.Locator(".product-name h1, h1[itemprop='name']").First;
        private ILocator Price      => _page.Locator(".product-price .price, .actual-price").First;
        private ILocator QtyInput   => _page.Locator("input.qty-input, input[name='addtocart_*_EnteredQuantity']").First;
        private ILocator AddToCart  => _page.Locator("[id^='add-to-cart-button-'], button.add-to-cart-button").First;

        public async Task WaitReadyAsync()
        {
            await Title.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        }

        public async Task<string> ReadTitleAsync() => (await Title.InnerTextAsync()).Trim();
        public async Task<string> ReadPriceAsync() => (await Price.InnerTextAsync()).Trim();

        public async Task AddToCartAsync(int qty = 1)
        {
            if (await QtyInput.IsVisibleAsync())
                await QtyInput.FillAsync(qty.ToString());
            await AddToCart.ClickAsync();
        }
    }
}