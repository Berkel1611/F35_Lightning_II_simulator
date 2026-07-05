# F-35 Lightning II Simulator

A flight simulator for the Lockheed Martin F-35A Lightning II, built in Unity as my Bachelor's thesis project.

![F-35 external view](screenshots/02-external-view.png)

🎥 **Demo video:** _coming soon_

---

## About

This project was developed as a Bachelor's thesis with the goal of building a flight simulator for the F-35A that reproduces its flight characteristics through mathematical modeling, and supports combat mission scenarios for testing and demonstration.

Precise aerodynamic data for the F-35 is classified, so the flight model is not - and doesn't aim to be - a professional-fidelity training device. Instead, aerodynamic coefficients are scaled from a publicly available F-16 dataset (Stevens, Lewis & Johnson, *Aircraft Control and Simulation*) to match F-35A specifications. This trade-off, along with other simplifications, is a deliberate design decision made under the constraint of working without access to classified technical data - not an oversight.

## Features

- **Custom 3D aircraft model** - self-modeled in Blender, with PBR textures, animated control surfaces, functional landing gear, and a detailed cockpit
- **6-DOF flight physics** - six-degrees-of-freedom model using tabular interpolation of aerodynamic coefficients, scaled from F-16 reference data to F-35A parameters (dimensions, mass, inertia tensor)
- **Simplified Flight Control System (FCS)** - PID-based control augmentation with angle-of-attack and G-load limiting (+9G/-3G, matching F-35A specs)
- **HUD** - flight instrument display with airspeed/altitude tapes, compass, and weapon status
- **Real-world terrain** - environment modeled on the geographic data of Łask Air Base, Poland
- **Mission scenarios**, selectable from the main menu:
  - **Tutorial** - training scenario
  - **Mission 1** - air-to-ground combat
  - **Mission 2** - air-to-air combat
  - **Free Flight** - no mission objectives
- **Keyboard & mouse controls**

## Screenshots

| | |
|---|---|
| ![Main menu](screenshots/01-main-menu.png) | ![HUD](screenshots/03-hud.png) |
| Main menu | HUD in free flight |
| ![Target lock](screenshots/04-hud-targeting.png) | ![PCD display](screenshots/05-pcd-display.png) |
| Locking onto a target | PCD (cockpit display) |
| ![Tutorial](screenshots/06-tutorial.png) | ![Weapons loadout](screenshots/07-weapons-loadout.png) |
| Tutorial mode | Weapons loadout |

## Tech Stack

- **Engine:** Unity 6 (6000.0.50f1 LTS), HDRP
- **Language:** C#
- **Shaders:** ShaderLab, HLSL
- **3D Modeling:** Blender

## Technical Challenges & Solutions

**1. Flight physics without access to classified data.**
The F-35's real aerodynamic data is military-classified. As a base, I used [vazgriz's C# port](https://github.com/vazgriz/FlightSim_F16) (MIT licensed) of the F-16 flight model from Stevens, Lewis & Johnson's *Aircraft Control and Simulation* - one of the few publicly available, implementation-ready 6-DOF datasets for a real military aircraft. My own work here was scaling the model to F-35A specifications (wing geometry, mass, inertia tensor) and retuning the FCS's PID controllers, since the gains that kept the aircraft stable at low speed became unstable at high speed and vice versa - solved by scaling the controller gains as a function of airspeed rather than using fixed coefficients.

**2. Diagnosing and fixing a severe performance bottleneck.**
The simulator initially ran at ~6 FPS in the heaviest combat scenario. Profiling (Unity Profiler, Deep Profile) showed the bottleneck was HDRP rendering - frame prep, culling, and render graph construction - not physics or game logic. I fixed it one change at a time, re-measuring after each:
- The cockpit's secondary display (PCD) camera was inheriting the main camera's full HDRP feature set for a static, single-layer view - enabling *Custom Frame Settings* and disabling unneeded features cut its render cost from 28.1 ms to 12.3 ms.
- The sky was recalculating every frame despite being fully static; switching its update mode from *Realtime* to *On Changed* saved ~3.7 ms.
- HUD elements (compass, altitude/airspeed tapes) were re-reading `transform.rect` and `gameObject` through properties on every tick, every frame; caching them saved over half the HUD's update cost (6.9 ms → 3.3 ms).
- The camera's far clip plane was set far beyond what any mission scenario needed, forcing HDRP to cull across a much larger frustum than necessary; reducing it cut culling cost from 12.5 ms to 4.1 ms.

Combined, these changes took the simulator from ~6 FPS to ~18 FPS (Editor, Deep Profile on) and ~27 FPS in a Standalone Development Build - several times more playable on the same hardware, with no visible change in quality.

## Limitations

- Not intended as a professional-fidelity training simulator - aerodynamic data is approximated from F-16 data, not the real (classified) F-35A dataset.

## Getting Started

**Requirements:** Unity 6000.0.50f1 LTS (or compatible Unity 6 LTS version) with HDRP support.

This repository does not include a handful of third-party Asset Store packages used for environment art (their licenses don't permit redistribution in a public repo). Before opening the project, import these into the `Assets/ThirdParty/` folder:

- [HQ Hangar Free](https://assetstore.unity.com/packages/3d/environments/hq-hangar-free-212795)
- [JMO Assets](http://www.jeanmoreno.com/unity/faq/)
- [RPG/FPS Game Assets (Industrial Set)](https://assetstore.unity.com/packages/3d/environments/industrial/rpg-fps-game-assets-for-pc-mobile-industrial-set-v6-0-155439)
- [Unity Terrain - HDRP Demo Scene](https://assetstore.unity.com/packages/3d/environments/unity-terrain-hdrp-demo-scene-213198)

```bash
git clone https://github.com/<your-username>/f35-lightning-simulator.git
```

1. Open the project folder in Unity Hub.
2. Import the third-party packages listed above.
3. Let Unity import all assets and packages.
4. Open the main scene from `Assets/Scenes/`.
5. Press Play.

## Acknowledgements

- Flight physics methodology: Stevens, B. L., Lewis, F. L., & Johnson, E. N. - *Aircraft Control and Simulation: Dynamics, Controls Design and Autonomous Systems*.
- F-16 flight model C# implementation (base for `Flight/Core` and `Flight/FCS`, MIT License): [vazgriz/FlightSim_F16](https://github.com/vazgriz/FlightSim_F16)