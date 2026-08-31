---
name: Heritage Modern
colors:
  surface: '#faf9fd'
  surface-dim: '#dbd9dd'
  surface-bright: '#faf9fd'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f4f3f7'
  surface-container: '#efedf1'
  surface-container-high: '#e9e7eb'
  surface-container-highest: '#e3e2e6'
  on-surface: '#1a1b1e'
  on-surface-variant: '#564145'
  inverse-surface: '#2f3033'
  inverse-on-surface: '#f1f0f4'
  outline: '#897174'
  outline-variant: '#ddbfc3'
  surface-tint: '#a73453'
  primary: '#6c0029'
  on-primary: '#ffffff'
  primary-container: '#8b1e3f'
  on-primary-container: '#ff9db0'
  inverse-primary: '#ffb2bf'
  secondary: '#89502e'
  on-secondary: '#ffffff'
  secondary-container: '#feb289'
  on-secondary-container: '#794222'
  tertiary: '#62152c'
  on-tertiary: '#ffffff'
  tertiary-container: '#802c42'
  on-tertiary-container: '#ff9db1'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffd9de'
  primary-fixed-dim: '#ffb2bf'
  on-primary-fixed: '#3f0015'
  on-primary-fixed-variant: '#871b3c'
  secondary-fixed: '#ffdbca'
  secondary-fixed-dim: '#ffb68f'
  on-secondary-fixed: '#331200'
  on-secondary-fixed-variant: '#6d3919'
  tertiary-fixed: '#ffd9de'
  tertiary-fixed-dim: '#ffb1c0'
  on-tertiary-fixed: '#3f0016'
  on-tertiary-fixed-variant: '#7c293f'
  background: '#faf9fd'
  on-background: '#1a1b1e'
  surface-variant: '#e3e2e6'
typography:
  display-lg:
    fontFamily: Montserrat
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  display-lg-mobile:
    fontFamily: Montserrat
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Montserrat
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-sm:
    fontFamily: Montserrat
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-caps:
    fontFamily: Montserrat
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
  button:
    fontFamily: Montserrat
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

This design system establishes a "Premium Discount" identity—a sophisticated alternative to the cluttered, loud visuals typically found in marketplaces. The brand personality is grounded, confident, and warm, avoiding the high-energy "sale" tropes of neon colors and aggressive gradients. 

The aesthetic leans into **Minimalism** with a **Tactile** touch. It uses heavy whitespace and a restricted color palette to convey quality, while subtle shadows and warm neutrals ensure the experience feels approachable and human rather than clinical. The goal is to make the user feel like they are shopping a curated collection, even at a discount price point.

## Colors

The palette is anchored by a deep, authoritative Burgundy, providing a sense of heritage and stability. 

- **Primary & Dark:** Used for key brand moments, primary buttons, and active states. 
- **Accent Peach:** Reserved strictly for high-value callouts, "New" badges, or subtle highlights. It should never be used for large surfaces.
- **Background & Surface:** The off-white background creates a softer, more premium contrast than pure white, while pure white is reserved for elevated surface cards to create a clear "layering" effect.
- **Functional Colors:** Success and Error tones are calibrated for high legibility against the warm background without appearing neon or synthetic.

## Typography

The system utilizes a dual-font strategy to balance character with utility.

**Montserrat** is used for headlines and labels. Its geometric nature provides a confident, architectural feel. For large display titles, a slight negative letter-spacing is applied to maintain a tight, editorial look.

**Inter** is the workhorse for all body copy, inputs, and descriptions. It is chosen for its exceptional legibility and neutral tone, ensuring that product information remains the focus.

- Use `label-caps` for category headers or small badges to differentiate from standard body text.
- Maintain generous line heights (1.5x) for body text to ensure a relaxed reading experience.

## Layout & Spacing

The layout follows a **Fixed Grid** philosophy on desktop to maintain an editorial, "boutique" feel, while transitioning to a fluid model on mobile. 

- **Desktop (1280px+):** 12-column grid with 24px gutters. Content is centered.
- **Tablet (768px - 1024px):** 8-column grid with 20px gutters. 24px side margins.
- **Mobile (below 768px):** 4-column grid with 16px gutters. 16px side margins.

Vertical rhythm is strictly controlled via a 4px base unit. Components should prioritize "Stack" spacing (vertical) to separate distinct product sections, using `stack-lg` (32px) between unrelated content blocks.

## Elevation & Depth

Hierarchy is established primarily through **Tonal Layers** and extremely **Ambient Shadows**.

1. **Level 0 (Base):** Off-white `#FFF9F5`. Used for the main canvas.
2. **Level 1 (Card):** White `#FFFFFF` surface. Applied to product cards and containers. These use a very soft shadow: `0px 4px 20px rgba(139, 30, 63, 0.04)`—note the slight burgundy tint in the shadow to maintain warmth.
3. **Level 2 (Interaction):** Hover states for cards increase shadow spread and blur: `0px 8px 30px rgba(139, 30, 63, 0.08)`.
4. **Outlines:** Use `#E9E2DD` for low-contrast borders on input fields or secondary buttons. This keeps the UI from feeling "sharp" or "heavy."

## Shapes

The design system uses a consistent **Rounded** language to evoke friendliness and accessibility.

- **Standard Elements:** 12px (`0.75rem`) for standard cards and large buttons.
- **Small Elements:** 8px (`0.5rem`) for input fields and smaller components.
- **Extreme Elements:** 24px+ (`1.5rem`) for chips and badges to create a "pill" effect that contrasts against the structured cards.

Avoid sharp corners entirely; even "inner" elements like images within cards should have a nested radius (8px) to maintain the soft aesthetic.

## Components

### Buttons
- **Primary:** Deep Burgundy background, White text. 12px corner radius. Heavy padding (16px 32px).
- **Secondary:** Transparent background, Burgundy border (1px), Burgundy text.
- **Ghost:** No background or border. Used for "Cancel" or less important actions.

### Cards
- Pure White background, 12px radius, subtle Burgundy-tinted shadow.
- 16px internal padding. 
- Imagery should have an 8px radius and a very light border `#E9E2DD` to define edges against the white card.

### Chips & Badges
- **Status Badges (New/Discount):** Peach `#FFB38A` background with Charcoal text. Fully rounded (pill).
- **Category Chips:** Off-white background with a thin warm-gray border. Charcoal text.

### Input Fields
- White background with a 1px `#E9E2DD` border.
- On focus, the border changes to Primary Burgundy with a 2px outer "glow" of 10% opacity Burgundy.
- Label text uses `label-caps` style for clarity.

### Progress Indicators
- Use the Primary Burgundy for the active track and a light version of the Warm Peach for the inactive track to keep the tone warm.