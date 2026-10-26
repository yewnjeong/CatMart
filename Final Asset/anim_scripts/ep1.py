import bpy, sys, math, importlib.util
P = r"C:\Users\mbc\Documents\YW_Project\CatMart\Final Asset\anim_scripts\catlib.py"
spec = importlib.util.spec_from_file_location("catlib", P); L = importlib.util.module_from_spec(spec); spec.loader.exec_module(L); sys.modules["catlib"] = L

END = 4500
E = L.Episode("EP1_TownTour", "EP1")
t = E.track("Black", (-3.3, -9.45), 2.75)
K = {}

# 1. asleep on the START bridge, morning
K["sleep"] = t.sit(210, "sleep")
K["stretch"] = t.act("stretch", 125)
K["look0"] = t.idle(100, look=[(0, -0.65), (38, 0.65), (75, None)])
# 2. through the START gate
K["gate"] = t.walk([(-2.3, -7.0), (-1.9, -6.0)], speed=0.8)
t.turn((-2.28, -6.99), 16)
K["cheer0"] = t.act("cheer", 54)
# 3. broom
t.turn((-0.3, -5.5), 12)
t.walk([(-0.3, -5.5)], speed=0.9)
t.turn((0.44, -5.22), 12)
K["broom"] = t.act("sniff", 85)
K["sneeze"] = t.act("surprise", 40)
t.act("shake", 34)
# 4. flower garden
t.walk([(-1.6, -4.9), (-2.75, -4.55)], speed=0.9)
t.turn((-3.8, -3.7), 14)
K["garden"] = t.act("sniff", 95)
t.idle(60, look=[(0, -0.5), (30, 0.35)])
t.act("nod", 40)
# 5. fountain plaza
K["plaza"] = t.walk([(-2.4, -3.1), (-1.6, -2.3)], speed=0.9)
t.turn((0, 0), 14)
K["fountain"] = t.idle(90, look=[(0, -0.4), (45, 0.4), (80, None)])
t.hop(2, h=0.22, each=15)
# 6. bread mart
K["toBread"] = t.walk([(-2.3, -0.3), (-2.2, 0.8), (-3.3, 1.55)], speed=0.95)
t.turn((-4.4, 2.6), 14)
K["bread"] = t.act("sniff", 95)
t.idle(60, look=[(0, -0.6), (30, 0.2)])
t.act("cheer", 48)
# food on the ground: sniff, but no stealing (yet)
t.turn((-3.85, 0.75), 10)
t.walk([(-3.85, 0.75)], speed=0.8)
t.turn((-4.6, 0.95), 12)
K["food"] = t.act("sniff", 75)
t.act("shake", 40)
# 7. up on the brown box for a look around
K["toBox"] = t.walk([(-3.0, 2.3), (-2.2, 3.3), (-1.0, 3.75), (-0.8, 3.95)], speed=1.0)
t.turn((-0.78, 4.54), 10)
box_h = E.surf("Brown Box_Low.001", -0.78, 4.54) or 0.58
K["boxJump"] = t.jump((-0.78, 4.52), h=0.45, base_to=box_h)
t.turn(-0.6, 16)
K["boxSit"] = t.sit(170, "swing", look=[(0, -0.7), (60, 0.6), (120, 0.0)])
t.idle(20)
t.jump((-0.15, 3.85), h=0.3)
# 8. lollipop + snack mart
t.walk([(0.4, 4.3)], speed=0.9)
t.turn((1.0, 5.4), 12)
K["lolli"] = t.idle(60, look=[(0, 0.3), (30, -0.3)])
t.act("point", 50)
K["toSnack"] = t.walk([(1.6, 3.0), (2.2, 2.35)], speed=1.0)
t.turn((3.0, 3.3), 12)
K["snack"] = t.act("cheer", 60)
t.act("wave", 66)
t.hop(3, h=0.28, each=15)
# 9. bench
K["toBench"] = t.walk([(3.4, 1.9), (4.3, 1.25), (4.55, 0.15)], style="trot", speed=1.4)
t.turn((5.05, 0.5), 10)
seat = E.surf("Bench_A_Low.001", 5.05, 0.5) or 0.43
K["benchJump"] = t.jump((5.05, 0.5), h=0.35, base_to=seat)
t.turn(-0.91, 18)
K["bench"] = t.sit(250, "swing", look=[(0, None), (70, -0.6), (140, 0.5), (210, None)])
t.idle(20)
t.jump((4.45, 0.0), h=0.3)
# 10. fruit mart + giant apple
K["toFruit"] = t.walk([(3.6, -1.2), (2.9, -2.1)], speed=0.95)
t.turn((3.8, -2.4), 12)
K["fruit"] = t.idle(70, look=[(0, -0.2), (40, 0.3)])
t.act("surprise", 45)
t.walk([(2.3, -3.3), (1.95, -4.12)], speed=0.9)
t.turn((2.35, -4.56), 12)
K["apple"] = t.act("sniff", 70)
t.act("hug", 85)
t.act("cheer", 45)
# 11. back to the fountain, nap in the sun
K["home"] = t.walk([(1.2, -3.3), (0.35, -2.55)], speed=0.85)
t.turn(0.2, 16)
K["sitEnd"] = t.sit(150, "rest", look=[(0, -0.4), (60, 0.4), (110, None)])
rest = END - t.f + 1
K["sleepEnd"] = t.sit(max(60, rest), "sleep")
LAST = t.f

# ---------------------------------------------------------------- camera
C = L.Cam(E.tracks)
f = K
C.move(1, f["sleep"] + 170, (6, -20, 10), (-0.5, -13.6, 2.2), (0, -3, 0.5), (-3.3, -9.4, 0.7), 28, 35)
C.static(f["sleep"] + 171, f["stretch"] + 124, (-2.15, -7.7, 1.55), (-3.3, -9.45, 0.7), 40)
C.follow(f["look0"], f["look0"] + 99, "Black", off=(1.4, 1.6, 0.6), aim_off=(0, 0, 0.45), lens=45, lag=0.2)
C.follow(f["gate"], f["cheer0"] - 1, "Black", off=(1.6, -2.0, 1.9), aim_off=(0, 0.4, 0.4), lens=32, lag=0.06)
C.static(f["cheer0"], f["broom"] - 1, (-2.75, -8.6, 1.0), (-1.95, -6.1, 0.75), 30)
C.static(f["broom"], f["garden"] - 1, (1.2, -6.6, 0.8), (0.0, -5.4, 0.45), 38)
C.follow(f["garden"], f["plaza"] - 1, "Black", off=(0.9, -1.6, 0.8), aim_off=(-0.3, 0.3, 0.35), lens=40, lag=0.1)
C.move(f["plaza"], f["fountain"] + 120, (-3.2, -5.6, 1.4), (-2.9, -4.6, 1.0), (-1.6, -2.3, 0.6), (-0.6, -1.0, 1.4), 30, 32)
C.follow(f["toBread"], f["bread"] - 1, "Black", off=(1.6, -1.3, 1.1), aim_off=(0, 0, 0.4), lens=35, lag=0.07)
C.move(f["bread"], f["food"] - 1, (-1.9, 0.6, 1.0), (-2.0, 0.3, 1.7), (-3.3, 1.6, 0.5), (-4.2, 2.4, 1.6), 32, 30)
C.static(f["food"], f["toBox"] - 1, (-3.0, -0.4, 0.8), (-4.2, 0.85, 0.4), 42)
C.follow(f["toBox"], f["boxJump"] - 1, "Black", off=(1.8, -1.0, 1.0), aim_off=(0, 0, 0.4), lens=35, lag=0.06)
C.orbit(f["boxJump"], f["boxSit"] + 169, (-0.78, 4.52, 0.58), 2.6, 0.9, -1.2, -0.3, aim_h=0.45, lens=40)
C.follow(f["boxSit"] + 170, f["toSnack"] - 1, "Black", off=(-1.2, -1.8, 1.0), aim_off=(0, 0, 0.6), lens=35, lag=0.06)
C.static(f["toSnack"], f["toBench"] - 1, (3.7, 1.25, 0.8), (2.4, 2.8, 0.75), 30)
C.follow(f["toBench"], f["benchJump"] - 1, "Black", off=(-2.0, 0.4, 1.1), aim_off=(0, 0, 0.4), lens=35, lag=0.07)
C.move(f["benchJump"], f["bench"] + 249, (3.3, -0.7, 0.9), (3.6, -0.3, 0.9), (5.05, 0.5, 0.75), (5.05, 0.5, 0.75), 42, 52)
C.follow(f["toFruit"], f["fruit"] - 1, "Black", off=(-1.6, 0.6, 1.0), aim_off=(0, 0, 0.5), lens=32, lag=0.07)
C.static(f["fruit"], f["apple"] - 1, (2.0, -3.6, 0.8), (3.3, -2.0, 1.1), 28)
C.static(f["apple"], f["home"] - 1, (3.0, -3.25, 0.85), (2.1, -4.3, 0.5), 36)
C.follow(f["home"], f["sitEnd"] + 60, "Black", off=(0.2, -2.4, 0.9), aim_off=(0, 0, 0.4), lens=40, lag=0.06)
C.move(f["sitEnd"] + 61, END, (0.35, -4.2, 0.75), (2.0, -17.0, 9.0), (0.35, -2.55, 0.45), (0.0, -1.0, 0.8), 45, 30)
C.bake(E.cam, E.aim, END, "EP1")

E.bake(END)

# ---------------------------------------------------------------- morning light
sc = E.sc
sc.world = L.gradient_world("W_EP1_Morning", (0.22, 0.50, 1.0), (1.0, 0.70, 0.80), (0.62, 0.50, 0.78), 1.15)
L.grade(E)
sun = E.M["Sun_Key"]
sun.data.color = (1.0, 0.84, 0.66)
L.write_fcurves(sun, "EP1_Sun", {
    ("rotation_euler", 0): [(1, 1.22), (END, 0.92)],
    ("rotation_euler", 1): [(1, 0.0), (END, 0.0)],
    ("rotation_euler", 2): [(1, 0.35), (END, 0.75)]}, "BEZIER")
L.write_fcurves(sun.data, "EP1_SunE", {("energy", 0): [(1, 2.2), (900, 3.0), (END, 3.4)]}, "BEZIER")
sc.timeline_markers.clear()
for k, v in K.items():
    sc.timeline_markers.new(k, frame=v)
RESULT = {"last": LAST, "markers": K}
