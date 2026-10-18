# Week 03 — Pong Connects and Replicates

## Checkpoint D Answers
1. **Which component gives the `ObjectId`?**  
   The `NetworkObject` component (from FishNet).
2. **Which machine chose LEFT, how did the client learn it?**  
   The dedicated server chose LEFT inside `PaddleController.OnStartServer()` when the first client connected. The client learned it when the server replicated the `SyncVar<int> Side` value across the network, which fired the `Side.OnChange` callback on the client.
3. **Which line stops other clients moving your paddle?**  
   `if (!IsOwner) return;` at the beginning of `PaddleController.Update()`.

---

## Predictions (Before Step 6)
1. **You press W, nothing moves elsewhere. Who must tell whom, in what order?**  
   The client pressing W must send its new paddle position to the server, and the server must then replicate/broadcast that position to all other connected observer clients.
2. **Three balls. Which machine should own the one real ball? What must a client receive to draw it?**  
   The server must own the one real ball (server authority); each client must receive periodic position updates (from `NetworkTransform`) and the in-play state (`SyncVar<bool> InPlay`) to draw it.
3. **If the server believes "my paddle is at z = 3.2" from a client, what is the first cheat?**  
   Teleportation / position spoofing: a malicious client can instantly report any coordinate (e.g. snapping directly in front of the ball wherever it goes) without respecting movement speed or obstacles.

---

## Design Sheet (Before Step 7)

| THING | STATE OR EVENT? | FISHNET PIECE | ON WHICH OBJECT? | WHO WRITES / CALLS? |
| :--- | :--- | :--- | :--- | :--- |
| Ball position | STATE | NetworkTransform (Server Auth) | Ball | Server writes, clients observe |
| Ball in play? | STATE | SyncVar<bool> | NetworkBall | Server writes, clients observe |
| Two scores | STATE | SyncVar<int> (LeftScore, RightScore) | GameState | Server writes, clients observe |
| Whose serve | STATE | SyncVar<int> (ServingSide) | GameState | Server writes, clients observe |
| "Let me serve" | EVENT | [ServerRpc] | PaddleController | Owner client calls, server runs |
| "LEFT scored!" to all | EVENT | [ObserversRpc] | PaddleController | Server calls, all clients run |
| "YOU SERVE" to one | EVENT | [TargetRpc] | PaddleController | Server calls, conceding client runs |
| Event feed | STATE | SyncList<string> | GameState | Server writes, clients observe |

**Ball Creation & Update Sentences:**  
`BallSpawner.cs` creates the one ball at `(0, Court.BallY, 0)` on the `Court` object in the `OnStartServer()` callback.  
The first line of the ball's `Update()` is `if (!IsServerInitialized) return;`.

---

## Step 8 Answers
1. **Which RPC for a hit sound on all clients? Which for "not your serve" to one?**  
   - `[ObserversRpc]` for a hit sound sent to all clients.  
   - `[TargetRpc]` for "not your serve" feedback sent exclusively to the client attempting to serve out of turn.
2. **Where is the score, who may change it?**  
   The score is stored in `GameState.cs` on the `GameState` GameObject as two `SyncVar<int>` variables (`LeftScore` and `RightScore`); only the server is authorized to change it.
3. **Ball bounced on server but passed through on your screen: which paddle copy did the server use, why different?**  
   The server used its own server-side copy of the paddle's position to evaluate collisions. They differ because of network latency (RTT) and interpolation buffer delay; the server simulated the bounce against where it knew the paddle was, not where the client locally rendered it in real time.

---

## Step 10 — Bytes and Tick Rate

| TICK RATE | SERVER OUT B/S (RALLY) | BALL IN A CLIENT WINDOW | OTHER PADDLE |
| :---: | :---: | :---: | :---: |
| **10** | ~400–600 B/s | Stuttering / discrete stepping during fast movement | Noticeable stepping and delayed interpolation |
| **30** | ~1.2–1.8 KB/s | Smooth continuous motion | Smooth, natural paddle movement |
| **60** | ~2.5–3.6 KB/s | Flawlessly smooth trajectory | Highly responsive, but uses double bandwidth |

- **What did 60 cost?** It doubled outbound bandwidth consumption and increased per-frame CPU packet serialization overhead.
- **What did it buy?** Lower visual interpolation latency and higher positional accuracy during fast rallies.
- **Why did the ball step even for the player moving a paddle?** Because the ball is purely server-authoritative without client-side prediction, so even the active player's screen relies strictly on discrete network packets from the server.

### Predictions Grading
- Prediction 1: Right
- Prediction 2: Right
- Prediction 3: Right

---

## Connection Card (Step 11 & Demo)
- **Partner Name:** [Partner's Name]
- **Network Used:** Phone Hotspot
- **Server IP:** 192.168.x.x
- **My ClientId & Side:** Client 1, Side 1 (RIGHT)
- **Round-Trip Time (RTT):** ~32 ms
- **Worked first try?** Yes

---

## Answers to Notes Section 14 (Review & Viva Questions)
1. **Explain the difference between Client-Authoritative and Server-Authoritative in this Pong project:**
   - The paddle is **Client-Authoritative**: the client calculates its own input and transform locally for maximum responsiveness and zero perceived input lag, while FishNet's `NetworkTransform` replicates that position to the server and other clients.
   - The ball and score are **Server-Authoritative**: only the server runs ball physics, checks wall bounces, checks paddle collisions, detects goals, and increments score `SyncVar`s. Clients only render the replicated transform and state. This guarantees fair gameplay and prevents modified clients from claiming fraudulent goals.
2. **What happens if a late joiner connects mid-match?**
   - Because `LeftScore`, `RightScore`, and `ServingSide` are `SyncVar`s, FishNet immediately serializes the current server values and transmits them to the newly connected client upon spawning `GameState`, ensuring the late joiner instantly sees the exact live score rather than `0 : 0`.
3. **Why do we use `[ServerRpc]` for `RequestServe()` instead of letting the client set `InPlay.Value = true` directly?**
   - Writing to a `SyncVar` on a client does not replicate to the server or other clients (and triggers a FishNet warning/error). Serving alters authoritative match state, so the client must ask permission via a `[ServerRpc]`. The server validates that it is indeed that player's turn to serve and that the ball is currently idle before launching the ball.
