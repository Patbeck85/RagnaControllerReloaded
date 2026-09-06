
## 🎮 Controller & RO Gamepad Improvement List (Derived from User Request)

Based on user-provided Controller & RO Gamepad Improvement List, here are the items to integrate into the roadmap. Current state verified against codebase.

### ✅ Already Implemented (Verified)
- SDL2 GameController wrapper with hot-plug detection ✅
- DualSense (PS5), DualShock (PS4), Nintendo Switch, Xbox controller detection ✅
- ButtonState struct with all buttons/axes/triggers ✅
- Rumble and LED support ✅ (DualSenseHardwareService with adaptive triggers)
- Button remapping UI (ButtonRemappingWindow) ✅
- ButtonKey/VirtualKey struct for type-safe mappings ✅ (ARCH-003)
- Class rotation system via DefaultRotationProvider ✅
- Ground spell system integrated ✅

### 🔧 Improvement Areas to Implement

**1. Advanced Profile Per-Controller Mapping**
- Save/load controller configurations per profile
- Remember which profile was active with which controller
- Auto-switch profiles when controller changes
- *Priority: HIGH - Core user workflow*

**2. Stick & Trigger Deadzone Normalization**
- Exponential/linear curve selection
- Per-axis deadzone configuration per profile
- Deadzone visualizer in remapping window
- *Priority: MEDIUM - Improves RO gameplay precision*

**3. DualSense/Haptic Feedback Integration**
- Rumble patterns for game events (critical HP, level up, skill cooldown)
- Touchpad button integration
- Adaptive trigger resistance for skill "pressure" feeling
- *Priority: MEDIUM - Enhances immersion*

**4. Controller Button Long-Press Detection**
- Hold button X seconds to trigger alternate skill
- Configurable long-press actions per button
- Useful for "Auto-Run" (hold D-Pad) or "Auto-Potion" (hold key)
- *Priority: HIGH - Critical QoL feature*

**5. Multi-Controller Support**
- Dual controller support (keyboard + gamepad simultaneously)
- Controller A handles movement, Controller B handles skills
- Split-input configurations
- *Priority: MEDIUM - Power user feature*

**6. Steam Controller / Backbone / Mobile Gamepad Profiles**
- Non-XInput controllers with custom mappings
- Profile presets for different hardware
- *Priority: LOW - Broad compatibility*

**7. In-Game OSD Display**
- Small overlay showing current active skill bind
- Visual indicator which layer/profile is active
- Cooldown timers on controller LEDs
- *Priority: HIGH - Visible feedback*

**8. Profile Import/Export for Controller Setups**
- Export complete controller configuration
- Share configs within community
- GitHub Gist integration like Community Hub
- *Priority: MEDIUM - Community feature*

**9. Analog Stick Movement Normalization for RO**
- RO uses 8-directional movement (no diagonal acceleration)
- Normalize stick input to cardinal directions
- Option for smooth diagonal movement if desired
- *Priority: HIGH - RO core gameplay mechanic*

**10. Quick-Profile Switching via Controller**
- Press "Start + X" to switch to next profile
- Long-press "Back" to toggle auto-run
- Configurable shortcuts per profile
- *Priority: MEDIUM - Quick access*

**11. Game-Specific Profiles**
- Pre-Renewal vs Renewal control schemes
- Different key mappings for skill hotbars
- Class-specific default mappings (Knight vs Mage vs Archer)
- *Priority: HIGH - RO version support*

**12. Vibration Patterns for Status Effects**
- Short buzz: Skill ready
- Long buzz: Cooldown active
- Pattern: Critical hit / Miss detection
- Configurable per user preference
- *Priority: LOW - Nice-to-have*

**13. UI/Gamepad Navigation**
- Full UI navigable via D-Pad/Arrow keys
- Tab navigation with gamepad
- Focus management when controller is active
- *Priority: HIGH - Usability*

