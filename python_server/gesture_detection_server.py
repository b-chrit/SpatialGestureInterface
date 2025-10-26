"""
SpatialGestureInterface - Gesture Detection Server
---------------------------------------------------
This module implements a vision-based gesture detection system using MediaPipe
and OpenCV. It detects common hand gestures (swipe, pinch, open palm, fist) and
sends them to a Unity client in real time over a WebSocket connection.

Author: Baraa Chrit
Date: 2025
"""

import cv2
import mediapipe as mp
import time
import math
from websocket_server import WebsocketServer
import threading

# ==========================
# Global Configuration
# ==========================
clients = set()
server = None
last_gesture_time = {}           # Stores timestamp for last sent gesture
GESTURE_COOLDOWN = 1.2           # Seconds between repeats of same gesture

# ==========================
# WebSocket Callbacks
# ==========================
def on_new_client(client, server_):
    """Triggered when Unity connects."""
    clients.add(client["id"])
    print(f"Unity connected (client #{client['id']})")

def on_client_left(client, server_):
    """Triggered when Unity disconnects."""
    clients.discard(client["id"])
    print(f"Unity disconnected (client #{client['id']})")

def send_gesture(gesture):
    """
    Sends a gesture string to all connected Unity clients,
    respecting the cooldown to prevent flooding.
    """
    now = time.time()
    last_time = last_gesture_time.get(gesture, 0)

    if now - last_time < GESTURE_COOLDOWN:
        return

    last_gesture_time[gesture] = now

    if clients:
        server.send_message_to_all(gesture)
        print(f"Gesture sent to Unity: {gesture}")

# ==========================
# MediaPipe Setup
# ==========================
mp_hands = mp.solutions.hands
mp_draw = mp.solutions.drawing_utils

def distance(a, b):
    """Computes Euclidean distance between two landmark points."""
    return math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2)

# ==========================
# Gesture Detection Logic
# ==========================
def detect_gestures():
    cap = cv2.VideoCapture(0)
    prev_x, prev_y = None, None
    last_swipe_time = 0

    with mp_hands.Hands(
        max_num_hands=1,
        min_detection_confidence=0.7,
        min_tracking_confidence=0.7
    ) as hands:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(rgb)

            if results.multi_hand_landmarks:
                for hand in results.multi_hand_landmarks:
                    mp_draw.draw_landmarks(frame, hand, mp_hands.HAND_CONNECTIONS)
                    h, w, _ = frame.shape
                    coords = [(int(l.x * w), int(l.y * h)) for l in hand.landmark]

                    # Finger states
                    tips = [4, 8, 12, 16, 20]
                    fingers = []

                    # Thumb
                    fingers.append(1 if hand.landmark[4].x < hand.landmark[3].x else 0)
                    # Other fingers
                    for i in range(1, 5):
                        fingers.append(1 if hand.landmark[tips[i]].y < hand.landmark[tips[i] - 2].y else 0)

                    total_fingers = fingers.count(1)

                    # Swipe detection
                    x, y = coords[8]
                    if prev_x is not None and prev_y is not None:
                        dx, dy = x - prev_x, y - prev_y
                        if abs(dx) > 80 or abs(dy) > 80:
                            now = time.time()
                            if now - last_swipe_time > 1:
                                if abs(dx) > abs(dy):
                                    gesture = "SWIPE_RIGHT" if dx > 0 else "SWIPE_LEFT"
                                else:
                                    gesture = "SWIPE_DOWN" if dy > 0 else "SWIPE_UP"
                                send_gesture(gesture)
                                last_swipe_time = now
                    prev_x, prev_y = x, y

                    # Pinch detection
                    thumb_tip = hand.landmark[4]
                    index_tip = hand.landmark[8]
                    pinch_dist = distance(thumb_tip, index_tip)
                    if pinch_dist < 0.05:
                        send_gesture("PINCH")
                        cv2.putText(frame, "PINCH", (30, 80),
                                    cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 255, 255), 2)

                    # Open palm (all fingers up) → toggle dark mode
                    elif total_fingers == 5:
                        send_gesture("OPEN_PALM")
                        cv2.putText(frame, "OPEN PALM (Dark Mode)", (30, 80),
                                    cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 255, 0), 2)

                    # Fist (all fingers down) → toggle light mode
                    elif total_fingers == 0:
                        send_gesture("FIST")
                        cv2.putText(frame, "FIST (Light Mode)", (30, 80),
                                    cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 0, 255), 2)

            cv2.imshow("Gesture Detection", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

    cap.release()
    cv2.destroyAllWindows()

# ==========================
# WebSocket Server
# ==========================
def run_ws():
    """Starts a local WebSocket server for Unity communication."""
    global server
    server = WebsocketServer(host="127.0.0.1", port=8765, loglevel=0)
    server.set_fn_new_client(on_new_client)
    server.set_fn_client_left(on_client_left)
    print("WebSocket server running at ws://127.0.0.1:8765")
    server.run_forever()

# ==========================
# Main Entry Point
# ==========================
if __name__ == "__main__":
    threading.Thread(target=run_ws, daemon=True).start()
    detect_gestures()
