import bpy, sys, math, importlib.util
from mathutils import Vector
P = r"C:\Users\mbc\Documents\YW_Project\CatMart\Final Asset\anim_scripts\catlib.py"
spec = importlib.util.spec_from_file_location("catlib", P); L = importlib.util.module_from_spec(spec); spec.loader.exec_module(L); sys.modules["catlib"] = L

END = 2700
E = L.Episode("EP4_RainbowFinale", "EP4")
L.park_carts_at(E, {"Cart_A": (-2.7, 2.3), "Cart_B": (2.7, 2.4)})
K = {}


def sync(*ts):
    f = max(t.f for t in ts)
    for t in ts:
        if f > t.f:
            t.idle(f - t.f)
    return f


GATE = (2.27, -6.99)
b = E.track("Black", (0.4, -2.8), L.yaw_to(1.3, -2.8))
r = E.track("Rainbow", (60, 0), 0)
w = E.track("White", (60, 5), 0)

# ---------------------------------------------------------------- 1. alone at sunset
K["start"] = b.idle(40, look=[(0, 0.9)])
K["walkOut"] = b.walk([(1.2, -4.4), (1.7, -5.55)], speed=0.55)
b.turn((0.0, 0.0), 20)
K["lookBack"] = b.idle(80, look=[(0, -0.45), (40, 0.45), (70, None)])
b.turn(GATE, 20)
APPEAR = b.f + 150
K["sulk"] = b.sit(APPEAR + 70 - b.f, "sulk")
OPEN = APPEAR + 140      # bars sink (like ExitGate.cs) once black has noticed

# ---------------------------------------------------------------- 2. rainbow appears on the EXIT bridge
r.idle(APPEAR - 1)
r.place((3.25, -9.5), L.yaw_to(-0.95, 2.55))
K["appear"] = APPEAR
r.spin(1.5, 54, 0.35)
r.act("cheer", 40)
# black notices
K["notice"] = b.idle(20, look=[(0, None)])
b.act("surprise", 45)
b.idle(40, look=[(0, 0.2)])
# rainbow walks in through the EXIT gate
r.idle(OPEN + 40 - r.f)
R_IN = r.f
K["rIn"] = r.walk([(2.8, -8.25), (2.3, -7.05), (1.98, -6.08)], speed=0.7)
b.idle(max(1, r.f - b.f))
f_meet = sync(b, r)
K["meet"] = f_meet
b.turn((1.98, -6.08), 10); r.turn((1.7, -5.55), 10)
r.act("shy", 80); b.act("shy", 80)
r.act("wave", 60, side="R")
b.idle(10)
b.act("wave", 60, side="L")
r.act("nod", 50)
b.act("nod", 40)
sync(b, r)
K["hug"] = b.act("hug", 80)
r.act("hug", 80)
r.act("cheer", 45); b.act("cheer", 45)
r.turn((1.0, -3.6), 12)
K["invite"] = r.act("point", 50)
b.idle(62)

# ---------------------------------------------------------------- 3. off to the plaza, play around the fountain
K["toPlaza"] = b.walk([(1.0, -3.7), (0.6, -2.55)], speed=1.2, style="trot")
r.walk([(1.3, -5.0), (1.45, -3.75), (1.25, -2.35)], speed=1.25, style="trot")
sync(b, r)
R = 2.35


def ring(a0, a1, n=30):
    return [(R * math.cos(a0 + (a1 - a0) * i / n), R * math.sin(a0 + (a1 - a0) * i / n)) for i in range(1, n + 1)]


a_r = math.atan2(-2.35, 1.25)
a_b = math.atan2(-2.55, 0.6)
K["lap"] = b.f
r.walk(ring(a_r, a_r + math.tau), speed=1.7, style="skip")
b.walk(ring(a_b, a_b + math.tau), speed=1.62, style="skip")
LAP_END = max(b.f, r.f)

# ---------------------------------------------------------------- 4. white joins, dance
SPOT_W, SPOT_B, SPOT_R = (-0.8, -2.6), (0.0, -2.75), (0.8, -2.6)
w.idle(LAP_END - 160)
w.place((-3.3, 1.0), L.yaw_to(1, -1))
K["white"] = w.act("wave", 50, side="both")
w.walk([(-2.4, -1.2), SPOT_W], speed=1.5, style="trot")
b.walk([SPOT_B], speed=0.8)
r.walk([SPOT_R], speed=0.8)
sync(b, r, w)
b.turn(0.0, 14); r.turn(0.0, 14); w.turn(0.0, 14)
b.act("surprise", 30)
w.act("cheer", 30)
r.act("wave", 30)
sync(b, r, w)
K["dance"] = b.f
DANCE = 320
for t in (b, r, w):
    t.act("dance", DANCE, beat=18, twist=0.3)
for t in (b, r, w):
    t.spin(1.0, 30, 0.3)
    t.act("cheer", 50)
sync(b, r, w)

# ---------------------------------------------------------------- 5. starry night, together
K["sit"] = b.f
b.sit(260, "lean", look=[(0, None)])
r.sit(260, "leanR")
w.sit(260, "rest", look=[(0, 0.5), (60, None)])
K["gaze"] = b.f
b.sit(END - b.f + 1, "gaze")
r.sit(END - r.f + 1, "gaze")
w.sit(END - w.f + 1, "gaze")

# rainbow pops in (scale) with a sparkle
sc_keys = {("scale", i): [(1, 0.0), (APPEAR - 1, 0.0), (APPEAR, 0.05), (APPEAR + 22, 1.15), (APPEAR + 34, 0.95), (APPEAR + 44, 1.0)] for i in range(3)}
L.exit_bars(E, open_at=OPEN)
E.bake(END, extra={"Rainbow": sc_keys})

# ---------------------------------------------------------------- sparkle light
lt = bpy.data.lights.get("EP4_Sparkle") or bpy.data.lights.new("EP4_Sparkle", 'POINT')
lt.color = (1.0, 0.75, 0.95)
lt.shadow_soft_size = 0.4
lo = bpy.data.objects.get("EP4_Sparkle") or bpy.data.objects.new("EP4_Sparkle", lt)
if lo.name not in E.sc.collection.objects:
    E.sc.collection.objects.link(lo)
lo.location = (3.25, -9.5, 1.0)
L.write_fcurves(lt, "EP4_SparkleE", {("energy", 0): [(1, 0), (APPEAR - 5, 0), (APPEAR + 8, 400), (APPEAR + 30, 160), (APPEAR + 70, 0)]}, "BEZIER")
L.write_fcurves(lt, "EP4_SparkleE", {("energy", 0): [(1, 0), (APPEAR - 5, 0), (APPEAR + 8, 400), (APPEAR + 30, 160), (APPEAR + 70, 0)]}, "BEZIER")
# soft glow that follows the rainbow cat for the rest of the film
gl = bpy.data.lights.get("EP4_RainbowGlow") or bpy.data.lights.new("EP4_RainbowGlow", 'POINT')
gl.color = (0.85, 0.8, 1.0)
go = bpy.data.objects.get("EP4_RainbowGlow") or bpy.data.objects.new("EP4_RainbowGlow", gl)
if go.name not in E.sc.collection.objects:
    E.sc.collection.objects.link(go)
go.parent = E.mover["Rainbow"]
go.location = (0, 0.1, 0.5)
NIGHT = K["dance"] - 200
L.write_fcurves(gl, "EP4_GlowE", {("energy", 0): [(1, 0), (APPEAR, 0), (APPEAR + 30, 6), (NIGHT, 10), (END, 18)]}, "BEZIER")

# ---------------------------------------------------------------- sunset -> night
sc = E.sc
world, ramp, bg, fac = L.night_world("W_EP4_SunsetNight")
sc.world = world
L.grade(E, sat=1.35, bloom=0.7, bloom_thr=0.6)
el = ramp.color_ramp.elements   # 0 ground, 1 horizon, 2 zenith
pal = [
    (1,     (0.45, 0.33, 0.38), (1.00, 0.52, 0.30), (0.40, 0.38, 0.75)),
    (700,   (0.35, 0.25, 0.35), (0.95, 0.42, 0.40), (0.25, 0.22, 0.55)),
    (1300,  (0.16, 0.10, 0.24), (0.70, 0.30, 0.55), (0.10, 0.10, 0.38)),
    (1800,  (0.06, 0.05, 0.14), (0.28, 0.12, 0.45), (0.03, 0.05, 0.24)),
]
for f, g_, h_, z_ in pal:
    for i, c in enumerate((g_, h_, z_)):
        el[i].color = (*c, 1)
        el[i].keyframe_insert("color", frame=f)
    bg.inputs["Strength"].default_value = {1: 1.0, 700: 0.9, 1300: 0.6, 1800: 0.45}[f]
    bg.inputs["Strength"].keyframe_insert("default_value", frame=f)
for f, v in [(1, 0.0), (1100, 0.0), (1700, 1.0)]:
    fac.outputs[0].default_value = v
    fac.outputs[0].keyframe_insert("default_value", frame=f)
# camera sky = Unity skybox panorama, tinted/dimmed into night
sky = L.unity_sky(world)
nt = world.node_tree
tint = nt.nodes.new("ShaderNodeMix"); tint.name = "USKY_tint"
tint.data_type = 'RGBA'; tint.blend_type = 'MULTIPLY'; tint.inputs["Factor"].default_value = 1.0
nt.links.new(nt.nodes["USKY_tex"].outputs["Color"], tint.inputs["A"])
nt.links.new(tint.outputs["Result"], sky.inputs["Color"])
for f, c, st in [(1, (1.0, 1.0, 1.0), 1.0), (700, (1.0, 0.85, 0.85), 0.9),
                 (1300, (0.55, 0.45, 0.75), 0.55), (1800, (0.22, 0.25, 0.55), 0.35)]:
    tint.inputs["B"].default_value = (*c, 1)
    tint.inputs["B"].keyframe_insert("default_value", frame=f)
    sky.inputs["Strength"].default_value = st
    sky.inputs["Strength"].keyframe_insert("default_value", frame=f)

sun = E.M["Sun_Key"]
L.write_fcurves(sun, "EP4_Sun", {
    ("rotation_euler", 0): [(1, 1.3), (1500, 1.5)],
    ("rotation_euler", 1): [(1, 0.0), (1500, 0.0)],
    ("rotation_euler", 2): [(1, -1.1), (1500, -1.2)]}, "BEZIER")
L.write_fcurves(sun.data, "EP4_SunE", {
    ("energy", 0): [(1, 2.6), (900, 1.6), (1500, 0.0)],
    ("color", 0): [(1, 1.0), (1500, 1.0)],
    ("color", 1): [(1, 0.62), (1500, 0.45)],
    ("color", 2): [(1, 0.40), (1500, 0.35)]}, "BEZIER")
md = bpy.data.lights.get("EP4_Moon") or bpy.data.lights.new("EP4_Moon", 'SUN')
md.color = (0.55, 0.65, 1.0)
mo = bpy.data.objects.get("EP4_Moon") or bpy.data.objects.new("EP4_Moon", md)
if mo.name not in sc.collection.objects:
    sc.collection.objects.link(mo)
mo.rotation_euler = (0.7, 0.2, 2.4)
L.write_fcurves(md, "EP4_MoonE", {("energy", 0): [(1, 0.0), (1000, 0.0), (1800, 0.9)]}, "BEZIER")
for o in sc.objects:
    if o.type == 'LIGHT' and o.get("src", "").startswith("WinGlow"):
        base = o.get("ep4_base", o.data.energy)
        o["ep4_base"] = base
        L.write_fcurves(o.data, "EP4_" + o.name + "_E", {("energy", 0): [(1, base * 0.6), (1000, base), (1800, base * 2.5)]}, "BEZIER")

fl = bpy.data.lights.get("EP4_Fill") or bpy.data.lights.new("EP4_Fill", 'AREA')
fl.color = (1.0, 0.85, 0.7)
fl.size = 3.0
fo = bpy.data.objects.get("EP4_Fill") or bpy.data.objects.new("EP4_Fill", fl)
if fo.name not in sc.collection.objects:
    sc.collection.objects.link(fo)
fo.location = (0.0, -5.0, 2.4)
d = Vector((0.0, -2.6, 0.4)) - fo.location
fo.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
L.write_fcurves(fl, "EP4_FillE", {("energy", 0): [(1, 0.0), (1100, 0.0), (1800, 120.0)]}, "BEZIER")

L.key_vis(E.M["Broom_Body"], [], END, "EP4_OBroom")

# ---------------------------------------------------------------- camera
C = L.Cam(E.tracks)
C.move(1, K["walkOut"] + 40, (9, -15, 6.5), (4.2, -9.6, 2.4), (0.5, -2.5, 0.5), (1.4, -4.8, 0.5), 26, 32)
C.follow(K["walkOut"] + 41, K["sulk"] - 1, "Black", off=(0.2, -2.3, 0.8), aim_off=(0, 0, 0.4), lens=38, rel=True, lag=0.05)
C.static(K["sulk"], APPEAR + 20, (1.1, -4.45, 0.95), (2.75, -8.3, 0.7), 30)
C.static(APPEAR + 21, APPEAR + 69, (4.3, -8.0, 1.0), (3.2, -9.45, 0.65), 38)
C.static(APPEAR + 70, OPEN - 11, (2.2, -6.65, 0.6), (1.7, -5.55, 0.45), 34)
C.static(OPEN - 10, R_IN + 60, (1.0, -4.55, 1.0), (2.6, -7.8, 0.8), 30)
C.follow(R_IN + 61, f_meet - 1, "Rainbow", off=(0.5, 2.2, 1.1), aim_off=(0, 0, 0.4), lens=34, rel=True, lag=0.06)
C.static(f_meet, K["hug"] - 1, (3.35, -5.05, 0.8), (1.84, -5.8, 0.45), 32)
C.move(K["hug"], K["toPlaza"] - 1, (3.35, -5.05, 0.8), (3.2, -4.6, 1.3), (1.84, -5.8, 0.45), (1.6, -5.4, 0.5), 32, 28)
C.follow(K["toPlaza"], K["lap"] - 1, "Black", off=(1.6, -1.9, 1.2), aim_off=(0.3, 0, 0.4), lens=32, lag=0.07)
C.orbit(K["lap"], LAP_END - 40, (0, 0, 0.2), 6.0, 3.4, -2.8, -1.2, aim_h=0.3, lens=26, r1=5.6, h1=2.6)
C.static(LAP_END - 39, K["dance"] - 1, (-0.9, -5.6, 1.5), (-1.0, -1.5, 0.5), 28)
C.move(K["dance"], K["sit"] - 1, (0.0, -6.2, 1.2), (0.0, -4.9, 0.9), (0.0, -2.65, 0.5), (0.0, -2.65, 0.5), 30, 34)
C.static(K["sit"], K["gaze"] + 60, (0.0, -5.2, 0.85), (0.0, -2.65, 0.45), 34)
C.move(K["gaze"] + 61, END, (0.0, -5.2, 0.85), (0.0, -8.5, 3.8), (0.0, -2.65, 0.45), (0.0, 0.5, 2.6), 34, 26)
C.bake(E.cam, E.aim, END, "EP4")

sc.timeline_markers.clear()
for k, v in K.items():
    sc.timeline_markers.new(k, frame=int(v))
RESULT = {"K": {k: int(v) for k, v in K.items()}, "appear": APPEAR, "lapEnd": LAP_END, "b": b.f, "r": r.f, "w": w.f}
