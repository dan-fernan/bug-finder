using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BugDefense
{
    /// <summary>
    /// Singleton that builds the scene, owns game state, and handles game over / restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Wave;
        public bool IsPlaying => State == GameState.Build || State == GameState.Wave;

        [Header("Economy ([Req B] limited number of towers)")]
        public int startingCapacity = 8;
        public int maxTowers = 14;            // hard cap regardless of points
        [Range(0f, 1f)] public float midWaveRefund = 0.5f;

        /// <summary>Total sprint capacity earned so far (starting points + wave rewards).</summary>
        public int CapacityTotal { get; private set; }
        /// <summary>Points currently tied up in placed towers.</summary>
        public int CapacitySpent { get; private set; }
        public int CapacityAvailable => CapacityTotal - CapacitySpent;
        public int TowerCount { get; private set; }
        public int BugsSquashed { get; private set; }

        public GridMap Map { get; private set; }
        public ProdServer Prod { get; private set; }
        public WaveSpawner Spawner { get; private set; }
        public TowerPlacer Placer { get; private set; }
        public HUD Hud { get; private set; }

        public static readonly Color BackgroundColor = new Color32(0x0B, 0x0F, 0x1A, 0xFF);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Reset static state on every boot / restart.
            Time.timeScale = 1f;
            EnemyRegistry.Clear();
            CapacityTotal = startingCapacity;

            BuildScene();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildScene()
        {
            SetupCamera();
            SetupLight();

            Map = new GameObject("GridMap").AddComponent<GridMap>();
            Map.transform.SetParent(transform, false);
            Map.Build();

            SpawnPortal.Create(transform);
            Prod = ProdServer.Create(transform);

            Spawner = new GameObject("WaveSpawner").AddComponent<WaveSpawner>();
            Spawner.transform.SetParent(transform, false);

            Placer = new GameObject("TowerPlacer").AddComponent<TowerPlacer>();
            Placer.transform.SetParent(transform, false);

            EnsureEventSystem();
            Hud = new GameObject("HUD").AddComponent<HUD>();
            Hud.transform.SetParent(transform, false);
            Hud.Build();
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" }; // MainCamera is a built-in tag
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Frame();
        }

        void SetupLight()
        {
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.SetParent(transform, false);
            }
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 1f;
            sun.color = Color.white;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.32f);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void Update()
        {
            // Uses unscaled input polling, so R works even while Time.timeScale == 0.
            if (GameInput.KeyDown(KeyCodeLite.R)) Restart();
        }

        public void RegisterBugSquashed() => BugsSquashed++;

        public bool CanAfford(TowerType t) => t != null && CapacityAvailable >= t.cost;
        public bool AtTowerCap => TowerCount >= maxTowers;

        public void AddCapacity(int points) => CapacityTotal += points;

        /// <summary>Deducts the tower's cost. Call only after validating placement.</summary>
        public void OnTowerPlaced(TowerType t)
        {
            CapacitySpent += t.cost;
            TowerCount++;
        }

        /// <summary>Refunds a removed tower: full cost during build, partial mid-wave. Returns points refunded.</summary>
        public int OnTowerRemoved(TowerType t)
        {
            int refund = State == GameState.Wave ? Mathf.CeilToInt(t.cost * midWaveRefund) : t.cost;
            CapacitySpent -= t.cost;
            CapacityTotal -= t.cost - refund; // the non-refunded part is lost
            TowerCount = Mathf.Max(0, TowerCount - 1);
            return refund;
        }

        /// <summary>[Req C] Game ends when an enemy completes its path.</summary>
        public void GameOver(Enemy culprit)
        {
            if (!IsPlaying) return;
            State = GameState.GameOver;
            Time.timeScale = 0f;
            if (Prod != null) Prod.SetAlarm(true);
            if (Hud != null) Hud.ShowGameOver(culprit != null ? culprit.DisplayName : "A bug");
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            EnemyRegistry.Clear();
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }
    }
}
