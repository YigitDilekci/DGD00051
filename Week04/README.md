# Week 4 · Homework 2: The server decides

## Step 3 — Check the input

> The server receives `axis = 50`. Which line makes the paddle move at normal speed, and on which machine does that line run?

- **Line:** `axis = Mathf.Clamp(axis, -1f, 1f);` in `ServerSetAxis`.
- **Machine:** Runs on the **server**. The server clamps the incoming value so `_serverAxis` can never exceed 1, making the paddle move at normal speed (`1 * speed * TickDelta`).

---

## Step 4 — The rate limit

> Why 30 ms? What happens with 5 ms? What happens with 500 ms?

- **Why 30 ms:**
  At a tick rate of 30 Hz, each tick occurs roughly every 33.3 ms. A human using a physical keyboard cannot press/release keys faster than one tick, so legitimate inputs arrive at ~33 ms intervals. Any input arriving faster than 30 ms is sent by a script/bot (an input flood/spam cheat) rather than human interaction. 30 ms is just under one tick interval, allowing valid 30 Hz inputs while filtering spam.

- **What happens with 5 ms:**
  A 5 ms limit is far too permissive. Cheat scripts sending dozens of rapid inputs (e.g. 20 calls per frame or every 10 ms) would bypass the rate limit completely and flood the server.

- **What happens with 500 ms:**
  A 500 ms limit is way too slow (half a second). Players would only be able to change direction twice per second, making paddle movement laggy, unresponsive, and unplayable.
