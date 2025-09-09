using NUnit.Framework;
using System.Threading.Tasks;

using WebTests.Core;
using WebTests.Pages;
using WebTests.Utils;

// ✅ Allure yeni namespace
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using Allure.NUnit;

namespace WebTests.Tests
{
    /// <summary>
    /// E2E: Login → Search (Home) → Add to Cart (Search)
    /// → Validate name & price (Cart) → Qty=2 → Clear cart.
    /// </summary>
    [AllureNUnit]
    [TestFixture]
    [AllureSuite("E2E")]
    [AllureFeature("CartFlow")]
    [AllureOwner("husnuye")]
    [AllureSeverity(SeverityLevel.critical)]
    public class FocusPointE2ETests : TestBase
    {
        private HomePage hp = default!;
        private SearchPage sp = default!;
        private LoginPage lp = default!;
        private CartPage cp = default!;

        [SetUp]
        public void SetupPages()
        {
            hp = new HomePage(Page);
            sp = new SearchPage(Page);
            lp = new LoginPage(Page);
            cp = new CartPage(Page);
        }

        [Test(Description = "Full E2E: login → search → add to cart → cart validations → qty 2 → clear")]
        [AllureTag("happy-path", "playwright")]
        public async Task FocusPoint_Full_E2E_Search_AddToCart_Clear()
        {
            await Step_NavigateToHome(Config["BaseUrl"]!);
            await Step_OpenLogin();
            await Step_SubmitCredentials(Config["Credentials:Email"]!, Config["Credentials:Password"]!);
            await Step_VerifyLogin();

            var (kw1, kw2, expectedPrice) = await Step_ReadSearchData(Config["SearchDataExcelPath"]!, "Sheet1");
            await Step_SearchOnHome(kw1, kw2);

            await Step_WaitSearchResults();
            await Step_AddFirstProductFromSearch();

            await Step_OpenCartViaHeader();
            await Step_AssertCartProductNameContains(kw2);
            await Step_AssertCartProductPriceEquals(expectedPrice);

            await Step_ChangeQtyTo2AndAssert();
            await Step_ClearCartAndAssertEmpty();
        }

        // ---------- Allure Steps ----------
        [AllureStep("1 Navigating to Home Page: {0}")]
        private async Task Step_NavigateToHome(string baseUrl)
        {
            await hp.GoToAsync(baseUrl);
            TestContext.WriteLine("1 Navigating to Home Page...");
        }

        [AllureStep("2 Opening Login Page")]
        private async Task Step_OpenLogin()
        {
            await lp.GoToLoginAsync();
            TestContext.WriteLine("2 Opening Login Page...");
        }

        [AllureStep("2.1 Submitting credentials for '{0}'")]
        private async Task Step_SubmitCredentials(string email, string password)
        {
            await lp.LoginAsync(email, password);
            TestContext.WriteLine("2.1 Submitting credentials...");
        }

        [AllureStep("2.2 Verifying login")]
        private async Task Step_VerifyLogin()
        {
            Assert.That(await lp.IsLoginSuccessfulAsync(), Is.True, "Login failed - account page not detected.");
            TestContext.WriteLine("2.2 Verifying login...");
        }

        [AllureStep("3 Reading keywords & expected price from Excel (sheet '{1}')")]
        private Task<(string, string, string)> Step_ReadSearchData(string path, string sheet)
        {
            TestContext.WriteLine("3 Reading keywords & expected price from Excel (strict)...");
            var tuple = ExcelReader.ReadFirstRowStrict(path, sheet);
            return Task.FromResult(tuple);
        }

        [AllureStep("3.1 Searching on Home: '{0}' (clear) → '{1}'")]
        private async Task Step_SearchOnHome(string first, string second)
        {
            TestContext.WriteLine($"3.1 Searching: '{first}' (clear) → '{second}'");
            await hp.FocusSearchAsync();
            await hp.TypeSearchAsync(first);
            await hp.ClearSearchAsync();
            await hp.TypeSearchAsync(second);
            await hp.SubmitSearchAsync();
        }

        [AllureStep("4 Waiting for search results")]
        private async Task Step_WaitSearchResults()
        {
            TestContext.WriteLine("4 Waiting for search results...");
            await sp.WaitResultsAsync();
        }

        [AllureStep("4.1 Adding first product to cart from search page")]
        private async Task Step_AddFirstProductFromSearch()
        {
            TestContext.WriteLine("4.1 Adding first product to cart from search page...");
            await sp.AddFirstProductToCartAsync();
        }

        [AllureStep("5 Opening cart via header")]
        private async Task Step_OpenCartViaHeader()
        {
            TestContext.WriteLine("5 Opening cart via header...");
            await cp.OpenCartViaHeaderAsync();
        }

        [AllureStep("5.1 Asserting cart product NAME contains '{0}'")]
        private async Task Step_AssertCartProductNameContains(string expectedPart)
        {
            TestContext.WriteLine("5.1 Asserting cart product NAME contains typed keyword...");
            await cp.AssertProductNameContainsAsync(expectedPart);
        }

        [AllureStep("5.2 Asserting cart product PRICE equals expected '{0}'")]
        private async Task Step_AssertCartProductPriceEquals(string expectedPrice)
        {
            TestContext.WriteLine("5.2 Asserting cart product PRICE equals expected...");
            await cp.AssertUnitPriceEqualsAsync(expectedPrice);
        }

        [AllureStep("6 Changing quantity to 2 → Update → assert")]
        private async Task Step_ChangeQtyTo2AndAssert()
        {
            TestContext.WriteLine("6 Changing quantity to 2 → Update → assert...");
            await cp.ChangeQuantityAsync(2);
            await cp.AssertQuantityAsync(2);
        }

        [AllureStep("7 Clearing shopping cart and asserting empty message")]
        private async Task Step_ClearCartAndAssertEmpty()
        {
            TestContext.WriteLine("7 Clearing shopping cart and asserting empty message...");
            await cp.ClearCartAsync();
            await cp.AssertCartEmptyAsync();
            TestContext.WriteLine("[TEST COMPLETED] E2E scenario executed successfully.");
        }
    }
}