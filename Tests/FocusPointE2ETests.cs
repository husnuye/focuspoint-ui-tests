using NUnit.Framework;
using System.Threading.Tasks;

using WebTests.Core;
using WebTests.Pages;
using WebTests.Utils;

using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

namespace WebTests.Tests
{
    [AllureNUnit]
    [TestFixture]
    [AllureSuite("E2E")]
    [AllureFeature("CartFlow")]
    [AllureOwner("husnuye")]
    [AllureSeverity(SeverityLevel.critical)]
    public class FocusPointE2ETests : TestBase
    {
        private HomePage  hp = default!;
        private SearchPage sp = default!;
        private LoginPage  lp = default!;
        private CartPage   cp = default!;

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
            // 1–4: Home + Login
            await Step_NavigateToHome(Config["BaseUrl"]!);                    // 1
            await Step_OpenLogin();                                           // 2
            await Step_SubmitCredentials(Config["Credentials:Email"]!,
                                        Config["Credentials:Password"]!);    // 3
            await Step_VerifyLogin();                                         // 4

            // 5–6: Excel + Search
            var (kw1, kw2, expectedPrice) = await Step_ReadSearchData(
                Config["SearchDataExcelPath"]!, "Sheet1");                    // 5
            await Step_SearchOnHome(kw1, kw2);                                // 6

            // 7–8: Results + Add to cart
            await Step_WaitSearchResults();                                   // 7
            await Step_AddFirstProductFromSearch();                           // 8

            // 9–11: Cart assertions
            await Step_OpenCartViaHeader();                                   // 9
            await Step_AssertCartProductNameContains(kw2);                    // 10
            await Step_AssertCartProductPriceEquals(expectedPrice);           // 11

            // 12–13: Qty & Clear
            await Step_ChangeQtyTo2AndAssert();                               // 12
            await Step_ClearCartAndAssertEmpty();                             // 13
        }

        // ---------- Allure Steps (1–13) ----------

        [AllureStep("1 Navigate to Home Page")]
        private async Task Step_NavigateToHome(string baseUrl)
        {
            await hp.GoToAsync(baseUrl);
            TestContext.WriteLine($"[1] Navigating to Home Page → {baseUrl}");
        }

        [AllureStep("2 Open Login Page")]
        private async Task Step_OpenLogin()
        {
            await lp.GoToLoginAsync();
            TestContext.WriteLine("[2] Opening Login Page...");
        }

        [AllureStep("3 Submit Credentials")]
        private async Task Step_SubmitCredentials(string email, string password)
        {
            await lp.LoginAsync(email, password);
            TestContext.WriteLine($"[3] Submitting credentials for user '{email}'");
        }

        [AllureStep("4 Verify Login Success")]
        private async Task Step_VerifyLogin()
        {
            Assert.That(await lp.IsLoginSuccessfulAsync(), Is.True,
                "Login failed - account page not detected.");
            TestContext.WriteLine("[4] Verifying login...");
        }

        [AllureStep("5 Read Search Data from Excel")]
        private Task<(string, string, string)> Step_ReadSearchData(string path, string sheet)
        {
            TestContext.WriteLine($"[5] Reading keywords & expected price from Excel → {path} (sheet: {sheet})");
            var tuple = ExcelReader.ReadFirstRowStrict(path, sheet);
            return Task.FromResult(tuple);
        }

        [AllureStep("6 Execute Two-Phase Search on Home")]
        private async Task Step_SearchOnHome(string first, string second)
        {
            TestContext.WriteLine($"[6] Searching on Home: '{first}' (clear) → '{second}'");
            await hp.FocusSearchAsync();
            await hp.TypeSearchAsync(first);
            await hp.ClearSearchAsync();
            await hp.TypeSearchAsync(second);
            await hp.SubmitSearchAsync();
        }

        [AllureStep("7 Wait for Search Results")]
        private async Task Step_WaitSearchResults()
        {
            TestContext.WriteLine("[7] Waiting for search results...");
            await sp.WaitResultsAsync();
        }

        [AllureStep("8 Add First Product to Cart (from Search Results)")]
        private async Task Step_AddFirstProductFromSearch()
        {
            TestContext.WriteLine("[8] Adding first product to cart from search page...");
            await sp.AddFirstProductToCartAsync();
        }

        [AllureStep("9 Open Cart via Header")]
        private async Task Step_OpenCartViaHeader()
        {
            TestContext.WriteLine("[9] Opening cart via header...");
            await cp.OpenCartViaHeaderAsync();
        }

        [AllureStep("10 Assert Cart Product Name Contains Keyword")]
        private async Task Step_AssertCartProductNameContains(string expectedPart)
        {
            TestContext.WriteLine($"[10] Asserting cart product NAME contains '{expectedPart}'...");
            await cp.AssertProductNameContainsAsync(expectedPart);
        }

        [AllureStep("11 Assert Cart Unit Price Equals Expected")]
        private async Task Step_AssertCartProductPriceEquals(string expectedPrice)
        {
            TestContext.WriteLine($"[11] Asserting cart product PRICE equals expected '{expectedPrice}'...");
            await cp.AssertUnitPriceEqualsAsync(expectedPrice);
        }

        [AllureStep("12 Change Quantity to 2 and Assert")]
        private async Task Step_ChangeQtyTo2AndAssert()
        {
            TestContext.WriteLine("[12] Changing quantity to 2 → Update → assert...");
            await cp.ChangeQuantityAsync(2);
            await cp.AssertQuantityAsync(2);
        }

        [AllureStep("13 Clear Shopping Cart and Assert Empty")]
        private async Task Step_ClearCartAndAssertEmpty()
        {
            TestContext.WriteLine("[13] Clearing shopping cart and asserting empty message...");
            await cp.ClearCartAsync();
            await cp.AssertCartEmptyAsync();
            TestContext.WriteLine("[TEST COMPLETED] E2E scenario executed successfully.");
        }
    }
}