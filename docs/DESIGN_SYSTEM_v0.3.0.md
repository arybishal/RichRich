# RichRich v0.3.0: Visual Design System & Architecture Specification

**Project:** RichRich: Life & Enterprise Simulation  
**Version:** v0.3.0  
**Target Platform:** Android ARM64 Portrait Mobile (1080×1920 reference canvas)  
**Style Direction:** Modern Dark FinTech / Prestigious Capital Tycoon / Stylized Isometric Metropolis  

---

## 1. Design Tokens & Palette

| Token Name | Hex Code | Role |
|---|---|---|
| `bg-app` | `#080C14` | Deep obsidian backdrop |
| `surface-ground` | `#0F172A` | Secondary surface / Canvas background |
| `surface-card` | `#151E2E` | Elevated primary cards (12px radius) |
| `surface-elevated` | `#1E293B` | Modal headers, interactive rows |
| `border-subtle` | `#2A3548` | 1px hairline container borders |
| `accent-gold` | `#F59E0B` | Prestige, milestones, achievements |
| `status-profit` | `#10B981` | Cash income, capital gains, buy action |
| `status-loss` | `#EF4444` | Living expenses, debt liabilities, sell action |
| `status-liquid` | `#38BDF8` | Liquid cash balances, checking, dividends |
| `text-primary` | `#F8FAFC` | Main headings, primary amounts |
| `text-secondary` | `#94A3B8` | Subtitles, labels, dates |
| `text-muted` | `#64748B` | Footers, inactive tab icons |

---

## 2. Navigation Architecture: 4-Category Bottom Dock

Replaces the 16-button vertical grid with a mobile-first bottom dock:

1. **Metropolis (3D):**
   - Direct toggle to 3D City exploration mode.
   - Interacts directly with 3D buildings via player avatar movement.
2. **Life & Career:**
   - Career Board & Vacancies
   - Higher Education & Degrees
   - Housing & Leases
   - Lifestyle & Energy Recovery
3. **Enterprises:**
   - Commercial Business Marketplace
   - Commercial Enterprise Dashboard
   - Auto Repair Workshop
   - Corner Convenience Retail
   - Digital Marketing Agency
   - Staff Recruitment & HR
   - Commercial Debt & Credit
4. **Wealth:**
   - Real Estate & Property Investments
   - Financial Markets & Stock Exchange
   - Life Goals & FIRE Progress
   - Financial Health & Balance Sheet

---

## 3. 3D Metropolis & Player Avatar Specification

- **Metropolis Layout:**
  - Center: The Exchange Tower (Equities)
  - Left Front: Metropolitan Bank & Trust (Finance)
  - Right Front: Skyline Real Estate Plaza (Real Estate)
  - Left Rear: Apex Corporate Tower (Enterprise)
  - Right Rear: Residential District (Housing)
- **Environment Props:**
  - Boulevard lanes and crosswalk markings
  - Low-poly street trees with tiered canopy geometry
  - Streetlamps with warm emissive light caps
- **Player Character Avatar:**
  - Stylized low-poly humanoid in business smart-casual attire.
  - When the player taps a 3D building, the avatar walks toward the building entrance before opening the subsystem modal (with a 0.5s timeout safety fallback).

---

## 4. Animation & Micro-Interactions

- **Floating Currency Feedback:**
  - Income / Dividends / Sales: Rising emerald `+$X.XX` floating text (1.2s duration).
  - Expenses / Tuition / Taxes: Sinking crimson `-$X.XX` floating text (1.2s duration).
- **Tactile Button Press:**
  - Scale down to 0.96 on pointer down, bounce back to 1.0 on release.
- **Modal Transitions:**
  - Bottom sheet slides up with cubic ease-out curve.
