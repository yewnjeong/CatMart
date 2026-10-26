import bpy, sys, math, importlib.util
from mathutils import Vector
P = r"C:\Users\mbc\Documents\YW_Project\CatMart\Final Asset\anim_scripts\catlib.py"
spec = importlib.util.spec_from_file_location("catlib", P); L = importlib.util.module_from_spec(spec); spec.loader.exec_module(L); sys.modules["catlib"] = L

END = 2700
E = L.Episode("EP2_FoodThieves", "EP2")
L.park_carts(E)
K = {}
TOAST = (-4.22, 0.97)
CROI = (-4.68, 0.98)

# ---------------------------------------------------------------- Black : the first thief
b = E.track("Black", (-1.6, -2.9), L.yaw_to(-2.0, 3.0))
K["start"] = b.idle(95, look=[(0, -0.7), (32, 0.7), (64, None)])
K["sneak"] = b.walk([(-2.2, -1.4), (-2.25, 0.35), (-3.72, 0.55)], speed=0.55, style="sneak")
b.turn(TOAST, 12)
K["suspense"] = b.idle(48, look=[(0, -0.8), (16, 0.8), (32, None)])
K["grabB"] = b.act("grab", 50)
GRAB_B = K["grabB"] + 25
b.idle(40, kind="hold", look=[(0, -0.6), (20, 0.6)])
K["away"] = b.walk([(-2.25, 0.35), (-1.9, -1.5), (-1.0, -2.6)], speed=0.6, style="sneak", hold=True)
K["skipB"] = b.walk([(-0.2, -2.75)], speed=1.2, style="skip", hold=True)
b.turn(0.25, 14)
K["eatB"] = b.sit(330, "eat")
# White sneaks up; Black notices
K["noticeB"] = b.idle(30, kind="hold", look=[(0, -1.0)])
b.turn((-1.0, -2.9), 10)
K["surpB"] = b.act("surprise", 40, recoil=0.0)
b.act("stomp", 50)
# the chase round the fountain (counter-clockwise, r=2.3)
R = 2.3


def ring(a0, a1, n=28):
    return [(R * math.cos(a0 + (a1 - a0) * i / n), R * math.sin(a0 + (a1 - a0) * i / n)) for i in range(1, n + 1)]


a_b = math.atan2(-2.75, -0.2)
CH_END = a_b + math.tau * 1.1
K["chaseB"] = b.walk(ring(a_b, CH_END), speed=2.4, style="run", hold=True, ramp=8)
b.turn(CH_END, 10)   # stop and turn back to look
K["waitB"] = b.f
STOP_B = Vector((R * math.cos(CH_END), R * math.sin(CH_END)))

# ---------------------------------------------------------------- White : the cat who appears
w = E.track("White", (-6.15, 0.85), L.yaw_to(1.0, 0.1))
w.sit(K["grabB"] + 75, "rest")
K["peekW"] = w.act("sniff", 60, look=[(0, 0.6)])
K["outW"] = w.walk([(-5.6, 1.55), (-5.15, 1.45)], speed=0.7, style="sneak")
w.turn(CROI, 12)
K["grabW"] = w.act("grab", 52)
GRAB_W = K["grabW"] + 26
w.act("cheer", 40)
w.idle(30, kind="hold", look=[(0, 0.8)])
K["stalkW"] = w.walk([(-4.4, -0.25), (-3.6, -1.7), (-2.4, -2.85), (-1.15, -3.05)], speed=0.6, style="sneak", hold=True)
# arrive behind Black, then the face-off
w.turn((-0.2, -2.75), 10)
gap = K["noticeB"] - w.f
if gap > 0:
    w.idle(gap, kind="hold")
w.idle(40, kind="hold")
K["surpW"] = w.act("surprise", 40, recoil=0.0)
w.act("point", 50)
# chase: same ring, starting a little later
a_w = math.atan2(-3.05, -1.15)
w.walk([(R * math.cos(a_b), R * math.sin(a_b))], speed=2.4, style="run", hold=True, ramp=6)
w.walk(ring(a_b, CH_END - 0.17), speed=2.4, style="run", hold=True, ramp=8)

# sync: both bump into each other at F
F = max(b.f, w.f) + 6
b.idle(F - b.f, kind="hold")
w.idle(F - w.f, kind="hold")
K["bump"] = F
b.act("surprise", 45, recoil=0.4)
w.act("surprise", 45, recoil=-0.4)
SWAP = F + 32
b.idle(40, kind="hold", look=[(0, None)])
w.idle(40, kind="hold")
K["laugh"] = b.act("cheer", 50)
w.act("cheer", 50)
b.act("nod", 30)
w.act("nod", 30)

# sit side by side and share
SEAT_B = (1.45, -4.55)
SEAT_W = (1.15, -5.15)
K["toSeat"] = b.walk([(1.4, -3.3), SEAT_B], speed=0.9, hold=True)
w.walk([(1.1, -3.4), SEAT_W], speed=0.9, hold=True)
FS = max(b.f, w.f)
b.idle(FS - b.f, kind="hold"); w.idle(FS - w.f, kind="hold")
b.turn(-0.785, 14); w.turn(-0.785, 14)
K["shareEat"] = b.sit(END - b.f + 1, "eat", look=[(0, None), (150, 0.6), (200, None), (330, 0.9)])
w.sit(END - w.f + 1, "eat", look=[(0, None), (120, -0.6), (180, None), (340, 0.9)])

# ---------------------------------------------------------------- Rainbow : just a peek (teaser)
r = E.track("Rainbow", (60, 0), 0)
TEASE = K["shareEat"] + 300
r.idle(TEASE - 1)
r.place((4.45, -5.15), L.yaw_to(-3.3, 2.4))
K["tease"] = r.idle(30)
r.walk([(4.15, -5.3)], speed=0.4, style="sneak")
r.idle(110, look=[(0, 0.4), (40, -0.2), (80, 0.3)])
r.act("wave", 50, side="L")
r.walk([(4.7, -5.9), (5.3, -6.2)], speed=1.4, style="trot")
r.place((60, 0))
r.idle(max(1, END - r.f + 1))

# ---------------------------------------------------------------- props
b.hold(GRAB_B, END)
w.hold(GRAB_W, END)
E.bake(END)
held = {}
held["B_toast"] = L.make_held(E, "Black", "Toast_Low.001", "EP2_Held_Black_Toast", 0.26, yaw=0.3)
held["W_croi"] = L.make_held(E, "White", "Croissant_Low.003", "EP2_Held_White_Croissant", 0.30)
held["B_croi"] = L.make_held(E, "Black", "Croissant_Low.003", "EP2_Held_Black_Croissant", 0.26)
held["W_toast"] = L.make_held(E, "White", "Toast_Low.001", "EP2_Held_White_Toast", 0.30, yaw=0.3)
L.key_vis(held["B_toast"], [(GRAB_B, F + 5)], END, "EP2_HBT")
L.key_vis(held["W_croi"], [(GRAB_W, F + 5)], END, "EP2_HWC")
L.key_vis(held["B_croi"], [(SWAP, END)], END, "EP2_HBC")
L.key_vis(held["W_toast"], [(SWAP, END)], END, "EP2_HWT")
L.key_vis(E.M["Toast_Low.001"], [(1, GRAB_B - 1)], END, "EP2_OT")
L.key_vis(E.M["Croissant_Low.003"], [(1, GRAB_W - 1)], END, "EP2_OC")

# flying swap at the bump
fly_t = L.make_free_copy(E, "Toast_Low.001", "EP2_Fly_Toast")
fly_c = L.make_free_copy(E, "Croissant_Low.003", "EP2_Fly_Croissant")
for o, src, frm, to, sz in [(fly_t, "Toast_Low.001", "Black", "White", 0.3), (fly_c, "Croissant_Low.003", "White", "Black", 0.3)]:
    s = E.M[src]
    k = sz / max(s.dimensions)
    o.scale = s.matrix_world.to_scale() * k
    p0 = L.chest(E, frm, F + 6)
    p1 = L.chest(E, to, SWAP)
    keys = []
    for i in range(0, 27, 2):
        u = i / 26.0
        p = p0.lerp(p1, u) + Vector((0, 0, 1.1 * 4 * u * (1 - u)))
        keys.append((F + 6 + i, tuple(p), (u * 6.0, u * 3.0, u * 4.0)))
    L.key_obj_path(o, keys, o.name + "_Path", "LINEAR")
    L.key_vis(o, [(F + 6, SWAP - 1)], END, o.name)

# ---------------------------------------------------------------- camera
C = L.Cam(E.tracks)
C.move(1, K["sneak"] + 60, (2.6, -7.2, 3.2), (0.4, -4.9, 1.4), (-2.6, 0.2, 0.6), (-1.9, -1.8, 0.45), 28, 32)
C.follow(K["sneak"] + 61, K["suspense"] - 1, "Black", off=(1.3, -1.1, 0.55), aim_off=(0, 0, 0.35), lens=35, lag=0.07)
C.static(K["suspense"], K["peekW"] - 1, (-3.85, -0.75, 0.65), (-4.0, 0.75, 0.35), 38)
C.static(K["peekW"], K["stalkW"] - 1, (-3.75, 2.15, 0.9), (-5.3, 1.0, 0.5), 32)
C.follow(K["stalkW"], K["stalkW"] + 150, "White", off=(1.4, -1.6, 0.9), aim_off=(0, 0, 0.45), lens=35, lag=0.07)
C.static(K["stalkW"] + 151, K["noticeB"] - 1, (0.3, -4.9, 1.9), (-0.4, -2.8, 0.4), 38)
C.two(K["noticeB"], K["chaseB"] + 20, "Black", "White", side=-1.0, dist=2.4, h=0.7, lens=36)
C.orbit(K["chaseB"] + 21, F - 30, (0, 0, 0.2), 6.0, 3.0, -1.6, 0.4, aim_h=0.3, lens=28, r1=5.0, h1=2.2)
C.static(F - 29, K["laugh"] + 60, (2.1, -4.05, 1.25), (1.04, -2.05, 0.4), 34)
C.follow(K["laugh"] + 61, K["shareEat"] - 1, "Black", off=(-1.3, -1.5, 1.0), aim_off=(0, 0, 0.4), lens=34, lag=0.07)
C.move(K["shareEat"], TEASE + 40, (-0.3, -6.6, 0.85), (-0.15, -6.45, 0.8), (1.3, -4.85, 0.45), (1.3, -4.85, 0.45), 40, 46)
C.move(TEASE + 41, TEASE + 210, (-0.15, -6.45, 0.8), (0.4, -6.6, 0.85), (1.3, -4.85, 0.45), (4.15, -5.2, 0.5), 46, 50)
C.move(TEASE + 211, END, (0.4, -6.6, 0.85), (0.6, -10.0, 7.0), (4.15, -5.2, 0.5), (1.0, -3.0, 0.5), 50, 30)
C.bake(E.cam, E.aim, END, "EP2")

# ---------------------------------------------------------------- daylight
sc = E.sc
sc.world = L.gradient_world("W_EP2_Day", (0.18, 0.48, 1.0), (0.88, 0.78, 1.0), (0.55, 0.58, 0.85), 1.2)
L.grade(E)
sun = E.M["Sun_Key"]
sun.rotation_euler = (0.75, 0.0, 0.6)
sun.data.energy = 3.4
sun.data.color = (1.0, 0.93, 0.82)
sc.timeline_markers.clear()
for k, v in K.items():
    sc.timeline_markers.new(k, frame=int(v))
RESULT = {"F": F, "swap": SWAP, "tease": TEASE, "b_end": b.f, "w_end": w.f, "markers": K}
