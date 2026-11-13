# Week 5 · In-Class Assignment: Pong on a Host

**Course:** DGD0005 Multiplayer Programming · Block 2 · Listen Server  
**Topic:** Pong on a host (Steps 0–8, fix list, self-review)  
**Student:** Yiğit Dilekçi  

---

## Step 3 — Both Callbacks, One Paddle

### Observation & Notes
When clicking **Start Host**, the Console logs paddle instantiation and registration for client 0:
```text
[Server] Spawned paddle #3 for client 0: LEFT.
[Server] PaddleController.All.Count = 1.
[Client] I am client 0, playing LEFT. My paddle is #3. Only I read the keyboard for it.
[Client] PaddleController.All.Count = 1.
```

- **Which callback ran first:** `OnStartServer()` ran first.
- **Which half of the host ran it:** The **server half** of the host process ran it.
- **Why:** The host acts as both server and client in a single process. When the server spawns the paddle NetworkObject, it immediately runs the server-side lifecycle callback `OnStartServer()`. Only after server initialization does FishNet register local client observation and invoke `OnStartClient()` on the client half.
- **Guarding `All.Add`:** Both `OnStartServer` and `OnStartClient` are guarded with `if (!All.Contains(this)) All.Add(this);`. Without this guard, the host adds its own paddle to the static `All` list twice, causing `All.Count` to report 2 instead of 1. With the guard in place, `All.Count` correctly reports 1 on the host.

---

## Step 5 — The Host Wins (Host Advantage)

### Observation & Notes
- **Guest RTT:** ~2 ms (local loopback / LAN connection).
- **Host Advantage Analysis:**
  - On the host, the ball bounce calculation is executed locally on the server simulation. The host's paddle position is already authoritative on the server with 0 delay (RTT = 0 ms). The ball never clips through the host paddle.
  - On the guest, the ball sometimes appears to penetrate or pass slightly through the paddle before snapping back into a bounce. This happens because the guest renders the ball locally, but collision resolution is strictly server-authoritative. The guest paddle's position on the server is delayed by half an RTT (one tick old).
  - When serving (pressing Space), the host's serve begins immediately on the current frame because the `[ServerRpc]` is executed locally with zero transmission delay. The guest's serve requires a network round-trip ($1/2$ RTT to reach the server, plus tick simulation, plus $1/2$ RTT to replicate back), resulting in an observable delay of 1 RTT before the ball launches.
- **Lower Tick Rate Experiment (`TimeManager.TickRate` = 10):**
  - When `TimeManager.TickRate` is set to 10 ticks/s (each tick takes 100 ms instead of ~33.3 ms at 30 ticks/s), the simulation updates much more coarsely.
  - On the guest screen, the ball visibly steps/jitters and passes noticeably deeper into the paddle before the server registers the collision and snaps the ball back. The serve delay on the guest is also significantly increased (~100 ms tick delay + network latency).
  - Restored `TimeManager.TickRate` back to 30.

---

## Step 7 — Connection Card

| Field | Value |
| :--- | :--- |
| **Partner's Name** | Test Partner / Multiplayer Play Mode Guest |
| **Host's Address** | 127.0.0.1 (UDP 7770) |
| **My ClientId** | 1 (Host is Client 0, joining guest is Client 1) |
| **My RTT** | 2 ms |
| **Connected First Try or Fix Line** | Connected first try; host verified `Client 1 connected` and spawned RIGHT paddle. |

---

## Step 8 — The Same Scripts, Three Ways

1. **(1) Start Server only:**
   - **Scripts that run:** `ServerBootstrap`, `ConnectionLogger` (server events `OnServerState`, `OnRemoteState`), `BallSpawner` (server ball spawning), `GameState` (server score and match rules), `NetworkBall` (authoritative physics & movement in `FixedUpdate`), `PaddleController.OnStartServer` (registers server paddle tracking).
   - **Scripts that do NOT run:** `OnStartClient`, `PaddleController.Update` (no owner inputs read), client-side HUD display/rendering logic (`!asServer`).
2. **(2) Start Host:**
   - **Scripts that run twice:** Both halves of `NetworkBehaviour` run in the same process: `OnStartServer` and `OnStartClient` execute for each spawned networked object (`PaddleController`, `NetworkBall`, `GameState`). `ConnectionLogger` receives both server-side connection events and client-side connection events.
3. **(3) Start Client:**
   - **What the guest knows about the host:** The guest knows the host only as a remote IP and UDP port running the game server, and as Client 0 controlling the opposing LEFT paddle. The guest has no internal knowledge that the server is also running a player instance; it simply receives replicated SyncVars, transform updates, and RPCs.

---

## Section 5 — Self-Review Answers

### 1. Start Host: which two calls? Why is the host's RTT 0?
- **Two calls:**
  1. `_nm.ServerManager.StartConnection();`
  2. `_nm.ClientManager.StartConnection("127.0.0.1", port);`
- **Why RTT is 0:**
  The server and client share the same process memory space. Network packets do not traverse physical network hardware, routers, or external interfaces; Tugboat / FishNet passes local loopback traffic through in-memory buffers immediately, resulting in 0 ms round-trip time.

---

### 2. The guest's paddle exists on the host, the guest, and a spectator. For each: `IsOwner`, `IsServerInitialized`, which callbacks ran, who reads the keyboard.

| Machine | `IsOwner` | `IsServerInitialized` | Callbacks That Ran | Reads Keyboard? |
| :--- | :--- | :--- | :--- | :--- |
| **Host** | `false` | `true` | `OnStartServer`, `OnStartClient` | **No** (`if (!IsOwner) return;`) |
| **Guest (Owner)** | `true` | `false` | `OnStartClient` | **Yes** (`IsOwner == true`) |
| **Spectator** | `false` | `false` | `OnStartClient` | **No** (`if (!IsOwner) return;`) |

---

### 3. The host presses Space. The guest presses Space. Trace both serves: key → ? → ? → ball moves. Which one waits?
- **Host serve trace:**
  Space pressed → `PaddleController.Update` (`IsOwner == true`) → `ServerRequestServe()` `[ServerRpc]` → caller is already on the server, so FishNet invokes the method body in-memory on the same frame → `NetworkBall.Instance.ServerServe()` executes → ball velocity applied immediately.
- **Guest serve trace:**
  Space pressed → `PaddleController.Update` (`IsOwner == true`) → `ServerRequestServe()` `[ServerRpc]` serialized into network packet → sent over network to server ($1/2$ RTT) → Server validates serve turn and calls `NetworkBall.Instance.ServerServe()` → ball state updated and replicated back over network to guest ($1/2$ RTT) → ball moves on guest screen.
- **Which one waits:**
  The **guest waits** (exactly 1 full RTT plus any server tick delay). The host experiences 0 ms wait time.

---

### 4. Step 6: Why did the guest's own paddle vanish when the host stopped? Why did the host's game survive when the guest stopped?
- **Why guest's paddle vanished:**
  All networked entities are owned and managed by the server authority. When the host stops, the server disconnects all clients. The guest cleans up and destroys all server-spawned `NetworkObject` instances (including paddles and ball) because clients cannot maintain networked objects without an active server connection.
- **Why host's game survived when guest stopped:**
  The host IS the server. When the guest leaves (`Stop Client`), the server simply detects a client disconnection, despawns the guest's paddle (#1) via `OnStopServer()`, and keeps the game session alive. Only the server has authoritative control over the lifecycle of the session.

---

### 5. State or event:
- **The score:** **State** (persists over time, synchronized via `SyncVar<int>`)
- **"RIGHT scored!":** **Event** (transient notification dispatched via `ObserversRpc`)
- **Whose serve:** **State** (persists until served, synchronized via `SyncVar<int> ServingSide`)
- **"YOU SERVE":** **Event** (targeted one-time prompt sent to a specific client via `TargetRpc`)
- **The ball's position:** **State** (continuous variable replicated continuously via `NetworkTransform`)
- **The feed:** **State** (persistent synchronized list of events via `SyncList<string>`)

---

### 6. A guest writes `GameState.Instance.LeftScore.Value = 9` in its own code. What happens? Why does the same line work on the host?
- **What happens on the guest:**
  `LeftScore` is a server-authoritative `SyncVar<int>`. In FishNet, clients are prohibited from modifying server-authoritative SyncVars. If a client writes to `.Value`, FishNet logs an error/warning and discards the write, or the value is immediately overwritten back to the server's authoritative value on the next sync tick.
- **Why it works on the host:**
  The host process is the server (`IsServerInitialized == true`). Because the host possesses server authority, setting `.Value` directly modifies the authoritative state and triggers automatic replication to all connected clients.

---

### 7. Your feed logs twice on the host. Which line stops a goal sound from playing twice? Why does the score log not double?
- **Which line stops a goal sound from playing twice:**
  Guarding audio presentation behind `if (!asServer)` within the SyncVar `OnChange` callback, or placing audio playback exclusively inside the client RPC event handler (`ObserversRpc`).
- **Why the score log does not double:**
  The `OnChange` callback on `SyncVar` passes `(int prev, int next, bool asServer)`. By checking `if (!asServer)` (or handling announcements solely on the server side in `ServerServe` / `GameState.Announce`), score notifications execute for client presentation only once.

---

## Section 7 — At Home: Homework 2 on a Host

> **Instruction:** Homework 2 (the server decides) is unchanged. Do it with `Start Host` + one guest instead of a server instance. Test every step from the guest window. When `Client Authoritative` goes off, watch which paddle gets late. Not the host's. Write that in the Homework 2 README.

### Findings & Detailed Comparison:
- **Which paddle gets late:** The **guest's paddle** gets late. The **host's paddle does NOT get late.**
- **Detailed Mechanics:**
  - In Homework 1, `NetworkTransform` was client-authoritative (`Client Authoritative = on`). The guest client moved its own paddle immediately on local keypress and replicated position to the server. Both host and guest paddles responded instantly to their respective local keyboards.
  - In Homework 2, `Client Authoritative` is disabled (`Client Authoritative = off`). Movement becomes completely server-authoritative. Clients cannot alter their own transform; they can only send directional input intent via `[ServerRpc] ServerSetAxis(axis)`.
  - **Host Advantage on a Listen Server:**
    - The **Host** is the server. Its inputs never leave the machine; they are applied in local memory instantly ($RTT = 0$ ms). The host paddle experiences zero input lag.
    - The **Guest** must send its input over the network ($1/2$ RTT), wait for the server tick to calculate the new position, and wait for `NetworkTransform` to replicate the new position back ($1/2$ RTT). The guest experiences 1 full RTT of input delay.
  - **Result:** The guest's paddle feels delayed, heavy, and unresponsive compared to the host's paddle. This demonstrates the fundamental problem of listen-server architectures with server authority without client-side prediction.

