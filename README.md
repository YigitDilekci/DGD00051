# Multiplayer Pong (FishNet Dedicated Server)

### Game Architecture: Ownership & Synchronization
In our multiplayer Pong architecture, each player has network ownership of their own paddle `NetworkObject`, granting them client authority to compute and replicate their movement input. Nobody owns the ball or the match coordinators (`GameState` and `Court` GameObjects); these are strictly server-authoritative entities owned and simulated exclusively by the dedicated server to prevent cheating and state desynchronization. Everybody shares the replicated match state, which includes synchronized ball coordinates (`NetworkTransform`), live match scores and serving turns (`SyncVar`), and the rolling announcement feed (`SyncList`).

### How to Run Server and Client
1. Open the project in Unity 6000.3.2f1, load `Scenes/Pong.unity`, and press Play.
2. In the first instance (or dedicated server build), click **Start Server** on the Connection HUD (port 7770).
3. In subsequent client instances or partner machines, enter the server IP (or `127.0.0.1` locally) and click **Start Client**.
