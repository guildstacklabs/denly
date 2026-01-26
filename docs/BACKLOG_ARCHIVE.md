# Denly Backlog Archive

Completed items moved here after code review. See `BACKLOG.md` for active work.

---

## Completed Items

### P0-1: App Icon + Splash Screen
**Completed:** Pre-2026-01-21

Basic app branding with Coral Reef palette.

---

### P0-3: Supabase Auth Setup
**Completed:** 2026-01-21

Email/password + Google auth integration with Supabase.

---

### P0-4: Cloud Sync
**Completed:** 2026-01-21

Sync calendar, expenses, vault between co-parents via Supabase.

---

### P0-5: Onboarding Flow
**Completed:** 2026-01-21

First-run experience and invite co-parent flow.

---

### P0-7: App Color Scheme
**Completed:** 2026-01-21 | **Agent:** Claude

Implemented "Animal Den" design system from VISUAL-DESIGN.md.

**Files Modified:**
- `wwwroot/index.html` - Added Google Fonts (Nunito + Inter)
- `wwwroot/app.css` - Design system variables and base styles
- `Components/Layout/MainLayout.razor.css` - Font heading
- `Components/Layout/NavMenu.razor.css` - Font body

**Key Changes:**
- Teal (#3D8B8B) = primary action color
- Coral (#E07A5F) = alerts/urgent items only
- Nunito headings, Inter body text
- Nook containers (24px radius, inner shadows)
- Pebble buttons (pill-shaped, squish animation)

---

### P0-8: Unit Test Foundation
**Completed:** 2026-01-21 | **Agent:** Claude

Created unit test foundation with 29 passing tests.

**Files Modified:**
- `Denly.Tests/Denly.Tests.csproj` - xUnit + NSubstitute
- `Denly.Tests/Services/BalanceCalculatorTests.cs` - 14 tests
- `Denly.Tests/Services/DenServiceBehaviorTests.cs` - 6 tests
- `Denly.Tests/Services/AuthServiceBehaviorTests.cs` - 9 tests
- `Services/BalanceCalculator.cs` - Pure business logic
- `Denly.csproj` - Test project exclusion
- `Denly.sln` - Added test project

**Test Coverage:**
- Balance calculations (50/50, 60/40, 70/30 splits)
- Den guardrails (empty results when no den)
- Auth session restore (no-throw on missing storage)

---

### P0-9: Scheduler AM/PM Bug Fix
**Completed:** 2026-01-21 | **Agent:** Codex | **Reviewed by:** Claude

Fixed AM/PM persistence when saving/editing calendar events.

**Files Modified:**
- `Components/Pages/Calendar.razor` - Added ConvertTo12Hour/ConvertTo24Hour helpers
- `Services/SupabaseScheduleService.cs` - NormalizeDateTime for proper timezone handling

**Key Changes:**
- 12:00 AM correctly saves as 00:00 UTC
- 12:00 PM correctly saves as 12:00 UTC
- Edit mode populates correct AM/PM from stored time
- Events with Kind=Unspecified treated as local (no double conversion)

---

### P0-10: Calendar Time Edit Not Saving
**Completed:** 2026-01-22 | **Agent:** Codex | **Reviewed by:** Claude

Fixed time persistence when editing existing calendar events.

**Files Modified:**
- `Components/Pages/Calendar.razor` - GetLocalStartTime() helper normalizes UTC to local before edit
- `Services/SupabaseScheduleService.cs` - Use JValue.CreateNull() for nullable EndsAt in updates

**Key Changes:**
- Edit modal now correctly shows existing event time in local timezone
- Supabase update sends proper null token for EndsAt instead of omitting field
- Time picker correctly populates hour/minute/AM-PM from stored event

---

### P0-11: Date Picker Overflow
**Completed:** 2026-01-22 | **Agent:** Codex | **Reviewed by:** Claude

Fixed date picker overflowing modal container.

**Files Modified:**
- `Components/Pages/Calendar.razor.css` - Layered date input approach with overflow containment

**Key Changes:**
- Overlay technique with date-display and date-input-native for consistent styling
- Modal content has overflow-x: hidden to prevent horizontal scroll
- Form groups use min-width: 0 to prevent flex overflow

---

### P0-12: Time Picker Validation Styling
**Completed:** 2026-01-22 | **Agent:** Codex | **Reviewed by:** Claude

Fixed green success border showing on time picker without prior error.

**Files Modified:**
- `Components/Pages/Calendar.razor` - Added _timeErrorTriggered state flag

**Key Changes:**
- Success styling only shows when HasValidTime AND _timeErrorTriggered is true
- Flag properly reset when modal opens/closes
- Green border now only appears after recovering from validation error

---

### P1-2: Exportable Reports
**Completed:** 2026-01-22 | **Agent:** Gemini | **Reviewed by:** Claude

Implemented CSV and PDF export for expense reports.

**Files Created:**
- `Services/IReportService.cs` - Interface for report generation
- `Services/ReportService.cs` - CSV and PDF implementation using SkiaSharp

**Key Changes:**
- CSV export with proper quote escaping and date formatting
- PDF export with A4 dimensions and page break handling
- Date range filtering on expense data

---

### P1-3: Info Bank Expansion
**Completed:** 2026-01-22 | **Agent:** Gemini | **Reviewed by:** Claude

Created Info Bank page for shared child information.

**Files Created:**
- `Components/Pages/InfoBank.razor` - New page with child selector and medical/school forms

**Key Changes:**
- Tab-based child selection
- Medical section: Doctor name, contact, allergies
- School/sizes section: School name, clothing size, shoe size
- Uses existing Child model properties and IDenService.UpdateChildAsync()

---

### P0-6: Push Notifications
**Completed:** 2026-01-22 | **Agent:** Claude

Implemented full push notification infrastructure with FCM (Android) and APNs (iOS) support.

**Files Created:**
- `supabase/migrations/20260122000000_create_device_tokens.sql` - Device tokens table with RLS
- `Models/DeviceToken.cs` - Supabase model for device tokens
- `Services/IPushNotificationService.cs` - Interface with NotificationType enum and NotificationPayload record
- `Services/PushNotificationService.cs` - Shared base class with token registration via Supabase
- `Platforms/Android/PushNotificationHandler.cs` - FCM implementation with FirebaseMessagingService
- `Platforms/iOS/PushNotificationHandler.cs` - APNs implementation with UNUserNotificationCenterDelegate
- `Platforms/iOS/Entitlements.plist` - Push notification entitlement
- `supabase/functions/push-notify/index.ts` - Edge Function using FCM HTTP v1 API

**Files Modified:**
- `Platforms/iOS/AppDelegate.cs` - Register for remote notifications
- `Platforms/iOS/Info.plist` - Added UIBackgroundModes with remote-notification
- `Platforms/Android/AndroidManifest.xml` - Added POST_NOTIFICATIONS, VIBRATE, RECEIVE_BOOT_COMPLETED permissions
- `MauiProgram.cs` - Platform-conditional DI registration for IPushNotificationService
- `Denly.csproj` - Added Xamarin.Firebase.Messaging and Xamarin.Google.Dagger packages

**Key Features:**
- Token registration/unregistration with Supabase device_tokens table
- Permission request APIs for Android 13+ and iOS
- Foreground notification handling with local display
- Deep linking support via NotificationTapped event
- Edge Function fetches den members, excludes triggering user, sends to all registered devices
- FCM HTTP v1 API with OAuth 2.0 service account authentication

**Notes:**
- APNs Edge Function implementation is a placeholder - full JWT signing needed for production
- Requires Firebase project setup with google-services.json
- Requires Supabase secrets: FIREBASE_PROJECT_ID, FIREBASE_SERVICE_ACCOUNT (base64 encoded)

---

### P0-7: Children Management
**Completed:** 2026-01-23 | **Agent:** Claude

Full CRUD for children with name validation, display name disambiguation, and soft-delete.

**Files Created:**
- `supabase/migrations/20260122100000_extend_children_table.sql` - Schema migration (first_name, middle_name, last_name, deactivated_at)
- `Services/IChildService.cs` - Interface with CRUD, validation, display name methods
- `Services/ChildService.cs` - Full implementation inheriting from SupabaseServiceBase
- `Components/Shared/AddEditChildModal.razor` - Reusable modal with validation
- `Components/Shared/AddEditChildModal.razor.css`
- `Components/Shared/ChildSelector.razor` - Chip-based multi-select component
- `Components/Shared/ChildSelector.razor.css`

**Files Modified:**
- `Models/Child.cs` - Name split into FirstName/MiddleName/LastName + DeactivatedAt
- `Components/Pages/CreateDen.razor` - Two-step wizard requiring at least one child
- `Components/Pages/CreateDen.razor.css` - Step 2 styles
- `Components/Pages/Settings.razor` - Added Children management section
- `Components/Pages/Settings.razor.css` - Children section styles
- `Components/Pages/InfoBank.razor` - Updated to use FirstName
- `Services/SupabaseDenService.cs` - Updated column names
- `MauiProgram.cs` - Registered IChildService
- `docs/DATABASE.md` - Updated schema documentation

**Key Features:**
- Name validation: exact match (first+middle+last) blocks, partial match (first+last) warns
- Display name disambiguation: first name only → first + middle initial → first + last initial
- Soft-delete via `deactivated_at` timestamp preserves historical records
- CreateDen flow requires at least one child before completing
- Color picker auto-selects next unused color for each child
- Settings page has active/inactive children sections with deactivate/reactivate

---
