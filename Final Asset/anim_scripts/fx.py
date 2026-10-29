# Particle FX for the episodes. Re-running is safe (rebuilds every FX_ object).
#  - fountain spray in EP1-4 (droplets arc from around the star into the basin, like Unity's WaterSpray)
#  - EP4: sparkle dust trailing the rainbow cat + a burst when it pops in
# Run AFTER the ep scripts (EP4 reads the rainbow cat's appear frame from its Mover scale keys).
import bpy, math, bmesh

EPS = {"EP1": "EP1_TownTour", "EP2": "EP2_FoodThieves", "EP3": "EP3_MartStories", "EP4": "EP4_RainbowFinale"}
HIDE_Z = -100.0   # instance sources live far below the island


def emis_mat(name, color, strength, base=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs["Base Color"].default_value = (*(base or color), 1)
    b.inputs["Emission Color"].default_value = (*color, 1)
    b.inputs["Emission Strength"].default_value = strength
    b.inputs["Roughness"].default_value = 0.2
    return m


def sphere(name, r, mat, stretch=1.0, subdiv=2):
    me = bpy.data.meshes.get(name) or bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    for v in bm.verts:
        v.co.x *= stretch            # particle X axis follows velocity
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.clear(); me.materials.append(mat)
    o = bpy.data.objects.get(name) or bpy.data.objects.new(name, me)
    o.location = (0, 0, HIDE_Z)
    return o


def link(sc, o):
    if o.name not in sc.collection.objects:
        sc.collection.objects.link(o)


def clear_fx(sc, prefix):
    for o in [o for o in sc.objects if o.name.startswith(prefix)]:
        bpy.data.objects.remove(o)


def add_psys(o, name):
    md = o.modifiers.new(name, 'PARTICLE_SYSTEM')
    ps = md.particle_system
    old = bpy.data.particles.get(name)
    if old:
        bpy.data.particles.remove(old)
    ps.settings = bpy.data.particles.new(name)
    ps.settings.name = name
    return ps.settings


# ----------------------------------------------------------------- fountain
DROP = sphere("FXsrc_Droplet", 0.02, emis_mat("M_FX_Droplet", (0.55, 0.82, 1.0), 0.35, base=(0.62, 0.86, 1.0)), stretch=2.0)


def fountain(tag, sc):
    clear_fx(sc, "%s_FX_Fountain" % tag)
    link(sc, DROP)
    fw = next(o for o in sc.objects if o.name.startswith("Fountain_Water"))
    c = fw.matrix_world.translation
    # vertical band around the star base: normals point outward, Z-align adds the lift
    me = bpy.data.meshes.new("%s_FX_FountainRing" % tag)
    bm = bmesh.new()
    n, r = 24, 0.32
    lo = [bm.verts.new((r * math.cos(i * math.tau / n), r * math.sin(i * math.tau / n), 0.0)) for i in range(n)]
    hi = [bm.verts.new((v.co.x, v.co.y, 0.04)) for v in lo]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((lo[i], lo[j], hi[j], hi[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    em = bpy.data.objects.new("%s_FX_Fountain" % tag, me)
    em.location = (c.x, c.y, c.z + 0.04)
    sc.collection.objects.link(em)
    s = add_psys(em, "%s_FX_FountainSpray" % tag)
    s.count = int((sc.frame_end + 30) * 10)   # ~170 droplets in the air at once
    s.frame_start = -30            # already spraying on frame 1
    s.frame_end = sc.frame_end
    s.lifetime = 17
    s.lifetime_random = 0.25
    s.emit_from = 'FACE'
    s.normal_factor = 0.8
    s.object_align_factor = (0, 0, 3.2)
    s.factor_random = 0.35
    s.render_type = 'OBJECT'
    s.instance_object = DROP
    s.particle_size = 1.0
    s.size_random = 0.5
    s.use_rotations = True
    s.rotation_mode = 'VEL'
    s.display_method = 'DOT'
    em.show_instancer_for_render = False
    em.show_instancer_for_viewport = False
    return em


# ----------------------------------------------------------------- EP4 rainbow sparkle dust
SPARK_COLS = [("Pink", (1.0, 0.45, 0.8)), ("Gold", (1.0, 0.85, 0.35)),
              ("Cyan", (0.4, 0.9, 1.0)), ("Lilac", (0.75, 0.55, 1.0))]


def spark_collection():
    col = bpy.data.collections.get("FXsrc_Sparkles") or bpy.data.collections.new("FXsrc_Sparkles")
    for name, c in SPARK_COLS:
        o = sphere("FXsrc_Spark_" + name, 0.016, emis_mat("M_FX_Spark_" + name, c, 3.5), subdiv=1)
        if o.name not in col.objects:
            col.objects.link(o)
        o.location = (0, 0, 0)   # collection instances use the source offset
    return col


def appear_frame(mover):
    act = mover.animation_data.action if mover.animation_data else None
    if act:
        for fc in L_fcurves(act):
            if fc.data_path == "scale" and fc.array_index == 2:
                for f in range(1, 5000):
                    if fc.evaluate(f) > 0.05:
                        return f
    return 1


def L_fcurves(act):
    if hasattr(act, "fcurves"):
        return list(act.fcurves)
    out = []                      # layered actions (4.4+)
    for layer in act.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                out += list(bag.fcurves)
    return out


def rainbow_dust(sc):
    clear_fx(sc, "EP4_FX_Rainbow")
    mover = sc.objects["EP4_Mover_Rainbow"]
    col = spark_collection()     # not linked to the scene: only its instances render
    f0 = appear_frame(mover)
    me = bpy.data.meshes.new("EP4_FX_RainbowEmit")
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.28)
    bm.to_mesh(me); bm.free()

    def emitter(name, z):
        o = bpy.data.objects.new(name, me)
        sc.collection.objects.link(o)
        o.parent = mover
        o.location = (0, 0, z)
        o.show_instancer_for_render = False
        o.show_instancer_for_viewport = False
        return o

    def sparkle(s):
        s.render_type = 'COLLECTION'
        s.instance_collection = col
        s.use_collection_pick_random = True
        s.particle_size = 1.0
        s.size_random = 0.7
        s.display_method = 'DOT'
        s.effector_weights.gravity = 0.0

    # trailing dust: emitted in world space, so it stays behind as the cat walks
    d = emitter("EP4_FX_RainbowDust", 0.42)
    s = add_psys(d, "EP4_FX_RainbowDust")
    sparkle(s)
    s.count = int((sc.frame_end - f0) * 2.5)
    s.frame_start = f0 + 4
    s.frame_end = sc.frame_end
    s.lifetime = 45
    s.lifetime_random = 0.5
    s.emit_from = 'VOLUME'
    s.normal_factor = 0.05
    s.object_align_factor = (0, 0, 0.12)   # slow upward drift
    s.factor_random = 0.08
    s.brownian_factor = 0.04
    # pop-in burst
    b = emitter("EP4_FX_RainbowBurst", 0.45)
    s = add_psys(b, "EP4_FX_RainbowBurst")
    sparkle(s)
    s.count = 160
    s.frame_start = f0
    s.frame_end = f0 + 6
    s.lifetime = 40
    s.lifetime_random = 0.4
    s.emit_from = 'VOLUME'
    s.normal_factor = 0.0
    s.factor_random = 1.6
    s.drag_factor = 0.6
    return f0


def run():
    out = {}
    for tag, sn in EPS.items():
        sc = bpy.data.scenes[sn]
        fountain(tag, sc)
        out[tag] = "fountain"
    out["EP4_appear"] = rainbow_dust(bpy.data.scenes["EP4_RainbowFinale"])
    return out


if __name__ == "__main__" or True:
    RESULT = run()
