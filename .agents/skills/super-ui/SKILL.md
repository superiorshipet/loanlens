---
name: super-ui
description: Premium UI/UX architecture, responsive design systems, accessible patterns, micro-interactions, and high-performance frontend engineering.
---

# Super UI / Premium UX Engineering

## Core Design & Technical Standards
1. **Visual Hierarchy & Tokens**:
   - Strict spacing scale (4px/8px grid system).
   - Consistent typography scales and contrast ratios meeting WCAG AAA / AA standards.
   - Elegant dark/light color schemes with subtle elevation (shadows, borders, glassmorphism where tasteful).

2. **State & Interaction UX**:
   - Every async interaction has 4 states: `Idle`, `Loading` (skeleton screens, not raw spinners), `Success`, `Error` (actionable, non-punitive).
   - Optimistic UI updates with instant rollback on failure.
   - Smooth micro-interactions & transitions (sub-200ms ease-out).

3. **Performance First**:
   - Cumulative Layout Shift (CLS) = 0. Explicit width/height on images and media.
   - Lazy load below-the-fold content.
   - Component isolation & reusable design primitives (Buttons, Inputs, Modals, Tables, Cards).
   - Responsive by design: Mobile-first layout with smooth desktop breakpoint expansion.
