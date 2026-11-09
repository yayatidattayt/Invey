"""Explicit generated-image WPF fixture; never a live camera feed."""
import time
from finger_visual import FingerVisual
from test_finger_visual import frame, full


def visual_fixture(send,stop,preview):
    tracker=FingerVisual()
    send("ready")
    for sequence in range(140):
        if stop.is_set(): return
        x=150+max(0,min(12,sequence-29))*2
        pixels=frame(x,visible=not 65<=sequence<95)
        points,fields=full(x) if sequence<30 or 42<=sequence<65 or sequence>=105 else ([],{})
        if 95<=sequence<105: fields={"ambiguous":True}
        stamp=time.perf_counter_ns()//1_000_000
        points,fields=tracker.update(pixels,sequence,stamp,points,fields)
        send("observation",seq=sequence,captureMonoMs=stamp,resultMonoMs=time.perf_counter_ns()//1_000_000,
             tracked=bool(points),landmarks=points,frame={"width":320,"height":240},**fields)
        if preview is not None: preview.offer(sequence,pixels)
        stop.wait(.04)
    send("stopped")
