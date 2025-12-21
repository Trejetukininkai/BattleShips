# Console Commands - Quick Reference Card

## Opening the Console

| Key | Action |
|-----|--------|
| `~` | Toggle console on/off |
| `Esc` | Close console |
| `↑` / `↓` | Navigate command history |

---

## Essential Commands

### Connection
```bash
connect http://localhost:5000    # Connect to local server
connect http://your-server:5000  # Connect to remote server
status                           # Check connection & game state
```

### Ship Placement
```bash
place auto                       # Auto-place all ships (recommended!)
place ship Carrier 0 0 H         # Place Carrier at (0,0) horizontally
place ship Battleship 2 3 V      # Place Battleship at (2,3) vertically
place ship Destroyer 5 5 H       # Place Destroyer at (5,5) horizontally
place ship Submarine 7 2 V       # Place Submarine at (7,2) vertically
place ship PatrolBoat 9 0 H      # Place PatrolBoat at (9,0) horizontally
```

Ship orientations: `H` = Horizontal, `V` = Vertical

### Mine Placement
```bash
place mine 3 4                   # Place mine at (3,4)
place mine 7 8                   # Place mine at (7,8)
```

### Combat
```bash
fire A 5                         # Fire at column A, row 5
fire G 10                        # Fire at column G, row 10
```

### Power-ups
```bash
powerup MiniNuke                 # Next shot is 3x3 explosion (Cost: 5 AP)
powerup Repair                   # Heal one damaged cell (Cost: 3 AP)
powerup ForceDisaster            # Trigger disaster now (Cost: 4 AP)
```

### Information
```bash
help                             # Show all commands
status                           # Show detailed game state
quit                             # Exit game
```

---

## Command Cheat Sheet by Game Phase

### Phase 1: Connection
```
> connect http://localhost:5000
> status
```

### Phase 2: Ship Placement (120 seconds)
```
> place auto
  OR
> place ship Carrier 0 0 H
> place ship Battleship 2 3 V
> place ship Destroyer 5 5 H
> place ship Submarine 7 2 V
> place ship PatrolBoat 9 0 H
```

### Phase 3: Mine Placement (120 seconds)
```
> place mine 3 4
> place mine 7 8
> place mine 2 5
```

### Phase 4: Combat (Your Turn Only)
```
> fire A 5
> fire B 5
> fire C 5

# When you have 5+ AP:
> powerup MiniNuke
> fire H 8    # This will be a 3x3 explosion!

# When damaged:
> powerup Repair
```

---

## Quick Examples

### Example 1: Quick Start
```bash
connect http://localhost:5000
place auto
# Wait for opponent...
# When your turn:
fire F 6
```

### Example 2: Manual Placement
```bash
connect http://localhost:5000
place ship Carrier 0 0 H
place ship Battleship 0 2 H
place ship Destroyer 0 4 H
place ship Submarine 0 6 V
place ship PatrolBoat 0 8 H
# Mines...
place mine 9 9
```

### Example 3: Power-up Strategy
```bash
status                    # Check AP: 5
powerup MiniNuke         # Activate (Cost: 5 AP)
fire G 7                 # 3x3 explosion!
```

---

## Coordinates Reference

Board is 10x10 with letter columns (A-J) and numbered rows (1-10):

```
      A B C D E F G H I J
   ┌─────────────────────┐
 1 │ · · · · · · · · · · │
 2 │ · · · · · · · · · · │
 3 │ · · · · · · · · · · │
 4 │ · · · · · · · · · · │
 5 │ · · · · · · · · · · │
 6 │ · · · · · · · · · · │
 7 │ · · · · · · · · · · │
 8 │ · · · · · · · · · · │
 9 │ · · · · · · · · · · │
10 │ · · · · · · · · · · │
   └─────────────────────┘
```

Examples:
- Top-left corner: `A 1`
- Center: `F 6`
- Bottom-right corner: `J 10`

---

## Ship Sizes

| Ship | Length | Cells |
|------|--------|-------|
| Carrier | 5 | █████ |
| Battleship | 4 | ████ |
| Destroyer | 3 | ███ |
| Submarine | 3 | ███ |
| Patrol Boat | 2 | ██ |

**Total: 17 cells**

---

## Power-ups

| Name | Cost (AP) | Effect |
|------|-----------|--------|
| **MiniNuke** | 5 | Next shot hits 3x3 area |
| **Repair** | 3 | Heal one damaged cell |
| **ForceDisaster** | 4 | Trigger disaster event now |

**Earning AP:**
- Hit opponent's ship: +1 AP
- Ship destroyed: +2 AP

---

## Status Display

```
> status

=== Game Status ===
Connection: Connected
State: Playing
Current Status: Your turn - click opponent's board to fire!
Turn: Your Turn
Action Points: 5

Ships Placed: 5/5
Mines Placed: 2

Your Hits on Opponent: 12
Opponent Hits on You: 8

Disaster Countdown: 3
==================
```

---

## Common Workflows

### Workflow 1: Fastest Game Start
```bash
connect http://localhost:5000
place auto
# Done in 2 commands!
```

### Workflow 2: Strategic Placement
```bash
connect http://localhost:5000
# Place ships on edges
place ship Carrier 0 0 H
place ship Battleship 9 0 V
place ship Destroyer 0 9 H
place ship Submarine 9 6 V
place ship PatrolBoat 0 5 H
# Place mines near ships for protection
place mine 1 1
place mine 8 8
```

### Workflow 3: Aggressive Attack
```bash
# Rapid fire pattern
fire F 6
fire F 7
fire F 8
fire F 9
fire F 10
# Save AP for MiniNuke
status  # Check AP
powerup MiniNuke
fire H 8  # Devastate area!
```

---

## Error Messages

| Message | Meaning | Solution |
|---------|---------|----------|
| "Not connected to server" | No connection | Run `connect <url>` |
| "Not your turn!" | Opponent's turn | Wait for your turn |
| "Invalid coordinates" | Out of bounds | Use X: A-J, Y: 1-10 |
| "Cannot place ships now" | Wrong game state | Check `status` |
| "Cannot activate power-up" | Not enough AP | Check `status` for AP |

---

## Tips & Tricks

1. **Use `place auto`** - Fastest way to start
2. **Check `status` often** - Know your AP and game state
3. **Save AP for MiniNuke** - Most powerful (5 AP)
4. **Fire in patterns** - Systematic search is better than random
5. **Use history** - Press `↑` to repeat commands
6. **Mine placement** - Place near ships for protection

---

## Keyboard Shortcuts

| Key | Function |
|-----|----------|
| `~` | Toggle console |
| `Esc` | Close console |
| `Enter` | Execute command |
| `↑` | Previous command |
| `↓` | Next command |
| `Tab` | (Future: Auto-complete) |

---

## Remember

✅ Console is **in-game** - use with mouse/keyboard gameplay
✅ Commands work **anytime** - even during mouse gameplay
✅ Press `~` to **show/hide** console
✅ Use `help` if you **forget** commands
✅ `status` shows **everything** you need to know

---

**Happy Gaming! 🎮⚓**
