# Denly Visuals Backlog (Codex)

This backlog is for the visual system and UI styling only. It is intentionally separated from product features and data work so Codex can own the visual layer without touching services or business logic.

**Scope:** CSS tokens, component styling, layout composition, icon usage, animation, and light/dark theme visuals.  
**Out of scope:** Data services, Supabase calls, auth logic, or feature behavior changes.

General constraints:
- Prefer CSS isolation (`.razor.css`) for component styling.
- Use tokens from `wwwroot/app.css` (no raw hex values in component CSS).
- Keep animations subtle and honor `prefers-reduced-motion`.

---

## P1: Foundations

### V1. Design Tokens + Theme Scaffold
**Source:** Design feedback | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** Visual tokens are partially defined and not consistently applied.

**Solution:**
- Expand and normalize tokens (colors, shadows, spacing, typography scale, focus ring).
- Add light/dark theme variables with `data-theme` support.
- Set global base styles for body, headings, and default text.

**Targets:**
- `wwwroot/app.css`

#### Delegation Prompt (Codex)
Implement token updates and base styles in `wwwroot/app.css`. Do not change component logic. Keep colors/values aligned with `docs/VISUAL-DESIGN.md`.

---

### V2. Typography Scale + Utility Classes
**Source:** Design feedback | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** Typography sizes/weights are inconsistent across pages.

**Solution:**
- Add a small type scale and utility classes (e.g., `.text-sm`, `.text-lg`, `.text-muted`).
- Standardize heading styles and paragraph spacing.

**Targets:**
- `wwwroot/app.css`

#### Delegation Prompt (Codex)
Add a minimal set of text utility classes and update global heading styles. Do not re-layout pages yet.

---

## P1: Core Components

### V3. Nook Container Component
**Source:** Visual design system | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** The core "Nook" container does not exist as a reusable component.

**Solution:**
- Create a reusable Nook component with a title slot.
- Use inset shadow and rounded corners from tokens.

**Targets:**
- `Components/Shared/Nook.razor` (new)
- `Components/Shared/Nook.razor.css` (new)

#### Delegation Prompt (Codex)
Create the Nook component per `docs/VISUAL-DESIGN.md` with CSS isolation. No functional changes beyond layout and styling.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `Components/Shared/Nook.razor`
  - `Components/Shared/Nook.razor.css`
- **Summary:** Added reusable Nook container with title slot and token-based styling.

---

### V4. Pebble Button Component
**Source:** Visual design system | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** Primary/secondary button styles are inconsistent.

**Solution:**
- Add a `PebbleButton` component with primary/secondary/disabled states.
- Add press "squish" interaction and subtle hover style.

**Targets:**
- `Components/Shared/PebbleButton.razor` (new)
- `Components/Shared/PebbleButton.razor.css` (new)

#### Delegation Prompt (Codex)
Build a button component with variants and a pressed state using tokens. Do not change navigation or click handlers.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `Components/Shared/PebbleButton.razor`
  - `Components/Shared/PebbleButton.razor.css`
- **Summary:** Added a reusable Pebble button component with primary/secondary styles and squish interaction using design tokens.

---

### V5. Input Field + Validation States
**Source:** Visual design system | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** Inputs lack a consistent visual style and validation feedback.

**Solution:**
- Define a shared input style (pill/soft rectangle).
- Add focus, error, and recovery-success states.

**Targets:**
- `wwwroot/app.css`
- `Components/Pages/Login.razor.css`
- `Components/Pages/Signup.razor.css`
- `Components/Pages/JoinDen.razor.css`
- `Components/Pages/CreateDen.razor.css`

#### Delegation Prompt (Codex)
Add shared input styles and update auth/den forms to use them. Keep markup changes minimal; no logic changes.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `wwwroot/app.css`
  - `Components/Pages/Login.razor`
  - `Components/Pages/Login.razor.css`
  - `Components/Pages/Signup.razor`
  - `Components/Pages/Signup.razor.css`
  - `Components/Pages/JoinDen.razor`
  - `Components/Pages/JoinDen.razor.css`
  - `Components/Pages/CreateDen.razor`
  - `Components/Pages/CreateDen.razor.css`
- **Summary:** Added shared input styles in app.css and applied the `den-input` class across auth/den forms; simplified per-page input overrides.

---

### V6. Toggle Switch + Alert Dot
**Source:** Visual design system | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Awaiting Review

**Problem:** Toggles and alert indicators vary across screens.

**Solution:**
- Implement a consistent toggle switch style.
- Add a small alert-dot pattern for notifications.

**Targets:**
- `wwwroot/app.css`
- `Components/Pages/Settings.razor.css`

#### Delegation Prompt (Codex)
Create shared CSS patterns for toggles and alert dots; apply them in Settings.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `wwwroot/app.css`
  - `Components/Pages/Settings.razor`
  - `Components/Pages/Settings.razor.css`
- **Summary:** Added shared toggle + alert-dot styles and applied them to Settings (notifications row + members alert dot).

---

## P1: Navigation + Signature Components

### V7. Frosted Bottom Navigation
**Source:** Visual design system | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Ready

**Problem:** Bottom navigation lacks the frosted/glass style shown in mockups.

**Solution:**
- Restyle bottom navigation with a translucent surface, rounded container, and glow state for active tab.
- Provide a fallback for WebViews without `backdrop-filter`.

**Targets:**
- `Components/Layout/NavMenu.razor.css`
- `wwwroot/app.css`

#### Delegation Prompt (Codex)
Update nav styling only. Do not change routing or menu structure.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `Components/Layout/NavMenu.razor.css`
- **Summary:** Restyled bottom nav with frosted glass surface, rounded container, active glow, and a fallback for non-supporting WebViews.

---

### V8. Organic "Vine" Progress Component
**Source:** Visual design system | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Ready

**Problem:** Progress visualization is generic and not on-brand.

**Solution:**
- Build a reusable progress component with an SVG arc and leaf indicator.
- Expose progress percent and label.

**Targets:**
- `Components/Shared/VineProgress.razor` (new)
- `Components/Shared/VineProgress.razor.css` (new)
- `wwwroot/app.css`

#### Delegation Prompt (Codex)
Implement the component visually; keep the API simple (value + label). No data wiring needed.

#### Completion Report
- **Status:** Awaiting Review
- **Files Modified:**
  - `Components/Shared/VineProgress.razor`
  - `Components/Shared/VineProgress.razor.css`
- **Summary:** Added a flat, modern vine progress component with SVG arc, percent label, and gold leaf accent aligned to the HomePage1 reference.

---

## P1: Screen Visual Passes

### V9. Home Screen Visual Pass
**Source:** Visual design system | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Ready

**Problem:** Home screen does not match the "Nook" layout and visual hierarchy.

**Solution:**
- Apply Nook containers, spacing scale, and CTA styling.
- Use Vine progress component and pebble buttons.

**Targets:**
- `Components/Pages/Home.razor`
- `Components/Pages/Home.razor.css`

#### Delegation Prompt (Codex)
Restyle the Home page to align with the "Modern Sanctuary" system. No new data or logic.

---

### V10. Calendar Screen Visual Pass
**Source:** Visual design system | **Effort:** High | **Risk:** Medium

> **Delegate:** Codex | **Status:** Ready

**Problem:** Calendar is functional but not aligned with the flowing-grid design.

**Solution:**
- Restyle the month grid to remove hard boxes.
- Add user filter bubbles and view toggle styling.
- Use stacked strips for multi-user events.

**Targets:**
- `Components/Pages/Calendar.razor`
- `Components/Pages/Calendar.razor.css`

#### Delegation Prompt (Codex)
Focus on styling and layout only. Keep existing calendar logic intact.

---

### V11. Settings Screen Visual Pass
**Source:** Visual design system | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Settings screen lacks the soft card and toggle styling.

**Solution:**
- Wrap sections in Nooks.
- Apply toggle and button styles.

**Targets:**
- `Components/Pages/Settings.razor`
- `Components/Pages/Settings.razor.css`

#### Delegation Prompt (Codex)
Restyle Settings using Nooks and Pebble buttons. No behavior changes.

---

### V12. Auth Screens Visual Pass (Login/Signup/Join/Create)
**Source:** Visual design system | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Codex | **Status:** Ready

**Problem:** Auth flows lack cohesive "Den" look and feel.

**Solution:**
- Apply Nook card styling, input styles, and primary button design.
- Add simple header copy styling that matches the landing mockup.

**Targets:**
- `Components/Pages/Login.razor`
- `Components/Pages/Login.razor.css`
- `Components/Pages/Signup.razor`
- `Components/Pages/Signup.razor.css`
- `Components/Pages/JoinDen.razor`
- `Components/Pages/JoinDen.razor.css`
- `Components/Pages/CreateDen.razor`
- `Components/Pages/CreateDen.razor.css`

#### Delegation Prompt (Codex)
Restyle the auth views with Nook containers and Pebble buttons. Keep the form fields and submit logic unchanged.

---

## P2: Quality and Consistency

### V13. Accessibility and Contrast Audit
**Source:** Design feedback | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Some text colors may fall below contrast guidelines on warm backgrounds.

**Solution:**
- Check contrast and adjust token values or text usage as needed.
- Add focus-ring styles and ensure tap targets meet minimum size.

**Targets:**
- `wwwroot/app.css`
- Component `.razor.css` files touched in prior items

#### Delegation Prompt (Codex)
Adjust styles to improve contrast and focus visibility. Do not change layout or data logic.
