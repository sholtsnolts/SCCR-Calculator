# SCCR WPF App

Build a WPF/.NET desktop application for calculating industrial control panel SCCR using a UL 508A Supplement SB style workflow.

Core requirements:
- Tree-style circuit editor.
- Nodes represent feeder components, branch OCPDs, fuse blocks, distribution blocks, drives, contactors, transformers, power supplies, SPDs, and other devices.
- User can input manufacturer, part number, device type, SCCR, interrupting rating, fuse class, fuse amp rating, let-through current, voltage, notes, and documentation source.
- Use editable JSON databases for devices, default SCCR values, fuse let-through tables, and manufacturer combination ratings.
- Calculate branch SCCR, feeder SCCR, and overall panel SCCR.
- Show weakest limiting device and a clear calculation log.
- Do not raise an OCPD interrupting rating using an upstream current-limiting device.
- Treat OCPD interrupting rating and component SCCR as separate values.
- Manufacturer combination ratings must be condition-specific and source-tracked.