using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Simulador en dos etapas, con fotos reales etiquetadas por el experto:
    ///   Etapa 1 · Identificación: ¿de qué color es? ¿de qué tamaño?
    ///   Etapa 2 · Corte: ¿en qué estado está? ¿se corta?
    /// YOLO no participa aquí: la referencia es la etiqueta del agrónomo.
    /// </summary>
    public class SimulatorController : MonoBehaviour
    {
        [Header("Datos")]
        public RoseClassCatalog catalog;
        public AttributeCatalog colorCatalog;
        public AttributeCatalog sizeCatalog;
        [Tooltip("Largos de corte (Etapa 2). Se pregunta cuando el operario decide cortar y la foto tiene largo etiquetado.")]
        public AttributeCatalog lengthCatalog;
        public SimulatorDataset dataset;
        public int rosesPerSession = 12;
        [Tooltip("Porcentaje de aciertos en la Etapa 1 para recomendar pasar a la Etapa 2.")]
        [Range(0f, 1f)] public float passThreshold = 0.8f;

        [Header("Escena")]
        public RoseSpecimen cardPrefab;
        [Tooltip("Estaciones con tarjetas de foto (Etapa 1, o Etapa 2 sin modelos 3D).")]
        public List<Transform> beds = new List<Transform>();
        [Tooltip("Puntos de la cama donde se plantan las rosas 3D (Etapa 2).")]
        public List<Transform> roseSlots = new List<Transform>();
        [Tooltip("Escala de los modelos 3D (1 = tamaño original del archivo).")]
        public float roseScale = 1.5f;
        public DecisionPanel decisionPanel;
        public FeedbackPanel feedbackPanel;
        public TMP_Text statusText;

        readonly List<RoseItem> _cards = new List<RoseItem>();
        TrainingStage _stage;
        RoseItem _current;
        float _t0;
        bool _busy;

        // Conteos
        int _rosesDone;          // rosas evaluadas
        int _questions;          // preguntas respondidas
        int _correctAnswers;     // preguntas acertadas
        int _falseCuts;          // Etapa 2: falsos óptimos (cortaría una rosa que no está en punto)
        int _lengthErrors;       // Etapa 2: largo de corte equivocado
        int _missedCuts;         // Etapa 2: óptimas no reconocidas (dejaría una rosa en punto)

        // Respuestas de la rosa en curso (Etapa 1)
        AttributeOption _colorAnswer, _sizeAnswer;

        string StageName => _stage == TrainingStage.Identificacion ? "Etapa 1 · Identificación" : "Etapa 2 · Corte";
        string StageLogId => _stage == TrainingStage.Identificacion ? "etapa1_identificacion" : "etapa2_corte";

        void Start()
        {
            _stage = OperatorSession.Stage;
            SessionLogger.BeginSession(StageLogId);
            if (decisionPanel != null) decisionPanel.Hide();
            if (feedbackPanel != null) feedbackPanel.Hide();
            BuildBeds();
        }

        // ───────────── Camas ─────────────

        bool HasColor(RoseSample s) => colorCatalog != null && colorCatalog.GetById(s.colorId) != null;
        bool HasSize(RoseSample s) => sizeCatalog != null && sizeCatalog.GetById(s.sizeId) != null;
        bool HasLength(RoseSample s) => lengthCatalog != null && lengthCatalog.GetById(s.lengthId) != null;
        bool HasClass(RoseSample s) => catalog != null && catalog.GetById(s.classId) != null;

        void BuildBeds()
        {
            // Etapa 2 con modelos 3D: rosas plantadas en la cama
            if (_stage == TrainingStage.Corte && dataset != null && roseSlots.Count > 0)
            {
                var models = dataset.samples.FindAll(s => s.model != null && HasClass(s));
                if (models.Count > 0)
                {
                    foreach (var b in beds) if (b != null && b.parent != null) b.parent.gameObject.SetActive(false);
                    int n3 = Mathf.Min(rosesPerSession, roseSlots.Count);
                    // Repartir los modelos de forma equilibrada y en orden aleatorio
                    var order = new List<RoseSample>();
                    for (int i = 0; i < n3; i++) order.Add(models[i % models.Count]);
                    Shuffle(order);
                    for (int i = 0; i < n3; i++)
                    {
                        var go = new GameObject("Rosa3D_" + (i + 1));
                        go.transform.SetParent(roseSlots[i], false);
                        go.transform.localScale = Vector3.one * roseScale;
                        var item = go.AddComponent<RoseSpecimen3D>();
                        item.Setup(order[i]);
                        item.Selected += OnRoseSelected;
                        _cards.Add(item);
                    }
                    UpdateStatus();
                    return;
                }
            }

            if (dataset == null || dataset.samples.Count == 0 || beds.Count == 0 || cardPrefab == null)
            {
                SetStatus("Falta configurar: fotos del dataset (menú Rosas XR > 2), camas o prefab de tarjeta.");
                return;
            }

            var pool = dataset.samples.FindAll(s =>
                s.photo != null &&
                (_stage == TrainingStage.Identificacion ? (HasColor(s) || HasSize(s)) : HasClass(s)));

            if (pool.Count == 0)
            {
                SetStatus(_stage == TrainingStage.Identificacion
                    ? "No hay fotos con color o tamaño etiquetados (Fotos/etiquetas.csv)."
                    : "No hay fotos con estado de corte (carpetas Fotos/<estado>/).");
                return;
            }

            Shuffle(pool);
            int n = Mathf.Min(rosesPerSession, pool.Count);
            for (int i = 0; i < n; i++)
            {
                var card = Instantiate(cardPrefab, beds[i % beds.Count]);
                card.Setup(pool[i]);
                card.Selected += OnRoseSelected;
                _cards.Add(card);
            }
            UpdateStatus();
        }

        void OnRoseSelected(RoseItem card)
        {
            if (_busy) return;
            _busy = true;
            _current = card;
            _t0 = Time.realtimeSinceStartup;

            if (_stage == TrainingStage.Identificacion) AskColor();
            else decisionPanel.Show("¿En qué estado está esta rosa?", card.Sample.photo, OnCutDecision);
        }

        // ───────────── Etapa 1: color y tamaño ─────────────

        void AskColor()
        {
            _colorAnswer = null;
            _sizeAnswer = null;
            if (HasColor(_current.Sample))
                decisionPanel.Show(_current.Sample.photo, colorCatalog, opt => { _colorAnswer = opt; AskSize(); });
            else
                AskSize();
        }

        void AskSize()
        {
            if (HasSize(_current.Sample))
                decisionPanel.Show(_current.Sample.photo, sizeCatalog, opt => { _sizeAnswer = opt; FinishIdentification(); });
            else
                FinishIdentification();
        }

        void FinishIdentification()
        {
            var s = _current.Sample;
            int ms = Mathf.RoundToInt((Time.realtimeSinceStartup - _t0) * 1000f);
            var sb = new StringBuilder();
            bool allOk = true;

            if (_colorAnswer != null)
                allOk &= Grade(colorCatalog, colorCatalog.GetById(s.colorId), _colorAnswer, "etapa1_color", ms, sb);
            if (_sizeAnswer != null)
                allOk &= Grade(sizeCatalog, sizeCatalog.GetById(s.sizeId), _sizeAnswer, "etapa1_tamano", ms, sb);

            _rosesDone++;
            _current.MarkResult(allOk);
            feedbackPanel.Show(allOk, allOk ? "¡Correcto!" : "Revisa estos puntos", sb.ToString().TrimEnd(), OnContinue);
            UpdateStatus();
        }

        bool Grade(AttributeCatalog cat, AttributeOption truth, AttributeOption answer, string mode, int ms, StringBuilder sb)
        {
            bool ok = answer.id == truth.id;
            _questions++;
            if (ok) _correctAnswers++;

            SessionLogger.Log(new DecisionRecord
            {
                mode = mode,
                itemId = _current.Sample.sampleId,
                operatorAnswer = answer.id,
                reference = truth.id,
                referenceSource = "experto",
                modelConfidence = -1f,
                match = ok,
                responseMs = ms
            });

            if (ok)
            {
                sb.AppendLine($"{cat.attributeName}: <b>{truth.displayName}</b>  <color=#7FD8A8>correcto</color>");
            }
            else
            {
                sb.AppendLine($"{cat.attributeName}: respondiste {answer.displayName}, era <b>{truth.displayName}</b>");
                if (!string.IsNullOrWhiteSpace(truth.explanation))
                    sb.AppendLine($"<size=85%>{truth.explanation}</size>");
            }
            sb.AppendLine();
            return ok;
        }

        // ───────────── Etapa 2: corte ─────────────

        void OnCutDecision(RoseClass chosen)
        {
            var truth = catalog.GetById(_current.Sample.classId);
            bool ok = chosen.id == truth.id;
            bool cutError = chosen.shouldCut != truth.shouldCut;

            _rosesDone++;
            _questions++;
            if (ok) _correctAnswers++;
            if (cutError && chosen.shouldCut) _falseCuts++;
            _pendingCutOk = ok;
            if (cutError && !chosen.shouldCut) _missedCuts++;

            SessionLogger.Log(new DecisionRecord
            {
                mode = "etapa2_corte",
                itemId = _current.Sample.sampleId,
                operatorAnswer = chosen.id,
                reference = truth.id,
                referenceSource = "experto",
                modelConfidence = -1f,
                match = ok,
                responseMs = Mathf.RoundToInt((Time.realtimeSinceStartup - _t0) * 1000f)
            });

            // Si decidió cortar y la foto tiene el largo correcto etiquetado: preguntar a qué largo corta.
            if (chosen.shouldCut && truth.shouldCut && HasLength(_current.Sample))
            {
                _pendingChosen = chosen;
                var options = lengthCatalog;
                if (_current is RoseSpecimen3D rose3D)
                {
                    // Solo los largos que caben en el tallo, marcados sobre la rosa
                    float stem = (rose3D.StemTopY - rose3D.transform.position.y) * 100f;
                    options = ScriptableObject.CreateInstance<AttributeCatalog>();
                    options.attributeName = lengthCatalog.attributeName;
                    options.question = "¿A qué largo cortas? Mira las marcas en el tallo";
                    options.options = lengthCatalog.options.FindAll(o => int.TryParse(o.id, out int cm) && cm < stem - 2f);
                    if (options.options.Find(o => o.id == _current.Sample.lengthId) == null) options = lengthCatalog;
                    rose3D.ShowCutMarks(options.options);
                }
                decisionPanel.Show(_current.Sample.photo, options, OnLengthDecision);
                return;
            }
            ShowCutFeedback(chosen, null);
        }

        RoseClass _pendingChosen;
        bool _pendingCutOk;

        void OnLengthDecision(AttributeOption length)
        {
            ShowCutFeedback(_pendingChosen, length);
        }

        void ShowCutFeedback(RoseClass chosen, AttributeOption lengthAnswer)
        {
            var truth = catalog.GetById(_current.Sample.classId);
            bool ok = _pendingCutOk;
            bool cutError = chosen.shouldCut != truth.shouldCut;

            string lengthLine = null;
            if (lengthAnswer != null)
            {
                var lengthTruth = lengthCatalog.GetById(_current.Sample.lengthId);
                bool lengthOk = lengthAnswer.id == lengthTruth.id;
                _questions++;
                if (lengthOk) _correctAnswers++;
                else { ok = false; _lengthErrors++; }

                SessionLogger.Log(new DecisionRecord
                {
                    mode = "etapa2_largo",
                    itemId = _current.Sample.sampleId,
                    operatorAnswer = lengthAnswer.id,
                    reference = lengthTruth.id,
                    referenceSource = "experto",
                    modelConfidence = -1f,
                    match = lengthOk,
                    responseMs = Mathf.RoundToInt((Time.realtimeSinceStartup - _t0) * 1000f)
                });

                lengthLine = lengthOk
                    ? $"Largo de corte: <b>{lengthTruth.displayName}</b>  <color=#7FD8A8>correcto</color>"
                    : $"Largo de corte: respondiste {lengthAnswer.displayName}, era <b>{lengthTruth.displayName}</b>" +
                      (string.IsNullOrWhiteSpace(lengthTruth.explanation) ? "" : $"\n<size=85%>{lengthTruth.explanation}</size>");
            }

            _current.MarkResult(ok);

            var sb = new StringBuilder();
            sb.AppendLine($"Estado correcto: <b>{truth.displayName}</b> ({(truth.shouldCut ? "se corta" : "no se corta")})");
            if (!ok) sb.AppendLine($"Tu respuesta: {chosen.displayName}");
            if (cutError)
            {
                sb.AppendLine(chosen.shouldCut
                    ? "<color=#FF8A80>Falso óptimo: con tu respuesta, esta rosa se habría cortado fuera de punto.</color>"
                    : "<color=#FF8A80>Con tu respuesta, esta rosa se habría quedado sin cortar.</color>");
            }
            if (lengthLine != null) sb.AppendLine(lengthLine);
            if (!string.IsNullOrWhiteSpace(truth.explanation))
            {
                sb.AppendLine();
                sb.Append(truth.explanation);
            }

            feedbackPanel.Show(ok, ok ? "¡Correcto!" : "No es correcto", sb.ToString(), OnContinue);
            UpdateStatus();
        }

        // ───────────── Cierre ─────────────

        void OnContinue()
        {
            _busy = false;
            _current = null;
            if (_cards.Count > 0 && _rosesDone >= _cards.Count) ShowSummary();
        }

        void ShowSummary()
        {
            _busy = true;
            float rate = _questions > 0 ? (float)_correctAnswers / _questions : 0f;
            int pct = Mathf.RoundToInt(rate * 100f);
            string log = $"\n\n<size=70%>Registro guardado en:\n{SessionLogger.CurrentPath}</size>";

            if (_stage == TrainingStage.Identificacion)
            {
                bool passed = rate >= passThreshold;
                string body = $"Respuestas correctas: {_correctAnswers} de {_questions} ({pct}%)\n\n" +
                              (passed
                                  ? "Ya reconoces bien el color y el tamaño. Puedes pasar a la Etapa 2: decidir el corte."
                                  : $"Para pasar a la Etapa 2 se recomienda al menos {Mathf.RoundToInt(passThreshold * 100f)}%. Repite la etapa.") +
                              log;
                feedbackPanel.Show(passed ? (bool?)true : null, "Etapa 1 terminada", body,
                    passed ? GoToStage2 : (System.Action)SceneNav.ReloadCurrent,
                    passed ? "Ir a la Etapa 2" : "Repetir Etapa 1");
            }
            else
            {
                feedbackPanel.Show(null, "Etapa 2 terminada",
                    $"Aciertos: {_correctAnswers} de {_questions} ({pct}%)\n" +
                    $"Falsos óptimos (cortarías fuera de punto): {_falseCuts}\n" +
                    $"Largos de corte equivocados: {_lengthErrors}\n" +
                    $"Óptimas no reconocidas (dejarías sin cortar): {_missedCuts}" + log,
                    SceneNav.ReloadCurrent, "Nueva sesión");
            }
        }

        void GoToStage2()
        {
            OperatorSession.Stage = TrainingStage.Corte;
            SceneNav.ReloadCurrent();
        }

        void UpdateStatus() =>
            SetStatus($"{StageName}  ·  {OperatorSession.OperatorId}  ·  Rosas {_rosesDone}/{_cards.Count}  ·  Aciertos {_correctAnswers}/{_questions}");

        void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }

        public void GoToMenu() => SceneNav.Load(SceneNav.Menu);
        public void Restart() => SceneNav.ReloadCurrent();

        static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
