import bpy
import json
import math
import os
from mathutils import Vector


PROJECT_ROOT = r"C:\ProjetoAurora-Game"
BLEND_PATH = os.path.join(PROJECT_ROOT, "SourceAssets", "Blender", "AuroraCoin", "Aurora_HoloCoin.blend")
FBX_PATH = os.path.join(PROJECT_ROOT, "Assets", "_ProjectAurora", "Art", "Collectibles", "AuroraCoin", "Models", "Aurora_HoloCoin.fbx")
PREVIEW_PATH = os.path.join(PROJECT_ROOT, "SourceAssets", "Blender", "AuroraCoin", "Aurora_HoloCoin_Preview.png")
PREVIEW_BACK_PATH = os.path.join(PROJECT_ROOT, "SourceAssets", "Blender", "AuroraCoin", "Aurora_HoloCoin_Preview_Back.png")
METRICS_PATH = os.path.join(PROJECT_ROOT, "SourceAssets", "Blender", "AuroraCoin", "Aurora_HoloCoin_metrics.json")

MODEL_NAMES = (
    "Coin_Frame",
    "Coin_HologramCore",
    "Coin_AuroraSymbol",
    "Coin_EmissionDetails",
    "Coin_BackPlate",
)


def ensure_parent(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def set_input(node, name, value):
    socket = node.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def make_material(name, base_color, metallic, roughness, emission=None, emission_strength=0.0, alpha=1.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*base_color[:3], alpha)
    nodes = material.node_tree.nodes
    principled = nodes.get("Principled BSDF")
    set_input(principled, "Base Color", (*base_color[:3], 1.0))
    set_input(principled, "Metallic", metallic)
    set_input(principled, "Roughness", roughness)
    set_input(principled, "Alpha", alpha)
    if emission is not None:
        set_input(principled, "Emission Color", (*emission[:3], 1.0))
        set_input(principled, "Emission", (*emission[:3], 1.0))
        set_input(principled, "Emission Strength", emission_strength)
    if alpha < 1.0:
        set_input(principled, "Transmission Weight", 0.22)
        set_input(principled, "Transmission", 0.22)
        if hasattr(material, "surface_render_method"):
            material.surface_render_method = "DITHERED"
        elif hasattr(material, "blend_method"):
            material.blend_method = "BLEND"
        if hasattr(material, "use_transparency_overlap"):
            material.use_transparency_overlap = False
    return material


def regular_polygon(radius, sides=8, angle_offset=math.pi / 8.0):
    return [
        (radius * math.cos(angle_offset + 2.0 * math.pi * i / sides),
         radius * math.sin(angle_offset + 2.0 * math.pi * i / sides))
        for i in range(sides)
    ]


def create_prism(name, points, depth, material, y_center=0.0, bevel=0.0):
    half = depth * 0.5
    count = len(points)
    verts = [(x, y_center - half, z) for x, z in points]
    verts += [(x, y_center + half, z) for x, z in points]
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    for i in range(count):
        j = (i + 1) % count
        faces.append((i, j, count + j, count + i))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    if bevel > 0.0:
        add_bevel(obj, bevel)
    return obj


def create_ring(name, outer_points, inner_points, depth, material, y_center=0.0, bevel=0.0):
    count = len(outer_points)
    half = depth * 0.5
    verts = []
    verts += [(x, y_center - half, z) for x, z in outer_points]
    verts += [(x, y_center + half, z) for x, z in outer_points]
    verts += [(x, y_center - half, z) for x, z in inner_points]
    verts += [(x, y_center + half, z) for x, z in inner_points]
    faces = []
    of, ob, inf, inb = 0, count, count * 2, count * 3
    for i in range(count):
        j = (i + 1) % count
        faces.append((of + j, of + i, inf + i, inf + j))
        faces.append((ob + i, ob + j, inb + j, inb + i))
        faces.append((of + i, of + j, ob + j, ob + i))
        faces.append((inf + j, inf + i, inb + i, inb + j))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    if bevel > 0.0:
        add_bevel(obj, bevel)
    return obj


def create_box(name, location, dimensions, rotation_y, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=(0.0, rotation_y, 0.0))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    if bevel > 0.0:
        add_bevel(obj, bevel)
    return obj


def create_bar(name, start, end, width, depth, y_center, material, bevel=0.0):
    x1, z1 = start
    x2, z2 = end
    dx, dz = x2 - x1, z2 - z1
    length = math.hypot(dx, dz)
    angle = math.atan2(dx, dz)
    return create_box(
        name,
        ((x1 + x2) * 0.5, y_center, (z1 + z2) * 0.5),
        (width, depth, length),
        angle,
        material,
        bevel,
    )


def add_bevel(obj, width, segments=1):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new("EdgeBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(28.0)
    modifier.harden_normals = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def join_parts(parts, final_name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    result = bpy.context.object
    result.name = final_name
    result.data.name = final_name + "_Mesh"
    for polygon in result.data.polygons:
        polygon.use_smooth = True
    return result


def triangulate_and_uv(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    triangulate = obj.modifiers.new("ExportTriangulate", "TRIANGULATE")
    triangulate.keep_custom_normals = True
    bpy.ops.object.modifier_apply(modifier=triangulate.name)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if obj.type == "MESH" and obj.data.polygons:
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        try:
            bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
        except TypeError:
            bpy.ops.uv.smart_project()
        bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def build_asset():
    frame_mat = make_material(
        "MAT_AuroraCoin_Frame",
        (0.025, 0.045, 0.075, 1.0),
        metallic=0.78,
        roughness=0.27,
    )
    hologram_mat = make_material(
        "MAT_AuroraCoin_Hologram",
        (0.01, 0.30, 0.48, 1.0),
        metallic=0.08,
        roughness=0.12,
        emission=(0.0, 0.62, 1.0, 1.0),
        emission_strength=0.9,
        alpha=0.58,
    )
    emission_mat = make_material(
        "MAT_AuroraCoin_Emission",
        (0.02, 0.72, 1.0, 1.0),
        metallic=0.0,
        roughness=0.16,
        emission=(0.0, 0.82, 1.0, 1.0),
        emission_strength=3.0,
    )

    root = bpy.data.objects.new("Aurora_HoloCoin", None)
    bpy.context.collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.06

    frame_parts = []
    frame_parts.append(create_ring(
        "Frame_BaseRing",
        regular_polygon(0.240),
        regular_polygon(0.184),
        0.086,
        frame_mat,
        bevel=0.0035,
    ))
    frame_parts.append(create_ring(
        "Frame_InnerBezelFront",
        regular_polygon(0.190),
        regular_polygon(0.176),
        0.012,
        frame_mat,
        y_center=-0.047,
        bevel=0.0018,
    ))
    frame_parts.append(create_ring(
        "Frame_InnerBezelBack",
        regular_polygon(0.190),
        regular_polygon(0.176),
        0.012,
        frame_mat,
        y_center=0.047,
        bevel=0.0018,
    ))
    for side in range(8):
        theta = 2.0 * math.pi * side / 8.0
        x = 0.211 * math.cos(theta)
        z = 0.211 * math.sin(theta)
        rotation_y = -theta - math.pi * 0.5
        panel_width = 0.106 if side % 2 == 0 else 0.095
        for y in (-0.050, 0.050):
            frame_parts.append(create_box(
                f"Frame_Panel_{side}_{'F' if y < 0 else 'B'}",
                (x, y, z),
                (panel_width, 0.014, 0.039),
                rotation_y,
                frame_mat,
                bevel=0.003,
            ))
    frame = join_parts(frame_parts, "Coin_Frame")

    core = create_prism(
        "Coin_HologramCore",
        regular_polygon(0.174),
        0.047,
        hologram_mat,
        bevel=0.003,
    )

    back_parts = [
        create_prism(
            "BackPlate_Core",
            regular_polygon(0.128),
            0.008,
            frame_mat,
            y_center=0.028,
            bevel=0.002,
        ),
        create_ring(
            "BackPlate_Ring",
            regular_polygon(0.151),
            regular_polygon(0.139),
            0.008,
            frame_mat,
            y_center=0.027,
            bevel=0.0015,
        ),
    ]
    back_plate = join_parts(back_parts, "Coin_BackPlate")

    symbol_parts = []
    for y in (-0.031, 0.035):
        symbol_parts.extend([
            create_bar("Symbol_OuterLeft", (-0.083, -0.087), (0.0, 0.083), 0.020, 0.010, y, emission_mat, 0.0028),
            create_bar("Symbol_OuterRight", (0.0, 0.083), (0.083, -0.087), 0.020, 0.010, y, emission_mat, 0.0028),
            create_bar("Symbol_InnerLeft", (-0.047, -0.060), (0.0, 0.003), 0.014, 0.010, y - 0.0005, emission_mat, 0.0022),
            create_bar("Symbol_InnerRight", (0.0, 0.003), (0.047, -0.060), 0.014, 0.010, y - 0.0005, emission_mat, 0.0022),
        ])
        diamond = [(0.0, -0.049), (0.014, -0.067), (0.0, -0.085), (-0.014, -0.067)]
        symbol_parts.append(create_prism("Symbol_Diamond", diamond, 0.010, emission_mat, y_center=y, bevel=0.0015))
    symbol = join_parts(symbol_parts, "Coin_AuroraSymbol")

    emission_parts = []
    emission_parts.append(create_ring(
        "Emission_CoreRimFront",
        regular_polygon(0.178),
        regular_polygon(0.166),
        0.007,
        emission_mat,
        y_center=-0.056,
        bevel=0.0012,
    ))
    emission_parts.append(create_ring(
        "Emission_CoreRimBack",
        regular_polygon(0.178),
        regular_polygon(0.166),
        0.007,
        emission_mat,
        y_center=0.056,
        bevel=0.0012,
    ))
    for side in range(8):
        theta = 2.0 * math.pi * side / 8.0
        x = 0.215 * math.cos(theta)
        z = 0.215 * math.sin(theta)
        rotation_y = -theta - math.pi * 0.5
        bar_width = 0.050 if side % 2 == 0 else 0.034
        for y in (-0.059, 0.059):
            emission_parts.append(create_box(
                f"Emission_Bar_{side}_{'F' if y < 0 else 'B'}",
                (x, y, z),
                (bar_width, 0.008, 0.009),
                rotation_y,
                emission_mat,
                bevel=0.0018,
            ))
    emission = join_parts(emission_parts, "Coin_EmissionDetails")

    for obj in (frame, core, symbol, emission, back_plate):
        obj.parent = root
        triangulate_and_uv(obj)
        obj["aurora_asset_role"] = obj.name.replace("Coin_", "")
        obj["unity_scale_meters"] = 1.0

    root["asset_name"] = "Aurora_HoloCoin"
    root["dimensions_m"] = "0.48 x 0.48 x 0.118"
    root["orientation"] = "Blender Z-up; FBX -Z forward, Y up"
    bpy.context.view_layer.objects.active = root
    root.select_set(True)
    return root, [frame, core, symbol, emission, back_plate]


def point_camera(camera, target=(0.0, 0.0, 0.0)):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_preview(model_objects):
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        pass
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.filepath = PREVIEW_PATH
    scene.view_settings.exposure = -1.15
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass
    scene.world.color = (0.002, 0.006, 0.014)
    world_nodes = scene.world.node_tree.nodes if scene.world.use_nodes else None
    if world_nodes:
        background = world_nodes.get("Background")
        if background:
            background.inputs["Color"].default_value = (0.002, 0.008, 0.018, 1.0)
            background.inputs["Strength"].default_value = 0.22

    bpy.ops.object.camera_add(location=(0.46, -1.22, 0.28))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 66
    point_camera(camera, (0.0, 0.0, 0.0))
    scene.camera = camera

    lights = []
    for name, light_type, location, energy, color, size in (
        ("Preview_Key", "AREA", (-0.55, -0.70, 0.75), 240.0, (0.72, 0.90, 1.0), 0.55),
        ("Preview_Rim", "AREA", (0.70, 0.20, 0.35), 320.0, (0.0, 0.55, 1.0), 0.35),
        ("Preview_Fill", "AREA", (0.0, -0.15, -0.55), 110.0, (0.05, 0.24, 0.42), 0.45),
    ):
        data = bpy.data.lights.new(name=name, type=light_type)
        data.energy = energy
        data.color = color
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = location
        point_camera(light, (0.0, 0.0, 0.0))
        lights.append(light)

    ensure_parent(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)

    camera.location = (-0.46, 1.22, 0.28)
    point_camera(camera, (0.0, 0.0, 0.0))
    for light in lights:
        light.location.x *= -1.0
        light.location.y *= -1.0
        point_camera(light, (0.0, 0.0, 0.0))
    scene.render.filepath = PREVIEW_BACK_PATH
    bpy.ops.render.render(write_still=True)

    for obj in [camera, *lights]:
        bpy.data.objects.remove(obj, do_unlink=True)
    scene.camera = None


def collect_metrics(model_objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    total_vertices = 0
    total_triangles = 0
    per_object = {}
    for obj in model_objects:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        vertices = len(mesh.vertices)
        triangles = len(mesh.loop_triangles)
        per_object[obj.name] = {
            "vertices": vertices,
            "triangles": triangles,
            "materials": [slot.material.name for slot in obj.material_slots if slot.material],
        }
        total_vertices += vertices
        total_triangles += triangles
        evaluated.to_mesh_clear()
    return {
        "asset": "Aurora_HoloCoin",
        "dimensions_m": {"width": 0.48, "height": 0.48, "depth": 0.118},
        "vertices": total_vertices,
        "triangles": total_triangles,
        "objects": per_object,
        "materials": ["MAT_AuroraCoin_Frame", "MAT_AuroraCoin_Hologram", "MAT_AuroraCoin_Emission"],
        "source_blend": BLEND_PATH,
        "fbx": FBX_PATH,
        "preview": PREVIEW_PATH,
        "preview_back": PREVIEW_BACK_PATH,
    }


def save_and_export(root, model_objects, metrics):
    for path in (BLEND_PATH, FBX_PATH, METRICS_PATH):
        ensure_parent(path)

    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene["AuroraCoin_Metrics"] = json.dumps(metrics, sort_keys=True)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in model_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )

    with open(METRICS_PATH, "w", encoding="utf-8") as handle:
        json.dump(metrics, handle, indent=2, sort_keys=True)


def main():
    clear_scene()
    root, model_objects = build_asset()
    render_preview(model_objects)
    metrics = collect_metrics(model_objects)
    save_and_export(root, model_objects, metrics)
    print("AURORA_COIN_BUILD_OK")
    print(json.dumps(metrics, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
