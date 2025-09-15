import { test, expect } from '@playwright/test';

test('homepage loads successfully', async ({ page }) => {
  await page.goto('/');
  
  // Wait for redirect to onboarding page
  await page.waitForURL('**/onboarding');
  
  // Check if the page title contains expected text
  await expect(page).toHaveTitle(/Get Started - Financial Aid Assistant/i);
  
  // Check if the onboarding chat interface is present
  await expect(page.locator('#chat-container')).toBeVisible();
  
  // Check if the main content area is present
  await expect(page.locator('main')).toBeVisible();
});

test('onboarding chat interface elements are present', async ({ page }) => {
  await page.goto('/');
  
  // Wait for redirect to onboarding
  await page.waitForURL('**/onboarding');
  
  // Check if chat interface elements are present and functional
  const chatInput = page.locator('input[placeholder*="Type your answer"]');
  const sendButton = page.locator('button:has(i.fa-paper-plane)');
  
  await expect(chatInput).toBeVisible();
  await expect(sendButton).toBeVisible();
  await expect(chatInput).toBeEditable();
  
  // Verify initial AI message is present
  await expect(page.locator('.chat-bubble-ai')).toBeVisible();
  await expect(page.locator('p.font-semibold:has-text("FinBot")')).toBeVisible();
  
  // Test input functionality
  await chatInput.fill('Test input');
  await expect(chatInput).toHaveValue('Test input');
});

test('page is responsive', async ({ page }) => {
  await page.goto('/');
  
  // Test mobile viewport
  await page.setViewportSize({ width: 375, height: 667 });
  await page.waitForTimeout(1000);
  
  // Ensure page still loads properly on mobile
  await expect(page.locator('body')).toBeVisible();
  
  // Test desktop viewport
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.waitForTimeout(1000);
  
  await expect(page.locator('body')).toBeVisible();
});