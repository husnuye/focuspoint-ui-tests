#  WebTests – Playwright E2E Automation

## 1. 📖 Project Description
End-to-End UI Test Automation project built with **C#**, **Playwright**, **NUnit**, and **Allure Reporting**.  
It automates critical flows such as **Login**, **Search**, **Add to Cart**, and **Clear Cart**, ensuring stability and reliability of the e-commerce platform.

---

## 2. 🎥 Demo Video
  
[![Demo Video](image-3.png)](https://vimeo.com/manage/videos/1117441315)

---

## 3. 🔧 Setup

### Requirements
- .NET 9 SDK  
- Playwright for .NET (browsers must be installed once)  
- NUnit (via `dotnet test`)  
- Allure CLI (for reports)

### Installation
```bash
git clone https://github.com/your-org/FocusPointUiTests.git
cd FocusPointUiTests/WebTests

# Restore dependencies
dotnet restore

# Build (generates Playwright helper scripts)
dotnet build

# Install Playwright browsers (one-time)
# Windows PowerShell:
pwsh bin/Debug/net9.0/playwright.ps1 install
# macOS/Linux alternative:
npx playwright install

---

## 4. Playwright Browsers Installation  
If Playwright was freshly installed, make sure browsers are also installed:  

```bash
npx playwright install

---
## 5. ▶️ Running Tests
dotnet test

Run with custom settings (recommended)

We use Tests.runsettings to define:
	•	test session timeout
	•	TRX output folder
	•	environment variables (e.g., Allure results directory

dotnet test --settings ./Tests.runsettings

Run a specific test by name
dotnet test --filter "Name~FocusPoint_Full_E2E_Search_AddToCart_Clear"

---

## 6. 🧪 Allure Reporting

Install Allure CLI

On Mac (via brew):
brew install allure

On Windows
scoop install allure

Generate Report


After tests finish, Allure results are written under:
bin/Debug/net9.0/allure-results
Serve the report:
allure serve bin/Debug/net9.0/allure-results

This opens a live Allure dashboard in your browser.

Optional environment banner
You can enrich the report with environment context:

mkdir -p bin/Debug/net9.0/allure-results
cat > bin/Debug/net9.0/allure-results/environment.properties <<EOF
Browser=Chromium
Playwright=1.55.0
OS=$(sw_vers -productVersion)
BaseUrl=https://45demo.focuspointb1.com
EOF

--- 

## 7. 📂 Project Structure

WebTests/
├─ Config/
│  ├─ appsettings.json              # local config (not committed)
│  └─ appsettings.sample.json       # sample config to copy & fill
├─ Core/
│  ├─ TestBase.cs                   # test lifecycle + Allure/trace attachments
│  └─ PlaywrightFactory.cs          # browser/context/page setup
├─ Pages/
│  ├─ HomePage.cs
│  ├─ LoginPage.cs
│  ├─ SearchPage.cs
│  └─ CartPage.cs
├─ TestData/
│  └─ SearchData.xlsx               # first row headers, row 2 contains data
├─ Tests/
│  └─ FocusPointE2ETests.cs         # main E2E scenario with Allure steps
├─ Utils/
│  ├─ ExcelReader.cs                # read first data row strictly
│  └─ FileHelper.cs                 # (optional) save product info to text
├─ Tests.runsettings                # test host + env vars (Allure dir, timeouts)
└─ WebTests.csproj


## 8. ✅ Test Scenario Steps (E2E)

	1.	Navigate to Home Page
	2.	Open My Account → click Login
	3.	Enter Email and Password
	4.	Click Login button
	5.	Verify successful login (account header visible)
	6.	Read keywords and expected price from Excel (TestData/SearchData.xlsx)
	•	type test, clear, then type the second keyword (e.g., husnuye)
	7.	Open search results
	8.	Add the first product to the cart from the search results page
	9.	Open the Cart via header
	10. Assert product name contains the typed keyword
	11. Assert product unit price equals the expected price from Excel
	12. Set quantity to 2 and validate
	13. Clear the cart and verify it is empty

--- 

## 9. ⚙️ Configuration

Config/appsettings.sample.json (check in)

Provide a sample file so others can copy it to appsettings.json:
    {
  "BaseUrl": "https://45demo.focuspointb1.com/",
  "Headless": false,
  "SlowMoMs": 0,
  "Viewport": { "Width": 1440, "Height": 900 },
  "Trace": true,
  "Video": true,
  "OutputDir": "allure-results",
  "Credentials": {
    "Email": "your@mail.com",
    "Password": "********"
  },
  "SearchDataExcelPath": "TestData/SearchData.xlsx"
}

Note: Config/appsettings.json is ignored by .gitignore and should not be committed.

Tests.runsettings
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <!-- Test host session timeout (ms) -->
    <TestSessionTimeout>300000</TestSessionTimeout>
    <!-- TRX output (not Allure) -->
    <ResultsDirectory>TestResults</ResultsDirectory>
  </RunConfiguration>
  <EnvironmentVariables>
    <!-- Allure.Net.Commons reads this directory for results -->
    <ALLURE_RESULTS_DIRECTORY>./bin/Debug/net9.0/allure-results</ALLURE_RESULTS_DIRECTORY>
    <!-- Increase vstest <-> testhost connection timeout for slower machines -->
    <VSTEST_CONNECTION_TIMEOUT>300000</VSTEST_CONNECTION_TIMEOUT>
  </EnvironmentVariables>
</RunSettings>

---

## 10. 🚨 Negative Test Scenarios (Roadmap)

These validate error handling and edge cases (to be added as separate tests):
	1.	Invalid Login – Wrong Password
	•	Enter a valid email and an invalid password
	•	Assert error message is displayed
	2.	Invalid Login – Wrong Email Format
	•	Enter abc@xyz without a domain
	•	Assert validation message is displayed
	3.	Search – No Results Found
	•	Search for a random string like asdlkjasd
	•	Assert "No results found" message is shown
	4.	Cart – Add and Remove Immediately
	•	Add a product then remove it right away
	•	Assert cart is empty
	5.	Checkout Without Login (if allowed)
	•	Add as guest and try to checkout
	•	Assert the system prompts for login/registration

--- 

## 11. 🧰 CI (GitHub Actions) – Minimal Example

This job runs tests on Ubuntu, installs Playwright browsers, and uploads Allure results.

name: CI - WebTests

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore & Build
        run: |
          dotnet restore ./WebTests.csproj
          dotnet build ./WebTests.csproj --configuration Debug --no-restore

      - name: Install Playwright Browsers
        run: npx playwright install --with-deps

      - name: Run Tests (with runsettings)
        run: dotnet test ./WebTests.csproj --configuration Debug --settings ./Tests.runsettings --logger "trx;LogFileName=test.trx"

      - name: Upload Allure Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: allure-results
          path: bin/Debug/net9.0/allure-results

---

## 12. 🩺 Troubleshooting

	•	appsettings.json not found in CI
Ensure Config/appsettings.sample.json exists and your workflow copies it:
cp Config/appsettings.sample.json Config/appsettings.json


	•	No Allure results generated
Verify ALLURE_RESULTS_DIRECTORY (from Tests.runsettings) matches the path you pass to allure serve.
	•	Playwright browsers missing
Run npx playwright install (or the generated PowerShell script on Windows).
	•	Long startup or aborted runs in CI
Increase timeouts via Tests.runsettings (TestSessionTimeout, VSTEST_CONNECTION_TIMEOUT).

⸻

Happy testing! 🎯

If you want, I can also add a **short badge row** (build status, Playwright, Allure) at the top—purely cosmetic and safe.
