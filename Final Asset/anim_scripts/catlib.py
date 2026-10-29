# CatMart procedural cat animation library (runs inside Blender)
import bpy, math, os
from mathutils import Vector

TAU = math.tau
FPS = 30


def clamp(x, a=0.0, b=1.0):
    return a if x < a else b if x > b else x


def ss(t):
    t = clamp(t)
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def angdiff(a, b):
    return (b - a + math.pi) % TAU - math.pi


def alerp(a, b, t):
    return a + angdiff(a, b) * t


def yaw_to(dx, dy):
    # rig faces -Y at yaw 0
    return math.atan2(dx, -dy)


def V2(p):
    return Vector((p[0], p[1]))


BONES = ["Hips", "Spine", "Head", "UpperArm.L", "LowerArm.L", "UpperArm.R", "LowerArm.R",
         "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
         "Tail.01", "Tail.02", "Tail.03", "Tail.04", "Tail.05"]
TAILS = ["Tail.01", "Tail.02", "Tail.03", "Tail.04", "Tail.05"]


def base_pose():
    p = {b: [0.0, 0.0, 0.0] for b in BONES}
    p["UpperArm.L"][2] = -0.52
    p["UpperArm.R"][2] = 0.52
    return p


def mix_pose(a, b, t):
    return {k: [lerp(a[k][i], b[k][i], t) for i in range(3)] for k in a}


def add(p, bone, x=0.0, y=0.0, z=0.0):
    v = p[bone]
    v[0] += x; v[1] += y; v[2] += z


def setb(p, bone, x=None, y=None, z=None):
    v = p[bone]
    if x is not None: v[0] = x
    if y is not None: v[1] = y
    if z is not None: v[2] = z


def arms(p, lx=None, lz=None, rx=None, rz=None, llz=None, lrz=None, llx=None, lrx=None):
    # lz: + raises left arm ; rz: - raises right arm ; x: + swings forward
    setb(p, "UpperArm.L", x=lx, z=lz)
    setb(p, "UpperArm.R", x=rx, z=rz)
    setb(p, "LowerArm.L", x=llx, z=llz)
    setb(p, "LowerArm.R", x=lrx, z=lrz)


def tail(p, t, amp=0.18, period=70.0, up=0.0, side=0.0):
    for i, b in enumerate(TAILS):
        ph = TAU * t / period - i * 0.55
        add(p, b, x=side * (1 if i == 0 else 0.3) + 0.06 * math.sin(ph * 0.5),
            z=amp * (0.6 + 0.15 * i) * math.sin(ph) + up * (1.0 if i == 0 else 0.25))


def breathe(p, t, k=1.0):
    s = math.sin(TAU * t / 84.0)
    add(p, "Spine", x=0.025 * s * k)
    add(p, "Head", x=-0.02 * s * k, z=0.015 * math.sin(TAU * t / 150.0) * k)
    add(p, "UpperArm.L", z=0.03 * s * k)
    add(p, "UpperArm.R", z=-0.03 * s * k)


# ----------------------------------------------------------------- gaits
STYLES = {
    #        stride  leg   knee  arm   lean  bob    sway  tail_amp tail_up
    "walk":  (0.42, 0.50, 0.45, 0.35, 0.08, 0.020, 0.06, 0.22, 0.0),
    "trot":  (0.50, 0.65, 0.60, 0.50, 0.14, 0.035, 0.07, 0.28, 0.1),
    "run":   (0.70, 0.90, 0.85, 0.75, 0.26, 0.055, 0.05, 0.35, 0.25),
    "sneak": (0.30, 0.35, 0.40, 0.15, 0.38, 0.008, 0.10, 0.10, -0.15),
    "skip":  (0.55, 0.60, 0.70, 0.60, 0.05, 0.070, 0.10, 0.35, 0.2),
}


def gait(style, phase, amt, t):
    stride, leg, knee, arm, lean, bob, sway, tamp, tup = STYLES[style]
    s = math.sin(TAU * phase)
    c = math.cos(TAU * phase)
    p = base_pose()
    a = amt
    add(p, "UpperLeg.L", x=leg * s * a)
    add(p, "UpperLeg.R", x=-leg * s * a)
    add(p, "LowerLeg.L", x=-knee * max(0.0, c) * a)
    add(p, "LowerLeg.R", x=-knee * max(0.0, -c) * a)
    add(p, "Foot.L", x=0.25 * s * a)
    add(p, "Foot.R", x=-0.25 * s * a)
    add(p, "UpperArm.L", x=-arm * s * a)
    add(p, "UpperArm.R", x=arm * s * a)
    add(p, "LowerArm.L", x=-0.25 * a)
    add(p, "LowerArm.R", x=-0.25 * a)
    add(p, "Spine", x=lean * a, z=sway * s * a, y=0.07 * s * a)
    add(p, "Head", x=-lean * 0.6 * a, z=-sway * 0.6 * s * a, y=-0.05 * s * a)
    if style == "sneak":
        add(p, "UpperLeg.L", x=0.35 * a); add(p, "UpperLeg.R", x=0.35 * a)
        add(p, "LowerLeg.L", x=-0.45 * a); add(p, "LowerLeg.R", x=-0.45 * a)
        arms(p, lx=0.55 + 0.15 * s, rx=0.55 - 0.15 * s, lz=-0.25, rz=0.25)
        add(p, "Head", y=0.25 * math.sin(TAU * t / 60.0))
    if style == "skip":
        arms(p, lz=0.4 + 0.3 * s, rz=-0.4 + 0.3 * s)
    tail(p, t, amp=tamp, period=36.0 if style in ("run", "trot", "skip") else 56.0, up=tup)
    z = bob * (1.0 - abs(s)) * a
    if style == "sneak":
        z -= 0.03 * a
    if style == "run":
        z = bob * abs(c) * a
    return p, z


# ----------------------------------------------------------------- emotes
def emote(kind, u, dur, t, sc, kw):
    """Return (pose, zoff, yaw_offset) for an in-place action."""
    p = base_pose()
    z = 0.0
    yo = 0.0
    k = u / max(1.0, dur)
    breathe(p, t)
    if kind == "idle":
        tail(p, t)
    elif kind == "stretch":
        # 0-.4 reach up + lean back (yawn), .4-.75 bow forward, then shake out
        if k < 0.45:
            w = ss(k / 0.2) * (1 - ss((k - 0.35) / 0.1))
            arms(p, lz=-0.52 + 1.5 * w, rz=0.52 - 1.5 * w)
            add(p, "Spine", x=-0.3 * w)
            add(p, "Head", x=-0.45 * w, z=0.12 * w)
            z = 0.02 * w
        elif k < 0.8:
            w = ss((k - 0.45) / 0.12) * (1 - ss((k - 0.7) / 0.1))
            arms(p, lx=1.3 * w, lz=-0.52 + 0.3 * w, rx=1.3 * w, rz=0.52 - 0.3 * w)
            add(p, "Spine", x=0.55 * w)
            add(p, "Hips", x=0.25 * w)
            add(p, "Head", x=-0.35 * w)
            for b in TAILS[:2]:
                add(p, b, z=0.3 * w)
            z = -0.02 * w
        else:
            w = 1 - ss((k - 0.8) / 0.2)
            add(p, "Spine", z=0.12 * math.sin(t * 1.4) * w)
            add(p, "Head", z=0.25 * math.sin(t * 1.4 + 0.5) * w)
        tail(p, t, amp=0.25)
    elif kind == "look":
        tail(p, t, amp=0.15)
    elif kind == "sniff":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        add(p, "Spine", x=0.42 * w)
        add(p, "Hips", x=0.12 * w)
        add(p, "Head", x=0.15 * w + 0.07 * math.sin(t * 1.9) * w, y=0.15 * math.sin(t * 0.35) * w)
        arms(p, lx=0.3 * w, rx=0.3 * w)
        tail(p, t, amp=0.3, period=30, up=0.15 * w)
    elif kind == "wave":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        side = kw.get("side", "R")
        if side in ("R", "both"):
            arms(p, rz=0.52 - 1.55 * w, rx=0.25 * w)
            add(p, "LowerArm.R", z=-0.4 * w + 0.55 * math.sin(t * 0.55) * w)
        if side in ("L", "both"):
            arms(p, lz=-0.52 + 1.55 * w, lx=0.25 * w)
            add(p, "LowerArm.L", z=0.4 * w - 0.55 * math.sin(t * 0.55) * w)
        add(p, "Head", z=0.15 * w)
        add(p, "Spine", z=0.06 * w)
        tail(p, t, amp=0.35, period=28, up=0.2)
        z = 0.012 * abs(math.sin(t * 0.275)) * w
    elif kind == "cheer":
        w = ss(k / 0.1) * (1 - ss((k - 0.9) / 0.1))
        b = abs(math.sin(TAU * u / 18.0))
        arms(p, lz=-0.52 + (1.15 + 0.3 * b) * w, rz=0.52 - (1.15 + 0.3 * b) * w, lx=0.2 * w, rx=0.2 * w)
        add(p, "Head", x=-0.25 * w)
        add(p, "Spine", x=-0.1 * w)
        add(p, "UpperLeg.L", x=0.3 * b * w); add(p, "LowerLeg.L", x=-0.5 * b * w)
        add(p, "UpperLeg.R", x=0.3 * b * w); add(p, "LowerLeg.R", x=-0.5 * b * w)
        tail(p, t, amp=0.45, period=18, up=0.3)
        z = 0.14 * b * w * sc
    elif kind == "grab":
        # reach forward + down, pick up, bring to chest
        if k < 0.5:
            w = ss(k / 0.35)
            arms(p, lx=1.35 * w, lz=-0.52 + 0.42 * w, rx=1.35 * w, rz=0.52 - 0.42 * w)
            add(p, "Spine", x=0.45 * w); add(p, "Hips", x=0.15 * w)
            add(p, "Head", x=0.2 * w)
            add(p, "UpperLeg.L", x=0.25 * w); add(p, "UpperLeg.R", x=0.25 * w)
            add(p, "LowerLeg.L", x=-0.35 * w); add(p, "LowerLeg.R", x=-0.35 * w)
            z = -0.025 * w * sc
        else:
            w = ss((k - 0.5) / 0.35)
            hold_pose(p, 1.0)
            add(p, "Spine", x=0.45 * (1 - w)); add(p, "Hips", x=0.15 * (1 - w))
            add(p, "Head", x=0.2 * (1 - w) - 0.1 * w)
            z = -0.025 * (1 - w) * sc
        tail(p, t, amp=0.3, period=30, up=0.2)
    elif kind == "eat":
        hold_pose(p, 1.0)
        n = math.sin(t * 0.9)
        add(p, "Head", x=0.12 + 0.08 * n)
        add(p, "UpperArm.L", x=0.15 + 0.1 * n); add(p, "UpperArm.R", x=0.15 + 0.1 * n)
        add(p, "Spine", x=0.05)
        tail(p, t, amp=0.35, period=40, up=0.15)
    elif kind == "surprise":
        a = ss(k / 0.12)
        r = 1 - ss((k - 0.6) / 0.4)
        w = a * r
        jump = math.sin(math.pi * clamp(k / 0.35)) if k < 0.35 else 0.0
        arms(p, lz=-0.52 + 1.25 * w, rz=0.52 - 1.25 * w, lx=0.4 * w, rx=0.4 * w)
        add(p, "Spine", x=-0.22 * w)
        add(p, "Head", x=-0.3 * w)
        add(p, "UpperLeg.L", x=0.4 * jump); add(p, "UpperLeg.R", x=0.4 * jump)
        add(p, "LowerLeg.L", x=-0.7 * jump); add(p, "LowerLeg.R", x=-0.7 * jump)
        for i, b in enumerate(TAILS):
            add(p, b, z=0.45 * w, x=0.05 * math.sin(t * 2.5 + i) * w)
        z = 0.22 * jump * sc
        yo = kw.get("recoil", 0.0) * w
    elif kind == "shy":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        sw = math.sin(t * 0.18)
        add(p, "Head", x=0.25 * w, z=0.22 * w)
        add(p, "Spine", y=0.2 * sw * w, z=0.06 * w)
        arms(p, lx=0.9 * w, rx=0.9 * w, lz=-0.52 + 0.3 * w, rz=0.52 - 0.3 * w,
             llz=0.6 * w, lrz=-0.6 * w)
        add(p, "UpperLeg.R", x=0.25 * max(0, sw) * w)
        add(p, "Foot.R", y=0.4 * sw * w)
        tail(p, t, amp=0.25, period=48, up=-0.1)
    elif kind == "dance":
        w = ss(k / 0.08) * (1 - ss((k - 0.92) / 0.08))
        bt = kw.get("beat", 20.0)
        ph = TAU * u / (bt * 2)
        s1 = math.sin(ph)
        b = abs(math.sin(ph))
        add(p, "Spine", z=0.25 * s1 * w, y=0.15 * s1 * w)
        add(p, "Head", z=-0.2 * s1 * w, x=-0.1 * w)
        arms(p, lz=-0.52 + (1.0 + 0.9 * max(0, s1)) * w, rz=0.52 - (1.0 + 0.9 * max(0, -s1)) * w,
             llz=0.4 * s1 * w, lrz=0.4 * s1 * w)
        add(p, "UpperLeg.L", x=0.45 * max(0, s1) * w); add(p, "LowerLeg.L", x=-0.6 * max(0, s1) * w)
        add(p, "UpperLeg.R", x=0.45 * max(0, -s1) * w); add(p, "LowerLeg.R", x=-0.6 * max(0, -s1) * w)
        tail(p, t, amp=0.45, period=bt * 2, up=0.25)
        z = (0.04 * b) * w * sc
        yo = kw.get("twist", 0.25) * s1 * w
    elif kind == "scratch":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        arms(p, rz=0.52 - 1.6 * w, rx=0.3 * w)
        add(p, "LowerArm.R", z=-0.9 * w + 0.25 * math.sin(t * 1.3) * w)
        add(p, "Head", z=-0.2 * w, x=0.05 * w)
        tail(p, t, amp=0.2)
    elif kind == "shake":  # head no / brr
        w = ss(k / 0.1) * (1 - ss((k - 0.8) / 0.2))
        add(p, "Head", y=0.45 * math.sin(t * 1.6) * w)
        add(p, "Spine", y=0.12 * math.sin(t * 1.6 - 0.6) * w)
        tail(p, t, amp=0.3, period=20)
    elif kind == "nod":
        w = ss(k / 0.1) * (1 - ss((k - 0.8) / 0.2))
        add(p, "Head", x=0.25 * max(0, math.sin(t * 0.7)) * w)
        tail(p, t, amp=0.3, period=30, up=0.1)
    elif kind == "point":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        arms(p, rx=1.4 * w, rz=0.52 - 0.6 * w)
        add(p, "Head", x=-0.15 * w)
        add(p, "Spine", x=-0.05 * w)
        tail(p, t, amp=0.3, period=30, up=0.15)
    elif kind == "hug":
        w = ss(k / 0.15) * (1 - ss((k - 0.85) / 0.15))
        arms(p, lx=1.2 * w, rx=1.2 * w, lz=-0.52 + 0.75 * w, rz=0.52 - 0.75 * w,
             llx=0.2 * w, lrx=0.2 * w, llz=0.7 * w, lrz=-0.7 * w)
        add(p, "Head", z=0.2 * w, x=0.1 * w)
        add(p, "Spine", x=0.12 * w, y=0.08 * math.sin(t * 0.2) * w)
        tail(p, t, amp=0.3, period=40, up=0.2)
    elif kind == "sleepy":
        w = ss(k / 0.2)
        add(p, "Head", x=0.35 * w + 0.05 * math.sin(t * 0.07))
        add(p, "Spine", x=0.12 * w)
        tail(p, t, amp=0.08, period=110)
    elif kind == "sweep":
        w = ss(k / 0.1) * (1 - ss((k - 0.9) / 0.1))
        s1 = math.sin(TAU * u / 42.0)
        arms(p, lx=0.85 * w, rx=0.85 * w, lz=-0.52 + 0.25 * w, rz=0.52 - 0.25 * w,
             llz=0.35 * w, lrz=-0.35 * w)
        add(p, "Spine", y=0.32 * s1 * w, x=0.15 * w)
        add(p, "Hips", y=0.1 * s1 * w)
        add(p, "Head", y=-0.12 * s1 * w, z=0.05 * w)
        add(p, "UpperLeg.L", x=0.15 * max(0, s1) * w)
        add(p, "UpperLeg.R", x=0.15 * max(0, -s1) * w)
        tail(p, t, amp=0.3, period=42, up=0.1)
        yo = 0.12 * s1 * w
    elif kind == "hold":
        hold_pose(p, 1.0)
        tail(p, t, amp=0.25, period=40, up=0.1)
    elif kind == "stomp":  # angry / fluster
        w = ss(k / 0.1) * (1 - ss((k - 0.85) / 0.15))
        s1 = math.sin(TAU * u / 10.0)
        add(p, "UpperLeg.L", x=0.5 * max(0, s1) * w); add(p, "LowerLeg.L", x=-0.6 * max(0, s1) * w)
        add(p, "UpperLeg.R", x=0.5 * max(0, -s1) * w); add(p, "LowerLeg.R", x=-0.6 * max(0, -s1) * w)
        arms(p, lz=-0.52 + 0.9 * w + 0.3 * s1 * w, rz=0.52 - 0.9 * w + 0.3 * s1 * w)
        add(p, "Head", x=0.1 * w, y=0.15 * s1 * w)
        for b in TAILS:
            add(p, b, z=0.4 * w, x=0.15 * s1 * w)
        z = 0.03 * abs(s1) * w * sc
    else:
        raise ValueError(kind)
    return p, z, yo


def hold_pose(p, w):
    arms(p, lx=1.05 * w, rx=1.05 * w, lz=-0.52 + 0.55 * w, rz=0.52 - 0.55 * w,
         llz=0.55 * w, lrz=-0.55 * w)


# sit = (thigh forward, knee bend, foot, body drop)
SIT = (0.5, -0.85, 0.35, 0.016)


def sit_pose(t, mode, u, sc):
    p = base_pose()
    breathe(p, t, 0.8)
    # light crouch: only the knees give a little, the body barely drops
    setb(p, "UpperLeg.L", x=SIT[0]); setb(p, "UpperLeg.R", x=SIT[0])
    setb(p, "LowerLeg.L", x=SIT[1]); setb(p, "LowerLeg.R", x=SIT[1])
    setb(p, "Foot.L", x=SIT[2]); setb(p, "Foot.R", x=SIT[2])
    add(p, "Hips", x=-0.06)
    add(p, "Spine", x=0.02)
    arms(p, lx=0.35, rx=0.35, lz=-0.62, rz=0.62)
    if mode == "swing":
        s1 = math.sin(t * 0.28)
        add(p, "LowerLeg.L", x=0.18 * s1); add(p, "LowerLeg.R", x=-0.18 * s1)
        add(p, "Head", z=0.1 * math.sin(t * 0.14))
        tail(p, t, amp=0.3, period=40, up=0.1)
    elif mode == "eat":
        hold_pose(p, 1.0)
        n = math.sin(t * 0.9)
        add(p, "Head", x=0.12 + 0.08 * n)
        add(p, "UpperArm.L", x=0.15 + 0.1 * n); add(p, "UpperArm.R", x=0.15 + 0.1 * n)
        tail(p, t, amp=0.35, period=40, up=0.15)
    elif mode == "sleep":
        add(p, "Head", x=0.4 + 0.04 * math.sin(t * 0.07))
        add(p, "Spine", x=0.18)
        tail(p, t, amp=0.06, period=120)
    elif mode == "sulk":
        add(p, "Head", x=0.3, z=0.12)
        add(p, "Spine", x=0.15)
        arms(p, lx=0.6, rx=0.6, lz=-0.45, rz=0.45)
        tail(p, t, amp=0.12, period=60, up=-0.2)
    elif mode == "leanR":
        add(p, "Head", z=-0.3, x=0.05)
        add(p, "Spine", z=-0.1)
        tail(p, t, amp=0.15, period=70)
    elif mode == "gaze":
        add(p, "Head", x=-0.3)
        add(p, "Spine", x=-0.12)
        tail(p, t, amp=0.12, period=80)
    elif mode == "lean":  # leaning on partner
        add(p, "Head", z=0.3, x=0.05)
        add(p, "Spine", z=0.1)
        tail(p, t, amp=0.15, period=70)
    else:
        tail(p, t, amp=0.15, period=70)
    return p, -SIT[3] * sc


# ----------------------------------------------------------------- tracks
class State:
    __slots__ = ("pos", "yaw", "z", "pose", "base", "scale")

    def __init__(s, pos, yaw, z, pose, base=None, scale=1.0):
        s.pos = pos; s.yaw = yaw; s.z = z; s.pose = pose; s.base = base; s.scale = scale


class Track:
    def __init__(s, name, pos, yaw=0.0, sc=1.0, ground=None, start=1):
        s.name = name
        s.sc = sc
        s.ground = ground
        s.segs = []
        s.f = start
        p = base_pose()
        s.state = State(V2(pos), yaw, 0.0, p)
        s.looks = []    # (f0, f1, target(x,y) or angle, kind)
        s.holds = []    # (f0, f1)

    # floor height under a state
    def floor(s, st):
        return st.base if st.base is not None else s.ground(st.pos)

    def _add(s, dur, fn, blend=8):
        dur = max(1, int(round(dur)))
        f0 = s.f
        prev = s.state
        s.segs.append((f0, dur, fn, blend, prev))
        s.state = fn(dur)
        s.f += dur
        return f0

    # ---- primitives
    def place(s, pos, yaw=None, dur=1):
        st0 = s.state
        P = V2(pos)
        Y = st0.yaw if yaw is None else yaw

        def fn(u, P=P, Y=Y):
            t = s.f
            p = base_pose()
            tail(p, u)
            return State(P, Y, 0.0, p)
        return s._add(dur, fn, blend=0)

    def idle(s, dur, kind="idle", look=None, blend=8, **kw):
        st0 = s.state
        P = st0.pos.copy(); Y = st0.yaw; B = st0.base
        f0 = s.f

        def fn(u):
            p, z, yo = emote(kind, u, dur, f0 + u, s.sc, kw)
            return State(P, Y + yo, z, p, base=B)
        r = s._add(dur, fn, blend)
        if look:
            s.look(f0, look)
        return r

    def act(s, kind, dur, look=None, blend=8, **kw):
        return s.idle(dur, kind=kind, look=look, blend=blend, **kw)

    def sit(s, dur, mode="rest", look=None, blend=12):
        st0 = s.state
        P = st0.pos.copy(); Y = st0.yaw; B = st0.base
        f0 = s.f

        def fn(u):
            p, z = sit_pose(f0 + u, mode, u, s.sc)
            return State(P, Y, z, p, base=B)
        r = s._add(dur, fn, blend)
        if look:
            s.look(f0, look)
        return r

    def turn(s, target, dur=14, blend=4):
        st0 = s.state
        P = st0.pos.copy(); Y0 = st0.yaw; B = st0.base
        if isinstance(target, (int, float)):
            Y1 = target
        else:
            T = V2(target)
            Y1 = yaw_to(T.x - P.x, T.y - P.y)
        f0 = s.f

        def fn(u):
            k = ss(u / dur)
            amt = math.sin(math.pi * clamp(u / dur)) * 0.6
            p, z = gait("walk", u / 10.0, amt, f0 + u)
            return State(P, alerp(Y0, Y1, k), z, p, base=B)
        return s._add(dur, fn, blend)

    def face(s, target, dur=14):
        return s.turn(target, dur)

    def walk(s, pts, speed=1.0, style="walk", blend=6, ramp=10, hold=False):
        st0 = s.state
        path = [st0.pos.copy()] + [V2(p) for p in pts]
        segl = [(path[i + 1] - path[i]).length for i in range(len(path) - 1)]
        L = sum(segl)
        stride = STYLES[style][0] * (s.sc ** 0.5)
        T = L / speed * FPS + ramp
        T = max(T, 2 * ramp + 1)
        vmax = L / (T - ramp)
        f0 = s.f

        def dist(u):
            if u < ramp:
                return 0.5 * vmax * u * u / ramp
            if u <= T - ramp:
                return 0.5 * vmax * ramp + vmax * (u - ramp)
            r = T - u
            return L - 0.5 * vmax * r * r / ramp

        def point(d):
            d = clamp(d, 0, L)
            acc = 0.0
            for i, l in enumerate(segl):
                if d <= acc + l or i == len(segl) - 1:
                    k = 0 if l == 0 else (d - acc) / l
                    return path[i].lerp(path[i + 1], clamp(k)), i
                acc += l

        def tangent_yaw(d):
            a, _ = point(max(0.0, min(d, L - 0.05)))
            b, _ = point(min(L, d + 0.35))
            v = b - a
            if v.length < 1e-4:
                v = path[-1] - path[-2]
            return yaw_to(v.x, v.y)

        def fn(u):
            u = min(u, T)
            d = dist(u)
            pos, _ = point(d)
            amt = clamp(min(u, T - u) / ramp)
            amt = 0.15 + 0.85 * amt if 0 < u < T else 0.0
            p, z = gait(style, d / stride, amt, f0 + u)
            if hold:
                hold_pose(p, 1.0)
            return State(pos, tangent_yaw(d), z * s.sc, p)
        return s._add(T, fn, blend)

    def jump(s, to=None, h=0.5, air=None, base_to="ground", antic=8, land=9):
        st0 = s.state
        P0 = st0.pos.copy()
        P1 = V2(to) if to is not None else P0.copy()
        dist = (P1 - P0).length
        if air is None:
            air = int(14 + 10 * h + 4 * dist)
        fl0 = s.floor(st0)
        if base_to == "ground":
            B1 = None
            fl1 = s.ground(P1)
        else:
            B1 = base_to
            fl1 = base_to
        Y0 = st0.yaw
        Y1 = yaw_to(P1.x - P0.x, P1.y - P0.y) if dist > 0.05 else Y0
        dur = antic + air + land
        f0 = s.f
        amp = 0.6 + 0.4 * clamp(h / 0.5)   # small hops squash less

        def fn(u):
            st = fn_(u)
            st.scale = squash_curve(u)
            return st

        def squash_curve(u):
            if u < antic:                       # crouch: squash down
                return 1 - 0.12 * amp * ss(u / antic)
            if u < antic + air:                 # takeoff stretch, relax, stretch into the landing
                k = (u - antic) / air
                return 1 + amp * (0.15 * (1 - ss(k / 0.45)) + 0.06 * ss((k - 0.7) / 0.3))
            k = (u - antic - air) / land        # impact squash, springy overshoot back to rest
            if k < 0.2:
                return lerp(1 + 0.06 * amp, 1 - 0.22 * amp, ss(k / 0.2))
            q = (k - 0.2) / 0.8
            return 1 - 0.22 * amp * math.cos(math.pi * 1.5 * q) * (1 - q)

        def fn_(u):
            t = f0 + u
            p = base_pose()
            if u < antic:
                k = ss(u / antic)
                add(p, "UpperLeg.L", x=0.5 * k); add(p, "UpperLeg.R", x=0.5 * k)
                add(p, "LowerLeg.L", x=-0.9 * k); add(p, "LowerLeg.R", x=-0.9 * k)
                arms(p, lx=-0.7 * k, rx=-0.7 * k)
                add(p, "Spine", x=0.3 * k)
                add(p, "Head", x=-0.2 * k)
                tail(p, t, amp=0.1, up=-0.1 * k)
                return State(P0, alerp(Y0, Y1, k), -0.04 * k * s.sc, p, base=st0.base)
            if u < antic + air:
                k = (u - antic) / air
                pos = P0.lerp(P1, k)
                zabs = lerp(fl0, fl1, k) + 4 * h * k * (1 - k)
                tuck = math.sin(math.pi * k)
                add(p, "UpperLeg.L", x=0.7 * tuck); add(p, "UpperLeg.R", x=0.5 * tuck)
                add(p, "LowerLeg.L", x=-1.0 * tuck); add(p, "LowerLeg.R", x=-0.8 * tuck)
                arms(p, lz=-0.52 + 1.3 * tuck, rz=0.52 - 1.3 * tuck, lx=0.3, rx=0.3)
                add(p, "Spine", x=-0.15 * tuck + 0.2 * k)
                add(p, "Head", x=-0.25 * tuck)
                for i, b in enumerate(TAILS):
                    add(p, b, z=0.4 * tuck - 0.2 * k)
                return State(pos, Y1, 0.0, p, base=zabs)
            k = (u - antic - air) / land
            sq = math.sin(math.pi * clamp(k))
            add(p, "UpperLeg.L", x=0.55 * sq); add(p, "UpperLeg.R", x=0.55 * sq)
            add(p, "LowerLeg.L", x=-1.0 * sq); add(p, "LowerLeg.R", x=-1.0 * sq)
            arms(p, lz=-0.52 + 0.5 * sq, rz=0.52 - 0.5 * sq)
            add(p, "Spine", x=0.25 * sq)
            tail(p, t, amp=0.2, up=0.2 * sq)
            return State(P1, Y1, -0.045 * sq * s.sc, p, base=B1)
        return s._add(dur, fn, blend=5)

    def hop(s, n=3, h=0.25, each=16):
        for i in range(n):
            s.jump(None, h=h, air=each, antic=5, land=6,
                   base_to=("ground" if s.state.base is None else s.state.base))

    def spin(s, turns=1.0, dur=30, h=0.3):
        st0 = s.state
        P = st0.pos.copy(); Y0 = st0.yaw; B = st0.base
        f0 = s.f

        def fn(u):
            k = ss(u / dur)
            p = base_pose()
            j = math.sin(math.pi * clamp(u / dur))
            arms(p, lz=-0.52 + 1.15 * j, rz=0.52 - 1.15 * j)
            add(p, "UpperLeg.L", x=0.5 * j); add(p, "LowerLeg.L", x=-0.8 * j)
            add(p, "UpperLeg.R", x=0.5 * j); add(p, "LowerLeg.R", x=-0.8 * j)
            add(p, "Head", x=-0.2 * j)
            tail(p, f0 + u, amp=0.2, up=0.4 * j)
            return State(P, Y0 + TAU * turns * k, h * j * s.sc, p, base=B)
        return s._add(dur, fn, blend=5)

    def look(s, f0, items):
        """items: list of (rel_frame, target) ; target=(x,y) world point, float angle, or None"""
        for i, (rf, tgt) in enumerate(items):
            fa = f0 + rf
            fb = f0 + items[i + 1][0] if i + 1 < len(items) else None
            s.looks.append((fa, fb, tgt))

    def hold(s, f0, f1):
        s.holds.append((f0, f1))

    # ---- evaluation
    def raw(s, f):
        segs = s.segs
        if not segs:
            st = s.state
            return st
        if f < segs[0][0]:
            return segs[0][2](0)
        for (f0, dur, fn, blend, prev) in segs:
            if f0 <= f < f0 + dur:
                u = f - f0
                st = fn(u)
                if blend > 0 and u < blend:
                    w = ss(u / blend)
                    fp = s.floor(prev)
                    fc = s.floor(st)
                    pz = fp + prev.z
                    cz = fc + st.z
                    return State(st.pos, alerp(prev.yaw, st.yaw, w), 0.0,
                                 mix_pose(prev.pose, st.pose, w), base=lerp(pz, cz, w),
                                 scale=lerp(prev.scale, st.scale, w))
                return st
        return segs[-1][2](segs[-1][1])

    def squash(s, f):
        """Vertical squash/stretch factor (1 = rest); the Mover scales around the feet."""
        return s.raw(f).scale

    def head_look(s, f, st):
        best = None
        for (fa, fb, tgt) in s.looks:
            if f >= fa and (fb is None or f < fb):
                best = (fa, tgt)
        if best is None:
            return 0.0
        fa, tgt = best
        if tgt is None:
            a = 0.0
        elif isinstance(tgt, (int, float)):
            a = tgt
        else:
            T = V2(tgt)
            a = angdiff(st.yaw, yaw_to(T.x - st.pos.x, T.y - st.pos.y))
        a = clamp(a, -1.1, 1.1)
        # find previous look value for easing
        prev = 0.0
        for (pa, pb, ptgt) in s.looks:
            if pb == fa:
                if ptgt is None:
                    prev = 0.0
                elif isinstance(ptgt, (int, float)):
                    prev = ptgt
                else:
                    T = V2(ptgt)
                    prev = clamp(angdiff(st.yaw, yaw_to(T.x - st.pos.x, T.y - st.pos.y)), -1.1, 1.1)
        k = ss((f - fa) / 10.0)
        return lerp(prev, a, k)

    def evaluate(s, f):
        st = s.raw(f)
        pose = {k: list(v) for k, v in st.pose.items()}
        a = s.head_look(f, st)
        if a:
            add(pose, "Head", y=a * 0.7)
            add(pose, "Spine", y=a * 0.3)
        for (h0, h1) in s.holds:
            if h0 - 8 <= f <= h1 + 8:
                w = ss((f - (h0 - 8)) / 8.0) * (1 - ss((f - h1) / 8.0))
                hp = {k: list(v) for k, v in pose.items()}
                hold_pose(hp, 1.0)
                for b in ("UpperArm.L", "UpperArm.R", "LowerArm.L", "LowerArm.R"):
                    pose[b] = [lerp(pose[b][i], hp[b][i], w) for i in range(3)]
        zabs = s.floor(st) + st.z
        return st.pos, st.yaw, zabs, pose


# ----------------------------------------------------------------- writing keys
def write_fcurves(idblock, action_name, channels, interp="BEZIER"):
    """channels: dict (data_path, index) -> list of (frame, value)"""
    ad = idblock.animation_data_create()
    if ad.action is not None:
        ad.action = None
    for tr in list(ad.nla_tracks):
        ad.nla_tracks.remove(tr)
    old = bpy.data.actions.get(action_name)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(action_name)
    ad.action = act
    for (dp, idx), keys in channels.items():
        fc = act.fcurve_ensure_for_datablock(idblock, dp, index=idx)
        fc.keyframe_points.clear()
        fc.keyframe_points.add(len(keys))
        flat = []
        for fr, v in keys:
            flat += [fr, v]
        fc.keyframe_points.foreach_set("co", flat)
        imode = {"CONSTANT": 0, "LINEAR": 1, "BEZIER": 2}[interp]
        fc.keyframe_points.foreach_set("interpolation", [imode] * len(keys))
        fc.update()
    return act


# Keep the arms close to the rig's T-pose: (down scale, up scale, forward/back scale)
ARM_REMAP = (0.3, 0.65, 0.75)


def arm_remap(bone, v):
    if not bone.startswith("UpperArm"):
        return v
    dn, up, fw = ARM_REMAP
    x, y, z = v
    lifted = z if bone.endswith(".L") else -z   # + = arm raised, - = arm lowered
    lifted *= up if lifted > 0 else dn
    z = lifted if bone.endswith(".L") else -lifted
    return [x * fw, y, z]


def _lin(keys, f):
    """Linear interpolation of sorted (frame, value) keys."""
    if f <= keys[0][0]:
        return keys[0][1]
    for (fa, va), (fb, vb) in zip(keys, keys[1:]):
        if fa <= f <= fb:
            return va if fb == fa else lerp(va, vb, (f - fa) / (fb - fa))
    return keys[-1][1]


def bake_cat(track, mover, arm, f_end, tag, smooth_z=0.35, bone_step=2, extra=None):
    has_tail = "Tail.01" in arm.pose.bones
    mv = {("location", 0): [], ("location", 1): [], ("location", 2): [], ("rotation_euler", 2): []}
    bones = {}
    zprev = None
    yprev = None
    for f in range(1, f_end + 1):
        pos, yaw, z, pose = track.evaluate(f)
        if zprev is not None and abs(z - zprev) < 0.2:
            z = lerp(zprev, z, 1 - smooth_z) if abs(z - zprev) > 0.0 else z
        zprev = z
        if yprev is not None:
            yaw = yprev + angdiff(yprev, yaw)
        yprev = yaw
        mv[("location", 0)].append((f, pos.x))
        mv[("location", 1)].append((f, pos.y))
        mv[("location", 2)].append((f, z))
        mv[("rotation_euler", 2)].append((f, yaw))
        if (f - 1) % bone_step == 0 or f == f_end:
            for b, v in pose.items():
                if not has_tail and b.startswith("Tail"):
                    continue
                v = arm_remap(b, v)
                for i in range(3):
                    bones.setdefault(('pose.bones["%s"].rotation_euler' % b, i), []).append((f, v[i]))
    # drop channels that never move
    bones = {k: v for k, v in bones.items() if max(abs(x[1]) for x in v) > 1e-4}
    sq = [track.squash(f) for f in range(1, f_end + 1)]
    extra = dict(extra or {})
    if any(abs(v - 1) > 1e-4 for v in sq):
        # volume-preserving squash on the Mover (origin = feet), multiplied into any extra scale keys
        for i in range(3):
            base = extra.pop(("scale", i), None)
            vals = []
            for f, v in zip(range(1, f_end + 1), sq):
                b = _lin(base, f) if base else 1.0
                vals.append((f, b * (v if i == 2 else 1 / math.sqrt(v))))
            mv[("scale", i)] = vals
    if extra:
        mv.update(extra)
    mover.scale = (1, 1, 1)
    write_fcurves(mover, tag + "_Move", mv, "LINEAR")
    # reset pose rotations that are not keyed
    for pb in arm.pose.bones:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
    write_fcurves(arm, tag + "_Pose", bones, "BEZIER")


# ----------------------------------------------------------------- scene helpers
def objmap(scene):
    return {o.get("src", o.name): o for o in scene.objects}


def make_ground(scene, names):
    M = objmap(scene)
    objs = [M[n] for n in names if n in M]
    dg = bpy.context.evaluated_depsgraph_get()
    cache = {}

    def ground(p):
        key = (round(p.x, 2), round(p.y, 2))
        if key in cache:
            return cache[key]
        best = None
        for o in objs:
            mi = o.matrix_world.inverted()
            org = mi @ Vector((p.x, p.y, 20.0))
            dst = (mi.to_3x3() @ Vector((0, 0, -1))).normalized()
            ok, loc, nor, idx = o.ray_cast(org, dst, depsgraph=dg)
            if ok:
                z = (o.matrix_world @ loc).z
                if best is None or z > best:
                    best = z
        if best is None:
            best = 0.07
        cache[key] = best
        return best
    return ground


def setup_mover(scene, arm, name, offset):
    col = scene.collection
    mv = bpy.data.objects.get(name)
    if mv is None:
        mv = bpy.data.objects.new(name, None)
        mv.empty_display_type = 'ARROWS'
        mv.empty_display_size = 0.4
        col.objects.link(mv)
    arm.animation_data_create()
    arm.animation_data.action = None
    for tr in list(arm.animation_data.nla_tracks):
        arm.animation_data.nla_tracks.remove(tr)
    arm.parent = mv
    arm.matrix_parent_inverse.identity()
    arm.location = offset
    arm.rotation_euler = (0, 0, 0)
    arm.scale = (1, 1, 1)
    return mv


class Cam:
    """Shot list -> per-frame camera/aim/lens keys."""

    def __init__(s, tracks):
        s.shots = []
        s.tracks = tracks

    def static(s, f0, f1, cam, aim, lens=40):
        s.shots.append((f0, f1, "move", (cam, cam, aim, aim, lens, lens, False)))

    def move(s, f0, f1, cam0, cam1, aim0, aim1, lens0=40, lens1=None, ease=True):
        s.shots.append((f0, f1, "move", (cam0, cam1, aim0, aim1, lens0, lens1 or lens0, ease)))

    def follow(s, f0, f1, who, off=(0, -3, 1.5), aim_off=(0, 0, 0.35), lens=45, rel=False, lag=0.08,
               off1=None, lens1=None):
        s.shots.append((f0, f1, "follow", (who, off, aim_off, lens, rel, lag, off1 or off, lens1 or lens)))

    def orbit(s, f0, f1, center, r, h, a0, a1, aim_h=0.4, lens=40, who=None, r1=None, h1=None):
        s.shots.append((f0, f1, "orbit", (center, r, h, a0, a1, aim_h, lens, who, r1 or r, h1 if h1 is not None else h)))

    def two(s, f0, f1, a, b, side=1.0, dist=3.0, h=1.2, lens=40):
        s.shots.append((f0, f1, "two", (a, b, side, dist, h, lens)))

    def bake(s, cam, aim, f_end, tag):
        ch_c = {("location", i): [] for i in range(3)}
        ch_a = {("location", i): [] for i in range(3)}
        ch_l = {("lens", 0): []}
        state = {}
        for f in range(1, f_end + 1):
            shot = None
            for sh in s.shots:
                if sh[0] <= f <= sh[1]:
                    shot = sh
            if shot is None:
                continue
            f0, f1, kind, a = shot
            k = (f - f0) / max(1, f1 - f0)
            if kind == "move":
                c0, c1, a0, a1, l0, l1, ease = a
                e = ss(k) if ease else k
                C = Vector(c0).lerp(Vector(c1), e)
                A = Vector(a0).lerp(Vector(a1), e)
                L = lerp(l0, l1, e)
            elif kind == "follow":
                who, off, aoff, lens, rel, lag, off1, lens1 = a
                pos, yaw, z, _ = s.tracks[who].evaluate(f)
                P = Vector((pos.x, pos.y, z))
                o = Vector(off).lerp(Vector(off1), ss(k))
                if rel:
                    o = Vector((o.x * math.cos(yaw) - o.y * math.sin(yaw),
                                o.x * math.sin(yaw) + o.y * math.cos(yaw), o.z))
                tc = P + o
                ta = P + Vector(aoff)
                key = id(shot)
                if key in state:
                    pc, pa = state[key]
                    tc = pc.lerp(tc, lag * 1.0)
                    ta = pa.lerp(ta, lag * 2.0)
                state[key] = (tc, ta)
                C, A = tc, ta
                L = lerp(lens, lens1, ss(k))
            elif kind == "orbit":
                ctr, r, h, a0, a1, aim_h, lens, who, r1, h1 = a
                if who:
                    pos, yaw, z, _ = s.tracks[who].evaluate(f)
                    ctr = (pos.x, pos.y, z)
                ang = lerp(a0, a1, ss(k))
                rr = lerp(r, r1, ss(k)); hh = lerp(h, h1, ss(k))
                C = Vector((ctr[0] + rr * math.cos(ang), ctr[1] + rr * math.sin(ang), ctr[2] + hh))
                A = Vector((ctr[0], ctr[1], ctr[2] + aim_h))
                L = lens
            elif kind == "two":
                wa, wb, side, dist, h, lens = a
                pa_, _, za, _ = s.tracks[wa].evaluate(f)
                pb_, _, zb, _ = s.tracks[wb].evaluate(f)
                mid = (pa_ + pb_) / 2
                d = pb_ - pa_
                if d.length < 1e-3:
                    d = Vector((1, 0))
                n = Vector((-d.y, d.x)).normalized() * side
                zz = (za + zb) / 2
                tc = Vector((mid.x + n.x * dist, mid.y + n.y * dist, zz + h))
                ta = Vector((mid.x, mid.y, zz + 0.35))
                key = id(shot)
                if key in state:
                    pc, pa2 = state[key]
                    tc = pc.lerp(tc, 0.1); ta = pa2.lerp(ta, 0.15)
                state[key] = (tc, ta)
                C, A, L = tc, ta, lens
            for i in range(3):
                ch_c[("location", i)].append((f, C[i]))
                ch_a[("location", i)].append((f, A[i]))
            ch_l[("lens", 0)].append((f, L))
        write_fcurves(cam, tag + "_Cam", ch_c, "LINEAR")
        write_fcurves(aim, tag + "_Aim", ch_a, "LINEAR")
        write_fcurves(cam.data, tag + "_Lens", ch_l, "LINEAR")


# ----------------------------------------------------------------- props
def key_vis(obj, ranges_visible, f_end, tag):
    """ranges_visible: list of (f0,f1) during which the object is visible."""
    keys_v = []
    keys_r = []
    last = None
    for f in range(1, f_end + 1):
        vis = any(a <= f <= b for a, b in ranges_visible)
        if vis != last:
            keys_v.append((f, 0.0 if vis else 1.0))
            last = vis
    ch = {("hide_viewport", 0): keys_v, ("hide_render", 0): list(keys_v)}
    write_fcurves(obj, tag + "_Vis", ch, "CONSTANT")


def held_copy(scene, src_obj, arm, bone, name, scale=0.35, loc=(0, 0, 0), rot=(0, 0, 0)):
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, src_obj.data)
        scene.collection.objects.link(o)
    o.parent = arm
    o.parent_type = 'BONE'
    o.parent_bone = bone
    o.matrix_parent_inverse.identity()
    base = src_obj.matrix_world.to_scale()
    o.scale = (base.x * scale, base.y * scale, base.z * scale)
    o.location = loc
    o.rotation_euler = rot
    return o


def unparent_keep(obj):
    mw = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = mw


def key_obj_path(obj, keys, tag, interp="BEZIER"):
    """keys: list of (frame, (x,y,z), (rx,ry,rz) or None)"""
    ch = {}
    for f, loc, rot in keys:
        for i in range(3):
            ch.setdefault(("location", i), []).append((f, loc[i]))
        if rot is not None:
            for i in range(3):
                ch.setdefault(("rotation_euler", i), []).append((f, rot[i]))
    write_fcurves(obj, tag, ch, interp)


# ----------------------------------------------------------------- world
def gradient_world(name, zenith, horizon, ground, strength=1.0):
    w = bpy.data.worlds.get(name) or bpy.data.worlds.new(name)
    w.use_nodes = True
    nt = w.node_tree
    if nt.animation_data:
        nt.animation_data_clear()
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = -0.15
    mr.inputs["From Max"].default_value = 0.6
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    cr = ramp.color_ramp
    cr.elements[0].position = 0.0
    cr.elements[0].color = (*ground, 1)
    cr.elements[1].position = 1.0
    cr.elements[1].color = (*zenith, 1)
    e = cr.elements.new(0.22)
    e.color = (*horizon, 1)
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Strength"].default_value = strength
    out = nt.nodes.new("ShaderNodeOutputWorld")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
    nt.links.new(mr.outputs["Result"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    return w


SKY_IMG = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "HDRI", "Sky_Anime_02_dusk_a.png")   # same panorama as the Unity skybox


def unity_sky(w, strength=1.0, rot_z=0.0):
    """Camera rays see the Unity skybox panorama; lighting keeps the gradient world.
    Returns the camera Background node (key its Strength / Color to dim it)."""
    nt = w.node_tree
    out = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeOutputWorld")
    old = nt.nodes.get("USKY_mix")   # re-run: the lighting shader feeds the old mix
    src = (old.inputs[1] if old else out.inputs["Surface"]).links[0].from_socket
    for n in [n for n in nt.nodes if n.name.startswith("USKY_")]:
        nt.nodes.remove(n)
    img = bpy.data.images.load(SKY_IMG, check_existing=True)
    tc = nt.nodes.new("ShaderNodeTexCoord"); tc.name = "USKY_tc"
    mp = nt.nodes.new("ShaderNodeMapping"); mp.name = "USKY_rot"
    mp.inputs["Rotation"].default_value[2] = rot_z
    env = nt.nodes.new("ShaderNodeTexEnvironment"); env.name = "USKY_tex"
    env.image = img
    cbg = nt.nodes.new("ShaderNodeBackground"); cbg.name = "USKY_bg"
    cbg.inputs["Strength"].default_value = strength
    lp = nt.nodes.new("ShaderNodeLightPath"); lp.name = "USKY_lp"
    mix = nt.nodes.new("ShaderNodeMixShader"); mix.name = "USKY_mix"
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    nt.links.new(env.outputs["Color"], cbg.inputs["Color"])
    nt.links.new(lp.outputs["Is Camera Ray"], mix.inputs["Fac"])
    nt.links.new(src, mix.inputs[1])
    cam = cbg.outputs["Background"]
    star = next((n for n in nt.nodes if n.bl_idname == "ShaderNodeEmission"), None)
    if star:   # night world: stars stay visible on top of the sky
        add_ = nt.nodes.new("ShaderNodeAddShader"); add_.name = "USKY_add"
        nt.links.new(cam, add_.inputs[0])
        nt.links.new(star.outputs["Emission"], add_.inputs[1])
        cam = add_.outputs[0]
    nt.links.new(cam, mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    return cbg


# ----------------------------------------------------------------- episode setup
CATS = {"Black": ("Cat_Black", (0.048, 0.03, 0), 1.0),
        "White": ("Cat_White", (-1.0, 0.0, 0), 1.35),
        "Rainbow": ("Cat_Rainbow", (0.048, 0.02, 0), 1.0)}
GROUND = ["Island_Grass", "Concre_Low", "Bridge_Low.003", "Bridge_Low.001"]


class Episode:
    def __init__(s, scene_name, tag):
        import math as _m
        s.sc = bpy.data.scenes[scene_name]
        bpy.context.window.scene = s.sc
        s.sc.frame_set(1)
        s.tag = tag
        s.M = objmap(s.sc)
        s.ground = make_ground(s.sc, GROUND)
        s.arm = {}
        s.mover = {}
        for k, (src, off, sc_) in CATS.items():
            a = s.M[src]
            s.arm[k] = a
            s.mover[k] = setup_mover(s.sc, a, "%s_Mover_%s" % (tag, k), off)
            for pb in a.pose.bones:
                for c in pb.constraints:
                    if c.type == 'LIMIT_ROTATION' and pb.name == "Head":
                        c.min_x, c.max_x = _m.radians(-15), _m.radians(15)
                        c.min_y, c.max_y = _m.radians(-40), _m.radians(40)
                        c.min_z, c.max_z = _m.radians(-15), _m.radians(15)
        s.cam = s.M["Main_Cam"]
        s.aim = s.M["Cam_Aim"]
        s.cam.data.dof.use_dof = True
        s.cam.data.dof.focus_object = s.aim
        s.cam.data.dof.aperture_fstop = 3.2
        s.sc.render.resolution_x, s.sc.render.resolution_y = 1920, 1080
        s.cam.data.sensor_fit = 'AUTO'
        s.cam.data.sensor_width = 36.0 * (1280 / 1920) * (1920 / 1080)   # 16:9 keeping the 3:2 vertical framing
        s.sc.render.use_motion_blur = True
        s.sc.render.motion_blur_shutter = 0.5   # half-frame shutter: smooth runs, no smeared idles
        s.tracks = {}
        ivory_white_cat(s)

    def track(s, who, pos, yaw=0.0):
        t = Track(who, pos, yaw, CATS[who][2], s.ground)
        s.tracks[who] = t
        return t

    def surf(s, name, x, y):
        o = s.M[name]
        dg = bpy.context.evaluated_depsgraph_get()
        mi = o.matrix_world.inverted()
        ok, loc, nor, idx = o.ray_cast(mi @ Vector((x, y, 10)), (mi.to_3x3() @ Vector((0, 0, -1))).normalized(), depsgraph=dg)
        return (o.matrix_world @ loc).z if ok else None

    def bake(s, f_end, extra=None):
        extra = extra or {}
        for k in CATS:
            t = s.tracks.get(k)
            if t is None:
                t = Track(k, (60 + 5 * len(k), 0), 0, CATS[k][2], lambda p: 0.0)
                t.idle(10)
            bake_cat(t, s.mover[k], s.arm[k], f_end, "%s_%s" % (s.tag, k), extra=extra.get(k))
        s.sc.frame_start = 1
        s.sc.frame_end = f_end


def make_held(E, who, src_name, name, size, fwd=0.12, down=0.05, bone="Spine", yaw=0.0, tilt=0.0):
    """Copy of a prop mesh carried at the chest of a cat (parented to a bone)."""
    from mathutils import Matrix, Euler
    src = E.M[src_name]
    arm = E.arm[who]
    k = CATS[who][2]
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, src.data)
        E.sc.collection.objects.link(o)
    o.parent = arm
    o.parent_type = 'BONE'
    o.parent_bone = bone
    o.matrix_parent_inverse.identity()
    s0 = src.matrix_world.to_scale()
    f = size / max(src.dimensions)
    o.scale = (s0.x * f, s0.y * f, s0.z * f)
    rot = Euler((-math.pi / 2 + tilt, yaw, 0), 'XYZ')
    o.rotation_euler = rot
    c = sum((Vector(v) for v in src.bound_box), Vector()) / 8
    c = Vector((c.x * o.scale.x, c.y * o.scale.y, c.z * o.scale.z))
    want = Vector((0, -down * k, fwd * k))
    o.location = want - rot.to_matrix() @ c
    return o


def make_free_copy(E, src_name, name):
    src = E.M[src_name]
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, src.data)
        E.sc.collection.objects.link(o)
    o.parent = None
    o.matrix_world = src.matrix_world.copy()
    return o


def chest(E, who, f, up=0.27, fwd=0.12):
    pos, yaw, z, _ = E.tracks[who].evaluate(f)
    k = CATS[who][2]
    fx, fy = math.sin(yaw), -math.cos(yaw)
    return Vector((pos.x + fx * fwd * k, pos.y + fy * fwd * k, z + up * k))


def park_carts(E, frame=1):
    """Freeze the plaza carts (follow-path) at their position on `frame`."""
    E.sc.frame_set(frame)
    for n in ("Cart_A", "Cart_B"):
        o = E.M.get(n)
        if o is None:
            continue
        mw = o.matrix_world.copy()
        for c in o.constraints:
            if c.type == 'FOLLOW_PATH':
                c.mute = True
        o.matrix_world = mw


def park_carts_at(E, targets, search=(1, 4000, 8)):
    """targets: {"Cart_A": (x, y), "Cart_B": (x, y)} -> freeze each cart where its
    follow-path position is closest to the target point."""
    best = {n: (1e9, None) for n in targets}
    for c in ("Cart_A", "Cart_B"):
        o = E.M.get(c)
        if o:
            for k in o.constraints:
                if k.type == 'FOLLOW_PATH':
                    k.mute = False
    for f in range(*search):
        E.sc.frame_set(f)
        for n, (x, y) in targets.items():
            p = E.M[n].matrix_world.translation
            d = (p.x - x) ** 2 + (p.y - y) ** 2
            if d < best[n][0]:
                best[n] = (d, E.M[n].matrix_world.copy())
    for n, (d, mw) in best.items():
        o = E.M[n]
        for k in o.constraints:
            if k.type == 'FOLLOW_PATH':
                k.mute = True
        o.matrix_world = mw
    E.sc.frame_set(1)


def night_world(name):
    """Gradient sky + procedural stars; returns (world, ramp node, bg node, star factor node)."""
    w = gradient_world(name, (0.35, 0.35, 0.7), (1.0, 0.55, 0.35), (0.45, 0.35, 0.4), 1.0)
    nt = w.node_tree
    N = {n.bl_idname: n for n in nt.nodes}
    ramp = N["ShaderNodeValToRGB"]
    bg = N["ShaderNodeBackground"]
    out = N["ShaderNodeOutputWorld"]
    tc = N["ShaderNodeTexCoord"]
    sep = N["ShaderNodeSeparateXYZ"]
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 260.0
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = 0.0
    mr.inputs["From Max"].default_value = 0.07
    mr.inputs["To Min"].default_value = 1.0
    mr.inputs["To Max"].default_value = 0.0
    up = nt.nodes.new("ShaderNodeMapRange")
    up.inputs["From Min"].default_value = 0.0
    up.inputs["From Max"].default_value = 0.25
    fac = nt.nodes.new("ShaderNodeValue")
    fac.name = "StarFactor"
    fac.outputs[0].default_value = 0.0
    m1 = nt.nodes.new("ShaderNodeMath"); m1.operation = 'MULTIPLY'
    m2 = nt.nodes.new("ShaderNodeMath"); m2.operation = 'MULTIPLY'
    pw = nt.nodes.new("ShaderNodeMath"); pw.operation = 'POWER'; pw.inputs[1].default_value = 3.0
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (1.0, 0.95, 0.85, 1)
    em.inputs["Strength"].default_value = 6.0
    add_ = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(tc.outputs["Generated"], vor.inputs["Vector"])
    nt.links.new(vor.outputs["Distance"], mr.inputs["Value"])
    nt.links.new(mr.outputs["Result"], pw.inputs[0])
    nt.links.new(sep.outputs["Z"], up.inputs["Value"])
    nt.links.new(pw.outputs[0], m1.inputs[0])
    nt.links.new(up.outputs["Result"], m1.inputs[1])
    nt.links.new(m1.outputs[0], m2.inputs[0])
    nt.links.new(fac.outputs[0], m2.inputs[1])
    nt.links.new(m2.outputs[0], em.inputs["Strength"])
    em.inputs["Strength"].default_value = 0.0
    nt.links.new(bg.outputs["Background"], add_.inputs[0])
    nt.links.new(em.outputs["Emission"], add_.inputs[1])
    nt.links.new(add_.outputs[0], out.inputs["Surface"])
    m3 = nt.nodes.new("ShaderNodeMath"); m3.operation = 'MULTIPLY'; m3.inputs[1].default_value = 8.0
    nt.links.new(m2.outputs[0], m3.inputs[0])
    nt.links.new(m3.outputs[0], em.inputs["Strength"])
    return w, ramp, bg, fac


def ivory_white_cat(E, color=(0.86, 0.72, 0.50)):
    """Give the white cat an ivory body in this episode only (object-linked slot)."""
    m = bpy.data.materials.get("M_Cat_Ivory") or bpy.data.materials.new("M_Cat_Ivory")
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = 0.55
    for o in E.arm["White"].children:
        if o.type != 'MESH' or "mini cat" not in o.name:
            continue
        if len(o.data.materials) == 0:
            o.data.materials.append(None)   # empty data slot keeps the original scene unchanged
        for sl in o.material_slots:
            sl.link = 'OBJECT'
            sl.material = m


# Per-scene colour grade (vivid, pastel "storybook" look). Leaves asset materials untouched.
GRADE = {"sat": 1.3, "val": 1.0, "bright": 0.0, "contrast": 6.0, "bloom": 0.5, "bloom_thr": 0.8}


def grade(E, **kw):
    g = dict(GRADE, **kw)
    ng = E.sc.compositing_node_group
    N = ng.nodes
    for n in [n for n in N if n.name in ("Grade_HueSat", "Grade_BC")]:
        N.remove(n)
    glare = N.get("Glare")
    seta = N.get("Set Alpha")
    hs = N.new("CompositorNodeHueSat"); hs.name = "Grade_HueSat"
    hs.inputs["Saturation"].default_value = g["sat"]
    hs.inputs["Value"].default_value = g["val"]
    bc = N.new("CompositorNodeBrightContrast"); bc.name = "Grade_BC"
    bc.inputs["Brightness"].default_value = g["bright"]
    bc.inputs["Contrast"].default_value = g["contrast"]
    hs.location = (glare.location.x + 200, glare.location.y - 200)
    bc.location = (glare.location.x + 400, glare.location.y - 200)
    ng.links.new(glare.outputs["Image"], hs.inputs["Image"])
    ng.links.new(hs.outputs["Image"], bc.inputs["Image"])
    ng.links.new(bc.outputs["Image"], seta.inputs["Image"])
    glare.inputs["Strength"].default_value = g["bloom"]
    glare.inputs["Threshold"].default_value = g["bloom_thr"]


def topple(E, src_name, f0, d, tip=14, settle=12, tag=None):
    """Box knocked over: tips around its bottom edge in direction d (x, y),
    drops to the ground if it was stacked, then rocks and settles."""
    from mathutils import Matrix, Quaternion
    o = E.M[src_name]
    E.sc.frame_set(1)
    unparent_keep(o)
    o.rotation_mode = 'QUATERNION'
    M0 = o.matrix_world.copy()
    d = Vector((d[0], d[1], 0)).normalized()
    bb = [M0 @ Vector(v) for v in o.bound_box]
    c = sum(bb, Vector()) / 8
    zb = min(v.z for v in bb)
    hd = max((v - c).dot(d) for v in bb)            # half extent along d
    hz = (max(v.z for v in bb) - zb) / 2
    P0 = Vector((c.x + d.x * hd, c.y + d.y * hd, zb))
    land = Vector((c.x + d.x * (hd + hz), c.y + d.y * (hd + hz)))
    gz = E.ground(land)
    drop = max(0.0, zb - gz)
    axis = Vector((-d.y, d.x, 0))
    T = lambda v: Matrix.Translation(v)
    ch = {}
    end = f0 + tip + settle
    frames = [1, f0 - 1] + list(range(f0, end + 1))
    for f in frames:
        if f < f0:
            th, P = 0.0, P0.copy()
        elif f <= f0 + tip:
            k = (f - f0) / tip
            th = (math.pi / 2) * k * k
            P = P0 + Vector((d.x * 0.08 * k, d.y * 0.08 * k, -drop * k * k))
        else:
            u = (f - f0 - tip) / settle
            th = math.pi / 2 - 0.16 * math.sin(math.pi * u) * (1 - u)
            P = P0 + Vector((d.x * 0.08, d.y * 0.08, -drop + 0.04 * math.sin(math.pi * u) * (1 - u)))
        Mw = T(P) @ Quaternion(axis, th).to_matrix().to_4x4() @ T(-P0) @ M0
        loc, rot, sca = Mw.decompose()
        for i in range(3):
            ch.setdefault(("location", i), []).append((f, loc[i]))
        for i in range(4):
            ch.setdefault(("rotation_quaternion", i), []).append((f, rot[i]))
    write_fcurves(o, tag or (E.tag + "_Topple_" + o.name), ch, "BEZIER")
    return end


# EXIT bars (Unity ExitGate/Bars = P_Fence): same proportions as in re_01.
BARS_SRC = "ExitBars"
BARS_WIDTH = 1.31          # Unity 3.1 m wide / 2.36 (Unity gate is 2.36x the Blender gate)
BARS_Z = 0.84              # centre height (Unity 1.84 m above the gate base)


def place_bars(o, gate):
    import math
    from mathutils import Matrix
    gw = gate.matrix_world
    yaw = gw.to_euler().z
    o.parent = None
    o.rotation_mode = 'XYZ'
    o.rotation_euler = (0, 0, 0)
    o.scale = (1, 1, 1)
    o.location = (0, 0, 0)
    bpy.context.view_layer.update()
    k = BARS_WIDTH / o.dimensions.x
    c = sum((Vector(v) for v in o.bound_box), Vector()) / 8
    o.scale = (k, k, k)
    o.rotation_euler = (0, 0, yaw)
    R = Matrix.Rotation(yaw, 4, 'Z')
    off = R @ Vector((c.x * k, c.y * k, c.z * k))
    o.location = (gw.translation.x - off.x, gw.translation.y - off.y, BARS_Z - off.z)


def exit_bars(E, open_at=None, dur=36):
    """Bars in this episode's EXIT gate; optional opening (sinks like ExitGate.cs, then hides)."""
    src = bpy.data.objects[BARS_SRC]
    name = "%s_ExitBars" % E.tag
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, src.data)
        E.sc.collection.objects.link(o)
    place_bars(o, E.M["Exit Wooden_Low.001"])
    o.animation_data_clear()
    if open_at:
        z0 = o.location.z
        drop = 3.2 / 2.36
        ch = {("location", 2): [(1, z0), (open_at, z0), (open_at + dur, z0 - drop)]}
        act = write_fcurves(o, name + "_Open", ch, "BEZIER")
        for dp in ("hide_viewport", "hide_render"):     # hidden once fully sunk
            fc = act.fcurve_ensure_for_datablock(o, dp)
            for f, v in ((1, 0.0), (open_at + dur + 1, 1.0)):
                k = fc.keyframe_points.insert(f, v)
                k.interpolation = 'CONSTANT'
    return o
