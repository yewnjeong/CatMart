import bpy, sys, math, importlib.util
from mathutils import Vector, Quaternion
P = r"C:\Users\mbc\Documents\YW_Project\CatMart\Final Asset\anim_scripts\catlib.py"
spec = importlib.util.spec_from_file_location("catlib", P); L = importlib.util.module_from_spec(spec); spec.loader.exec_module(L); sys.modules["catlib"] = L

END = 2700
E = L.Episode("EP3_MartStories", "EP3")
L.park_carts_at(E, {"Cart_A": (-0.6, 3.5), "Cart_B": (3.57, 0.0)})
K = {}
b = E.track("Black", (-2.3, -1.6), L.yaw_to(-0.3, 2))
w = E.track("White", (-3.3, 1.15), L.yaw_to(-0.6, -1.0))


def sync(*ts):
    f = max(t.f for t in ts)
    for t in ts:
        if f > t.f:
            t.idle(f - t.f, kind="hold" if getattr(t, "_holding", False) else "idle")
    return f


# =============================================================== A. BREAD MART (1-900)
TOAST = (-4.22, 0.97)
K["A"] = 1
w.act("sweep", 330)
b.idle(40, look=[(0, -0.4)])
K["A_enter"] = b.walk([(-2.0, -0.3), (-2.55, 0.55)], speed=0.8)
b.turn((-4.4, 2.6), 12)
K["A_smell"] = b.act("sniff", 90)
b.turn((-3.3, 1.15), 12)
f_meet = sync(b, w)
K["A_meet"] = f_meet
w.turn((-2.55, 0.55), 14)
w.act("wave", 60)
b.act("wave", 74, side="L")
# white fetches the toast and gives it
w.turn(TOAST, 12)
w.walk([(-3.85, 0.75)], speed=0.8)
w.turn(TOAST, 8)
K["A_grabW"] = w.act("grab", 50)
GRAB_W = K["A_grabW"] + 25
w._holding = True
w.turn((-2.55, 0.55), 12)
w.walk([(-2.95, 0.7)], speed=0.8, hold=True)
b.idle(1)
f = sync(b, w)
b.turn((-2.95, 0.7), 10)
w.idle(10, kind="hold")
K["A_give"] = w.f
HAND = w.f + 10
w.idle(30, kind="hold")
w._holding = False
b.idle(max(1, HAND - b.f))
b._holding = True
b.idle(20, kind="hold")
K["A_happy"] = b.act("cheer", 55)
b.spin(1.0, 30, 0.25)
w.act("nod", 50)
w.act("sweep", 901 - w.f)
b.sit(901 - b.f, "eat")
w.hold(GRAB_W, HAND)
b.hold(HAND, 900)
b._holding = False

# =============================================================== B. SNACK MART (901-1800)
K["B"] = 901
b.place((1.6, 1.35), L.yaw_to(1.2, 1.9))
w.place((60, 0))
b.idle(20, look=[(0, -0.2)])
K["B_walk"] = b.walk([(2.15, 2.05)], speed=0.7)
b.turn((3.0, 3.3), 10)
K["B_look"] = b.idle(30, look=[(0, -0.2)])
K["B_hops"] = b.f
b.jump(None, h=0.5, air=22)
b.jump(None, h=0.6, air=24)
b.act("stomp", 40)
# try the star box
b.turn((4.05, 1.7), 10)
b.walk([(4.05, 1.7)], speed=1.0, style="trot")
b.turn((4.59, 2.14), 8)
top = E.surf("Star Box_Low.001", 4.59, 2.14) or 0.98
K["B_box"] = b.jump((4.55, 2.12), h=0.5, base_to=top)
b.turn((3.6, 3.6), 12)
K["B_reach"] = b.act("point", 60)
b.act("surprise", 40)
K["B_kick"] = b.jump((3.85, 1.45), h=0.25)
b.turn((2.6, 1.0), 10)
b.walk([(2.6, 1.0)], speed=0.7)
b.turn(-0.3, 14)
K["B_sulk"] = b.sit(100, "sulk")
# white comes out of the shop with a candy
w.place((60, 0))
w.idle(max(1, K["B_sulk"] - 60 - w.f))
w.place((3.1, 3.05), L.yaw_to(-0.5, -1.6))
K["B_white"] = w.f
w._holding = True
w.walk([(2.95, 1.65)], speed=0.7, hold=True)
w.turn((2.6, 1.0), 10)
w.idle(max(1, K["B_sulk"] + 100 - w.f), kind="hold")
b.idle(1)
K["B_notice"] = b.f
b.act("surprise", 40)
b.turn((2.95, 1.65), 10)
K["B_give"] = sync(b, w)
w.idle(25, kind="hold")
HAND_B = w.f
w._holding = False
w.act("nod", 40)
b.idle(25)
b._holding = True
b.idle(20, kind="hold")
K["B_happy"] = b.spin(1.0, 34, 0.3)
b.act("cheer", 45)
b.hold(HAND_B, 1800)
w.hold(K["B_white"], HAND_B)
sync(b, w)
K["B_dance"] = b.f
b.act("dance", 1800 - b.f + 1, beat=18)
w.act("dance", 1800 - w.f + 1, beat=18)
b._holding = False

# =============================================================== C. FRUIT MART (1801-2700)
K["C"] = 1801
APPLE0 = Vector((2.35, -4.56))
b.place((1.0, -2.6), L.yaw_to(1, -1.4))
w.place((0.4, -2.95), L.yaw_to(1, -1.2))
b.walk([(1.45, -3.55)], speed=0.8)
w.walk([(1.0, -3.95)], speed=0.8)
sync(b, w)
b.turn(APPLE0, 10); w.turn(APPLE0, 10)
K["C_look"] = b.idle(60, look=[(0, 0.2)])
w.idle(60)
ROLL0 = b.f                      # apple starts to wobble
b.act("surprise", 30); w.act("surprise", 30)
ROLL_PATH = [APPLE0, Vector((1.3, -3.9)), Vector((0.1, -3.85)), Vector((-0.75, -4.1))]
ROLL_T = (ROLL0 + 22, ROLL0 + 115)
K["C_chase"] = b.f
b.walk([(0.6, -3.55), (-0.25, -3.55)], speed=2.2, style="run")
w.walk([(0.4, -4.3), (-0.2, -4.45)], speed=2.0, style="run")
sync(b, w)
b.turn((-0.75, -4.1), 8); w.turn((-0.75, -4.1), 8)
b.act("surprise", 34); w.act("shake", 34)
# push it back home together
b.turn((-1.4, -3.75), 12); w.turn((-1.35, -4.4), 12)
b.walk([(-1.4, -3.75)], speed=0.8); w.walk([(-1.35, -4.4)], speed=0.8)
sync(b, w)
b.turn(APPLE0, 12); w.turn(APPLE0, 12)
K["C_push"] = b.f
PUSH0 = b.f + 10
b.walk([(-0.6, -3.75), (0.6, -3.7), (1.35, -3.9)], speed=0.55, style="sneak", ramp=16)
w.walk([(-0.55, -4.4), (0.55, -4.25), (1.3, -4.45)], speed=0.55, style="sneak", ramp=16)
PUSH1 = max(b.f, w.f)
sync(b, w)
b.hold(PUSH0, PUSH1); w.hold(PUSH0, PUSH1)
K["C_done"] = b.f
b.turn((1.0, -3.95), 12); w.turn((1.45, -3.55), 12)
b.act("hug", 55); w.act("hug", 55)
b.act("cheer", 40); w.act("cheer", 40)
# share a pear
PEAR = (2.13, -5.0)
w.turn(PEAR, 10)
w.walk([(1.7, -5.05)], speed=0.7)
w.turn(PEAR, 8)
K["C_pear"] = w.act("grab", 50)
GRAB_P = K["C_pear"] + 25
w._holding = True
w.turn((1.45, -3.55), 10)
w.walk([(1.15, -4.55)], speed=0.7, hold=True)
w.turn(-0.6, 12)
b.walk([(0.6, -4.15)], speed=0.6)
b.turn(-0.6, 12)
sync(b, w)
K["C_sit"] = b.f
b.sit(END - b.f + 1, "lean", look=[(0, None), (120, -0.7), (200, None)])
w.sit(END - w.f + 1, "eat", look=[(0, None), (160, 0.6), (240, None)])
w.hold(GRAB_P, END)

# =============================================================== bake cats
L.exit_bars(E)
E.bake(END)

# =============================================================== props
L.topple(E, "Star Box_Low.001", K["B_kick"] + 9, (0.7, 0.67))
# broom for White in act A
held_broom = L.make_held(E, "White", "Broom_Body", "EP3_Held_Broom", 0.95, fwd=0.2, down=-0.05, tilt=0.35)
L.key_vis(held_broom, [(1, 900)], END, "EP3_HBroom")
L.key_vis(E.M["Broom_Body"], [], END, "EP3_OBroom")
# toast
hw = L.make_held(E, "White", "Toast_Low.001", "EP3_Held_White_Toast", 0.3, yaw=0.3)
hb = L.make_held(E, "Black", "Toast_Low.001", "EP3_Held_Black_Toast", 0.24, yaw=0.3)
L.key_vis(hw, [(GRAB_W, HAND - 1)], END, "EP3_HWT")
L.key_vis(hb, [(HAND, 900)], END, "EP3_HBT")
L.key_vis(E.M["Toast_Low.001"], [(1, GRAB_W - 1), (901, END)], END, "EP3_OT")
# candy
cw = L.make_held(E, "White", "Candy_Low.002", "EP3_Held_White_Candy", 0.3)
cb = L.make_held(E, "Black", "Candy_Low.002", "EP3_Held_Black_Candy", 0.26)
L.key_vis(cw, [(K["B_white"], HAND_B - 1)], END, "EP3_HWC")
L.key_vis(cb, [(HAND_B, 1800)], END, "EP3_HBC")
# pear
pw = L.make_held(E, "White", "Pear_F_Low.003", "EP3_Held_White_Pear", 0.28)
L.key_vis(pw, [(GRAB_P, END)], END, "EP3_HWP")
L.key_vis(E.M["Pear_F_Low.003"], [(1, GRAB_P - 1)], END, "EP3_OP")

# rolling apple: pivot at the apple's centre
apple = E.M["Apple_F_Low.001"]
L.unparent_keep(apple)
bb = [apple.matrix_world @ Vector(c) for c in apple.bound_box]
ctr = sum(bb, Vector()) / 8
rad = (max(v.z for v in bb) - min(v.z for v in bb)) / 2
piv = bpy.data.objects.get("EP3_ApplePivot") or bpy.data.objects.new("EP3_ApplePivot", None)
if piv.name not in E.sc.collection.objects:
    E.sc.collection.objects.link(piv)
piv.animation_data_clear()
piv.rotation_mode = 'QUATERNION'
piv.location = ctr
piv.rotation_quaternion = (1, 0, 0, 0)
piv.empty_display_size = rad
E.sc.view_layers[0].update()
apple.parent = piv
apple.matrix_parent_inverse = piv.matrix_world.inverted()
zc = ctr.z


def path_at(pts, k):
    segl = [(pts[i + 1] - pts[i]).length for i in range(len(pts) - 1)]
    Lt = sum(segl); d = k * Lt; acc = 0
    for i, l in enumerate(segl):
        if d <= acc + l or i == len(segl) - 1:
            return pts[i].lerp(pts[i + 1], L.clamp((d - acc) / max(l, 1e-6)))
        acc += l


keys = {("location", i): [] for i in range(3)}
qk = {("rotation_quaternion", i): [] for i in range(4)}
q = Quaternion()
prev = APPLE0.copy()
ROLL_END_POS = ROLL_PATH[-1]
for fr in range(1, END + 1):
    if fr < ROLL_T[0]:
        pos = APPLE0.copy()
        wob = 0.0
        if fr >= ROLL0:
            wob = 0.12 * math.sin((fr - ROLL0) * 0.6) * L.ss((fr - ROLL0) / 10)
    elif fr <= ROLL_T[1]:
        k = (fr - ROLL_T[0]) / (ROLL_T[1] - ROLL_T[0])
        k = 1 - (1 - k) ** 2          # rolls out fast, slows down
        pos = path_at(ROLL_PATH, k)
        wob = 0.0
    elif fr < PUSH0:
        pos = ROLL_END_POS.copy(); wob = 0.0
    elif fr <= PUSH1:
        pb, yb, zb, _ = E.tracks["Black"].evaluate(fr)
        pw_, yw, zw, _ = E.tracks["White"].evaluate(fr)
        mid = (pb + pw_) / 2
        fwd = Vector((math.sin((yb + yw) / 2), -math.cos((yb + yw) / 2)))
        pos = mid + fwd * 0.72
        wob = 0.0
    else:
        pos = prev.copy(); wob = 0.0
    d = pos - prev
    if d.length > 1e-5:
        axis = Vector((-d.y, d.x, 0)).normalized()
        q = Quaternion(axis, d.length / rad) @ q
    prev = pos.copy()
    qq = Quaternion((0, 1, 0), wob) @ q
    if (fr - 1) % 2 == 0 or fr == END:
        for i in range(3):
            keys[("location", i)].append((fr, (pos.x, pos.y, zc)[i]))
        for i in range(4):
            qk[("rotation_quaternion", i)].append((fr, qq[i]))
keys.update(qk)
L.write_fcurves(piv, "EP3_AppleRoll", keys, "BEZIER")

# =============================================================== camera
C = L.Cam(E.tracks)
# A
C.move(1, 140, (-0.4, -2.2, 2.6), (-1.4, -1.0, 1.4), (-3.3, 1.2, 0.5), (-3.3, 1.2, 0.6), 30, 34)
C.follow(141, K["A_smell"] - 1, "Black", off=(1.2, -1.2, 0.8), aim_off=(0, 0, 0.4), lens=35, lag=0.08)
C.static(K["A_smell"], K["A_grabW"] - 1, (-1.75, -0.6, 0.85), (-3.0, 0.95, 0.5), 32)
C.static(K["A_grabW"], K["A_happy"] - 1, (-2.55, -1.65, 1.0), (-3.4, 0.8, 0.45), 32)
C.static(K["A_happy"], 900, (-4.0, -0.95, 0.95), (-2.85, 0.65, 0.45), 32)
# B
C.move(901, K["B_hops"] - 1, (3.1, -0.9, 0.9), (2.8, -0.3, 1.0), (2.1, 2.0, 0.5), (2.5, 2.6, 1.5), 30, 30)
C.static(K["B_hops"], K["B_box"] - 1, (2.75, -0.15, 0.8), (2.15, 2.05, 0.8), 32)
C.static(K["B_box"], K["B_sulk"] - 1, (2.9, 0.05, 1.2), (4.2, 2.1, 1.3), 30)
C.static(K["B_sulk"], K["B_give"] + 20, (2.2, -0.35, 0.65), (2.75, 1.3, 0.45), 36)
C.two(K["B_give"] + 21, K["B_dance"] + 60, "Black", "White", side=-1.0, dist=2.0, h=0.6, lens=36)
C.orbit(K["B_dance"] + 61, 1800, (2.75, 1.3, 0.1), 2.6, 1.0, -2.2, -1.1, aim_h=0.45, lens=34)
# C
C.static(1801, ROLL0 + 30, (2.9, -2.6, 0.8), (1.6, -4.2, 0.5), 32)
C.move(ROLL0 + 31, K["C_push"] - 1, (2.1, -1.4, 1.9), (0.9, -1.7, 2.0), (1.2, -4.0, 0.45), (-0.5, -4.0, 0.4), 26, 28)
C.move(K["C_push"], PUSH1, (-0.6, -6.3, 2.3), (1.3, -6.5, 2.3), (-0.6, -4.0, 0.4), (1.3, -4.2, 0.45), 32, 32)
C.static(PUSH1 + 1, K["C_sit"] + 40, (0.0, -6.0, 0.9), (1.3, -4.2, 0.5), 34)
C.move(K["C_sit"] + 41, END, (0.0, -6.0, 0.8), (-1.5, -9.0, 4.5), (1.0, -4.2, 0.45), (1.6, -2.0, 0.6), 40, 30)
C.bake(E.cam, E.aim, END, "EP3")

# =============================================================== afternoon light
sc = E.sc
sc.world = L.gradient_world("W_EP3_Afternoon", (0.32, 0.42, 1.0), (1.0, 0.68, 0.62), (0.66, 0.50, 0.72), 1.15)
L.unity_sky(sc.world)   # camera sees the Unity skybox panorama
L.grade(E)
sun = E.M["Sun_Key"]
sun.rotation_euler = (0.95, 0.0, -0.6)
sun.data.energy = 3.0
sun.data.color = (1.0, 0.88, 0.72)
sc.timeline_markers.clear()
for k, v in K.items():
    sc.timeline_markers.new(k, frame=int(v))
RESULT = {"K": {k: int(v) for k, v in K.items()}, "roll": ROLL_T, "push": (PUSH0, PUSH1), "b": b.f, "w": w.f}
