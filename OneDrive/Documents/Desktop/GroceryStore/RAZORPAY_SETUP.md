# Razorpay Payment Integration Setup Guide

## Overview
This guide explains how to set up and configure Razorpay payment integration for the GroceryStore application.

## Prerequisites
- Razorpay account (free at https://razorpay.com)
- Active internet connection
- Admin access to your Razorpay dashboard

## Step 1: Create Razorpay Account
1. Go to https://razorpay.com and sign up for a free account
2. Complete email verification and basic account setup
3. Go to your Razorpay Dashboard

## Step 2: Get API Credentials
1. Navigate to **Settings** → **API Keys**
2. You'll see two tabs: **Test Mode** and **Live Mode**
3. For development, use **Test Mode** credentials
4. Copy your **Key ID** and **Key Secret**
   - Key ID looks like: `rzp_test_xxxxxxxxx`
   - Key Secret looks like: `xxxxxxxxxxxxxxx`

## Step 3: Configure Application Settings
Update your `appsettings.json` file with your Razorpay credentials:

```json
{
  "Razorpay": {
    "KeyId": "YOUR_TEST_KEY_ID_HERE",
    "KeySecret": "YOUR_TEST_KEY_SECRET_HERE",
    "SuccessUrl": "/Order/PaymentSuccess",
    "FailureUrl": "/Order/PaymentFailure",
    "WebhookSecret": "YOUR_WEBHOOK_SECRET_HERE"
  }
}
```

### For Production:
1. In Razorpay Dashboard, switch to **Live Mode**
2. Copy your **Live Mode** credentials
3. Update `appsettings.json` with live credentials
4. Ensure SSL/HTTPS is enabled on your website

## Step 4: Test Payment Integration

### Test Payment Details:
The following test cards can be used for testing:

**Success Card (Visa):**
- Card Number: 4111111111111111
- Expiry: Any future date (e.g., 12/25)
- CVV: Any 3-digit number (e.g., 123)

**Failed Card (Visa):**
- Card Number: 4000000000000002
- Expiry: Any future date
- CVV: Any 3-digit number

**UPI Testing:**
- Use any UPI ID for testing (e.g., success@razorpay)

## Step 5: Test the Flow

1. **Start Application:** Run your GroceryStore application
2. **Add Products to Cart:** Browse and add items
3. **Proceed to Checkout:** Go to checkout page
4. **Select Payment Method:** Choose UPI, Card, or Net Banking
5. **Complete Payment:** Use test credentials above
6. **Verify:** Check if payment shows as completed

## Features Implemented

✅ **Payment Methods Supported:**
- UPI (PhonePe, Google Pay, BHIM, etc.)
- Credit/Debit Cards
- Net Banking
- Digital Wallets
- Cash on Delivery (Non-Razorpay)

✅ **Security Features:**
- Signature verification
- Secure payment gateway integration
- Transaction ID tracking
- Payment status updates

✅ **User Experience:**
- Beautiful payment gateway page
- Success/Failure notifications
- Order tracking
- Payment retry options
- Email/SMS notifications (optional)

## Database Modifications

The following fields are updated on successful payment:
- `Payment.Status` → "Completed"
- `Payment.TransactionId` → Razorpay Payment ID
- `Payment.PaidAt` → Payment timestamp
- `Order.Status` → "Confirmed"

## Troubleshooting

### Issue: "Invalid API Key"
**Solution:** Check if your Key ID and Key Secret are correct and copied completely without spaces.

### Issue: "Payment Gateway Error"
**Solution:** Ensure your internet connection is stable and Razorpay service is accessible.

### Issue: "Signature Verification Failed"
**Solution:** This usually means the payment was tampered. Check your Key Secret is correct.

### Issue: Payments showing as "Pending" instead of "Completed"
**Solution:** Check the application logs for payment verification errors.

## Webhook Setup (Optional - for Real-time Updates)

For production, set up webhooks for real-time payment notifications:

1. Go to **Razorpay Dashboard** → **Settings** → **Webhooks**
2. Add Webhook URL: `https://yourdomaincom/api/razorpay/webhook`
3. Select events: `payment.authorized`, `payment.failed`, `payment.captured`
4. Copy your Webhook Secret and add to `appsettings.json`

## Going Live

Before going live with real payments:
1. Complete Razorpay's identity verification
2. Switch to **Live Mode** in Razorpay Dashboard
3. Update all credentials to Live Mode
4. Ensure HTTPS is enabled
5. Test with live cards (small amounts first)
6. Enable email notifications for customers
7. Set up proper logging and monitoring

## Support

- **Razorpay Support:** https://support.razorpay.com
- **Razorpay Documentation:** https://razorpay.com/docs/
- **API Reference:** https://razorpay.com/docs/api/

## Files Modified/Created

- ✅ `Services/RazorpayService.cs` - Razorpay integration service
- ✅ `Controllers/OrderController.cs` - Payment handling endpoints
- ✅ `ViewModels/PaymentGatewayViewModel.cs` - Payment page model
- ✅ `Views/Order/PaymentGateway.cshtml` - Payment gateway page
- ✅ `Views/Order/PaymentSuccess.cshtml` - Success page
- ✅ `Views/Order/PaymentFailure.cshtml` - Failure page
- ✅ `appsettings.json` - Configuration
- ✅ `Program.cs` - Service registration

## Architecture Overview

```
User Selects Payment Method
    ↓
Order Created in Database
    ↓
Online Payment? → YES → Create Razorpay Order
                          ↓
                    Redirect to Payment Gateway
                          ↓
                    User Pays via Razorpay
                          ↓
                    Razorpay Returns to App
                          ↓
                    Verify Signature
                          ↓
                    Update Payment Status
                          ↓
                    Show Success/Failure Page
    ↓
NO (Cash on Delivery) → Order Confirmed Immediately
```

## Next Steps

1. Get your Razorpay credentials
2. Update `appsettings.json`
3. Test with test credentials
4. Deploy to production when ready
