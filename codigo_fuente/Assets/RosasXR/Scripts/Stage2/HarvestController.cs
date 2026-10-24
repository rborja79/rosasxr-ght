using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RosasXR
{
    /// <summary>
    /// Etapa 2 "Cosecha con el carro": asociación tijeras → rosa, clasificador del carro, veredicto,
    /// LED de las tijeras, semáforo, corte, canasta, puntos, registro, resumen y tutorial obligatorio.
    /// Principio: el sistema forma el criterio, no decide por la persona.
    /// </summary>
    public class HarvestController : MonoBehaviour
    {
        [Header("Escena")]
        public HarvestCart cart;
        public PanTiltCamera panTilt;
        public SimulatedClassifier classifier;
        public Transform scissorsTip;
        public Renderer led;
        public Transform freeHand;          // MANO DE SOSTÉN (sigue la mano no dominante): sostiene tallos y el ramo
        public ScissorsTool scissors;       // MANO DE TIJERA
        public CameraFeedScreen cameraFeed;
        public Transform cutFlash;
        public Transform basket;
        public Renderer lightGreen, lightAmber, lightRed;
        public TMP_Text counterText;
        public TMP_Text screenText;
        public TMP_Text pointsText;
        public TMP_Text statusText;         // estado de la cámara del brazo y de la tijera
        [Header("Marcador al fondo del carro")]
        public TMP_Text boardGood, boardBad, boardHeight, boardPoints;
        public Image[] historyDots;
        public Renderer[] basketRenderers;
        public Button holdModeButton;
        [Header("Tutorial en panel propio")]
        public GameObject tutorialPanel;
        public TMP_Text tutorialText;
        public Button nextStepButton, skipTutorialButton, replayTutorialButton;
        public TMP_Text holdModeLabel;
        public LineRenderer associationLine;
        public GameObject summaryPanel;
        public TMP_Text summaryText;
        public Button againButton, menuButton, menuButtonCart, failureButton, debugButton, recenterButton, restartButton;
        public Button aisleLeftButton, aisleRightButton, finishButton;
        public LineRenderer budRing;
        [Tooltip("Banda verde sobre el tallo con la zona de corte correcta (modo novato).")]
        public LineRenderer cutBand;
        [Tooltip("Error de la cámara al medir la altura de corte (cm).")]
        public float lengthNoiseCm = 1.5f;
        public TMP_Text failureLabel, debugLabel;
        public Button handednessButton, cameraButton;
        public TMP_Text handednessLabel, cameraLabel;
        [Header("Voz guía")]
        public Button voiceButton;
        public TMP_Text voiceLabel;

        [Header("Reglas")]
        public float confidenceThreshold = 0.7f;
        public float associationRadius = 0.10f;
        public float ambiguousDelta = 0.03f;
        public float cutRadius = 0.045f;
        public float holdRadius = 0.07f;
        public float pickupRadius = 0.15f;
        public float maxBendDegrees = 12f;
        public float basketRadius = 0.4f;

        [Header("Puntos")]
        public int ptsCutGood = 10, ptsNoCutGood = 5, ptsCriterio = 15, ptsCutBad = -15, ptsCutVeryClosed = -20, ptsMissed = -3;

        static readonly Color Off = new Color(0.08f, 0.08f, 0.08f);
        static readonly Color Blue = new Color(0.15f, 0.45f, 1f);
        static readonly Color Green = new Color(0.1f, 0.95f, 0.3f);
        static readonly Color Red = new Color(1f, 0.12f, 0.1f);
        static readonly Color Amber = new Color(1f, 0.65f, 0.05f);
        static readonly Color Cyan = new Color(0.1f, 0.85f, 0.9f);

        // Estado visible para la prueba automática
        public HarvestRose Associated => _assoc;
        public string Verdict => _verdict;
        public bool OnStem => _onStem;
        public float CutCm => _cutCm;
        public Color LedColor { get; private set; }
        public string SemaphoreState { get; private set; } = "";
        public int Points => _points;

        IGradeClassifier _clf;
        int _lastAisle = -1;
        List<HarvestRose> _roses;
        HarvestRose _assoc;
        bool _assocAmbiguous;
        int _requestId;
        string _verdict = "";
        readonly Queue<float> _lastConf = new Queue<float>();
        readonly List<HarvestRose> _bouquet = new List<HarvestRose>();
        readonly List<HarvestRose> _cutRoses = new List<HarvestRose>();
        int _deposited, _points, _cuts, _good, _decisions, _agreeSys, _sysDecisions, _criterio, _heightOk, _heightChecks;
        // Dónde va a cortar: la tijera sobre el tallo de la rosa asociada
        bool _onStem;
        bool _autoHold;                    // modo simple: el tallo cortado pasa solo a la mano de sostén
        int _goodCuts, _badCuts, _heightBadCuts;
        readonly List<Color> _history = new List<Color>();
        Material[] _basketMats;
        bool _basketLit;
        float _cutCm = -1f, _lenNoise;
        float _roundStart;
        // Sostén
        bool _holdGrab;
        HarvestRose _held;
        Quaternion _heldRot;
        readonly List<HarvestRose> _fallen = new List<HarvestRose>();
        float _flashT = -1f;
        bool _roundOver, _complete, _debug;
        Material _ledMat, _gMat, _aMat, _rMat;
        HarvestOrder Order => HarvestOrder.Current;
        PlayerProfile P => ProfileStore.Current;

        // Tutorial
        int _tut = -1;          // -1 = sin tutorial
        float _tutTimer, _seenTime;
        bool _sawGreen, _sawRed, _sawAmber;
        HarvestRose _tutWrongRose;
        string _tutResult = "";
        string _semaphoreOverride = ""; // "amber" | "red" durante la demo

        void Awake()
        {
            _clf = classifier;
            _ledMat = led.material;
            _gMat = lightGreen.material; _aMat = lightAmber.material; _rMat = lightRed.material;
            againButton.onClick.AddListener(() => SceneNav.ReloadCurrent());
            menuButton.onClick.AddListener(() => SceneNav.Load(SceneNav.Menu));
            menuButtonCart.onClick.AddListener(() => SceneNav.Load(SceneNav.Menu));
            failureButton.onClick.AddListener(ToggleFailure);
            debugButton.onClick.AddListener(ToggleDebug);
            if (recenterButton != null) recenterButton.onClick.AddListener(() => cart.Recenter());
            if (restartButton != null) restartButton.onClick.AddListener(() => SceneNav.ReloadCurrent());
            if (aisleLeftButton != null) aisleLeftButton.onClick.AddListener(() => cart.ChangeAisle(-1));
            if (aisleRightButton != null) aisleRightButton.onClick.AddListener(() => cart.ChangeAisle(1));
            if (finishButton != null) finishButton.onClick.AddListener(() => { if (!_roundOver && _tut < 0) StartCoroutine(EndRound()); });
            if (handednessButton != null) handednessButton.onClick.AddListener(() =>
            {
                var b = XRInputBridge.Instance;
                if (b != null) b.SetLeftHanded(!b.LeftHanded);
                UpdateLabels(); RefreshScreen();
            });
            if (cameraButton != null) cameraButton.onClick.AddListener(() => { if (cameraFeed != null) cameraFeed.Toggle(); UpdateLabels(); });
            if (scissors != null) scissors.Cut += OnScissorsCut;
            cart.HandBlocked = right =>
            {
                var b = XRInputBridge.Instance;
                return b != null && right == b.HoldIsRight && HoldHandBusy;
            };
            if (nextStepButton != null) nextStepButton.onClick.AddListener(() =>
            {
                if (_tut < 0) return;
                if (_tut == 40) _tut = 4;          // esperando tras el paso 5
                _semaphoreOverride = "";
                AdvanceTutorial();
            });
            if (skipTutorialButton != null) skipTutorialButton.onClick.AddListener(() =>
            {
                if (_tut < 0) return;
                _semaphoreOverride = "";
                _tut = 5; AdvanceTutorial();      // termina el tutorial y empieza la ronda
            });
            if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(() =>
            {
                P.cartTutorialDone = false; ProfileStore.Save();
                SceneNav.ReloadCurrent();
            });
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (holdModeButton != null) holdModeButton.onClick.AddListener(() => { _autoHold = !_autoHold; UpdateLabels(); });
            if (voiceButton != null) voiceButton.onClick.AddListener(() => { VoiceCoach.On = !VoiceCoach.On; UpdateLabels(); if (VoiceCoach.On) VoiceCoach.Say("bien_1", 3); });
            if (basketRenderers != null) _basketMats = System.Array.ConvertAll(basketRenderers, r => r.material);
            if (cutFlash != null) cutFlash.gameObject.SetActive(false);
            summaryPanel.SetActive(false);
            if (associationLine != null) associationLine.enabled = false;
        }

        void Start()
        {
            _roses = FindObjectsByType<HarvestRose>(FindObjectsSortMode.None).ToList();
            _roundStart = Time.time;
            if (!P.cartTutorialDone) BeginTutorial();
            UpdateLabels();
            RefreshScreen();
        }

        void ToggleFailure()
        {
            classifier.available = !classifier.available;
            UpdateLabels();
            VoiceCoach.Say(classifier.available ? "sistema_ok" : "sistema_caido", 2);
        }

        void ToggleDebug()
        {
            _debug = !_debug;
            if (panTilt.debugRay != null) panTilt.debugRay.gameObject.SetActive(_debug);
            UpdateLabels();
        }

        void UpdateLabels()
        {
            if (failureLabel != null) failureLabel.text = classifier.available ? "Simular falla" : "Restaurar sistema";
            if (debugLabel != null) debugLabel.text = _debug ? "Depuración: SÍ" : "Depuración: NO";
            var br = XRInputBridge.Instance;
            bool left = br != null ? br.LeftHanded : P.leftHanded;
            if (handednessLabel != null) handednessLabel.text = left ? "Mano de tijera: IZQUIERDA" : "Mano de tijera: DERECHA";
            if (holdModeLabel != null) holdModeLabel.text = _autoHold ? "Sostener: AUTOMÁTICO" : "Sostener: OBLIGATORIO";
            if (cameraLabel != null) cameraLabel.text = cameraFeed != null && cameraFeed.Visible ? "Pantalla cámara: SÍ" : "Pantalla cámara: NO";
            if (voiceLabel != null) voiceLabel.text = VoiceCoach.On ? "Voz guía: SÍ" : "Voz guía: NO";
        }

        void Update()
        {
            if (_roundOver) return;
            bool sees = panTilt.SeesTarget;
            if (sees) _seenTime += Time.deltaTime;

            UpdateAssociation(sees);
            UpdateCutPosition();
            UpdateLed(sees);
            UpdateSemaphore();
            UpdateVoice();
            if (EditorCutPressed()) OnScissorsCut(scissorsTip.position);
            UpdateHold();
            UpdateFlash();
            if (_tut >= 0) UpdateTutorial();
            else if (!_roundOver && _complete) StartCoroutine(EndRound());
            UpdateRing();
            UpdateStatus(sees);
            if (cart.aisle != _lastAisle) { _lastAisle = cart.aisle; if (_tut < 0) RefreshScreen(); }
        }

        // ───────────── Asociación tijeras → rosa ─────────────

        void UpdateAssociation(bool sees)
        {
            HarvestRose best = null;
            bool ambiguous = false;
            if (sees)
            {
                var tip = scissorsTip.position;
                float bestDy = float.MaxValue, secondDy = float.MaxValue;
                foreach (var r in _roses)
                {
                    if (r == null || r.IsCut) continue;
                    var b = r.BudPos;
                    float dy = b.y - tip.y;
                    if (dy <= 0f) continue;
                    float dh = new Vector2(b.x - tip.x, b.z - tip.z).magnitude;
                    if (dh > associationRadius) continue;
                    if (dy < bestDy) { secondDy = bestDy; bestDy = dy; best = r; }
                    else if (dy < secondDy) secondDy = dy;
                }
                ambiguous = best != null && secondDy - bestDy < ambiguousDelta;
            }

            if (best != _assoc)
            {
                var prev = _assoc;
                _assoc = best;
                _assocAmbiguous = ambiguous;
                _verdict = "";
                _lenNoise = Random.Range(-lengthNoiseCm, lengthNoiseCm);
                _requestId++;
                if (prev != null && _tut == 4 && prev == _tutWrongRose && !prev.IsCut && prev.Evaluated)
                    _tutResult = "ok";
                if (_assoc != null) RequestClassification(_assoc, ambiguous, _requestId);
            }

            if (associationLine != null)
            {
                associationLine.enabled = _debug && _assoc != null;
                if (associationLine.enabled)
                {
                    associationLine.SetPosition(0, scissorsTip.position);
                    associationLine.SetPosition(1, _assoc.BudPos);
                }
            }
        }

        void RequestClassification(HarvestRose rose, bool ambiguous, int id)
        {
            if (!_clf.Available) { _verdict = "SIN SISTEMA"; return; }
            _clf.Classify(rose, ambiguous, (g, conf) =>
            {
                if (id != _requestId || rose == null) return; // lectura vieja
                string v = ambiguous || conf < confidenceThreshold ? "DUDA" : Order.Accepts(g) ? "CORTAR" : "NO CORTAR";
                _verdict = v;
                rose.Evaluated = true;
                rose.lastVerdict = v; rose.lastPred = g; rose.lastConf = conf;
                _lastConf.Enqueue(conf);
                while (_lastConf.Count > 5) _lastConf.Dequeue();
                if (v == "DUDA") _sawAmber = true;
            });
        }

        /// <summary>
        /// La cámara ve la tijera y el tallo: calcula el largo de tallo que resultaría de cortar en este punto,
        /// encuadra entre la tijera y el botón y dibuja la zona de corte correcta.
        /// </summary>
        void UpdateCutPosition()
        {
            _onStem = false; _cutCm = -1f;
            if (_assoc != null && panTilt.SeesTarget)
            {
                float d = _assoc.DistanceToStem(scissorsTip.position, out var al);
                if (d < cutRadius) { _onStem = true; _cutCm = (_assoc.budBase - al) * 100f + _lenNoise; }
                panTilt.AimOverride = (scissorsTip.position + _assoc.BudPos) * 0.5f;
            }
            else panTilt.AimOverride = null;

            if (cutBand != null)
            {
                bool show = _assoc != null && _verdict == "CORTAR" && panTilt.SeesTarget;
                cutBand.enabled = show;
                if (show)
                {
                    var up = _assoc.transform.up;
                    float aHigh = _assoc.budBase - Order.largoMinCm / 100f;
                    float aLow = Mathf.Max(0.02f, _assoc.budBase - Order.largoMaxCm / 100f);
                    var toCam = (panTilt.lens.position - _assoc.transform.position); toCam -= Vector3.Dot(toCam, up) * up;
                    var off = toCam.sqrMagnitude > 1e-4f ? toCam.normalized * 0.008f : Vector3.zero;
                    cutBand.SetPosition(0, _assoc.transform.position + up * aLow + off);
                    cutBand.SetPosition(1, _assoc.transform.position + up * aHigh + off);
                }
            }
        }

        // ───────────── Voz guía: avisos antes del corte ─────────────
        string _warnCand, _spokenKey;
        float _warnSince, _spokenAt = -99f;
        HarvestRose _spokenRose;

        /// <summary>Qué decir con la tijera en el tallo: punto de la rosa (inmadura/pasada/duda) o largo del tallo.</summary>
        string WarnKey()
        {
            if (_assoc == null || !_onStem || !panTilt.SeesTarget || (scissors != null && !scissors.Active)) return null;
            if (_tut == 4 && _assoc == _tutWrongRose) return null;   // la prueba de criterio: sin pistas
            switch (_verdict)
            {
                case "NO CORTAR": return _assoc.lastPred < Order.gradoMin ? "aviso_inmadura" : "aviso_pasada";
                case "DUDA": return "aviso_duda";
                case "CORTAR":
                    if (_cutCm < 0f) return null;
                    if (_cutCm < Order.largoMinCm) return "aviso_corto";
                    if (_cutCm > Order.largoMaxCm) return "aviso_largo";
                    return "ok_aqui";
            }
            return null;
        }

        void UpdateVoice()
        {
            var k = WarnKey();
            if (k != _warnCand) { _warnCand = k; _warnSince = Time.time; }
            if (k == null || Time.time - _warnSince < 0.6f) return;           // la tijera debe quedarse ahí un momento
            if (k == _spokenKey && _assoc == _spokenRose && Time.time - _spokenAt < 10f) return;
            // «Ahí sí» solo cuando corrige la altura después de un aviso en esta misma rosa
            if (k == "ok_aqui" && (_spokenRose != _assoc || (_spokenKey != "aviso_corto" && _spokenKey != "aviso_largo"))) return;
            if (VoiceCoach.Say(k, 1)) { _spokenKey = k; _spokenRose = _assoc; _spokenAt = Time.time; }
        }

        string _lastStatus;
        void UpdateStatus(bool sees)
        {
            if (statusText == null) return;
            string cam;
            if (!_clf.Available) cam = "<color=#FF6B6B>Sistema no disponible: decide con tu criterio</color>";
            else if (scissors != null && !scissors.Active) cam = "Cámara en reposo · haz la seña de tijera (índice y medio) para que te siga";
            else if (sees && _assoc == null) cam = "<color=#7CFC9A>Cámara siguiendo la tijera</color> · ponla en el tallo de un botón";
            else if (sees && _verdict == "") cam = "Leyendo el botón…";
            else if (sees && _verdict == "NO CORTAR") cam = $"<color=#FF7070>Rosa fuera de punto para el pedido (grado leído {_assoc.lastPred}): no cortar</color>";
            else if (sees && _verdict == "DUDA") cam = "<color=#FFC04D>Duda del sistema: decide tú</color>";
            else if (sees && !_onStem) cam = $"<color=#5FE3EA>Rosa en punto: lleva la tijera al tallo, entre {Order.largoMinCm:0} y {Order.largoMaxCm:0} cm bajo el botón</color>";
            else if (sees && Order.LengthOk(_cutCm)) cam = $"<color=#7CFC9A>Corta aquí: tallo de {_cutCm:0} cm</color>";
            else if (sees && _cutCm < Order.largoMinCm) cam = $"<color=#FF7070>Muy cerca del botón ({_cutCm:0} cm): baja la tijera {Order.largoMinCm - _cutCm:0} cm</color>";
            else if (sees) cam = $"<color=#FF7070>Muy abajo ({_cutCm:0} cm): sube la tijera {_cutCm - Order.largoMaxCm:0} cm y deja yemas en la planta</color>";
            else
            {
                var r = panTilt.Reason;
                cam = r == "tapada" ? "La cámara no ve la tijera: el follaje la tapa"
                    : r == "lejos" ? "La cámara no ve la tijera: está muy lejos"
                    : r == "fuera de alcance" ? "Tijera fuera del alcance del brazo: acércala al carro"
                    : "Cámara buscando la tijera…";
            }
            // Segunda línea: sostener y dejar el ramo
            string hold = "";
            bool overBasket = _bouquet.Count > 0 && freeHand != null && InBasketZone(freeHand.position);
            if (overBasket) hold = "\n<color=#7CFC9A>Abre la mano: el ramo cae en la canasta</color>";
            else if (_bouquet.Count > 0) hold = $"\nRamo: {_bouquet.Count} tallo(s) · llévalo sobre la canasta azul y abre la mano";
            else if (!_autoHold && sees && _verdict == "CORTAR" && !HoldPressed()) hold = "\nSostén el tallo con la otra mano antes de cortar";
            cam += hold;
            if (cam != _lastStatus) { statusText.text = cam; _lastStatus = cam; }
        }

        /// <summary>Anillo alrededor del botón asociado, del color del veredicto (para saber qué rosa se está leyendo).</summary>
        void UpdateRing()
        {
            if (budRing == null) return;
            bool on = _assoc != null && panTilt.SeesTarget;
            budRing.enabled = on;
            if (!on) return;
            var c = _verdict == "CORTAR" ? Green : _verdict == "NO CORTAR" ? Red : _verdict == "" ? Blue : Amber;
            budRing.startColor = budRing.endColor = c;
            if (budRing.material != null) budRing.material.color = c;
            var center = _assoc.BudPos;
            int n = budRing.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / (n - 1);
                budRing.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.055f);
            }
        }

        // ───────────── Señales ─────────────

        void UpdateLed(bool sees)
        {
            Color c;
            if (!sees || !_clf.Available) c = Off;
            else if (_assoc == null || _verdict == "") c = _assoc == null ? Blue * 0.35f : Blue;
            else if (_verdict == "CORTAR")
            {
                // Rosa en punto: el color depende de POR DÓNDE va a cortar
                if (!_onStem) c = Cyan;                          // en punto: lleva la tijera al tallo
                else c = Order.LengthOk(_cutCm) ? Green : Red;   // altura correcta / incorrecta
            }
            else if (_verdict == "NO CORTAR") c = Red;
            else c = (Mathf.Repeat(Time.time * 2f, 1f) < 0.5f) ? Amber : Off; // 2 Hz
            LedColor = c;
            if (c == Green) _sawGreen = true;
            else if (c == Red) _sawRed = true;
            SetEmissive(_ledMat, c);
        }

        /// <summary>
        /// Semáforo del carro. Mientras la cámara lee una rosa, repite el veredicto en grande (verde corta aquí,
        /// rojo no cortes, ámbar duda/en punto sin tijera en el tallo). Sin lectura muestra el estado del sistema:
        /// verde operando, ámbar fijo confianza baja reciente, rojo PARPADEANDO sistema no disponible,
        /// verde parpadeando pedido completo.
        /// </summary>
        void UpdateSemaphore()
        {
            bool blink = Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
            bool green = false, amber = false, red = false;
            string st;
            if (_semaphoreOverride == "red") { red = true; st = "rojo (demo)"; }
            else if (_semaphoreOverride == "amber") { amber = true; st = "ambar (demo)"; }
            else if (!_clf.Available) { red = blink; st = "rojo parpadeando (sin sistema)"; }
            else if (_complete) { green = blink; st = "verde parpadeando (pedido completo)"; }
            else if (_assoc != null && panTilt.SeesTarget && _verdict != "")
            {
                if (_verdict == "NO CORTAR") { red = true; st = "rojo (no cortar)"; }
                else if (_verdict == "DUDA") { amber = blink; st = "ambar parpadeando (duda)"; }
                else if (!_onStem) { amber = true; st = "ambar (en punto, lleva la tijera al tallo)"; }
                else if (Order.LengthOk(_cutCm)) { green = true; st = "verde (corta aqui)"; }
                else { red = true; st = "rojo (altura incorrecta)"; }
            }
            else if (_lastConf.Count(c => c < confidenceThreshold) >= 3) { amber = true; st = "ambar (confianza baja)"; }
            else { green = true; st = "verde (operando)"; }
            SemaphoreState = st;
            SetEmissive(_gMat, green ? Green : Off);
            SetEmissive(_aMat, amber ? Amber : Off);
            SetEmissive(_rMat, red ? Red : Off);
        }

        static void SetEmissive(Material m, Color c)
        {
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
        }

        // ───────────── Corte y canasta ─────────────

        bool EditorCutPressed()
        {
            if (!Application.isEditor) return false;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        bool HoldPressed()
        {
            var b = XRInputBridge.Instance;
            if (b != null && b.HoldGrab) return true;
#if ENABLE_INPUT_SYSTEM
            // Editor: mantener H = mano de sostén cerrada
            if (Application.isEditor && Keyboard.current != null && Keyboard.current.hKey.isPressed) return true;
#endif
            return false;
        }

        /// <summary>
        /// Mano de sostén: cerrar la mano sobre un tallo lo SOSTIENE (se puede doblar un poco, no se separa de la planta).
        /// Cerrarla sobre un tallo caído lo recoge. Mientras está cerrada guarda el ramo; al abrirla sobre la canasta
        /// el ramo se cuenta en el pedido, fuera de la canasta cae al suelo.
        /// </summary>
        void UpdateHold()
        {
            if (freeHand == null) return;
            bool grab = HoldPressed();
            var hp = freeHand.position;
            if (grab && !_holdGrab)
            {
                HarvestRose best = null;
                float bestD = holdRadius;
                foreach (var r in _roses)
                {
                    if (r == null || r.IsCut) continue;
                    float d = r.DistanceToStem(hp, out _);
                    if (d < bestD) { bestD = d; best = r; }
                }
                if (best != null) { _held = best; _heldRot = best.transform.rotation; }
                else
                {
                    // Recoger un tallo caído
                    HarvestRose f = null; float fd = pickupRadius;
                    foreach (var r in _fallen)
                    {
                        if (r == null) continue;
                        float d = Mathf.Min(Vector3.Distance(hp, r.BudPos), Vector3.Distance(hp, r.transform.position + r.transform.up * 0.3f));
                        if (d < fd) { fd = d; f = r; }
                    }
                    if (f != null) { _fallen.Remove(f); AddToBouquet(f); }
                }
            }
            if (grab && _held != null)
            {
                // Doblar el tallo levemente hacia la mano (sin desprenderlo)
                var up0 = _heldRot * Vector3.up;
                var to = (hp - _held.transform.position).normalized;
                var bend = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(up0, to), maxBendDegrees);
                _held.transform.rotation = bend * _heldRot;
            }
            if (!grab && _holdGrab)
            {
                if (_held != null && !_held.IsCut) _held.transform.rotation = _heldRot;
                _held = null;
                if (_bouquet.Count > 0)
                {
                    if (InBasketZone(hp)) Deposit();
                    else DropBouquet();
                }
            }
#if ENABLE_INPUT_SYSTEM
            if (Application.isEditor && Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame && _bouquet.Count > 0) Deposit();
#endif
            // Canasta iluminada cuando la mano con el ramo está encima: abrir la mano para dejarlo
            bool over = _bouquet.Count > 0 && InBasketZone(hp);
            if (over != _basketLit && _basketMats != null)
            {
                _basketLit = over;
                foreach (var m in _basketMats) m.color = over ? new Color(0.45f, 1.6f, 0.6f) : Color.white;
            }
            _holdGrab = grab;
        }

        /// <summary>La tijera se cerró (gesto o gatillo): corta solo si la zona de corte toca un tallo.</summary>
        void OnScissorsCut(Vector3 tipPos)
        {
            if (_roundOver) return;
            SoundFx.Play(SoundFx.Instance != null ? SoundFx.Instance.click : null, 0.9f, tipPos);
            Flash(tipPos);
            HarvestRose best = null;
            float bestD = cutRadius, along = 0f;
            foreach (var r in _roses)
            {
                if (r == null || r.IsCut) continue;
                float d = r.DistanceToStem(tipPos, out var a);
                if (d < bestD) { bestD = d; best = r; along = a; }
            }
            if (best == null) { Haptic(0.3f, 0.05f); return; } // corte en el aire: solo sonido

            string verdict = best.Evaluated ? best.lastVerdict : (_clf.Available ? "" : "SIN SISTEMA");
            bool holding = HoldPressed() || _autoHold;
            if (best == _held) _held = null;
            float cm;
            if (holding)
            {
                cm = best.CutAt(along, freeHand);   // la parte cortada pasa a la mano de sostén
                AddToBouquet(best, false);
            }
            else
            {
                cm = best.CutAt(along, null);       // sin sostener: cae al suelo y se puede recoger
                Drop(best);
            }
            _cutRoses.Add(best);
            Haptic(0.8f, 0.12f);
            ScoreDecision(best, true, verdict, cm);

            if (_tut == 4 && best == _tutWrongRose) _tutResult = "cut";
            if (best == _assoc) { _assoc = null; _verdict = ""; _requestId++; }
        }

        void AddToBouquet(HarvestRose r, bool reparent = true)
        {
            var rb = r.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            if (reparent)
            {
                r.transform.SetParent(freeHand, false);
                r.transform.localPosition = new Vector3(Random.Range(-0.02f, 0.02f), -0.3f, Random.Range(-0.02f, 0.02f));
                r.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
            }
            if (!_bouquet.Contains(r)) _bouquet.Add(r);
        }

        void Drop(HarvestRose r)
        {
            r.transform.SetParent(null, true);
            var col = r.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = r.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.3f, 0f);
                col.size = new Vector3(0.05f, 0.6f, 0.05f);
            }
            var rb = r.GetComponent<Rigidbody>();
            if (rb == null) rb = r.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.05f; rb.isKinematic = false; rb.detectCollisions = true;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
            if (!_fallen.Contains(r)) _fallen.Add(r);
        }

        void DropBouquet()
        {
            foreach (var r in _bouquet)
            {
                Drop(r);
                TrainingLog.Log(new TrainingDecision
                {
                    etapa = 2, nivel = P.stage2Level, item = r.id, pedido = Order.Short, gradoReal = r.grade,
                    decisionUsuario = "ramo_al_suelo", corto = true, acerto = false
                });
            }
            _bouquet.Clear();
            RefreshScreen("El ramo cayó al suelo: recógelo con la mano de sostén");
        }

        void Deposit()
        {
            foreach (var r in _bouquet)
            {
                // Acostadas a lo largo de la canasta, botones hacia adelante, apiladas
                r.transform.SetParent(basket, true);
                int k = _deposited + _bouquet.IndexOf(r);
                r.transform.localPosition = new Vector3(Random.Range(-0.13f, 0.13f), 0.03f + (k / 6) * 0.025f, -0.31f + Random.Range(0f, 0.03f));
                r.transform.localRotation = Quaternion.Euler(90f + Random.Range(-4f, 4f), Random.Range(-6f, 6f), Random.Range(0f, 360f));
            }
            _deposited += _bouquet.Count;
            _bouquet.Clear();
            Haptic(0.4f, 0.08f, false);
            SoundFx.Play(SoundFx.Instance != null ? SoundFx.Instance.deposit : null, 0.8f);
            if (_tut == 5) AdvanceTutorial();
            if (_tut < 0 && _deposited >= Order.metaTallos) _complete = true;
            RefreshScreen();
        }

        /// <summary>Volumen sobre la canasta (generoso): la mano está encima aunque no esté justo en el centro.</summary>
        bool InBasketZone(Vector3 p)
        {
            if (basket == null) return false;
            var l = basket.InverseTransformPoint(p);
            return Mathf.Abs(l.x) <= 0.34f && Mathf.Abs(l.z) <= 0.5f && l.y >= -0.15f && l.y <= 1.1f;
        }

        /// <summary>La mano de sostén está ocupada (sostiene un tallo o lleva ramo): no debe agarrar la manija del carro.</summary>
        public bool HoldHandBusy => _held != null || _bouquet.Count > 0;

        void Flash(Vector3 p)
        {
            if (cutFlash == null) return;
            cutFlash.position = p;
            cutFlash.gameObject.SetActive(true);
            _flashT = 0f;
        }

        void UpdateFlash()
        {
            if (cutFlash == null || _flashT < 0f) return;
            _flashT += Time.deltaTime;
            float k = _flashT / 0.18f;
            cutFlash.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.05f, k);
            if (k >= 1f) { cutFlash.gameObject.SetActive(false); _flashT = -1f; }
        }

        void Haptic(float amp, float secs, bool right = true) => StartCoroutine(HapticCo(amp, secs, right));

        IEnumerator HapticCo(float amp, float secs, bool right)
        {
            var br = XRInputBridge.Instance;
            bool scissorRight = br == null || br.ScissorIsRight;
            bool physRight = right ? scissorRight : !scissorRight; // right = mano de tijera
            var c = physRight ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
            OVRInput.SetControllerVibration(1f, amp, c);
            yield return new WaitForSeconds(secs);
            OVRInput.SetControllerVibration(0f, 0f, c);
        }

        // ───────────── Puntos y registro ─────────────

        void ScoreDecision(HarvestRose r, bool cut, string verdict, float cm)
        {
            bool inPoint = Order.Accepts(r.grade);
            bool correct = cut == inPoint;
            bool sysSaid = verdict == "CORTAR" || verdict == "NO CORTAR";
            bool sysWrong = sysSaid && ((verdict == "CORTAR") != inPoint);
            bool agree = sysSaid && ((verdict == "CORTAR") == cut);

            int pts;
            bool lengthOk = !cut || Order.LengthOk(cm);
            if (cut) pts = inPoint ? (lengthOk ? ptsCutGood : 3) : (r.grade <= 1 ? ptsCutVeryClosed : ptsCutBad);
            else pts = inPoint ? ptsMissed : ptsNoCutGood;
            if (correct && sysWrong) { pts += ptsCriterio; _criterio++; }

            if (_tut < 0)
            {
                _points += pts;
                _decisions++;
                if (correct) _good++;
                if (cut) _cuts++;
                if (cut && inPoint) { _heightChecks++; if (lengthOk) _heightOk++; }
                if (cut)
                {
                    Color hc;
                    if (!inPoint) { _badCuts++; hc = new Color(0.95f, 0.25f, 0.2f); }
                    else if (!lengthOk) { _heightBadCuts++; hc = new Color(1f, 0.72f, 0.15f); }
                    else { _goodCuts++; hc = new Color(0.2f, 0.85f, 0.4f); }
                    _history.Add(hc);
                    if (_history.Count > 12) _history.RemoveAt(0);
                }
                if (sysSaid) { _sysDecisions++; if (agree) _agreeSys++; }
            }
            TrainingLog.Log(new TrainingDecision
            {
                etapa = 2, nivel = P.stage2Level, item = r.id, pedido = Order.Short, gradoReal = r.grade,
                estadoReal = inPoint ? "en_punto" : (r.grade < Order.gradoMin ? "inmaduro" : "pasado"),
                gradoPredicho = r.lastPred, confianza = r.lastConf, veredicto = verdict,
                decisionUsuario = cut ? (lengthOk ? "cortar" : (cm < Order.largoMinCm ? "cortar (muy alto: tallo corto)" : "cortar (muy bajo: tallo largo)")) : "no cortar", corto = cut,
                coincidioConSistema = sysSaid ? (agree ? "1" : "0") : "", acerto = correct,
                largoTalloCm = cut ? cm : -1f
            });
            if (cut && SoundFx.Instance != null) SoundFx.Play(correct ? SoundFx.Instance.good : SoundFx.Instance.bad, 0.6f);
            if (cut && !(_tut == 4 && r == _tutWrongRose))
            {
                if (correct && sysWrong) VoiceCoach.Say("bien_criterio", 2);
                else if (correct && lengthOk) VoiceCoach.SayOneOf(2, "bien_1", "bien_2", "bien_3", "bien_4");
                else if (correct) VoiceCoach.Say(cm < Order.largoMinCm ? "mal_corto" : "mal_largo", 2);
                else VoiceCoach.Say(r.grade < Order.gradoMin ? "mal_inmadura" : "mal_pasada", 2);
            }
            if (cut && _tut < 0) StartCoroutine(ScorePopup(scissorsTip.position, pts, correct ? (lengthOk ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.8f, 0.2f)) : new Color(1f, 0.35f, 0.3f)));
            RefreshScreen(cut ? (correct ? (lengthOk ? $"<color=#7CFC9A>+{pts}</color> Bien cortada ({cm:0} cm)" : $"<color=#FFC04D>+{pts}</color> Rosa correcta, pero tallo de {cm:0} cm (pedido {Order.largoMinCm:0}–{Order.largoMaxCm:0})") + (sysWrong ? " · ¡BONO CRITERIO!" : "")
                                         : $"<color=#FF7070>{pts}</color> Estaba fuera de punto (grado {r.grade})") : null);
        }

        /// <summary>Puntaje flotante donde se cortó: sube y se desvanece.</summary>
        IEnumerator ScorePopup(Vector3 pos, int pts, Color c)
        {
            var go = new GameObject("PuntajeFlotante", typeof(RectTransform), typeof(Canvas));
            go.layer = 5;
            go.transform.position = pos + Vector3.up * 0.06f;
            go.transform.localScale = Vector3.one * 0.001f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = new Vector2(260, 110);
            var t = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(go.transform, false);
            var rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            t.text = (pts > 0 ? "+" : "") + pts; t.fontSize = 90; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.color = c; t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 30, 15, 255); t.raycastTarget = false;
            float time = 0f;
            while (time < 1.3f)
            {
                time += Time.deltaTime;
                go.transform.position += Vector3.up * 0.12f * Time.deltaTime;
                var cam = Camera.main;
                if (cam != null) go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position);
                var col = c; col.a = Mathf.Clamp01(1.6f - time * 1.2f); t.color = col;
                yield return null;
            }
            Destroy(go);
        }

        IEnumerator EndRound()
        {
            _roundOver = true;
            SetEmissive(_ledMat, Off);
            VoiceCoach.Say(_complete ? "pedido_completo" : "fin_ronda", 3);
            yield return new WaitForSeconds(_complete ? 1.5f : 0.3f);

            // Rosas no cortadas en el tramo recorrido
            foreach (var r in _roses)
            {
                if (r == null || r.IsCut || r.transform.position.z > cart.VisitedZ(r.transform.position.x) + 1.0f) continue;
                bool inPoint = Order.Accepts(r.grade);
                if (inPoint || r.Evaluated) ScoreDecision(r, false, r.Evaluated ? r.lastVerdict : "", -1f);
            }

            float acc = _decisions > 0 ? (float)_good / _decisions : 0f;
            float secs = Time.time - _roundStart;
            int speedBonus = acc >= 0.8f && _complete && secs < 240f ? 10 : 0;
            _points += speedBonus;
            int stars = acc >= 0.9f ? 3 : acc >= 0.75f ? 2 : 1;
            var p = P;
            p.harvestRounds++;
            p.xp += Mathf.Max(0, _points);
            ProfileStore.Save();

            summaryText.text =
                $"<size=120%><b>{(_complete ? "¡Pedido completo!" : "Fin del pasillo")}</b></size>   {new string('*', stars)}{new string('·', 3 - stars)}\n\n" +
                $"Tallos en la canasta: <b>{_deposited} / {Order.metaTallos}</b>    Tiempo: {secs / 60f:0.0} min\n" +
                $"Aciertos frente al grado real: <b>{_good} de {_decisions}</b> ({acc:P0})\n" +
                $"Coincidencia con el sistema: {(_sysDecisions > 0 ? ((float)_agreeSys / _sysDecisions).ToString("P0") : "—")}\n" +
                $"Cortes a la altura correcta: <b>{_heightOk} de {_heightChecks}</b>\n" +
                $"Veces que tuviste razón y el sistema no: <b>{_criterio}</b>\n" +
                $"Puntos: <b>{_points}</b>{(speedBonus > 0 ? " (incluye bono de velocidad)" : "")}    XP total: {p.xp}\n\n" +
                $"<size=80%>Registro: {TrainingLog.PathFor(p.id)}</size>";
            summaryPanel.SetActive(true);
            XRUi.PlaceInFront(summaryPanel.transform, 1.1f, -0.05f);
        }

        void RefreshScreen(string last = null)
        {
            if (counterText != null) counterText.text = $"Tallos: {_deposited} / {Order.metaTallos}";
            if (pointsText != null && (last != null || _tut >= 0)) pointsText.text = _tut >= 0 ? "Tutorial: los cortes no cuentan" : last;
            if (boardGood != null) boardGood.text = _goodCuts.ToString();
            if (boardBad != null) boardBad.text = _badCuts.ToString();
            if (boardHeight != null) boardHeight.text = _heightBadCuts.ToString();
            if (boardPoints != null) boardPoints.text = _points.ToString();
            if (historyDots != null)
                for (int i = 0; i < historyDots.Length; i++)
                {
                    int k = _history.Count - historyDots.Length + i;
                    historyDots[i].color = k >= 0 ? _history[k] : new Color(1f, 1f, 1f, 0.12f);
                }
            if (screenText != null && _tut < 0)
                screenText.text = $"<b>Pedido:</b> {Order.Label}\nMeta: {Order.metaTallos} tallos · {cart.AisleName}\n\n" +
                                  "<size=85%>LED: celeste = rosa en punto, lleva la tijera al tallo · verde = corta aquí · rojo = no cortes (rosa o altura) · ámbar parpadeando = duda.\n" +
                                  "Tijera: seña de tijera (índice y medio) y júntalos para cortar · con control, gatillo.\n" +
                                  "Sostén el tallo con la otra mano (puño, pellizco o grip) antes de cortar; abre la mano sobre la canasta.\n" +
                                  "Carro: agarra la manija y empuja para avanzar, jala para retroceder.\n" +
                                  "De lado (otro corredor): apunta a un letrero «Corredor 1/2/3» de la cabecera y pellizca, joystick a los lados o menú.</size>";
        }

        // ───────────── Tutorial del carro ─────────────

        void BeginTutorial()
        {
            _tut = 0;
            // Rosas de práctica cerca del inicio con salidas forzadas del sistema
            var near = _roses.Where(r => Mathf.Abs(r.transform.position.x - cart.transform.position.x) < 0.7f &&
                                         r.transform.position.z > cart.transform.position.z + 0.4f &&
                                         r.transform.position.z < cart.transform.position.z + 3.5f)
                             .OrderBy(r => r.transform.position.z).ToList();
            void Force(HarvestRose r, int real, int pred, float conf) { r.grade = real; r.forcedPredGrade = pred; r.forcedConfidence = conf; r.occluded = false; }
            if (near.Count >= 4)
            {
                Force(near[0], 3, 3, 0.93f);   // verde
                Force(near[1], 1, 1, 0.9f);    // rojo
                Force(near[2], 4, 4, 0.52f);   // ámbar
                Force(near[3], 2, 3, 0.91f);   // el sistema se equivoca: dice cortar, está inmadura
                _tutWrongRose = near[3];
                AddLabel(_tutWrongRose, "Rosa de práctica 5");
            }
            ShowTutorial();
        }

        void AddLabel(HarvestRose r, string text)
        {
            // Placa oscura con texto blanco: se lee contra el techo blanco
            var go = new GameObject("Etiqueta", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(r.transform, false);
            go.transform.position = r.BudPos + Vector3.up * 0.14f;
            go.transform.localScale = Vector3.one * 0.001f / Mathf.Max(0.01f, r.transform.lossyScale.x);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = new Vector2(420, 90);
            go.layer = 5;
            var bg = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bg.transform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var img = bg.GetComponent<Image>(); img.color = new Color(0f, 0.19f, 0.11f, 0.92f); img.raycastTarget = false;
            var tgo = new GameObject("Texto", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = tgo.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 44; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            go.AddComponent<FaceCamera>().offsetRight = 0f;
        }

        void TutText(string t)
        {
            if (tutorialText != null) tutorialText.text = t;
            else if (screenText != null) screenText.text = t;
        }

        void AdvanceTutorial()
        {
            _tut++;
            _tutTimer = 0f;
            if (_tut > 5)
            {
                _tut = -1;
                P.cartTutorialDone = true;
                ProfileStore.Save();
                if (_tutWrongRose != null) { var l = _tutWrongRose.transform.Find("Etiqueta"); if (l != null) Destroy(l.gameObject); }
                foreach (var r in _roses) { r.forcedPredGrade = -1; r.forcedConfidence = -1f; }
                if (tutorialPanel != null) tutorialPanel.SetActive(false);
                _deposited = 0; _points = 0; _roundStart = Time.time;
                _goodCuts = _badCuts = _heightBadCuts = 0; _history.Clear();
                RefreshScreen("Tutorial completo. ¡A cosechar!");
                VoiceCoach.Say("tut_fin", 3);
                return;
            }
            ShowTutorial();
        }

        void ShowTutorial()
        {
            string[] steps =
            {
                "<b>Tutorial 1/6 · Mover el carro</b>\nCierra la mano sobre la manija (puño, pellizco o grip) y llévala hacia adelante: el carro avanza. Hacia ti, retrocede. Al soltar, se detiene. Avanza 1 metro.",
                "<b>Tutorial 2/6 · La cámara te sigue</b>\nHaz la seña de tijera con tu mano dominante (índice y medio extendidos) o usa el control. Mueve las tijeras: la cámara del brazo sigue el LED del mango y la pantalla muestra lo que ve.",
                "<b>Tutorial 3/6 · El LED de las tijeras</b>\nPon la tijera en el tallo de las rosas de práctica. La cámara mira qué vas a cortar y por dónde.\nCeleste = en punto, baja al tallo · Verde = corta aquí · Rojo = no cortes · Ámbar = duda.",
                "<b>Tutorial 4/6 · El semáforo del carro</b>\nÁmbar fijo: las últimas lecturas tienen confianza baja, revisa con más cuidado.\nRojo fijo: el sistema no está disponible, trabaja con tu criterio.",
                "<b>Tutorial 5/6 · El sistema también se equivoca</b>\nApunta a la «Rosa de práctica 5». El LED dirá cortar, pero mira los sépalos: ¿de verdad está en punto?\nCórtala o déjala según TU criterio.",
                "<b>Tutorial 6/6 · Sostener, cortar y dejar en la canasta</b>\nSostén el tallo de una rosa en punto con la mano de sostén, córtalo con la tijera (junta los dedos o gatillo) y abre la mano sobre la canasta azul.",
            };
            if (tutorialPanel != null && !tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(true);
                XRUi.PlaceInFront(tutorialPanel.transform, 0.62f, -0.2f);
            }
            TutText(steps[Mathf.Clamp(_tut, 0, steps.Length - 1)]);
            VoiceCoach.Say("tut_" + (Mathf.Clamp(_tut, 0, steps.Length - 1) + 1), 3);
            RefreshScreen();
        }

        void UpdateTutorial()
        {
            _tutTimer += Time.deltaTime;
            switch (_tut)
            {
                case 0:
                    if (cart.Travelled >= 1f) AdvanceTutorial();
                    break;
                case 1:
                    if (_seenTime >= 1.5f) AdvanceTutorial();
                    break;
                case 2:
                    {
                        string Mark(bool b) => b ? "[x]" : "[  ]";
                        var baseTxt = "<b>Tutorial 3/6 · El LED de las tijeras</b>\nPon las tijeras debajo de un botón de las rosas de práctica.\n";
                        TutText(baseTxt + $"{Mark(_sawGreen)} Verde = corta aquí   {Mark(_sawRed)} Rojo = no cortes   {Mark(_sawAmber)} Ámbar parpadeando = duda");
                    }
                    if (_sawGreen && _sawRed && _sawAmber) AdvanceTutorial();
                    break;
                case 3:
                    _semaphoreOverride = _tutTimer < 4f ? "amber" : _tutTimer < 8f ? "red" : "";
                    if (_tutTimer >= 9f) { _semaphoreOverride = ""; AdvanceTutorial(); }
                    break;
                case 4:
                    if (_tutResult == "cut")
                    {
                        _tutResult = "";
                        TutText("<b>La cortaste.</b> Estaba inmadura (grado 2): el sistema se equivocó y no tiene vuelta atrás.\nCuando dudes, mira los sépalos: aún pegados = no está en punto.");
                        VoiceCoach.Say("tut_cortaste", 3);
                        StartCoroutine(AdvanceLater(8f));
                        _tut = 40; // espera
                    }
                    else if (_tutResult == "ok")
                    {
                        _tutResult = "";
                        TutText("<b>¡Bien! La dejaste.</b> Estaba inmadura (grado 2) y el sistema decía cortar.\nEso es criterio: en la cosecha vale el BONO CRITERIO.");
                        VoiceCoach.Say("tut_bien", 3);
                        StartCoroutine(AdvanceLater(6f));
                        _tut = 40;
                    }
                    break;
            }
        }

        IEnumerator AdvanceLater(float s)
        {
            yield return new WaitForSeconds(s);
            _tut = 4;
            AdvanceTutorial();
        }
    }
}
