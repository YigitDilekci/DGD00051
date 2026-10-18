using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Managing.Client;
using FishNet.Managing.Timing;
using FishNet.Managing.Observing;
using FishNet.Managing.Transporting;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using System.IO;

public static class PongSetupAutomator
{
    private static Material GetOrCreateMaterial(string name, Color color)
    {
        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
        }
        return mat;
    }

    [MenuItem("Pong/1. Generate Single-player Pong (Step 1)")]
    [MenuItem("Tools/Pong/1. Generate Single-player Pong (Step 1)")]
    [MenuItem("GameObject/Pong/1. Generate Single-player Pong (Step 1)", false, 10)]
    public static void GenerateSinglePlayerPong()
    {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 13f, 0);
        camGo.transform.rotation = Quaternion.Euler(90f, 0, 0);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);

        // 2. Directional Light
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0);

        // Materials
        Material courtMat = GetOrCreateMaterial("CourtMat", new Color(0.12f, 0.12f, 0.16f));
        Material wallMat = GetOrCreateMaterial("WallMat", Color.white);
        Material blueMat = GetOrCreateMaterial("BluePaddleMat", Color.blue);
        Material orangeMat = GetOrCreateMaterial("OrangePaddleMat", new Color(1f, 0.5f, 0f));
        Material ballMat = GetOrCreateMaterial("BallMat", Color.white);

        // 3. Court GameObject & Floor
        GameObject courtGo = new GameObject("Court");
        courtGo.transform.position = Vector3.zero;
        courtGo.AddComponent<Court>();

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = courtGo.transform;
        floor.transform.position = new Vector3(0, -0.05f, 0);
        floor.transform.localScale = new Vector3(Court.HalfWidth * 2f, 0.1f, Court.HalfHeight * 2f);
        floor.GetComponent<Renderer>().material = courtMat;

        // Top Wall
        GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topWall.name = "TopWall";
        topWall.transform.parent = courtGo.transform;
        topWall.transform.position = new Vector3(0, 0.25f, Court.HalfHeight + 0.25f);
        topWall.transform.localScale = new Vector3(Court.HalfWidth * 2f + 0.5f, 0.5f, 0.5f);
        topWall.GetComponent<Renderer>().material = wallMat;

        // Bottom Wall
        GameObject bottomWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomWall.name = "BottomWall";
        bottomWall.transform.parent = courtGo.transform;
        bottomWall.transform.position = new Vector3(0, 0.25f, -Court.HalfHeight - 0.25f);
        bottomWall.transform.localScale = new Vector3(Court.HalfWidth * 2f + 0.5f, 0.5f, 0.5f);
        bottomWall.GetComponent<Renderer>().material = wallMat;

        // Center line
        GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerLine.name = "CenterLine";
        centerLine.transform.parent = courtGo.transform;
        centerLine.transform.position = new Vector3(0, 0.01f, 0);
        centerLine.transform.localScale = new Vector3(0.08f, 0.02f, Court.HalfHeight * 2f);
        centerLine.GetComponent<Renderer>().material = wallMat;
        Object.DestroyImmediate(centerLine.GetComponent<Collider>());

        // 4. Left Paddle (W/S)
        GameObject leftPaddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPaddle.name = "Paddle";
        leftPaddle.transform.position = new Vector3(-Court.PaddleX, Court.BallY, 0);
        leftPaddle.transform.localScale = new Vector3(0.3f, 0.5f, Court.PaddleHalfLength * 2f);
        leftPaddle.GetComponent<Renderer>().material = blueMat;
        PaddleMovement pmLeft = leftPaddle.AddComponent<PaddleMovement>();
        pmLeft.IsLeftPaddle = true;

        // 5. Right Paddle (Arrows)
        GameObject rightPaddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPaddle.name = "Paddle (1)";
        rightPaddle.transform.position = new Vector3(Court.PaddleX, Court.BallY, 0);
        rightPaddle.transform.localScale = new Vector3(0.3f, 0.5f, Court.PaddleHalfLength * 2f);
        rightPaddle.GetComponent<Renderer>().material = orangeMat;
        PaddleMovement pmRight = rightPaddle.AddComponent<PaddleMovement>();
        pmRight.IsLeftPaddle = false;

        // 6. Local Ball
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0, Court.BallY, 0);
        ball.transform.localScale = Vector3.one * (Court.BallRadius * 2f);
        ball.GetComponent<Renderer>().material = ballMat;
        LocalBall lb = ball.AddComponent<LocalBall>();

        // Link court's localBall
        courtGo.GetComponent<Court>().localBall = lb;

        // Save scenes
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Pong-Start.unity");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Pong.unity");
        AssetDatabase.SaveAssets();

        Debug.Log("==========================================================");
        Debug.Log("[PongSetup] STEP 1 COMPLETE! Scenes/Pong-Start.unity and Scenes/Pong.unity created.");
        Debug.Log("Press Play: W/S moves Blue paddle, Arrows move Orange paddle, ball bounces and scores!");
        Debug.Log("==========================================================");
    }

    [MenuItem("Pong/2. Upgrade to Multiplayer (Steps 2 to 10)")]
    [MenuItem("Tools/Pong/2. Upgrade to Multiplayer (Steps 2 to 10)")]
    [MenuItem("GameObject/Pong/2. Upgrade to Multiplayer (Steps 2 to 10)", false, 11)]
    public static void UpgradeToMultiplayer()
    {
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

        // 1. Setup NetworkManager GameObject
        GameObject nmGo = GameObject.Find("NetworkManager");
        if (nmGo == null)
        {
            nmGo = new GameObject("NetworkManager");
            Undo.RegisterCreatedObjectUndo(nmGo, "Create NetworkManager");
        }

        NetworkManager nm = nmGo.GetComponent<NetworkManager>();
        if (nm == null) nm = nmGo.AddComponent<NetworkManager>();

        Tugboat tugboat = nmGo.GetComponent<Tugboat>();
        if (tugboat == null) tugboat = nmGo.AddComponent<Tugboat>();
        tugboat.SetPort(7770);
        tugboat.SetClientAddress("localhost");

        // Sub-managers as components
        ServerManager sm = nmGo.GetComponent<ServerManager>();
        if (sm == null) sm = nmGo.AddComponent<ServerManager>();
        sm.SetStartOnHeadless(false);

        TimeManager tm = nmGo.GetComponent<TimeManager>();
        if (tm == null) tm = nmGo.AddComponent<TimeManager>();
        tm.SetTickRate(30);

        if (nmGo.GetComponent<ClientManager>() == null) nmGo.AddComponent<ClientManager>();
        if (nmGo.GetComponent<ObserverManager>() == null) nmGo.AddComponent<ObserverManager>();
        if (nmGo.GetComponent<TransportManager>() == null) nmGo.AddComponent<TransportManager>();

        PlayerSpawner playerSpawner = nmGo.GetComponent<PlayerSpawner>();
        if (playerSpawner == null) playerSpawner = nmGo.AddComponent<PlayerSpawner>();

        ServerBootstrap bootstrap = nmGo.GetComponent<ServerBootstrap>();
        if (bootstrap == null) bootstrap = nmGo.AddComponent<ServerBootstrap>();

        ConnectionLogger logger = nmGo.GetComponent<ConnectionLogger>();
        if (logger == null) logger = nmGo.AddComponent<ConnectionLogger>();

        EditorUtility.SetDirty(nmGo);

        // 2. Setup HUD
        GameObject hudGo = GameObject.Find("Hud");
        if (hudGo == null)
        {
            hudGo = new GameObject("Hud");
            Undo.RegisterCreatedObjectUndo(hudGo, "Create Hud");
        }
        if (hudGo.GetComponent<ConnectionHud>() == null)
        {
            hudGo.AddComponent<ConnectionHud>();
        }

        // 3. Setup GameState
        GameObject gsGo = GameObject.Find("GameState");
        if (gsGo == null)
        {
            gsGo = new GameObject("GameState");
            Undo.RegisterCreatedObjectUndo(gsGo, "Create GameState");
        }
        if (gsGo.GetComponent<NetworkObject>() == null) gsGo.AddComponent<NetworkObject>();
        if (gsGo.GetComponent<GameState>() == null) gsGo.AddComponent<GameState>();

        // 4. Create Paddle Prefab
        GameObject paddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Paddle.prefab");
        if (paddlePrefab == null)
        {
            GameObject paddleTemp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleTemp.name = "Paddle";
            paddleTemp.transform.localScale = new Vector3(0.3f, 0.5f, Court.PaddleHalfLength * 2f);

            Collider col = paddleTemp.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            paddleTemp.AddComponent<NetworkObject>();
            paddleTemp.AddComponent<PaddleController>();

            NetworkTransform nt = paddleTemp.AddComponent<NetworkTransform>();
            SerializedObject ntSo = new SerializedObject(nt);
            SetSerializedBool(ntSo, "_clientAuthoritative", true);
            SetSerializedInt(ntSo, "_interpolation", 2);
            SetSerializedBool(ntSo, "_synchronizeRotation", false);
            SetSerializedBool(ntSo, "_synchronizeScale", false);
            ntSo.ApplyModifiedProperties();

            paddlePrefab = PrefabUtility.SaveAsPrefabAsset(paddleTemp, "Assets/Prefabs/Paddle.prefab");
            Object.DestroyImmediate(paddleTemp);
            Debug.Log("[PongSetup] Created Assets/Prefabs/Paddle.prefab");
        }

        if (paddlePrefab != null)
        {
            playerSpawner.SetPlayerPrefab(paddlePrefab.GetComponent<NetworkObject>());
            EditorUtility.SetDirty(playerSpawner);
        }

        // 5. Create Ball Prefab
        GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ball.prefab");
        if (ballPrefab == null)
        {
            GameObject ballTemp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballTemp.name = "Ball";
            ballTemp.transform.localScale = Vector3.one * (Court.BallRadius * 2f);

            Collider col = ballTemp.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            ballTemp.AddComponent<NetworkObject>();
            ballTemp.AddComponent<NetworkBall>();

            NetworkTransform nt = ballTemp.AddComponent<NetworkTransform>();
            SerializedObject ntSo = new SerializedObject(nt);
            SetSerializedBool(ntSo, "_clientAuthoritative", false); // Server Auth
            SetSerializedBool(ntSo, "_synchronizeRotation", false);
            SetSerializedBool(ntSo, "_synchronizeScale", false);
            ntSo.ApplyModifiedProperties();

            ballPrefab = PrefabUtility.SaveAsPrefabAsset(ballTemp, "Assets/Prefabs/Ball.prefab");
            Object.DestroyImmediate(ballTemp);
            Debug.Log("[PongSetup] Created Assets/Prefabs/Ball.prefab");
        }

        // 6. Setup Court
        GameObject courtGo = GameObject.Find("Court");
        if (courtGo == null)
        {
            courtGo = new GameObject("Court");
            Undo.RegisterCreatedObjectUndo(courtGo, "Create Court");
        }
        if (courtGo.GetComponent<Court>() == null) courtGo.AddComponent<Court>();
        if (courtGo.GetComponent<NetworkObject>() == null) courtGo.AddComponent<NetworkObject>();

        // Disable local ball on court
        LocalBall localBall = Object.FindAnyObjectByType<LocalBall>();
        if (localBall != null)
        {
            localBall.enabled = false;
            localBall.gameObject.SetActive(false);
        }

        BallSpawner ballSpawner = courtGo.GetComponent<BallSpawner>();
        if (ballSpawner == null) ballSpawner = courtGo.AddComponent<BallSpawner>();
        if (ballPrefab != null)
        {
            ballSpawner.ballPrefab = ballPrefab;
        }
        EditorUtility.SetDirty(ballSpawner);

        // 7. Delete Scene Paddles
        GameObject scenePaddle = GameObject.Find("Paddle");
        if (scenePaddle != null && scenePaddle.scene.IsValid())
        {
            Undo.DestroyObjectImmediate(scenePaddle);
        }
        GameObject rightPaddle = GameObject.Find("Paddle (1)");
        if (rightPaddle != null && rightPaddle.scene.IsValid())
        {
            Undo.DestroyObjectImmediate(rightPaddle);
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        // 8. Refresh Default Prefabs
        EditorApplication.ExecuteMenuItem("Fish-Networking/Refresh Default Prefabs");

        Debug.Log("==========================================================");
        Debug.Log("[PongSetup] SUCCESS! Multiplayer Pong configured (Steps 2 to 10).");
        Debug.Log("Press Play to test server & client replication!");
        Debug.Log("==========================================================");
    }

    private static void SetSerializedBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null) prop.boolValue = value;
    }

    private static void SetSerializedInt(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null) prop.intValue = value;
    }
}
