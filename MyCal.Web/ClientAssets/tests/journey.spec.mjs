import { test, expect } from "@playwright/test";
import { readFile, mkdir } from "node:fs/promises";
import { fileURLToPath } from "node:url";
const baseURL = process.env.MYCAL_E2E_URL || "http://localhost:5099";
const password = "Browser-Local-Only-2026!";
const credentials =
  process.env.MYCAL_E2E_EMAIL && process.env.MYCAL_E2E_PASSWORD
    ? ""
    : await readFile(
        new URL("../../../.local/admin-credentials.txt", import.meta.url),
        "utf8",
      );
const adminEmail =
  process.env.MYCAL_E2E_EMAIL || credentials.match(/^Email: (.+)$/m)[1];
const adminPassword =
  process.env.MYCAL_E2E_PASSWORD || credentials.match(/^Password: (.+)$/m)[1];
async function signIn(page, email, secret) {
  await page.goto("/account/login");
  await page.getByLabel("Email address").fill(email);
  await page.getByLabel("Password", { exact: true }).fill(secret);
  await page.getByRole("button", { name: /Sign in/ }).click();
  await expect(
    page.getByRole("navigation", { name: "Main navigation" }),
  ).toBeVisible();
}
async function member(browser, testInfo) {
  const admin = await browser.newContext({ baseURL });
  const adminPage = await admin.newPage();
  await signIn(adminPage, adminEmail, adminPassword);
  await adminPage.goto("/admin");
  const email = `mycal-e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.invalid`;
  await adminPage.getByLabel("Email address").fill(email);
  await adminPage
    .getByRole("button", { name: "Create single-use link" })
    .click();
  const link = adminPage.locator(".link-output");
  await expect(link).toBeVisible();
  const invitation = await link.inputValue();
  await admin.close();
  const { viewport, userAgent, deviceScaleFactor, isMobile, hasTouch } =
    testInfo.project.use;
  const context = await browser.newContext({
    baseURL,
    viewport,
    userAgent,
    deviceScaleFactor,
    isMobile,
    hasTouch,
  });
  const page = await context.newPage();
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.goto(invitation);
  await page.getByLabel("New password").fill(password);
  await page.getByLabel("Confirm password").fill(password);
  await page.getByRole("button", { name: "Set password" }).click();
  await expect(page.getByText("Your password is set.")).toBeVisible();
  await page.getByRole("link", { name: /Continue to sign in/ }).click();
  await signIn(page, email, password);
  await expect(page).toHaveURL(/\/settings$/);
  await page.getByLabel("Name", { exact: true }).fill("Browser Tester");
  await page.getByRole("button", { name: "Create my starting plan" }).click();
  await expect(
    page.getByText("Profile saved. Your diary is ready."),
  ).toBeVisible();
  await page.getByRole("link", { name: "Today", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Daily energy" }),
  ).toBeVisible();
  return { context, page, errors };
}
async function screenshot(page, testInfo, theme) {
  const directory = new URL("../../../.local/screenshots/", import.meta.url);
  await mkdir(directory, { recursive: true });
  await page.screenshot({
    path: fileURLToPath(
      new URL(`mycal-${testInfo.project.name}-${theme}.png`, directory),
    ),
    fullPage: true,
  });
}
test("daily food can be saved, edited, copied, completed, and revisited", async ({
  browser,
}, testInfo) => {
  const { context, page, errors } = await member(browser, testInfo);
  try {
    await page.getByRole("button", { name: "Add food", exact: true }).click();
    await page.getByRole("tab", { name: "Custom", exact: true }).click();
    await page
      .getByLabel("Food name", { exact: true })
      .fill("Browser test oats");
    await page.getByLabel("Calories (kcal)", { exact: true }).fill("200");
    await page.getByLabel("Protein (g)", { exact: true }).fill("10");
    await page.getByLabel("Carbohydrate (g)", { exact: true }).fill("20");
    await page.getByLabel("Fat (g)", { exact: true }).fill("5");
    await page.getByRole("button", { name: /Preview portion/ }).click();
    await page.getByLabel("Amount (g)", { exact: true }).fill("75.5");
    await page
      .getByRole("combobox", { name: "Meal", exact: true })
      .selectOption("Lunch");
    await page.getByRole("button", { name: /Favourite/ }).click();
    await expect(page.getByRole("button", { name: "★ Saved" })).toBeVisible();
    await page
      .getByRole("button", { name: "Add to diary", exact: true })
      .click();
    await expect(page.getByRole("dialog")).not.toBeVisible();
    await expect(page.locator(".energy-value")).toContainText("151");
    await page.getByRole("button", { name: "Edit", exact: true }).click();
    await page.getByLabel("Quantity", { exact: true }).fill("50");
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.locator(".energy-value")).toContainText("100");
    await page.getByLabel("Day completion status").selectOption("Complete");
    await expect(
      page.getByText("All food logged · day complete"),
    ).toBeVisible();
    const date = await page
      .getByLabel("Diary date", { exact: true })
      .inputValue();
    const previous = new Date(`${date}T12:00:00Z`);
    previous.setUTCDate(previous.getUTCDate() - 1);
    const previousDate = previous.toISOString().slice(0, 10);
    await page.getByText("Copy this day or meal", { exact: true }).click();
    await page
      .getByLabel("Destination date", { exact: true })
      .fill(previousDate);
    await page
      .getByRole("button", { name: "Copy entries", exact: true })
      .click();
    await expect(page.getByText(/Entries copied to/)).toBeVisible();
    await page.getByLabel("Diary date", { exact: true }).fill(previousDate);
    await expect(page.locator(".energy-value")).toContainText("100");
    await page.getByRole("link", { name: "History", exact: true }).click();
    await expect(
      page.getByRole("heading", { name: "Every day adds up." }),
    ).toBeVisible();
    await expect(page.locator(".calendar-day.is-complete")).toHaveCount(1);
    await page.getByRole("link", { name: "Progress", exact: true }).click();
    await page.getByLabel("Weight (kg)", { exact: true }).fill("74.8");
    await page
      .getByRole("button", { name: "Save weigh-in", exact: true })
      .click();
    await expect(
      page.getByText("Weigh-in saved.", { exact: false }),
    ).toBeVisible();
    await page.getByRole("button", { name: /Review my last 28 days/ }).click();
    await expect(
      page
        .locator(".review-result")
        .getByText(
          "Keep your calorie target unchanged for 28 complete days before an adjustment.",
        ),
    ).toBeVisible();
    await page.getByRole("link", { name: "Today", exact: true }).click();
    await expect(page.locator(".energy-value")).toContainText("100");
    await screenshot(page, testInfo, "light");
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    expect(errors).toEqual([]);
  } finally {
    await context.close();
  }
});
test("future targets preserve today and both themes remain usable", async ({
  browser,
}, testInfo) => {
  const { context, page, errors } = await member(browser, testInfo);
  try {
    const original = await page.locator(".energy-ring strong").innerText();
    await page.getByRole("link", { name: "Settings", exact: true }).click();
    await page.getByLabel("Calories (kcal)", { exact: true }).fill("2200");
    await page.getByLabel("Protein (g)", { exact: true }).fill("150");
    await page.getByLabel("Fat (g)", { exact: true }).fill("60");
    await page.getByLabel("Carbohydrate (g)", { exact: true }).fill("265");
    await page
      .getByRole("button", { name: "Schedule targets for tomorrow" })
      .click();
    await expect(
      page.getByText(/Your new targets are scheduled for tomorrow/),
    ).toBeVisible();
    await page
      .getByRole("combobox", { name: "Appearance", exact: true })
      .selectOption("dark");
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.getByRole("link", { name: "Today", exact: true }).click();
    await expect(page.locator(".energy-ring strong")).toHaveText(original);
    await screenshot(page, testInfo, "dark");
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.getByRole("link", { name: "Settings", exact: true }).click();
    await page
      .getByRole("combobox", { name: "Appearance", exact: true })
      .selectOption("light");
    await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
    expect(errors).toEqual([]);
  } finally {
    await context.close();
  }
});
test("diet presets reload correctly and manual edits retain today's targets", async ({
  browser,
}, testInfo) => {
  const { context, page, errors } = await member(browser, testInfo);
  try {
    const original = await page.locator(".energy-ring strong").innerText();
    await page.getByRole("link", { name: "Settings", exact: true }).click();
    await page
      .getByRole("combobox", { name: "Training focus", exact: true })
      .selectOption("ResistanceTraining");
    await page
      .getByRole("combobox", { name: "Protein preference", exact: true })
      .selectOption("Higher");
    await page
      .getByRole("combobox", { name: "Diet preference", exact: true })
      .selectOption("LowerFat");
    await page.getByLabel("Calories (kcal)", { exact: true }).fill("2000");
    await page
      .getByRole("button", { name: "Apply diet preferences", exact: true })
      .click();
    await expect(page.getByLabel("Protein (g)", { exact: true })).toHaveValue(
      /^165(?:\.0+)?$/,
    );
    await expect(
      page.getByLabel("Carbohydrate (g)", { exact: true }),
    ).toHaveValue(/^235(?:\.0+)?$/);
    await expect(page.getByLabel("Fat (g)", { exact: true })).toHaveValue(
      "44.4",
    );
    await page
      .getByRole("button", { name: "Schedule targets for tomorrow" })
      .click();
    await expect(
      page.getByText(/Your new targets are scheduled for tomorrow/),
    ).toBeVisible();
    await page.reload();
    await expect(
      page.getByRole("combobox", { name: "Training focus", exact: true }),
    ).toHaveValue("ResistanceTraining");
    await expect(
      page.getByRole("combobox", { name: "Protein preference", exact: true }),
    ).toHaveValue("Higher");
    await expect(
      page.getByRole("combobox", { name: "Diet preference", exact: true }),
    ).toHaveValue("LowerFat");
    await screenshot(page, testInfo, "diet-light");
    await page
      .getByRole("combobox", { name: "Appearance", exact: true })
      .selectOption("dark");
    await screenshot(page, testInfo, "diet-dark");
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
    await page.getByRole("link", { name: "Today", exact: true }).click();
    await expect(page.locator(".energy-ring strong")).toHaveText(original);
    await page.getByRole("link", { name: "Settings", exact: true }).click();
    await page.getByLabel("Fat (g)", { exact: true }).fill("50");
    await page
      .getByRole("button", { name: "Fill remaining calories with carbs" })
      .click();
    await expect(
      page.getByRole("combobox", { name: "Diet preference", exact: true }),
    ).toHaveValue("Custom");
    await expect(
      page.getByLabel("Carbohydrate (g)", { exact: true }),
    ).toHaveValue("222.5");
    await page
      .getByRole("button", { name: "Schedule targets for tomorrow" })
      .click();
    await expect(
      page.getByText(/Your new targets are scheduled for tomorrow/),
    ).toBeVisible();
    await page.reload();
    await expect(
      page.getByRole("combobox", { name: "Diet preference", exact: true }),
    ).toHaveValue("Custom");
    await expect(page.getByLabel("Protein (g)", { exact: true })).toHaveValue(
      /^165(?:\.0+)?$/,
    );
    expect(errors).toEqual([]);
  } finally {
    await context.close();
  }
});

test("denied camera permission and invalid barcodes offer a working fallback", async ({
  browser,
}, testInfo) => {
  const { context, page, errors } = await member(browser, testInfo);
  try {
    await page.evaluate(() => {
      Object.defineProperty(navigator.mediaDevices, "getUserMedia", {
        configurable: true,
        value: async () => {
          throw new DOMException("Permission denied", "NotAllowedError");
        },
      });
    });
    await page.getByRole("button", { name: "Add food", exact: true }).click();
    await page.getByRole("tab", { name: "Barcode", exact: true }).click();
    await page
      .getByRole("button", { name: "Open camera", exact: true })
      .click();
    await expect(page.getByText(/The camera couldn't open/)).toBeVisible();
    await page.getByLabel("Barcode digits", { exact: true }).fill("123");
    await page.getByRole("button", { name: "Look up", exact: true }).click();
    await expect(
      page.getByText(/Enter a valid 8, 12, or 13 digit/),
    ).toBeVisible();
    await page.getByRole("tab", { name: "Custom", exact: true }).click();
    await expect(page.getByLabel("Food name", { exact: true })).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).not.toBeVisible();
    expect(errors).toEqual([]);
  } finally {
    await context.close();
  }
});

test("the bundled decoder reads an EAN-13 fixture without a camera", async ({
  page,
}) => {
  await page.goto("/account/login");
  const decoded = await page.evaluate(async () => {
    await import("/vendor/zxing-browser.min.js");
    const code = "5000159484695";
    const left = [
      "0001101",
      "0011001",
      "0010011",
      "0111101",
      "0100011",
      "0110001",
      "0101111",
      "0111011",
      "0110111",
      "0001011",
    ];
    const alternate = [
      "0100111",
      "0110011",
      "0011011",
      "0100001",
      "0011101",
      "0111001",
      "0000101",
      "0010001",
      "0001001",
      "0010111",
    ];
    const parity = [
      "LLLLLL",
      "LLGLGG",
      "LLGGLG",
      "LLGGGL",
      "LGLLGG",
      "LGGLLG",
      "LGGGLL",
      "LGLGLG",
      "LGLGGL",
      "LGGLGL",
    ][Number(code[0])];
    let bits = "101";
    for (let i = 1; i <= 6; i++)
      bits += (parity[i - 1] === "L" ? left : alternate)[Number(code[i])];
    bits += "01010";
    for (let i = 7; i <= 12; i++)
      bits += [...left[Number(code[i])]]
        .map((bit) => (bit === "0" ? "1" : "0"))
        .join("");
    bits += "101";
    const canvas = document.createElement("canvas");
    canvas.width = (bits.length + 24) * 3;
    canvas.height = 160;
    const drawing = canvas.getContext("2d");
    drawing.fillStyle = "#fff";
    drawing.fillRect(0, 0, canvas.width, canvas.height);
    drawing.fillStyle = "#000";
    [...bits].forEach((bit, i) => {
      if (bit === "1") drawing.fillRect((i + 12) * 3, 10, 3, 140);
    });
    const reader = new globalThis.ZXingBrowser.BrowserMultiFormatOneDReader();
    return (
      await reader.decodeFromImageUrl(canvas.toDataURL("image/png"))
    ).getText();
  });
  expect(decoded).toBe("5000159484695");
});
