# aurora_lp_pipeline.py — PROJETO:AURORA · Pipeline HP→LP definitivo (v2, 2026-07-10)
# ---------------------------------------------------------------------------------
# Converte um asset high-poly de IA/Tripo (~2M tris) em low-poly game-ready com
# BaseColor+Normal bakeados em atlas 2048. UM ASSET POR VEZ — nunca em lote cego.
#
# Este script substitui o aurora_hp_to_lp_bake.py original e codifica TODOS os
# aprendizados da sessão de otimização (20 assets, ~32M→135k tris):
#
# ╔══════════════════ APRENDIZADOS QUE MOLDARAM ESTE PIPELINE ══════════════════╗
# ║ 1. DECIMATE COLLAPSE, NUNCA DISSOLVE/REMESH.                                 ║
# ║    `dissolve_limited` e `Decimate(DISSOLVE)` CRASHARAM o Blender 2× em      ║
# ║    malhas de 2M tris (single-thread, estoura RAM). Voxel remesh arredonda    ║
# ║    arestas vivas de hard-surface e apaga panel lines. Collapse puro é        ║
# ║    rápido (~1s), leve, e segura a silhueta até em feixes de laser finos.     ║
# ║                                                                              ║
# ║ 2. BAKE DE BASECOLOR VIA **EMIT**, NUNCA PASS DIFFUSE.                       ║
# ║    O pass DIFFUSE/COLOR do Cycles zera o albedo em material metálico         ║
# ║    (Aurora_Box_01 saiu quase preto). Religar basecolor→Emission→Output       ║
# ║    transfere o albedo fiel para dielétrico E metal. Restaurar depois.        ║
# ║                                                                              ║
# ║ 3. BAKE A PARTIR DE **MID-POLY 300k** COM ORIGINAIS PURGADOS.                ║
# ║    Bake selected-to-active direto do HP multi-parte (50-80 objetos, 2M       ║
# ║    tris) crashou o Cycles 2× (DataFile, 62 partes). Solução: juntar as       ║
# ║    partes numa cópia, colapsar para ~300k (visualmente idêntico para         ║
# ║    transferência), DELETAR os originais da cena e purgar orfãos. A cena      ║
# ║    de bake fica 6× menor com 1 único objeto de origem. Zero crashes.         ║
# ║                                                                              ║
# ║ 4. TEXTURAS DE ORIGEM >1024 SÃO REDUZIDAS NA MEMÓRIA (img.scale).            ║
# ║    Dezenas de 4K decodificadas = GBs de RAM. O atlas final é 2048 e cada     ║
# ║    parte ocupa fração dele — 1024 por parte é mais que suficiente.           ║
# ║    Os arquivos em disco NÃO são tocados.                                     ║
# ║                                                                              ║
# ║ 5. TEXTURAS TRIPO MORAM NA PASTA IRMÃ `.fbm` DO FBX.                         ║
# ║    Copiar só o .fbx quebra os links ("texturas ausentes" não significa       ║
# ║    inexistentes!). Este script procura e religa automaticamente. GLB traz    ║
# ║    tudo embutido; alguns FBX (Pilar) também têm texturas packed.             ║
# ║                                                                              ║
# ║ 6. PIVÔ: o FBX exportado pode ficar com pivô no MEIO do modelo (Painel_      ║
# ║    Porta afundou 0,73m no Unity). No Unity, aterre SEMPRE pelo AABB real     ║
# ║    da mesh (mesh.bounds × localToWorldMatrix), nunca por renderer.bounds     ║
# ║    em edit mode (pode estar desatualizado após rotação).                     ║
# ║                                                                              ║
# ║ 7. SKINNED (robô, Dr. Elias): NÃO usar este pipeline de join+bake!           ║
# ║    Decimar cada parte individualmente (Collapse preserva vertex groups       ║
# ║    e UVs), manter armature e materiais originais, exportar ARMATURE+MESH     ║
# ║    sem bake_space_transform. No Unity: importar como Humanoid.               ║
# ║                                                                              ║
# ║ 8. NO UNITY, DEPOIS: colliders de trigger em unidades LOCAIS (dividir        ║
# ║    pelo lossyScale!) e ~20% menores que o visual (margem de perdão).         ║
# ║    Editar cenas por script só com o Editor numa CENA NEUTRA (o editor        ║
# ║    regrava a cena da memória por cima e destrói trabalho).                   ║
# ╚══════════════════════════════════════════════════════════════════════════════╝
#
# USO (Blender aberto com o asset já importado, via Text Editor ou MCP):
#   preencha CONFIG e rode. O HP fica escondido (nunca deletado), o LP sai
#   selecionado, FBX + PNGs + .blend são salvos nos diretórios configurados.

import bpy, os, math, time
import numpy as np

# ----------------------------------------------------------------- CONFIG ---
CONFIG = {
    "name": "MeuAsset",            # nome Unity-safe do asset (vira <name>_LP)
    "target_tris": 5000,           # 3000 prop pequeno · 5000 obstáculo · 8000 peça-chave
    "tex_size": 2048,              # 1024/2048; 4096 SÓ hero; 8192 PROIBIDO
    "mid_tris": 300_000,           # mid-poly de bake (aprendizado #3)
    "fbm_dir": "",                 # pasta .fbm para religar texturas (aprendizado #5); "" = pular
    "out_fbx_dir": "",             # ex.: .../03_LowPoly_FBX/<name>
    "tex_dir": "",                 # ex.: .../04_Baked_Textures/<name>
    "blend_dir": "",               # ex.: Backups_Optimization/02_Blender_Work/<name>/blend
}

# -------------------------------------------------------------- helpers ---
def log(m): print("[aurora_lp] " + m)

def relink_fbm(fbm_dir):
    """Aprendizado #5: religa imagens ausentes à pasta .fbm de origem."""
    if not fbm_dir or not os.path.isdir(fbm_dir):
        return 0
    n = 0
    for img in bpy.data.images:
        if img.name in ("Render Result", "Viewer Node"):
            continue
        ok = img.packed_file or (img.filepath and os.path.exists(bpy.path.abspath(img.filepath)))
        cand = os.path.join(fbm_dir, img.name)
        if not ok and os.path.exists(cand):
            img.filepath, img.source = cand, 'FILE'
            img.reload(); n += 1
    return n

def downscale_guard(limit=1024):
    """Aprendizado #4: reduz texturas de origem na memória (disco intacto)."""
    n = 0
    for img in bpy.data.images:
        if img.name in ("Render Result", "Viewer Node"):
            continue
        if img.size[0] > limit or img.size[1] > limit:
            img.scale(limit, limit); n += 1
    return n

def solo(*objs, active=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]

def join_copy(sources, new_name):
    """Duplica as partes e junta em 1 objeto com transforms aplicados + weld."""
    solo(*sources, active=sources[0])
    bpy.ops.object.duplicate(linked=False)
    if len(bpy.context.selected_objects) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = new_name; o.data.name = new_name + "_mesh"
    solo(o)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=0.0001)   # solda shells de IA
    bpy.ops.object.mode_set(mode='OBJECT')
    return o

def collapse(o, target):
    """Aprendizado #1: Collapse puro — o ÚNICO redutor seguro em 2M tris."""
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    if tris <= target: return tris
    solo(o)
    m = o.modifiers.new("Collapse", 'DECIMATE')
    m.decimate_type = 'COLLAPSE'
    m.ratio = target / tris
    m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def new_img(name, size, cs):
    old = bpy.data.images.get(name)
    if old: bpy.data.images.remove(old)
    im = bpy.data.images.new(name, size, size, alpha=False)
    im.colorspace_settings.name = cs
    return im

def rewire_emit(obj):
    """Aprendizado #2: basecolor→Emission→Output para bake de albedo fiel."""
    restore = []
    for slot in obj.material_slots:
        m = slot.material
        if not (m and m.use_nodes): continue
        nt = m.node_tree
        out = next(n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL' and n.is_active_output)
        orig = out.inputs['Surface'].links[0].from_socket if out.inputs['Surface'].links else None
        b = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        base = b.inputs['Base Color'].links[0].from_socket if (b and b.inputs['Base Color'].links) else None
        em = nt.nodes.new('ShaderNodeEmission')
        if base: nt.links.new(base, em.inputs['Color'])
        elif b:  em.inputs['Color'].default_value = b.inputs['Base Color'].default_value
        nt.links.new(em.outputs['Emission'], out.inputs['Surface'])
        restore.append((nt, out, orig, em))
    return restore

def unwire_emit(restore):
    for nt, out, orig, em in restore:
        if orig is not None: nt.links.new(orig, out.inputs['Surface'])
        nt.nodes.remove(em)

def composite_misses(main, fb, is_normal):
    """Preenche texels que o raio tight perdeu com o resultado do raio longo."""
    a = np.empty(len(main.pixels), dtype=np.float32)
    b = np.empty(len(fb.pixels), dtype=np.float32)
    main.pixels.foreach_get(a); fb.pixels.foreach_get(b)
    a4, b4 = a.reshape(-1, 4), b.reshape(-1, 4)
    if is_normal:
        miss = a4[:, 2] < 0.1
        a4[miss] = b4[miss]
        a4[miss & (b4[:, 2] < 0.1)] = [0.5, 0.5, 1.0, 1.0]
    else:
        miss = np.all(a4[:, :3] < 0.004, axis=1)
        a4[miss] = b4[miss]
    main.pixels.foreach_set(a4.reshape(-1))
    return int(miss.sum())

# ------------------------------------------------------------------ main ---
def run(cfg):
    t0 = time.time()
    name, TT, TEX = cfg["name"], cfg["target_tris"], cfg["tex_size"]
    assert TEX <= 4096, "8192 PROIBIDO (e 4096 so em hero props)"
    scene = bpy.context.scene
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    sources = [o for o in bpy.data.objects if o.type == 'MESH' and not o.name.endswith(("_LP", "_MID"))]
    assert sources, "nenhuma malha HP na cena"
    if any(o.find_armature() for o in sources):
        raise RuntimeError("asset SKINNED — use o fluxo por-parte (aprendizado #7), nao este script")

    log("religadas %d texturas .fbm" % relink_fbm(cfg["fbm_dir"]))
    log("reduzidas %d texturas >1024" % downscale_guard())

    total = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in sources)
    log("HP: %d partes, %s tris" % (len(sources), format(total, ",")))

    # LP: join → weld → collapse → smooth 35° → UV novo
    lp = join_copy(sources, name + "_LP")
    final = collapse(lp, TT)
    bpy.ops.object.shade_smooth()
    try: bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))
    except Exception: pass
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.002)
    bpy.ops.uv.select_all(action='SELECT')
    bpy.ops.uv.pack_islands(rotate=True, margin=0.002)
    bpy.ops.object.mode_set(mode='OBJECT')
    log("LP: %d tris" % final)

    # MID de bake (aprendizado #3) + purge dos originais
    mid = join_copy(sources, name + "_MID")
    collapse(mid, cfg["mid_tris"])
    for o in sources:
        bpy.data.objects.remove(o, do_unlink=True)   # HP some DA CENA (arquivo original intacto)
    for _ in range(3):
        bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    log("MID de bake pronto; originais purgados da cena")

    # material do LP + Cycles GPU
    col = new_img(name + "_BaseColor", TEX, 'sRGB')
    nrm = new_img(name + "_Normal", TEX, 'Non-Color')
    mat = bpy.data.materials.new(name + "_LP_Baked_Mat"); mat.use_nodes = True
    nt = mat.node_tree; bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs['Roughness'].default_value = 0.6
    bsdf.inputs['Metallic'].default_value = 0.2      # acabamento uniforme nao-metalico
    tc = nt.nodes.new('ShaderNodeTexImage'); tc.image = col
    nt.links.new(tc.outputs['Color'], bsdf.inputs['Base Color'])
    tn = nt.nodes.new('ShaderNodeTexImage'); tn.image = nrm
    nm = nt.nodes.new('ShaderNodeNormalMap')
    nt.links.new(tn.outputs['Color'], nm.inputs['Color'])
    nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    lp.data.materials.clear(); lp.data.materials.append(mat)
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 4
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
        for d in prefs.devices: d.use = True
        scene.cycles.device = 'GPU'
    except Exception: scene.cycles.device = 'CPU'
    scene.render.bake.margin = 8
    try: scene.render.bake.margin_type = 'ADJACENT_FACES'
    except Exception: pass

    # bake: EMIT albedo + NORMAL, tight + fallback + composite
    def sel(node):
        for n in nt.nodes: n.select = False
        node.select = True; nt.nodes.active = node
    MD = max(lp.dimensions)
    tight = dict(cage_extrusion=0.013*MD, max_ray_distance=0.035*MD)
    far   = dict(cage_extrusion=0.025*MD, max_ray_distance=0.10*MD)
    solo(mid, lp, active=lp)

    restore = rewire_emit(mid)
    sel(tc); bpy.ops.object.bake(type='EMIT', use_selected_to_active=True, use_clear=True, **tight)
    fbc = new_img("_fb_c", TEX, 'sRGB')
    scr = nt.nodes.new('ShaderNodeTexImage'); scr.image = fbc
    sel(scr); bpy.ops.object.bake(type='EMIT', use_selected_to_active=True, use_clear=True, **far)
    unwire_emit(restore)

    sel(tn); bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', use_selected_to_active=True, use_clear=True, **tight)
    fbn = new_img("_fb_n", TEX, 'Non-Color'); scr.image = fbn
    sel(scr); bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', use_selected_to_active=True, use_clear=True, **far)

    log("composite: %d cor / %d normal texels preenchidos" %
        (composite_misses(col, fbc, False), composite_misses(nrm, fbn, True)))
    nt.nodes.remove(scr)
    bpy.data.images.remove(fbc); bpy.data.images.remove(fbn)

    # saidas
    if cfg["tex_dir"]:
        os.makedirs(cfg["tex_dir"], exist_ok=True)
        for im, sfx in ((col, "_BaseColor"), (nrm, "_Normal")):
            im.filepath_raw = os.path.join(cfg["tex_dir"], name + sfx + ".png")
            im.file_format = 'PNG'; im.save(); im.pack()
    mid.hide_set(True); mid.hide_render = True     # mid fica como referencia, escondido
    solo(lp)
    if cfg["out_fbx_dir"]:
        os.makedirs(cfg["out_fbx_dir"], exist_ok=True)
        bpy.ops.export_scene.fbx(
            filepath=os.path.join(cfg["out_fbx_dir"], name + "_LP.fbx"),
            use_selection=True, object_types={'MESH'}, use_mesh_modifiers=True,
            mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
            path_mode='COPY', embed_textures=False, apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_NONE', bake_space_transform=True)
    if cfg["blend_dir"]:
        os.makedirs(cfg["blend_dir"], exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(cfg["blend_dir"], name + "_LP_working.blend"))
    log("CONCLUIDO %s: %s -> %d tris em %.0fs" % (name, format(total, ","), final, time.time() - t0))

run(CONFIG)
