"""
Blender Python script to generate skateboard models for Very Long Boards.
Matches the exact dimensions and colors from PlayerManager.cs.

Usage:
  - In Blender: Scripting workspace → New → Paste → Run Script
  - Headless: blender --background --python skateboard.py

Generates 4 color variants (Classic, Neon, Dark, Natural) as:
  - Separate .glb files in Resources/
  - Collections in a single .blend file
"""

import bpy
import os
import math

# ── Color variants from PlayerManager.cs ──────────────────────────────

VARIANTS = {
    "classic": {
        "deck": (0.52, 0.26, 0.1, 1.0),
        "grip": (0.16, 0.16, 0.16, 1.0),
    },
    "neon": {
        "deck": (0.95, 0.25, 0.95, 1.0),
        "grip": (0.15, 0.15, 0.45, 1.0),
    },
    "dark": {
        "deck": (0.12, 0.12, 0.15, 1.0),
        "grip": (0.35, 0.05, 0.55, 1.0),
    },
    "natural": {
        "deck": (0.82, 0.65, 0.42, 1.0),
        "grip": (0.55, 0.45, 0.3, 1.0),
    },
}

# Shared colors (same across all variants)
TRUCK_COLOR = (0.62, 0.62, 0.65, 1.0)      # baseplate
AXLE_COLOR = (0.55, 0.55, 0.58, 1.0)        # axle
WHEEL_COLOR = (0.12, 0.12, 0.12, 1.0)       # wheel rubber
HUB_COLOR = (0.45, 0.45, 0.48, 1.0)         # wheel hub


# ── Helpers ────────────────────────────────────────────────────────────

def make_material(name, color, roughness=0.7, metallic=0.0):
    """Create a flat-color Principled BSDF material."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    if metallic > 0:
        bsdf.inputs["Metallic"].default_value = metallic
    return mat


def add_box(name, size, location, material):
    """Add a cube with given dimensions and position."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(scale=True)
    obj.data.materials.append(material)
    return obj


def add_cylinder(name, radius, depth, location, rotation=(0, 0, 0), material=None, segments=20):
    """Add a cylinder with given radius, depth, and position."""
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius,
        depth=depth,
        vertices=segments,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    if material:
        obj.data.materials.append(material)
    return obj


def shade_flat(obj):
    """Apply flat shading to an object."""
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_flat()
    obj.select_set(False)


def build_skateboard(variant_name, colors):
    """Build a single skateboard and return the root empty."""
    parts = []

    # ── Materials ──
    mat_deck = make_material(f"{variant_name}_Deck", colors["deck"], roughness=0.7)
    mat_grip = make_material(f"{variant_name}_Grip", colors["grip"], roughness=0.95)
    mat_truck = make_material("Truck", TRUCK_COLOR, roughness=0.3, metallic=0.5)
    mat_axle = make_material("Axle", AXLE_COLOR, roughness=0.25, metallic=0.6)
    mat_wheel = make_material("Wheel", WHEEL_COLOR, roughness=0.55)
    mat_hub = make_material("Hub", HUB_COLOR, roughness=0.3, metallic=0.4)

    # ── Deck ──
    deck_center = add_box(
        "Deck_Center",
        size=(0.62, 0.045, 1.4),
        location=(0, 0.13, 0),
        material=mat_deck,
    )
    parts.append(deck_center)

    deck_nose = add_box(
        "Deck_Nose",
        size=(0.48, 0.04, 0.4),
        location=(0, 0.13, 0.9),
        material=mat_deck,
    )
    parts.append(deck_nose)

    deck_tail = add_box(
        "Deck_Tail",
        size=(0.48, 0.04, 0.35),
        location=(0, 0.13, -0.88),
        material=mat_deck,
    )
    parts.append(deck_tail)

    # ── Grip tape ──
    grip = add_box(
        "Grip_Tape",
        size=(0.58, 0.015, 1.3),
        location=(0, 0.16, 0),
        material=mat_grip,
    )
    parts.append(grip)

    # ── Trucks ──
    for side, z_sign in [("Front", 1), ("Rear", -1)]:
        z_pos = z_sign * 0.55

        baseplate = add_box(
            f"{side}_Baseplate",
            size=(0.18, 0.04, 0.14),
            location=(0, 0.08, z_pos),
            material=mat_truck,
        )
        parts.append(baseplate)

        axle = add_cylinder(
            f"{side}_Axle",
            radius=0.015,
            depth=0.58,
            location=(0, 0.06, z_pos),
            rotation=(0, math.pi / 2, 0),
            material=mat_axle,
        )
        parts.append(axle)

        # ── Wheels ──
        for x_sign in [-1, 1]:
            x_pos = x_sign * 0.30

            wheel = add_cylinder(
                f"{side}_Wheel_{'L' if x_sign < 0 else 'R'}",
                radius=0.055,
                depth=0.07,
                location=(x_pos, 0.03, z_pos),
                rotation=(0, math.pi / 2, 0),
                material=mat_wheel,
                segments=20,
            )
            parts.append(wheel)

            hub = add_cylinder(
                f"{side}_Hub_{'L' if x_sign < 0 else 'R'}",
                radius=0.025,
                depth=0.075,
                location=(x_pos, 0.03, z_pos),
                rotation=(0, math.pi / 2, 0),
                material=mat_hub,
                segments=12,
            )
            parts.append(hub)

    # ── Apply flat shading to all parts ──
    for part in parts:
        shade_flat(part)

    # ── Parent all parts to an empty ──
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = f"Skateboard_{variant_name.capitalize()}"

    for part in parts:
        part.parent = root

    return root


def export_gltf(filepath):
    """Export selected objects (or scene) as glTF 2.0 .glb."""
    bpy.ops.export_scene.gltf(
        filepath=filepath,
        export_format='GLB',
        export_apply=True,
    )


# ── Main ───────────────────────────────────────────────────────────────

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(os.path.dirname(script_dir))
    resources_dir = os.path.join(project_root, "Resources")
    os.makedirs(resources_dir, exist_ok=True)

    # Clear default scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

    # Build each variant as a collection
    for variant_name, colors in VARIANTS.items():
        # Create a collection for this variant
        collection = bpy.data.collections.new(f"Skateboard_{variant_name.capitalize()}")
        bpy.context.scene.collection.children.link(collection)

        # Build the skateboard
        root = build_skateboard(variant_name, colors)

        # Move root and all children into the collection
        for obj in [root] + list(root.children):
            for col in obj.users_collection:
                col.objects.unlink(obj)
            collection.objects.link(obj)

        # Hide other variants, show this one
        for col in bpy.data.collections:
            col.hide_viewport = True
            col.hide_render = True
        collection.hide_viewport = False
        collection.hide_render = False

        # Export this variant as .glb
        glb_path = os.path.join(resources_dir, f"skateboard_{variant_name}.glb")
        # Select only objects in this collection for export
        bpy.ops.object.select_all(action='DESELECT')
        for obj in collection.objects:
            obj.select_set(True)
        export_gltf(glb_path)
        print(f"Exported: {glb_path}")

    # Show all collections for the .blend save
    for col in bpy.data.collections:
        col.hide_viewport = False
        col.hide_render = False

    # Save .blend file with all variants
    blend_path = os.path.join(resources_dir, "skateboards.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved: {blend_path}")


if __name__ == "__main__":
    main()
