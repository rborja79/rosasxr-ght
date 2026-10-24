using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// Etapa 1 "Aprende a ver": lecciones de 10 fotos reales, 3 vidas, XP, racha, repetición espaciada,
    /// insignias y candado de la etapa 2. Nada premia la velocidad: el tiempo solo se registra.
    /// </summary>
    public class Stage1Controller : MonoBehaviour
    {
        [Header("Lección")]
        public int photosPerLesson = 10;
        public int lives = 3;
        public int xpPerHit = 10;
        public int lessonBonusXp = 20;
        [Tooltip("Respuestas del ejercicio 1 necesarias para desbloquear el ejercicio 2 (con 80% de acierto).")]
        public int unlockEx2Window = 10;

        [Header("UI")]
        public TMP_Text orderText;
        public TMP_Text statsText;
        public TMP_Text questionText;
        public GameObject photoCard;
        public RawImage photo;
        public AspectRatioFitter photoFitter;
        public RectTransform highlightBox;
        public TMP_Text loadingText;
        public GameObject questionRoot;
        public Button[] answerButtons;
        public GameObject feedbackRoot;
        public Image feedbackBar;
        public TMP_Text feedbackHeadline;
        public TMP_Text feedbackBody;
        public GameObject referenceRoot;
        public RawImage referencePhoto;
        public AspectRatioFitter referenceFitter;
        public TMP_Text referenceCaption;
        public Button continueButton;
        public GameObject summaryRoot;
        public TMP_Text summaryText;
        public Button nextLessonButton;
        public Button menuButton;
        public Button menuButtonTop;

        public Color correctColor = new Color(0.11f, 0.64f, 0.42f);
        public Color wrongColor = new Color(0.90f, 0.10f, 0.10f);

        static readonly string[] States = { "inmaduro", "optimo", "pasado" };

        // Rasgo clave por estado cuando la foto no trae uno propio. [Validar con el agrónomo de GHT]
        static readonly Dictionary<string, string> DefaultTrait = new Dictionary<string, string>
        {
            { "inmaduro", "Botón cerrado y con forma de punta; los sépalos siguen pegados al botón. Todavía no está en punto." },
            { "optimo", "Sépalos reflejados hacia abajo y pétalos externos que empiezan a soltarse, con la copa aún cerrada: punto de corte." },
            { "pasado", "Pétalos externos abiertos y la copa ya visible: pasó el punto para este pedido." },
        };

        List<PhotoEntry> _lesson = new List<PhotoEntry>();
        int _idx, _lives, _hits, _xpLesson, _exercise;
        float _shownAt;
        PhotoEntry _current;
        readonly List<string> _newBadges = new List<string>();
        readonly List<string> _notes = new List<string>();
        PlayerProfile P => ProfileStore.Current;

        void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int k = i;
                answerButtons[i].onClick.AddListener(() => Answer(k));
            }
            continueButton.onClick.AddListener(Next);
            nextLessonButton.onClick.AddListener(StartLesson);
            menuButton.onClick.AddListener(GoToMenu);
            if (menuButtonTop != null) menuButtonTop.onClick.AddListener(GoToMenu);
        }

        IEnumerator Start()
        {
            ShowOnly(null);
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Cargando fotos…";
            yield return PhotoManifest.Load();
            if (!PhotoManifest.Loaded || PhotoManifest.Entries.Count == 0)
            {
                loadingText.text = "No hay fotos. Revisa StreamingAssets/fotos/manifiesto.csv\n" + PhotoManifest.Error;
                yield break;
            }
            yield return new WaitForSeconds(0.3f);
            XRUi.PlaceInFront(transform, 1.5f, -0.05f);
            StartLesson();
        }

        public void GoToMenu() => SceneNav.Load(SceneNav.Menu);

        // ───────────── Lección ─────────────

        public void StartLesson()
        {
            _lesson = PickLesson(photosPerLesson);
            _idx = 0; _lives = lives; _hits = 0; _xpLesson = 0;
            _newBadges.Clear(); _notes.Clear();
            orderText.text = "Pedido activo: " + HarvestOrder.Current.Label;
            StartCoroutine(ShowQuestion());
        }

        /// <summary>Selección balanceada por estado + repetición espaciada (las fotos falladas pesan más).</summary>
        List<PhotoEntry> PickLesson(int n)
        {
            var all = PhotoManifest.Entries;
            var byState = States.ToDictionary(s => s, s => all.Where(e => e.estado == s).ToList());
            var stateWeight = new Dictionary<string, float> { { "inmaduro", 0.4f }, { "optimo", 0.4f }, { "pasado", 0.2f } };
            var chosen = new List<PhotoEntry>();
            for (int i = 0; i < n; i++)
            {
                var avail = States.Where(s => byState[s].Count > 0).ToList();
                if (avail.Count == 0) break;
                string st = WeightedPick(avail, s => stateWeight[s]);
                var pool = byState[st];
                var e = WeightedPick(pool, PhotoWeight);
                chosen.Add(e);
                pool.Remove(e); // sin repetir dentro de la lección
            }
            return chosen;
        }

        float PhotoWeight(PhotoEntry e)
        {
            int fails = 0, oks = 0;
            foreach (var h in P.history)
                if (h.photo == e.archivo) { if (h.correct) oks++; else fails++; }
            return Mathf.Clamp(1f + 2f * fails - 0.4f * oks, 0.3f, 6f);
        }

        static T WeightedPick<T>(List<T> items, System.Func<T, float> w)
        {
            float tot = 0f;
            foreach (var it in items) tot += w(it);
            float r = Random.value * tot;
            foreach (var it in items) { r -= w(it); if (r <= 0f) return it; }
            return items[items.Count - 1];
        }

        IEnumerator ShowQuestion()
        {
            _current = _lesson[_idx];
            _exercise = P.exercise2Unlocked && Random.value < 0.5f ? 2 : 1;
            ShowOnly(questionRoot);
            highlightBox.gameObject.SetActive(false);
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Cargando…";
            photo.texture = null;
            foreach (var b in answerButtons) b.interactable = false;
            UpdateStats();

            Texture tex = null;
            yield return PhotoManifest.LoadPhoto(_current.archivo, t => tex = t);
            loadingText.gameObject.SetActive(false);
            SetPhoto(photo, photoFitter, tex);

            if (_exercise == 1)
            {
                questionText.text = "¿Se corta para este pedido?";
                SetButtons("Sí, se corta", "No se corta");
            }
            else
            {
                questionText.text = "¿En qué estado está el botón?";
                SetButtons("Inmaduro", "En punto", "Pasado");
            }
            foreach (var b in answerButtons) b.interactable = true;
            _shownAt = Time.realtimeSinceStartup;
        }

        static void SetPhoto(RawImage img, AspectRatioFitter fit, Texture tex)
        {
            img.texture = tex;
            img.color = tex != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            if (fit != null && tex != null) fit.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height);
        }

        void SetButtons(params string[] labels)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                bool on = i < labels.Length;
                answerButtons[i].gameObject.SetActive(on);
                if (on) answerButtons[i].GetComponentInChildren<TMP_Text>().text = labels[i];
            }
        }

        void Answer(int k)
        {
            if (_current == null || !questionRoot.activeSelf) return;
            var order = HarvestOrder.Current;
            bool shouldCut = order.Accepts(_current.grado);
            bool correct;
            string answerTxt;
            if (_exercise == 1)
            {
                bool saysCut = k == 0;
                correct = saysCut == shouldCut;
                answerTxt = saysCut ? "cortar" : "no cortar";
            }
            else
            {
                answerTxt = States[Mathf.Clamp(k, 0, 2)];
                correct = answerTxt == _current.estado;
            }
            int ms = Mathf.RoundToInt((Time.realtimeSinceStartup - _shownAt) * 1000f);

            // Perfil
            var p = P;
            p.history.Add(new AnswerRecord
            {
                photo = _current.archivo, estado = _current.estado, exercise = _exercise, correct = correct, ms = ms,
                when = System.DateTime.Now.ToString("s")
            });
            if (p.history.Count > 600) p.history.RemoveRange(0, p.history.Count - 600);
            if (correct)
            {
                _hits++;
                p.streak++;
                p.bestStreak = Mathf.Max(p.bestStreak, p.streak);
                int gain = xpPerHit + (p.streak >= 3 ? 2 * Mathf.Min(p.streak, 5) : 0);
                p.xp += gain; _xpLesson += gain;
            }
            else
            {
                p.streak = 0;
                _lives--;
            }
            CheckBadgesAndUnlocks(p);
            ProfileStore.Save();

            TrainingLog.Log(new TrainingDecision
            {
                etapa = 1, nivel = _exercise, item = _current.archivo, pedido = order.Short,
                gradoReal = _current.grado, estadoReal = _current.estado,
                decisionUsuario = answerTxt, corto = _exercise == 1 && k == 0, acerto = correct, ms = ms
            });

            StartCoroutine(ShowFeedback(correct, shouldCut));
        }

        void CheckBadgesAndUnlocks(PlayerProfile p)
        {
            if (p.streak >= 10 && p.AddBadge("Ojo de cortador")) _newBadges.Add("Ojo de cortador (10 aciertos seguidos)");
            var opt = p.history.Where(h => h.estado == "optimo").ToList();
            if (opt.Count >= 10 && opt.Skip(opt.Count - 10).Count(h => h.correct) >= 9 && p.AddBadge("Domina el punto de corte"))
                _newBadges.Add("Domina el punto de corte (9 de 10 en «En punto»)");

            if (!p.exercise2Unlocked)
            {
                var ex1 = p.history.Where(h => h.exercise == 1).ToList();
                if (ex1.Count >= unlockEx2Window &&
                    ex1.Skip(ex1.Count - unlockEx2Window).Count(h => h.correct) >= Mathf.CeilToInt(unlockEx2Window * 0.8f))
                {
                    p.exercise2Unlocked = true;
                    _notes.Add("Nuevo ejercicio desbloqueado: «¿En qué estado está?»");
                }
            }
        }

        IEnumerator ShowFeedback(bool correct, bool shouldCut)
        {
            ShowOnly(feedbackRoot);
            UpdateStats();
            feedbackBar.color = correct ? correctColor : wrongColor;
            feedbackHeadline.text = correct ? "¡Correcto!" : "No es correcto";
            if (SoundFx.Instance != null) SoundFx.Play(correct ? SoundFx.Instance.good : SoundFx.Instance.bad, 0.6f);
            if (!correct) VoiceCoach.Say("s1_mal", 2);
            else if (P.streak == 5 || P.streak == 10) VoiceCoach.Say("s1_racha", 2);
            else VoiceCoach.SayOneOf(2, "s1_bien_1", "s1_bien_2", "s1_bien_3");

            var trait = string.IsNullOrEmpty(_current.rasgo) ? DefaultTrait[_current.estado] : _current.rasgo;
            feedbackBody.text =
                $"Estado real: <b>{ProfileStore.StateName(_current.estado)}</b> (grado ≈{_current.grado})\n" +
                $"Para este pedido: <b>{(shouldCut ? "SE CORTA" : "NO SE CORTA")}</b>\n\n" +
                $"<b>Rasgo clave:</b> {trait}" +
                (correct ? "" : $"\n\nVidas: {_lives}");

            // Resaltar la zona del rasgo si el manifiesto la trae
            if (_current.HasZone)
            {
                var z = _current.zona;
                highlightBox.anchorMin = new Vector2(z.x, 1f - z.y - z.height);
                highlightBox.anchorMax = new Vector2(z.x + z.width, 1f - z.y);
                highlightBox.offsetMin = highlightBox.offsetMax = Vector2.zero;
                highlightBox.gameObject.SetActive(true);
            }

            referenceRoot.SetActive(false);
            if (!correct)
            {
                var refE = PickReference(_current.estado, _current.archivo);
                if (refE != null)
                {
                    Texture t = null;
                    yield return PhotoManifest.LoadPhoto(refE.archivo, x => t = x);
                    SetPhoto(referencePhoto, referenceFitter, t);
                    referenceCaption.text = "Referencia: " + ProfileStore.StateName(_current.estado);
                    referenceRoot.SetActive(t != null);
                }
            }
            continueButton.GetComponentInChildren<TMP_Text>().text =
                _lives <= 0 ? "Ver resultado" : (_idx + 1 >= _lesson.Count ? "Terminar lección" : "Continuar");
        }

        PhotoEntry PickReference(string estado, string exclude)
        {
            var refs = PhotoManifest.Entries.Where(e => e.estado == estado && e.archivo != exclude && e.referencia).ToList();
            if (refs.Count == 0) refs = PhotoManifest.Entries.Where(e => e.estado == estado && e.archivo != exclude).ToList();
            return refs.Count == 0 ? null : refs[Random.Range(0, refs.Count)];
        }

        void Next()
        {
            _idx++;
            if (_lives <= 0 || _idx >= _lesson.Count) ShowSummary(_lives > 0);
            else StartCoroutine(ShowQuestion());
        }

        void ShowSummary(bool completed)
        {
            var p = P;
            if (completed)
            {
                p.lessonsCompleted++;
                p.xp += lessonBonusXp; _xpLesson += lessonBonusXp;
                if (p.lessonsCompleted == 1 && p.AddBadge("Primera lección")) _newBadges.Add("Primera lección");
            }
            bool was = p.stage2Unlocked;
            bool ok = ProfileStore.EvaluateUnlock(p, out var missing, out var progress);
            if (ok) p.stage2Unlocked = true;
            ProfileStore.Save();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(completed ? "<size=120%><b>¡Lección completada!</b></size>" : "<size=120%><b>Te quedaste sin vidas</b></size>\nRepite la lección: así se aprende.");
            sb.AppendLine();
            sb.AppendLine($"Aciertos: <b>{_hits} de {_idx}</b>    XP ganado: <b>+{_xpLesson}</b>    Nivel {p.Level}");
            sb.AppendLine($"Mejor racha: {p.bestStreak}");
            foreach (var n in _notes) sb.AppendLine("<color=#9FE3B8>" + n + "</color>");
            foreach (var b in _newBadges) sb.AppendLine("<color=#FFD54F>Insignia: " + b + "</color>");
            sb.AppendLine();
            sb.AppendLine("<b>Etapa 2 «Cosecha con el carro»</b>");
            sb.AppendLine(progress);
            if (p.stage2Unlocked && !was) sb.AppendLine("<color=#9FE3B8><b>¡Desbloqueaste la etapa 2!</b></color>");
            else if (p.stage2Unlocked) sb.AppendLine("<color=#9FE3B8>Desbloqueada</color>");
            else sb.AppendLine(missing);
            summaryText.text = sb.ToString();
            ShowOnly(summaryRoot);
            UpdateStats();
        }

        void UpdateStats()
        {
            var p = P;
            int shown = Mathf.Min(_idx + 1, Mathf.Max(1, _lesson.Count));
            statsText.text = $"{p.id}   ·   Foto {shown}/{_lesson.Count}   ·   Vidas {_lives}/{lives}   ·   Racha {p.streak}   ·   XP {p.xp} (nivel {p.Level})";
        }

        void ShowOnly(GameObject go)
        {
            questionRoot.SetActive(go == questionRoot);
            feedbackRoot.SetActive(go == feedbackRoot);
            summaryRoot.SetActive(go == summaryRoot);
            if (photoCard != null) photoCard.SetActive(go != summaryRoot);
        }
    }
}
