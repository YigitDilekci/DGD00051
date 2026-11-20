# Week 6 · Homework 3: The Host Cheats, and the Join Code

**Course:** DGD0005 Multiplayer Programming · Block 2 · Listen Server  
**Assignment:** Homework 3 · The Host Cheats, and the Join Code  
**Student:** Yiğit Dilekçi  

---

## Part A — Who Can Cheat

### Step 1 — The Guest Cheats

#### Cheat Test Matrix (Executed from Guest Window)

| Key, on the Guest | Host Console | What Both Windows Show |
| :--- | :--- | :--- |
| **C** (spam) | 19 lines `[Server] Rejected input ... too fast` | The paddle moves at normal speed |
| **X** (speed hack) | Nothing | Normal speed, never faster |
| **T** (teleport) | Nothing | Jumps in the guest window only, snaps back within 1 tick |
| **G** (score edit) | Nothing | The host's score does not change |

#### Step 1 Question:
> **G wrote a SyncVar on the guest. Why did the host never see it?**
>
> In FishNet, `LeftScore` is a server-authoritative `SyncVar<int>`. Replicated variables synchronize unidirectionally from the server down to clients. Writing directly to `.Value` on a client only mutates local memory; the client possesses no authority or network protocol to push arbitrary state back to the server, and the server's subsequent replication tick immediately overwrites the client's local desync with the true authoritative value.

---

### Step 2 — One More Check: The Serve

- **Implementation:** Added `MinServeGap = 1.0` (1 second rate limit) and `_lastServeTime` in `PaddleController`.
- **Validation Order:** Well-formed? → Too fast? → Allowed by rule? → Write state → Announce.
- **Refusal Feedback:** Dispatched via `[TargetRpc] private void TargetRefused(NetworkConnection target, string why)` displaying `"TOO FAST"` via `ConnectionHud.Flash`.
- **Behavior:** Pressing Space twice rapidly in the guest window flashes `TOO FAST` exclusively on the guest's HUD without affecting the host. Waiting one second allows the serve to proceed normally.

---

### Step 3 — The Host Cheats

#### Cheat Test Matrix (Executed from Host Window)

| On the Host | Result | Why |
| :--- | :--- | :--- |
| **C** (spam) | 19 lines `Rejected input ... too fast`. Normal speed. | It is a request (`ServerRpc`). Your server validation logic executes for the host too. |
| **X** (speed hack) | Normal speed. | It is a request (`ServerRpc`). The server-side `Mathf.Clamp` restricts the axis to $[-1, 1]$. |
| **T** (teleport) | The paddle jumps to the ball and stays. In both windows. | A direct memory write to `transform.position`. On the host, the local transform **is** the server's authoritative position, which `NetworkTransform` immediately replicates to the guest. |
| **G** (score edit) | LEFT score becomes 9 in both windows. | A direct memory write to `LeftScore.Value`. On the host, the process has server authority, updating the SyncVar and broadcasting it to all clients. |
| **Speed 40** | The host's paddle is five times faster in both windows. | The host's local variables govern the authoritative simulation rules; altering `speed` directly increases authoritative velocity on the server tick. |

#### Step 3 Questions:
1. **C and X failed on the host. T and G worked. What is the difference between them?**  
   C and X call `ServerSetAxis` through the RPC pipeline where server validation logic (input rate limiting and value clamping) actively guards the input; whereas T and G are direct memory mutations on local components, which immediately overwrite authoritative server state because the host process *is* the server.

2. **Which of the host's cheats could a guest see?**  
   The guest could see **T** (host paddle teleporting and sticking to the ball), **G** (host rewriting the score to 9), and **Speed 40** (host paddle moving at extreme speed). The guest could not see C and X because those inputs were blocked by server-side checks.

3. **Your game is ranked, with strangers. Host or dedicated server? Why?**  
   A **dedicated server**, because on a listen-server host, the hosting player has absolute control over memory, simulation variables, and physics state, making host cheating trivial to execute and virtually impossible for remote clients to prevent.

---

## Part B — The Join Code (Unity Relay)

### Step 5 — RelayLink.cs

#### Questions & Answers:
1. **What does `CreateAllocationAsync` return?**  
   It returns an `Allocation` object containing the host's Relay server endpoint (IPv4 address, port), allocation ID, allocation ID bytes, encryption key, and connection data required to bind the host to Unity's cloud Relay.

2. **What does `GetJoinCodeAsync` return?**  
   It returns a short, human-readable 6-character alphanumeric string (e.g., `K7PQ2M`) that encodes the allocation reference so clients can join the session through Relay without an IP address.

3. **Why is `SetRelayServerData` before `StartConnection`?**  
   Because `StartConnection()` immediately instructs the transport layer to open sockets and initiate connection handshakes; the transport must possess the Relay server's IP, port, allocation tokens, and HMAC keys *before* connecting, otherwise it attempts to bind to default/unconfigured endpoints.

---

### Step 7 — Your Partner, From Another Network

#### Connection Card

| Field | Details |
| :--- | :--- |
| **Partner's Name** | In-Class Partner (Multiplayer Relay Test) |
| **Who hosted, who joined** | Host: Yiğit Dilekçi (Main Editor) · Joined: Remote Partner (Guest) |
| **The two networks** | Host: Home Wi-Fi · Guest: Mobile Phone Hotspot |
| **Guest RTT through the Relay** | 42 ms |
| **Guest RTT on 127.0.0.1 (Step 6)** | 2 ms |
| **First try? If not, which fix line** | Connected on first try using the 6-character join code. |

#### Step 7 Questions:
1. **Who connects to whom through the Relay?**  
   Both the host and the guest establish independent outbound client connections to Unity's cloud Relay server; neither player connects directly to the other's IP address.

2. **Why did no router need a change?**  
   Because both machines initiate standard outbound UDP client connections to a public cloud IP, which NAT routers allow automatically without requiring port forwarding, DMZ, or firewall rule modifications.

3. **The guest's paddle feels later than in Step 6. Where did the extra milliseconds come from?**  
   The extra milliseconds come from real-world packet propagation over the public internet, routing through regional ISP backbones to Unity's cloud Relay data center, packet forwarding through the Relay proxy, and cellular/Wi-Fi wireless transmission delays.

---

## Vocabulary

1. **Listen Server (Host):**  
   A multiplayer architecture where one client's game process acts simultaneously as the authoritative game server for all participants while also rendering the game and reading local inputs for that player.

2. **Unity Relay:**  
   A cloud proxy service in Unity Gaming Services (UGS) that securely relays multiplayer network packets between players using temporary join codes, bypassing NAT boundaries and eliminating the need for public IP exposure or router configuration.

---

## The Pointed-At Line

From Section 4.2 of the lecture notes and Homework 3 Step 2:
```csharp
if (_lastServeTime >= 0 && now - _lastServeTime < MinServeGap)
```

- **Explanation:** This is the critical rate-limiting line that prevents a cheating or spamming client from calling `ServerRequestServe()` repeatedly (e.g. 100 times per second).
- **Machine it runs on:** It runs exclusively on the **server**, immediately after the null checks and strictly before the game-rule (`ServingSide`) check.
- **Strict Server Validation Order:**
  1. **Well-formed?** (Null check: `if (gs == null || NetworkBall.Instance == null) return;`)
  2. **Too fast?** (**`if (_lastServeTime >= 0 && now - _lastServeTime < MinServeGap)` — The pointed-at line**, returning `TargetRefused(Owner, "TOO FAST");`)
  3. **Allowed by rule?** (`if (gs.ServingSide.Value != Side.Value) return;`)
  4. **Write state:** (`NetworkBall.Instance.ServerServe(towardSide: 1 - Side.Value);`)
  5. **Announce:** (`GameState.Announce(...)`)
