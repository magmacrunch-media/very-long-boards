#!/usr/bin/env python3
"""
Garage Layout Simulator for Very Long Boards
Generates top-down 2D views of the garage for each camera scene.
Shows object positions, camera frustums, and collision detection.
"""

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import numpy as np

# ═══════════════════════════════════════════
#  ROOM DATA (from GarageManager.cs)
# ═══════════════════════════════════════════

# Room boundaries (center_x, center_z, width, depth) in XZ plane
WALLS = [
    {"name": "Back wall",   "cx": 0,    "cz": -3,    "w": 12,   "d": 0.5,  "color": "#555555"},
    {"name": "Left wall",   "cx": -6,   "cz": 0.75,  "w": 0.5,  "d": 8.5,  "color": "#444444"},
    {"name": "Right wall",  "cx": 6,    "cz": 0.75,  "w": 0.5,  "d": 8.5,  "color": "#444444"},
]

# Furniture (center_x, center_z, width, depth)
FURNITURE = [
    {"name": "Workbench",    "cx": -3.5, "cz": -1.5,  "w": 2.5,  "d": 0.8,  "color": "#8B5A2B"},
    {"name": "Shelf",        "cx": 2,    "cz": -2.5,  "w": 2,    "d": 0.4,  "color": "#6B4226"},
    {"name": "Pegboard",     "cx": -1.5, "cz": -2.8,  "w": 1.8,  "d": 0.1,  "color": "#484848"},
    {"name": "Window",       "cx": 4,    "cz": -2.8,  "w": 1.2,  "d": 0.1,  "color": "#7AADCC"},
]

# Display models (center_x, center_z, width, depth)
DISPLAYS = [
    {"name": "Character",     "cx": -1.5, "cz": 1,    "w": 0.6,  "d": 1.0,  "color": "#FFD700"},
    {"name": "Board display", "cx": 0.5,  "cz": 1.5,  "w": 0.62, "d": 1.4,  "color": "#D2691E"},
    {"name": "Leaning board", "cx": 3.8,  "cz": -1,   "w": 0.4,  "d": 1.2,  "color": "#8B4513"},
]

# Cameras (pos_x, pos_z, look_x, look_z, fov_degrees)
CAMERAS = {
    "CharSelect":  {"pos": (3, 3),    "look": (-1.5, -1),  "fov": 55, "label": "Char Select"},
    "BoardSelect": {"pos": (-3, 3),   "look": (1.5, -1),   "fov": 55, "label": "Board Select"},
    "LevelSelect": {"pos": (0, 3.5),  "look": (0, -1),     "fov": 55, "label": "Level Select"},
}

# Room extent for plotting
ROOM_X = (-7, 7)
ROOM_Z = (-5, 6)


def rect_from_center(cx, cz, w, d):
    """Convert center+size to matplotlib Rectangle (x, z, width, depth)."""
    return patches.Rectangle((cx - w/2, cz - d/2), w, d)


def compute_frustum(pos_x, pos_z, look_x, look_z, fov_deg, length=12):
    """Compute camera frustum triangle vertices in XZ plane."""
    dx = look_x - pos_x
    dz = look_z - pos_z
    dist = np.sqrt(dx*dx + dz*dz)
    if dist == 0:
        return []
    # Direction angle
    angle = np.arctan2(dz, dx)
    half_fov = np.radians(fov_deg / 2)
    # Frustum edges
    left_angle = angle - half_fov
    right_angle = angle + half_fov
    left_x = pos_x + length * np.cos(left_angle)
    left_z = pos_z + length * np.sin(left_angle)
    right_x = pos_x + length * np.cos(right_angle)
    right_z = pos_z + length * np.sin(right_angle)
    return [(pos_x, pos_z), (left_x, left_z), (right_x, right_z)]


def check_collision(obj, walls):
    """Check if an object overlaps any wall. Returns list of wall names."""
    collisions = []
    ox_min = obj["cx"] - obj["w"]/2
    ox_max = obj["cx"] + obj["w"]/2
    oz_min = obj["cz"] - obj["d"]/2
    oz_max = obj["cz"] + obj["d"]/2
    for wall in walls:
        wx_min = wall["cx"] - wall["w"]/2
        wx_max = wall["cx"] + wall["w"]/2
        wz_min = wall["cz"] - wall["d"]/2
        wz_max = wall["cz"] + wall["d"]/2
        # AABB overlap check
        if ox_min < wx_max and ox_max > wx_min and oz_min < wz_max and oz_max > wz_min:
            collisions.append(wall["name"])
    return collisions


def render_scene(scene_name, camera, filename):
    """Render a top-down view for one camera scene."""
    fig, ax = plt.subplots(1, 1, figsize=(10, 8))
    ax.set_aspect('equal')
    ax.set_title(f"Garage Layout — {camera['label']}", fontsize=14, fontweight='bold')
    ax.set_xlabel("X (left-right)")
    ax.set_ylabel("Z (front-back)")
    ax.set_xlim(ROOM_X)
    ax.set_ylim(ROOM_Z)
    ax.grid(True, alpha=0.3)
    ax.invert_yaxis()  # Z increases going into the room (toward back wall)

    # Draw floor
    floor = rect_from_center(0, 0.75, 12, 8.5)
    floor.set_facecolor('#3a3a3a')
    floor.set_edgecolor('#555555')
    floor.set_linewidth(1)
    ax.add_patch(floor)

    # Draw walls
    for wall in WALLS:
        r = rect_from_center(wall["cx"], wall["cz"], wall["w"], wall["d"])
        r.set_facecolor(wall["color"])
        r.set_edgecolor('#888888')
        r.set_linewidth(2)
        ax.add_patch(r)
        # Label
        ax.text(wall["cx"], wall["cz"], wall["name"], ha='center', va='center',
                fontsize=7, color='white', fontweight='bold')

    # Draw furniture
    for obj in FURNITURE:
        r = rect_from_center(obj["cx"], obj["cz"], obj["w"], obj["d"])
        r.set_facecolor(obj["color"])
        r.set_edgecolor('#AAAAAA')
        r.set_linewidth(1)
        ax.add_patch(r)
        ax.text(obj["cx"], obj["cz"], obj["name"], ha='center', va='center',
                fontsize=6, color='white')

    # Draw display models with collision detection
    for obj in DISPLAYS:
        collisions = check_collision(obj, WALLS)
        r = rect_from_center(obj["cx"], obj["cz"], obj["w"], obj["d"])
        if collisions:
            r.set_facecolor('#FF4444')
            r.set_edgecolor('#FF0000')
            r.set_linewidth(3)
            ax.text(obj["cx"], obj["cz"] + 0.3, f"CLIPS: {', '.join(collisions)}",
                    ha='center', va='center', fontsize=6, color='red', fontweight='bold')
        else:
            r.set_facecolor(obj["color"])
            r.set_edgecolor('#FFFFFF')
            r.set_linewidth(1.5)
        ax.add_patch(r)
        ax.text(obj["cx"], obj["cz"], obj["name"], ha='center', va='center',
                fontsize=6, color='white', fontweight='bold')

        # Show bounding box coordinates
        ox_min = obj["cx"] - obj["w"]/2
        ox_max = obj["cx"] + obj["w"]/2
        oz_min = obj["cz"] - obj["d"]/2
        oz_max = obj["cz"] + obj["d"]/2
        ax.text(obj["cx"], oz_max + 0.15, f"X:[{ox_min:.1f},{ox_max:.1f}] Z:[{oz_min:.1f},{oz_max:.1f}]",
                ha='center', va='top', fontsize=5, color='#AAAAAA')

    # Draw camera
    cam_x, cam_z = camera["pos"]
    look_x, look_z = camera["look"]

    # Camera position marker
    ax.plot(cam_x, cam_z, 'k^', markersize=12, label='Camera', zorder=10)
    ax.text(cam_x + 0.3, cam_z, f"({cam_x}, {cam_z})", fontsize=7, color='black')

    # Camera frustum
    frustum = compute_frustum(cam_x, cam_z, look_x, look_z, camera["fov"])
    if frustum:
        fx = [p[0] for p in frustum] + [frustum[0][0]]
        fz = [p[1] for p in frustum] + [frustum[0][1]]
        ax.plot(fx, fz, 'b--', linewidth=1.5, alpha=0.6, label='Frustum')
        # Fill frustum lightly
        frustum_poly = plt.Polygon(frustum, alpha=0.08, color='blue')
        ax.add_patch(frustum_poly)

    # Look-at point
    ax.plot(look_x, look_z, 'rx', markersize=10, markeredgewidth=2, label='Look-at')

    # Legend
    ax.legend(loc='upper right', fontsize=8)

    # Room dimensions text
    ax.text(ROOM_X[0] + 0.2, ROOM_Z[1] - 0.2,
            f"Room: X[{ROOM_X[0]},{ROOM_X[1]}] Z[{ROOM_Z[0]},{ROOM_Z[1]}]",
            fontsize=7, color='#888888', va='top')

    plt.tight_layout()
    plt.savefig(filename, dpi=150, bbox_inches='tight')
    plt.close()
    print(f"Saved: {filename}")


def main():
    print("=== Garage Layout Simulator ===")
    print()

    # Check for collisions
    print("Collision Report:")
    all_ok = True
    for obj in DISPLAYS:
        collisions = check_collision(obj, WALLS)
        if collisions:
            print(f"  !! {obj['name']} collides with: {', '.join(collisions)}")
            all_ok = False
    for obj in FURNITURE:
        collisions = check_collision(obj, WALLS)
        if collisions:
            print(f"  !! {obj['name']} collides with: {', '.join(collisions)}")
            all_ok = False
    if all_ok:
        print("  No collisions detected.")
    print()

    # Object positions summary
    print("Object Positions (XZ center, bounding box):")
    for obj in DISPLAYS + FURNITURE:
        ox_min = obj["cx"] - obj["w"]/2
        ox_max = obj["cx"] + obj["w"]/2
        oz_min = obj["cz"] - obj["d"]/2
        oz_max = obj["cz"] + obj["d"]/2
        print(f"  {obj['name']:15s} center=({obj['cx']:6.1f}, {obj['cz']:6.1f})  "
              f"X:[{ox_min:6.1f},{ox_max:6.1f}]  Z:[{oz_min:6.1f},{oz_max:6.1f}]")
    print()

    # Render each scene
    for scene_name, camera in CAMERAS.items():
        filename = f"garage_{scene_name.lower()}.png"
        render_scene(scene_name, camera, filename)

    print()
    print("Done! Open the PNG files to see the layouts.")
    print("Tell me what looks wrong and I'll update the Godot code.")


if __name__ == "__main__":
    main()
