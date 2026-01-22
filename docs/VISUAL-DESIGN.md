# Animal Den Design System & Component Library

## 1. Design Philosophy: "Modern Sanctuary"

The interface is designed to mimic a natural shelter—rounded, soft, and warm. It avoids the harshness of digital screens by using paper-like tones and organic shapes.

* **Organic Minimalism:** Soft over sharp. Rounded corners (`24px+`).
* **The "Nook" Concept:** Content does not float on top; it is carved *into* the background using inner shadows.
* **Tactility:** Buttons feel like "Pebbles" (pill-shaped, squishy interaction).
* **User differentiation:** In multi-user contexts (Calendar), specific earthy tones distinguish different users.

---

## 2. Color Palette & Typography

### Base Colors

| Role | Name | Hex | Usage |
| --- | --- | --- | --- |
| **Canvas** | Warm Background | `#FFF9F0` | Main application background (Walls). |
| **Text** | Den Shadow | `#3E424B` | Primary text, headers, and icons. (No black). |

### Functional Colors

| Role | Name | Hex | Usage |
| --- | --- | --- | --- |
| **Action** | Teal | `#3D8B8B` | Primary buttons (Pebbles), active states. |
| **Secondary** | Seafoam | `#81B29A` | Backgrounds, tracks, multi-day spans. |
| **Highlight** | Gold | `#F2CC8F` | Progress indicators, bioluminescent glows, progress leaves. |
| **Selection** | Den Shadow | `#3E424B` | Active/Selected date background. |
| **Alert** | Coral | `#E07A5F` | Notification dots, urgent items. |

### Calendar User Assignments

* **User 1:** Teal (`#3D8B8B`)
* **User 2:** Coral (`#E07A5F`)
* **User 3:** Seafoam (`#81B29A`)
* **User 4:** Burnt Sienna (`#D4A373`)
* **User 5:** Lavender Grey (`#A9AABC`)

### Typography

* **Headings:** `Nunito` (Bold 700 / SemiBold 600)
* **Body:** `Inter` (Regular 400 / Medium 500)

---

## 3. Global CSS Variables

Add this to `wwwroot/app.css` (or a shared CSS file imported there). Use these tokens everywhere; avoid raw hex values in component CSS.

```css
:root {
    /* --- Palette --- */
    --color-warm-bg: #FFF9F0;
    --color-den-shadow: #3E424B;
    --color-teal: #3D8B8B;
    --color-seafoam: #81B29A;
    --color-gold: #F2CC8F;
    --color-coral: #E07A5F;
    --color-nook-bg: #FDFCF8;
    --color-border-soft: rgba(62, 66, 75, 0.08);
    --color-surface-glass: rgba(253, 252, 248, 0.7);
    
    /* --- Extended Earth Tones (For Users) --- */
    --color-user-sienna: #D4A373;
    --color-user-lavender: #A9AABC;

    /* --- Typography --- */
    --font-heading: 'Nunito', sans-serif;
    --font-body: 'Inter', sans-serif;
    --text-xs: 0.75rem;
    --text-sm: 0.875rem;
    --text-md: 1rem;
    --text-lg: 1.125rem;
    --text-xl: 1.5rem;
    --text-2xl: 2rem;

    /* --- Dimensions --- */
    --radius-nook: 24px;
    --radius-pebble: 999px; /* Pill shape */
    --spacing-unit: 8px;
    --tap-min: 44px;

    /* --- Shadows & Motion --- */
    --shadow-nook: inset 0 2px 6px rgba(242, 204, 143, 0.15), 0 4px 10px rgba(62, 66, 75, 0.05);
    --shadow-pebble: 0 4px 12px rgba(61, 139, 139, 0.3);
    --shadow-glow: 0 0 16px rgba(242, 204, 143, 0.45);
    --motion-fast: 120ms;
    --motion-base: 180ms;
    --motion-slow: 240ms;
    --ease-out: cubic-bezier(0.2, 0.8, 0.2, 1);
    --focus-ring: 0 0 0 3px rgba(61, 139, 139, 0.35);
}

body {
    background-color: var(--color-warm-bg);
    color: var(--color-den-shadow);
    font-family: var(--font-body);
    line-height: 1.4;
}

h1, h2, h3, h4 {
    font-family: var(--font-heading);
    color: var(--color-den-shadow);
}

```

---

## 4. Blazor Component Library

### 4.1. Core Component: The "Nook"

The standard container for the app. It uses an inset shadow to appear indented.

**`Shared/Components/Nook.razor`**

```csharp
<div class="nook @CssClass">
    @if (!string.IsNullOrEmpty(Title))
    {
        <h3 class="nook-header">@Title</h3>
    }
    @ChildContent
</div>

@code {
    [Parameter] public string Title { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public string CssClass { get; set; } = "";
}

```

**`Shared/Components/Nook.razor.css`**

```css
.nook {
    background-color: var(--color-nook-bg);
    border-radius: var(--radius-nook);
    padding: 24px;
    margin-bottom: 20px;
    /* The "Indented" Effect */
    box-shadow: var(--shadow-nook);
    border: 1px solid var(--color-border-soft);
}

.nook-header {
    margin-top: 0;
    font-size: 1.25rem;
    opacity: 0.9;
    font-weight: 700;
}

```

---

### 4.2. Home Page Component: "Focused Day Nook"

A specialized version of the Nook for the Home Dashboard. Displays today's date, weather, and agenda.

**`Pages/Components/FocusedDayNook.razor`**

```csharp
<div class="nook focused-day">
    <div class="focused-header">
        <div>
            <h2>@CurrentDate.ToString("dddd, MMM dd")</h2>
            <div class="weather-tag">
                <span class="weather-icon">☀️</span> @WeatherStatus
            </div>
        </div>
    </div>

    <div class="event-list">
        @if(Events == null || !Events.Any())
        {
            <p class="empty-state">The den is quiet today.</p>
        }
        else 
        {
            @foreach (var evt in Events)
            {
                <div class="event-item">
                    <div class="event-icon">
                        @((MarkupString)GetIconSvg(evt.Type))
                    </div>
                    <div class="event-details">
                        <span class="event-time">@evt.Time.ToString("h:mm tt")</span>
                        <span class="event-title">@evt.Title</span>
                        <span class="event-location">@evt.Location</span>
                    </div>
                </div>
            }
        }
    </div>
</div>

@code {
    [Parameter] public DateTime CurrentDate { get; set; }
    [Parameter] public string WeatherStatus { get; set; }
    [Parameter] public List<DayEvent> Events { get; set; }

    // Placeholder Data Model
    public class DayEvent {
        public string Title { get; set; }
        public DateTime Time { get; set; }
        public string Location { get; set; }
        public string Type { get; set; } // "Nature", "Focus", "Social"
    }

    private string GetIconSvg(string type) => type switch {
        // Simplified SVG strings
        "Nature" => "<svg viewBox='0 0 24 24' width='20' height='20' stroke='#3E424B' stroke-width='2' fill='none'><path d='M12 2L12 22 M12 2C12 2 17 7 17 12C17 17 12 22 12 22 M12 2C12 2 7 7 7 12C7 17 12 22 12 22'/></svg>",
        "Focus" => "<svg viewBox='0 0 24 24' width='20' height='20' stroke='#3E424B' stroke-width='2' fill='none'><circle cx='12' cy='12' r='10'/><path d='M12 6v6l4 2'/></svg>",
        _ => "<svg viewBox='0 0 24 24' width='20' height='20' stroke='#3E424B' stroke-width='2' fill='none'><circle cx='12' cy='12' r='3'/></svg>"
    };
}

```

**`Pages/Components/FocusedDayNook.razor.css`**

```css
.focused-day {
    /* Subtle gradient to differentiate from standard nooks */
    background: linear-gradient(180deg, #FDFCF8 0%, #FFF9F0 100%);
}

.focused-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 24px;
    border-bottom: 1px solid rgba(62, 66, 75, 0.1);
    padding-bottom: 16px;
}

.focused-header h2 {
    margin: 0;
    font-size: 1.8rem;
    color: var(--color-den-shadow);
}

.weather-tag {
    font-size: 0.9rem;
    color: var(--color-seafoam);
    font-weight: 600;
    margin-top: 4px;
    display: flex;
    align-items: center;
    gap: 6px;
}

.event-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.event-item {
    display: flex;
    align-items: center;
    gap: 16px;
}

.event-icon {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background-color: rgba(129, 178, 154, 0.2); /* Low opacity Seafoam */
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--color-den-shadow);
    flex-shrink: 0;
}

.event-details {
    display: flex;
    flex-direction: column;
}

.event-time {
    font-size: 0.75rem;
    font-weight: 700;
    color: var(--color-teal);
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.event-title {
    font-size: 1rem;
    font-weight: 600;
}

.event-location {
    font-size: 0.85rem;
    opacity: 0.7;
}

.empty-state {
    text-align: center;
    font-style: italic;
    opacity: 0.6;
}

```

---

### 4.3. Calendar Component: Multi-User Grid

A grid-based calendar that visualizes events as stacked color strips at the bottom of the date cell.

**`Pages/Components/CalendarGrid.razor`**

```csharp
<div class="calendar-container">
    <div class="user-filters">
        <button class="filter-bubble @(SelectedUser == "All" ? "active" : "")" @onclick="@(() => FilterUser("All"))">
            <span class="bubble-icon" style="background-color: var(--color-den-shadow)"></span>
            <span class="bubble-label">All</span>
        </button>
        @foreach (var user in Users)
        {
            <button class="filter-bubble @(SelectedUser == user.Name ? "active" : "")" @onclick="@(() => FilterUser(user.Name))">
                <span class="bubble-icon" style="background-color: @user.Color"></span>
                <span class="bubble-label">@user.Name</span>
            </button>
        }
    </div>

    <div class="view-toggle">
        <button class="toggle-option @(CurrentView == "Month" ? "active" : "")" @onclick="@(() => SetView("Month"))">
            Month Grid
        </button>
        <button class="toggle-option @(CurrentView == "Continuous" ? "active" : "")" @onclick="@(() => SetView("Continuous"))">
            Continuous View
        </button>
    </div>

    @if (CurrentView == "Month")
    {
        <div class="calendar-flowing-grid">
            <div class="grid-header">
                @foreach (var d in new[] { "S", "M", "T", "W", "T", "F", "S" })
                {
                    <div class="header-text">@d</div>
                }
            </div>

            <div class="grid-body">
                @foreach (var day in Days)
                {
                    <div class="flowing-cell 
                                @(day.Date == SelectedDate ? "selected" : "") 
                                @(day.IsToday ? "is-today" : "")" 
                         @onclick="() => SelectDate(day.Date)">
                        
                        <span class="day-number">@day.Date.Day</span>

                        <div class="strip-stack">
                            @foreach (var evt in day.Events.Where(e => SelectedUser == "All" || e.UserName == SelectedUser))
                            {
                                <div class="mini-strip" style="background-color: @evt.UserColor"></div>
                            }
                        </div>
                    </div>
                }
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public DateTime SelectedDate { get; set; }
    [Parameter] public string SelectedUser { get; set; } = "All";
    [Parameter] public string CurrentView { get; set; } = "Month";
    
    // ... EventCallback Logic for FilterUser, SetView, SelectDate ...
}

```

**`Pages/Components/CalendarGrid.razor.css`**

```css
/* --- 1. Filters --- */
.user-filters {
    display: flex;
    justify-content: center;
    gap: 12px;
    margin-bottom: 24px;
}
.filter-bubble {
    background: none;
    border: none;
    display: flex;
    flex-direction: column;
    align-items: center;
    cursor: pointer;
    opacity: 0.5; /* Inactive state */
    transition: opacity 0.2s;
}
.filter-bubble.active {
    opacity: 1.0;
}
.bubble-icon {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    margin-bottom: 4px;
}
.bubble-label {
    font-size: 0.75rem;
    font-family: var(--font-heading);
    color: var(--color-den-shadow);
}

/* --- 2. View Toggle --- */
.view-toggle {
    background-color: rgba(129, 178, 154, 0.2); /* Low opacity Seafoam */
    border-radius: 99px;
    padding: 4px;
    display: flex;
    margin-bottom: 24px;
}
.toggle-option {
    flex: 1;
    border: none;
    background: transparent;
    padding: 8px 16px;
    border-radius: 99px;
    font-family: var(--font-heading);
    font-size: 0.9rem;
    color: var(--color-den-shadow);
    cursor: pointer;
    transition: background-color 0.2s;
}
.toggle-option.active {
    background-color: var(--color-teal);
    color: white;
    font-weight: 700;
    box-shadow: 0 2px 8px rgba(61, 139, 139, 0.2);
}

/* --- 3. Flowing Grid --- */
.calendar-flowing-grid {
    /* No outer container background/shadow needed for the flowing look, 
       or use a very subtle transparent nooks style if preferred */
    padding: 0 8px;
}

.grid-header, .grid-body {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    row-gap: 16px; /* Vertical breathing room */
}

.header-text {
    text-align: center;
    font-size: 0.8rem;
    font-weight: 700;
    opacity: 0.6;
    margin-bottom: 8px;
}

.flowing-cell {
    display: flex;
    flex-direction: column;
    align-items: center;
    position: relative;
    cursor: pointer;
    min-height: 50px;
}

/* Day Number Styling */
.day-number {
    width: 32px;
    height: 32px;
    line-height: 32px;
    text-align: center;
    border-radius: 50%;
    font-size: 0.95rem;
    margin-bottom: 4px;
    transition: all 0.2s;
}

/* SELECTED STATE: Solid Den Shadow (Charcoal) */
.flowing-cell.selected .day-number {
    background-color: var(--color-den-shadow);
    color: white;
    font-weight: 700;
}

/* TODAY STATE (Not Selected): Faint/Low Opacity version of Selection Color */
.flowing-cell.is-today:not(.selected) .day-number {
    background-color: rgba(62, 66, 75, 0.1); /* 10% Opacity Den Shadow */
    color: var(--color-den-shadow);
    font-weight: 700;
}

/* Strips */
.strip-stack {
    display: flex;
    gap: 3px;
    justify-content: center;
    flex-wrap: wrap;
    width: 100%;
    padding: 0 4px;
}
.mini-strip {
    width: 100%; /* Full width of cell padding */
    height: 4px;
    border-radius: 2px;
}

```

---

### 4.4. Core Component: "Pebble" Button

A pill-shaped button that "squishes" when pressed.

**`Shared/Components/PebbleButton.razor`**

```csharp
<button class="pebble-btn @(IsSecondary ? "secondary" : "primary")" @onclick="OnClick">
    @Text
</button>

@code {
    [Parameter] public string Text { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public bool IsSecondary { get; set; } = false;
}

```

**`Shared/Components/PebbleButton.razor.css`**

```css
.pebble-btn {
    border: none;
    border-radius: var(--radius-pebble);
    padding: 16px 32px;
    font-family: var(--font-heading);
    font-weight: 700;
    font-size: 16px;
    cursor: pointer;
    transition: transform 0.1s ease-in-out;
    width: 100%;
    display: block;
}

.pebble-btn.primary {
    background-color: var(--color-teal);
    color: white;
    box-shadow: var(--shadow-pebble);
}

.pebble-btn.secondary {
    background-color: transparent;
    color: var(--color-seafoam);
    border: 2px solid var(--color-seafoam);
}

.pebble-btn:active {
    transform: scale(0.97); /* The "Squish" */
}

```

---

## 5. System Recall Prompt

*If you need to generate new pages or components in the future, paste the following prompt to your AI assistant:*

> **System Recall: "Animal Den" Style**
> **Aesthetic:** "Organic Minimalism" / "Modern Sanctuary"
> **Core Concept:** A digital burrow. Safe, warm, rounded, and tactile.
> **Key Colors:**
> * Background: `#FFF9F0` (Warm Off-White)
> * Text: `#3E424B` (Den Shadow - Charcoal)
> * Action: `#3D8B8B` (Teal - Pebbles)
> * Secondary: `#81B29A` (Seafoam)
> * Highlight: `#F2CC8F` (Gold - Glows/Leaves)
> * Alert: `#E07A5F` (Coral - Dots)
> 
> 
> **Components:**
> 1. **Nook:** Containers are indented with inner shadow, not floating cards. Large radius (`24px`).
> 2. **Pebble:** Buttons are pill-shaped, squish on click.
> 3. **Vine:** Progress bars are curved/organic.
> 4. **Calendar:** Stacked horizontal color strips for multi-user events.
> 5. **Icons:** Monoline, thin stroke, organic shapes (twigs, leaves).
> 6. **Spacing:** 8px scale; roomy padding (Nook 24px).
> 7. **Motion:** Soft ease-out, 120-240ms durations.
> 
> 
> **Typography:** Nunito (Headings) + Inter (Body).

---

## 6. Theme Extension: Dark Mode ("Midnight Canopy")

**Vibe:** Sleepy, bioluminescent, private, and infinite. This theme reinterprets the "Den" as a safe space at night. The walls become deep charcoal, and the visual cues become glowing "fireflies" and "starlight."

### Dark Mode Palette Mapping

| Role | Name | Hex | Usage |
|------|------|-----|-------|
| **Canvas** | Deep Cave | `#1A1C23` | Main background. Replaces Warm Background. |
| **Text** | Starlight | `#E2E8F0` | Primary text. Replaces Den Shadow. |
| **Action** | Firefly | `#2DD4BF` | Primary buttons (Pebbles). Replaces Teal. |
| **Secondary** | Slate | `#334155` | Nook backgrounds/Tracks. Replaces Seafoam. |
| **Highlight** | Amber | `#FBBF24` | Glows/Active states. Replaces Gold. |
| **Alert** | Rose | `#FB7185` | Alerts. Replaces Coral. |

### CSS Implementation

To implement this, add a `[data-theme="dark"]` selector to your global CSS. This overrides the root variables when the app is in Dark Mode.

> **Note:** Ensure your Nook component CSS uses a variable for its background color (e.g., `--color-nook-bg`) instead of a hardcoded hex, so it can switch automatically.

```css
/* Add to wwwroot/app.css */

/* 1. Define the Default Nook Color in Root (Light Mode) */
:root {
    /* ... existing variables ... */
    --color-nook-bg: #FDFCF8; /* The Light Mode Nook Color */
}

/* 2. Define the Dark Mode Overrides */
[data-theme="dark"] {
    /* Remap Palette to Midnight Canopy */
    --color-warm-bg: #1A1C23;       /* Deep Cave */
    --color-den-shadow: #E2E8F0;    /* Starlight (Text) */
    --color-teal: #2DD4BF;          /* Firefly (Action) */
    --color-seafoam: #475569;       /* Slate-Light (Secondary Elements) */
    --color-gold: #FBBF24;          /* Amber (Glows) */
    --color-coral: #FB7185;         /* Rose (Alerts) */

    /* Nook Specifics */
    --color-nook-bg: #334155;       /* Slate (Nook Background) */

    /* Adjust Shadows for Dark Mode (Glows instead of Shadows) */
    --shadow-nook: inset 0 1px 1px rgba(255, 255, 255, 0.05), 0 4px 20px rgba(0,0,0,0.5);
    --shadow-pebble: 0 6px 16px rgba(0,0,0,0.45);
    --shadow-glow: 0 0 18px rgba(251, 191, 36, 0.35);
}

/* 3. Update Nook CSS to use the variable */
.nook {
    background-color: var(--color-nook-bg);
    /* ... rest of nook styles ... */
}
```

---

## 7. Layout, Spacing, and Grid

Use a calm, breathable layout with a consistent spacing scale. Favor generous vertical spacing and rounded groupings over tight grids.

**Spacing scale:** `4, 8, 12, 16, 20, 24, 32, 40` (multiples of 4/8).

**Page padding:**
- Mobile: 20-24px horizontal.
- Desktop: 32-40px with max content width ~720-960px for text-heavy views.

**Stacking rhythm:**
- Default vertical gap between Nooks: 16-20px.
- Inside Nook: 12-16px between rows.

**Safe area:**
- Always respect device insets for bottom nav and top header.

---

## 8. Elevation, Surfaces, and Depth

Depth should feel carved or cushioned, not glossy. Use a small set of surface styles:

- **Nook (Inset):** inner shadow + light outer drop.
- **Pebble (Floating):** soft drop shadow.
- **Glass (Navigation):** translucent surface with subtle shadow; fallback to opaque surface on unsupported WebViews.

**Rule:** Do not stack multiple shadows; use the single token per surface (`--shadow-nook`, `--shadow-pebble`, `--shadow-glow`).

---

## 9. Interaction, Motion, and Feedback

Motion should be subtle and tactile, never bouncy.

- **Press:** scale to 0.97 over `--motion-fast`.
- **Hover (desktop):** 2-4% brightness shift.
- **Reveal:** fade + translateY 6-10px over `--motion-slow`.
- **Easing:** use `--ease-out`.

Respect user preference:

```css
@media (prefers-reduced-motion: reduce) {
  * { transition: none !important; animation: none !important; }
}
```

---

## 10. Component States and Patterns

Define explicit states so behavior is consistent:

- **Buttons:** default, pressed, disabled, loading.
- **Inputs:** default, focus, error, success (only after error recovery).
- **Toggles:** off/on with subtle glow on "on".
- **Badges/dots:** coral for urgent only.
- **Empty states:** gentle, reassuring copy (e.g., "The den is quiet today.").

Avoid color-only status. Pair color with icon or label.

---

## 11. Accessibility and Contrast

Accessibility is part of the style system.

- **Contrast:** minimum 4.5:1 for body text, 3:1 for large text (18px+).
- **Tap targets:** at least `--tap-min` (44px) height/width.
- **Focus:** visible focus ring using `--focus-ring`.
- **Text sizing:** body not smaller than 14px; labels 12px only for secondary metadata.

---

## 12. Iconography and Illustration

Icons should be monoline, organic, and friendly.

- **Stroke:** 1.5-2px, round caps, round joins.
- **Sizes:** 20px (inline), 24px (primary nav), 32px (feature icons).
- **Color:** `--color-den-shadow` default; teal or gold only for active state.

Illustrations (if used) should be soft, minimal, and abstract (leaves, vines, stones).

---

## 13. Platform Implementation Notes (MAUI Blazor)

- Use CSS isolation for components (`.razor.css`) and global tokens in `wwwroot/app.css`.
- Avoid raw hex values in component CSS; use variables.
- `backdrop-filter` is inconsistent on Android WebView. Provide fallback:
  - Fallback surface: `background-color: var(--color-surface-glass)` with a subtle border/shadow.
- Prefer SVG for the vine progress arc to avoid heavy filters.
- Fonts: bundle Nunito/Inter locally and register in `MauiProgram` (MAUI) or via `@font-face` in `wwwroot/app.css`.
