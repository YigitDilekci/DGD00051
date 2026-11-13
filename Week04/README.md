# Week 4 · Homework 2: The server decides

## Step 3 — Check the input

> The server receives `axis = 50`. Which line makes the paddle move at normal speed, and on which machine does that line run?

- **Line:** `axis = Mathf.Clamp(axis, -1f, 1f);` inside `ServerSetAxis`.
- **Machine:** This line runs on the **server**. Even if the client sends an extreme value such as 50, the server clamps it to 1.0 before setting `_serverAxis`, ensuring the paddle movement speed calculated on the server remains normal.

---

## Step 4 — The rate limit

> Why 30 ms? What happens with 5 ms? What happens with 500 ms?

- **Why 30 ms:**
  At a 30 Hz tick rate, ticks happen every ~33.3 ms. A human using physical hardware inputs cannot legitimately produce distinct direction inputs faster than one tick. A 30 ms threshold sits just below the 33.3 ms tick duration, allowing every legitimate tick input to pass while filtering out high-frequency programmatic spam from cheat tools.

- **What happens with 5 ms:**
  A 5 ms threshold is far too short. Malicious programs sending spam inputs (such as 20 inputs in a single frame or inputs every 10 ms) would easily slip through the rate limiter without being rejected.

- **What happens with 500 ms:**
  A 500 ms threshold allows only 2 inputs per second. The game would become virtually unplayable, unresponsive, and laggy for regular players trying to switch directions quickly.

---

## Step 5 — The cheating client

### Cheat Behavior Summary

| KEY | SERVER CONSOLE | PADDLE |
| :--- | :--- | :--- |
| **C** (spam) | 19 lines `[Server] Rejected input from client 0: too fast.` Counter +19 | moves at normal speed |
| **X** (speed hack) | nothing logged | moves at normal speed, never faster |
| **T** (teleport) | nothing logged | jumps locally, then snaps back within a tick |

### Step 5 Questions & Answers

1. **C sends 20 inputs and the server accepts one. Why is the paddle not 20 times faster?**
   The paddle is not 20 times faster because the server rate-limits inputs using `MinInputGap = 0.030`, rejecting the subsequent 19 RPC calls in the same frame without updating `_serverAxis`, so paddle movement calculation runs only once per tick at normal speed.

2. **T moved the paddle on your screen. Why did the ball on the server not notice?**
   The ball on the server did not notice because physics and collisions are computed on the server, which never accepted the client's local transform modification and promptly synchronized the authoritative server position back to the client via `NetworkTransform`.

3. **Which of the three cheats would also fail without the rate limit, and why?**
   Both **X (speed hack)** and **T (teleport)** would still fail without the rate limit: X fails because the server clamps the axis to `[-1, 1]` via `Mathf.Clamp`, and T fails because `Client Authoritative` is disabled on the paddle's `NetworkTransform`.

---

## Vocabulary

1. **Server-Authoritative:**
   A multiplayer architecture where the game server is the sole source of truth for the game state (object positions, physics, score, game rules). Clients only transmit user inputs/intentions to the server, and the server simulates the game world and replicates state down to all connected clients.

2. **Input Rate Limiting:**
   A network security technique where the server tracks the timestamp of incoming requests per client and discards any subsequent requests that arrive faster than a predefined minimum threshold (e.g., 30 ms), preventing input flooding and macro/bot abuse.

---

## Homework 2 on a Host (Week 5 Section 7)

> **Experiment:** Run Homework 2 using `Start Host` + one guest (Multiplayer Play Mode) instead of a dedicated server instance. Test every step from the guest window. When `Client Authoritative` goes off on `NetworkTransform`, watch which paddle gets late.

### Observations & Results:
- **Which paddle gets late:** The **guest's paddle (Right)** gets late. The **host's paddle (Left)** does NOT get late.
- **Why (The Host Advantage under Server Authority):**
  - When `Client Authoritative` is disabled on `NetworkTransform`, paddle movement is completely server-authoritative: the client only sends its directional input (axis) via `ServerSetAxis` `[ServerRpc]`, the server calculates the new transform position, and the server replicates that position back to clients.
  - **Host Paddle (Left):** Because the host process **is** the server, the host's input executes locally in-memory with **0 ms latency** (RTT = 0). The server updates the host paddle immediately on the current tick without traveling across any network interface.
  - **Guest Paddle (Right):** The guest must transmit its input over the network to the host ($1/2$ RTT), wait for the host server to simulate the movement, and wait for the updated position packet to travel back ($1/2$ RTT). As a result, the guest experiences a full round-trip delay (1 RTT + tick delay) between pressing an input key and observing paddle movement on screen.
  - **Conclusion:** Server-authoritative gameplay without client-side prediction gives the host an overwhelming unfair latency advantage over remote guests.

