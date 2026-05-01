# SCCR App Development Notes

<!-- style settings -->
<style>
body,
.markdown-preview,
.markdown-preview-enhanced,
.markdown-preview-view {
    font-size: 11px !important;
    line-height: 1.2 !important;
}

h1 {
    font-size: 14px !important;
}

h2 {
    font-size: 12px !important;
}

h3 {
    font-size: 12px !important;
}

p,
li {
    font-size: 11px !important;
    line-height: 1.2 !important;
}

code,
pre {
    font-size: 11px !important;
}

/* Formula size */
.katex {
    font-size: 1em !important;
}

.math {
    font-size: 11px !important;
}
</style>

## TO-DO

- Add filters to database columns.
- Allow copying/pasting a branch to other locations.
- Allow dragging a branch to different locations.
  - Show a preview of where the branch will be placed before dropping.
  - Support this in both:
    - Flowchart view
    - Hierarchy view
- Add more functionality to the flowchart view.
  - Let the user select what is shown in each branch/node, such as:
    - Picture
    - Device name
    - Internal part number
    - Manufacturer part number
    - SCCR
    - Fuse/OCPD data
- Allow a project to be saved directly, not only with **Save As**.
- Create a clean visual summary/report that the user can save, export, or print.
  - The report should show the flowchart and/or hierarchy view.
  - The report should show SCCR calculations at each branch.
  - Let the user select which values are shown for each branch/node, such as:
    - Image
    - Internal part number
    - Device name
    - Manufacturer
    - SCCR
    - Available fault current
    - OCPD/fuse information

---

# SCCR Calculation Improvements

## Fuse Let-Through Current

Add fuse let-through current logic.

The peak let-through current changes depending on the target SCCR. The program needs to store or calculate let-through current values for each target SCCR level.

Possible target SCCR values:

- 5 kA
- 10 kA
- 18 kA
- 25 kA
- 50 kA
- 65 kA
- 100 kA
- 200 kA

Possible implementation ideas:

- Store manufacturer-published let-through values in the part database.
- Add fields for let-through current at each target SCCR.
- Allow graph-based lookup using manufacturer log-log current limitation curves.
- Possibly display the log-log graph in the app, similar to Desmos.
- Allow the user to click or interpolate from the graph.
- Use interpolation between known points if exact values are unavailable.

Important distinction:

- Peak let-through current is used for SCCR comparison under UL 508A SB4.2.
- Clearing energy/current-squared-time data is a separate fuse performance value.

Peak let-through current:

$$
I_p
$$

Clearing energy:

$$
I^2t
$$

---

# Transformer and Power Supply SCCR Rules
1. If a transformer feeds power-circuit components:
	Calculate transformer secondary available fault current.
	Evaluate downstream power-circuit components using the calculated secondary fault current.

2. If a transformer feeds both control and power circuits:
	Separate the control-circuit branch from the power-circuit branch.
	Evaluate the power-circuit branch using the calculated transformer secondary fault current.
	Do not require SCCR values for control-only devices unless required by a specific product standard or listing condition.
## Main Rule

Transformers are not treated like normal SCCR-limiting components in the same way as disconnects, fuse holders, terminal blocks, contactors, overloads, bus bars, or drives.

The transformer itself usually does **not** lower the overall panel SCCR.

However, the downstream side of the transformer still needs to be evaluated.

In other words:

- The transformer itself is typically exempt from needing its own SCCR value as a component.
- The primary-side circuit must still be evaluated normally.
- The secondary-side circuit must still be evaluated based on the available short-circuit current on the transformer secondary.

---

## 1. Transformer Itself Usually Does Not Lower Panel SCCR

In the UL 508A SB4 process, SCCR is normally assigned to power-circuit components such as:

- Disconnects
- Fuse holders
- Terminal blocks
- Contactors
- Overloads
- Bus bars
- Motor controllers
- Drives

Control-circuit devices generally are not required to have SCCR.

A power transformer is treated differently. It can be used as a fault-current-limiting or modifying feeder component under SB4.3 instead of being treated as a normal “lowest SCCR wins” component.

---

## 2. Transformer Secondary Must Be Evaluated

If a transformer has an isolated secondary, UL 508A SB4.3 allows the available short-circuit current on the secondary side to be calculated.

Then the power-circuit components and overcurrent protective devices on the secondary side must be rated for at least that calculated available secondary fault current.

---

# Single-Phase Transformer Formulas

## Full-Load Current

$$
I_{FL} = \frac{kVA \times 1000}{V_{secondary}}
$$

Where:

- $I_{FL}$ = transformer secondary full-load current
- $kVA$ = transformer size in kilovolt-amperes
- $V_{secondary}$ = secondary voltage

---

## Transformer Impedance as a Decimal

$$
Z = \frac{\%Z}{100}
$$

Where:

- $Z$ = transformer impedance as a decimal
- $\%Z$ = transformer impedance as a percent

Example:

$$
Z = \frac{2.1}{100} = 0.021
$$

---

## Secondary Short-Circuit Current

$$
I_{SC} = \frac{I_{FL}}{Z}
$$

Where:

- $I_{SC}$ = available short-circuit current on the transformer secondary
- $I_{FL}$ = transformer secondary full-load current
- $Z$ = transformer impedance as a decimal

---

## Combined Single-Phase Formula

$$
I_{SC} = \frac{kVA \times 1000}{V_{secondary} \times Z}
$$

Using percent impedance directly:

$$
I_{SC} = \frac{kVA \times 1000}{V_{secondary} \times \left(\frac{\%Z}{100}\right)}
$$

---

# Single-Phase Example

Given:

- Transformer size: $1 \text{ kVA}$
- Secondary voltage: $120 \text{ V}$
- Transformer impedance: $2.1\%$

Convert impedance to decimal:

$$
Z = \frac{2.1}{100} = 0.021
$$

Calculate full-load current:

$$
I_{FL} = \frac{1 \times 1000}{120}
$$

$$
I_{FL} = 8.33 \text{ A}
$$

Calculate secondary short-circuit current:

$$
I_{SC} = \frac{8.33}{0.021}
$$

$$
I_{SC} \approx 397 \text{ A}
$$

Result:

The secondary side only needs to be evaluated against approximately:

$$
397 \text{ A}
$$

available short-circuit current, not the full available fault current from the main panel feeder.

For example, even if the panel feeder has:

$$
100 \text{ kA}
$$

available fault current, the transformer secondary may only have about:

$$
397 \text{ A}
$$

available fault current, depending on transformer size and impedance.

---

# Three-Phase Transformer Formulas

## Full-Load Current

$$
I_{FL} = \frac{kVA \times 1000}{\sqrt{3} \times V_{LL}}
$$

Where:

- $I_{FL}$ = transformer secondary full-load current
- $kVA$ = transformer size in kilovolt-amperes
- $V_{LL}$ = secondary line-to-line voltage

---

## Secondary Short-Circuit Current

$$
I_{SC} = \frac{I_{FL}}{Z}
$$

---

## Combined Three-Phase Formula

$$
I_{SC} = \frac{kVA \times 1000}{\sqrt{3} \times V_{LL} \times Z}
$$

Using percent impedance directly:

$$
I_{SC} = \frac{kVA \times 1000}{\sqrt{3} \times V_{LL} \times \left(\frac{\%Z}{100}\right)}
$$

---

# Unknown Transformer Impedance

If transformer impedance is unknown, UL guidance allows a transformer with unmarked impedance, or a transformer with known impedance not less than 2.1%, to use:

$$
\%Z = 2.1\%
$$

Therefore:

$$
Z = 0.021
$$

This is conservative because lower impedance produces higher available secondary short-circuit current.

Relationship:

$$
I_{SC} = \frac{I_{FL}}{Z}
$$

So as impedance decreases, short-circuit current increases.

---

# Control Transformers vs. Power Transformers

## Control Transformer Feeding Only Control Circuits

For a control transformer feeding only control circuits, the downstream control devices usually do not need individual SCCR values.

Control circuits and pilot devices are generally outside the SCCR component-rating evaluation.

Examples may include:

- Pushbuttons
- Pilot lights
- Relay coils
- PLC inputs
- Low-energy control devices

However, the transformer primary protection still matters.

The following still need to be evaluated:

- Primary-side fuse or circuit breaker
- Primary-side fuse holder
- Primary-side terminal blocks
- Any feeder-side power-circuit components

---

## Power Transformer Feeding Power Loads

If the transformer secondary feeds power-circuit devices, then those downstream devices must be evaluated using the calculated secondary available short-circuit current.

Examples may include:

- Secondary-side disconnects
- Secondary-side fuse holders
- Secondary-side breakers
- Contactors
- Motor starters
- Power distribution blocks
- Terminal blocks used in power circuits
- Drives or power controllers

---

# 24 VDC Power Supplies

24 VDC power supplies are different from transformers, but they are similar in SCCR practice.

A DC power supply is generally not required to have its own SCCR value and is generally not factored into the overall machine SCCR as a limiting component.

However, the AC input side of the power supply still matters.

The following still need to be evaluated:

- AC branch circuit protection
- AC-side fuse or breaker
- AC-side fuse holder
- AC-side terminal blocks
- Upstream feeder components

The 24 VDC output side is usually treated as a control circuit, assuming it only feeds control devices.

However, special cases may need additional review, such as power conversion equipment that creates a DC bus for a drive.

---

# Bottom Line

Transformers are not “SCCR exempt” in the sense that everything around them can be ignored.

But the transformer itself is typically exempt from needing an SCCR value as a component.

Use this workflow:

1. Evaluate the transformer primary side normally.
2. Treat the transformer as an isolating and fault-current-limiting component.
3. Calculate the secondary available short-circuit current using transformer kVA, secondary voltage, and impedance.
4. Evaluate the secondary-side power-circuit components against the calculated secondary fault current.
5. If the secondary feeds only control circuits, the downstream control devices usually do not need individual SCCR values.
6. Do not allow the transformer itself to become the “lowest SCCR wins” component unless a specific standard, listing, or application note requires otherwise.

---

# Program Logic Notes

## Transformer Node Logic

In the SCCR app, a transformer node should have special behavior.

The transformer should not simply be treated as a normal component with a single SCCR value.

Instead, it should modify the available fault current for its downstream branches.

Suggested transformer fields:

- Transformer type
  - Single-phase
  - Three-phase
- Primary voltage
- Secondary voltage
- kVA
- Percent impedance
- Impedance known?
- Secondary circuit type
  - Control only
  - Power circuit
  - Mixed
- Primary OCPD
- Secondary OCPD, if applicable

---

## Transformer Calculation Logic

If impedance is known:

$$
Z = \frac{\%Z}{100}
$$

If impedance is unknown:

$$
Z = 0.021
$$

For single-phase transformers:

$$
I_{SC} = \frac{kVA \times 1000}{V_{secondary} \times Z}
$$

For three-phase transformers:

$$
I_{SC} = \frac{kVA \times 1000}{\sqrt{3} \times V_{LL} \times Z}
$$

The downstream available fault current should become:

$$
I_{available,downstream} = I_{SC,secondary}
$$

The downstream branch SCCR should then be evaluated against:

$$
I_{available,downstream}
$$

instead of the original panel available fault current.

---

## Example Program Rule

If a transformer feeds only control circuits:

```text
Do not require SCCR values for downstream control devices.
Still evaluate primary-side power-circuit components.