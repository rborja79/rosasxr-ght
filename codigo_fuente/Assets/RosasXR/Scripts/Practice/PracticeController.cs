using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Modo 2. El operario mira una rosa real y pulsa "Evaluar rosa": se fija la detección
    /// más centrada, el operario decide primero y solo después se revela lo que dijo el modelo.
    /// </summary>
    public class PracticeController : MonoBehaviour
    {
        [Header("Datos")]
        public RoseClassCatalog catalog;
        [Tooltip("Se usa la primera fuente activa que tenga detecciones (modelo real primero, simuladas después).")]
        public List<DetectionSource> sources = new List<DetectionSource>();

        [Header("Escena")]
        public DecisionPanel decisionPanel;
        public FeedbackPanel feedbackPanel;
        public TMP_Text statusText;
        public TMP_Text liveToggleLabel;
        public Transform targetMarker;
        [Tooltip("Mira central (modo clasificador). Se pone amarilla mientras el operario responde.")]
        public AimReticle reticle;
        [Tooltip("Para guardar fotos de la cámara del visor (datos para el equipo del modelo).")]
        public CropCapture capture;
        public DetectionLabel labelPrefab;

        [Header("Criterios")]
        public float minConfidence = 0.25f;
        [Tooltip("Umbral del motor de decisión del equipo de modelo: si la clase más probable no lo alcanza → REVISIÓN HUMANA.")]
        [Range(0.5f, 1f)] public float decisionThreshold = 0.80f;
        [Tooltip("Ángulo máximo entre la mirada y la rosa para considerarla 'la que estoy mirando'.")]
        public float maxAimAngle = 30f;
        public bool showLiveDetections;
        [Tooltip("Modo en vivo: el resultado del modelo se muestra bajo la mira de forma continua (sin cuestionario).")]
        public bool liveResultMode = true;

        static readonly List<RoseDetection> Empty = new List<RoseDetection>();
        readonly List<DetectionLabel> _pool = new List<DetectionLabel>();
        RoseDetection _target;
        bool _busy;
        float _t0;
        int _evaluated, _agreements;

        void Start()
        {
            SessionLogger.BeginSession("practica_yolo");
            if (decisionPanel != null) decisionPanel.Hide();
            if (feedbackPanel != null) feedbackPanel.Hide();
            if (targetMarker != null) targetMarker.gameObject.SetActive(false);
            UpdateLiveLabel();
            SetStatus($"{OperatorSession.OperatorId}  ·  Centra una rosa en el recuadro y pulsa \"Evaluar rosa\"");
        }

        void Update()
        {
            UpdateLiveDetections();
            if (liveResultMode) UpdateLiveResult();
        }

        // Resultado continuo bajo la mira: estado, probabilidad y recomendación
        void UpdateLiveResult()
        {
            if (reticle == null || reticle.hint == null) return;
            var list = CurrentDetections();
            if (list.Count == 0 || list[0].confidence < minConfidence)
            {
                reticle.hint.text = "Encuadra el botón completo";
                reticle.SetHighlight(null);
                return;
            }
            var d = list[0];
            var rc = catalog.GetByModelIndex(d.classIndex);
            int pct = Mathf.RoundToInt(d.confidence * 100f);
            bool sure = d.confidence >= decisionThreshold;
            string rec = sure ? (rc != null && !string.IsNullOrEmpty(rc.recommendation) ? rc.recommendation : "") : "REVISIÓN HUMANA";
            string name = rc != null ? rc.displayName : "clase " + d.classIndex;
            reticle.hint.text = $"<b>{name}</b> {pct}%\n<size=75%>{rec}</size>";
            reticle.SetHighlight(sure && rc != null ? rc.color : (Color?)new Color(1f, 0.72f, 0.3f));
        }

        IReadOnlyList<RoseDetection> CurrentDetections()
        {
            foreach (var s in sources)
            {
                if (s == null || !s.isActiveAndEnabled) continue;
                var list = s.Latest;
                if (list != null && list.Count > 0) return list;
            }
            return Empty;
        }

        // Conectado al botón "Evaluar rosa".
        public void EvaluateTarget()
        {
            if (_busy) return;

            if (!TryPickTarget(out _target))
            {
                SetStatus("No reconozco ninguna rosa. Centra la rosa en el recuadro, acércate y vuelve a intentar.");
                return;
            }

            _busy = true;
            _t0 = Time.realtimeSinceStartup;
            HideLabels(); // que las etiquetas en vivo no "soplen" la respuesta
            if (reticle != null) reticle.SetLocked(true);

            if (targetMarker != null)
            {
                targetMarker.gameObject.SetActive(_target.hasWorldPosition);
                if (_target.hasWorldPosition) targetMarker.position = _target.worldPosition;
            }

            decisionPanel.Show("¿Qué estado tiene la rosa marcada?", null, OnDecision);
        }

        bool TryPickTarget(out RoseDetection best)
        {
            best = default;
            var cam = Camera.main;
            float bestScore = float.MaxValue;
            bool found = false;

            foreach (var d in CurrentDetections())
            {
                if (d.confidence < minConfidence) continue;

                float score;
                if (d.hasWorldPosition && cam != null)
                {
                    score = Vector3.Angle(cam.transform.forward, d.worldPosition - cam.transform.position);
                    if (score > maxAimAngle) continue;
                }
                else if (d.normalizedCenter.x >= 0f)
                {
                    score = Vector2.Distance(d.normalizedCenter, new Vector2(0.5f, 0.5f)) * 100f;
                }
                else
                {
                    score = (1f - d.confidence) * 100f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = d;
                    found = true;
                }
            }
            return found;
        }

        void OnDecision(RoseClass chosen)
        {
            var model = catalog.GetByModelIndex(_target.classIndex);
            string modelId = model != null ? model.id : $"clase_{_target.classIndex}";
            string modelName = model != null ? model.displayName : $"clase {_target.classIndex} (no está en el catálogo)";
            bool agree = model != null && model.id == chosen.id;
            int pct = Mathf.RoundToInt(_target.confidence * 100f);

            _evaluated++;
            if (agree) _agreements++;

            SessionLogger.Log(new DecisionRecord
            {
                mode = "practica_yolo",
                itemId = $"det_{_evaluated:000}",
                operatorAnswer = chosen.id,
                reference = modelId,
                referenceSource = "modelo",
                modelConfidence = _target.confidence,
                match = agree,
                responseMs = Mathf.RoundToInt((Time.realtimeSinceStartup - _t0) * 1000f)
            });

            bool uncertain = _target.confidence < decisionThreshold;
            string recommendation = uncertain
                ? "REVISIÓN HUMANA: apertura incierta"
                : (model != null && !string.IsNullOrEmpty(model.recommendation) ? model.recommendation : modelName.ToUpperInvariant());

            var sb = new StringBuilder();
            sb.AppendLine($"Tu respuesta: <b>{chosen.displayName}</b>");
            sb.AppendLine($"Modelo: <b>{modelName}</b> ({pct}%)");
            if (_target.probabilities != null && _target.probabilities.Length > 1)
                sb.AppendLine($"<size=80%>{FormatProbabilities(_target.probabilities)}</size>");
            sb.AppendLine();
            sb.AppendLine($"Recomendación del sistema: <b>{recommendation}</b>");
            if (uncertain)
                sb.AppendLine($"<color=#FFB74D>Ningún estado supera {Mathf.RoundToInt(decisionThreshold * 100f)}%: caso para el experto.</color>");
            else if (!agree)
                sb.AppendLine("El desacuerdo quedó registrado para revisión del experto.");

            sb.Append("\n<size=75%>El modelo es una segunda opinión. Color y lugar de corte se verifican manualmente.</size>");

            feedbackPanel.Show(agree, agree ? "Coincides con el modelo" : "El modelo opina distinto", sb.ToString(), OnContinue);
            UpdateStatus();
        }

        string FormatProbabilities(float[] probs)
        {
            var parts = new List<string>();
            for (int i = 0; i < probs.Length; i++)
            {
                var rc = catalog.GetByModelIndex(i);
                parts.Add($"{(rc != null ? rc.displayName : "clase " + i)} {Mathf.RoundToInt(probs[i] * 100f)}%");
            }
            return string.Join("  ·  ", parts);
        }

        void OnContinue()
        {
            _busy = false;
            if (targetMarker != null) targetMarker.gameObject.SetActive(false);
            if (reticle != null) reticle.SetLocked(false);
        }

        // Conectado al botón "Capturar foto".
        public void CaptureCrop()
        {
            if (capture == null) { SetStatus("No hay componente de captura configurado."); return; }
            capture.Capture();
            SetStatus(capture.LastSavedPath != null
                ? $"Foto {capture.Count} guardada  ·  {System.IO.Path.GetFileName(capture.LastSavedPath)}"
                : "La cámara aún no está lista.");
        }

        // Conectado al botón "Ver detecciones en vivo".
        public void ToggleLiveDetections()
        {
            showLiveDetections = !showLiveDetections;
            UpdateLiveLabel();
            if (!showLiveDetections) HideLabels();
        }

        void UpdateLiveDetections()
        {
            if (!showLiveDetections || _busy || labelPrefab == null) return;

            int used = 0;
            foreach (var d in CurrentDetections())
            {
                if (!d.hasWorldPosition || d.confidence < minConfidence) continue;

                if (used >= _pool.Count) _pool.Add(Instantiate(labelPrefab, transform));
                var lbl = _pool[used++];
                var rc = catalog.GetByModelIndex(d.classIndex);
                lbl.gameObject.SetActive(true);
                lbl.Set($"{(rc != null ? rc.displayName : "clase " + d.classIndex)} {Mathf.RoundToInt(d.confidence * 100f)}%",
                        rc != null ? rc.color : Color.gray,
                        d.worldPosition + Vector3.up * 0.08f);
            }
            for (int i = used; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        }

        void HideLabels()
        {
            foreach (var l in _pool) l.gameObject.SetActive(false);
        }

        void UpdateLiveLabel()
        {
            if (liveToggleLabel != null)
                liveToggleLabel.text = showLiveDetections ? "Ocultar detecciones" : "Ver detecciones en vivo";
        }

        void UpdateStatus() =>
            SetStatus($"{OperatorSession.OperatorId}  ·  Evaluadas {_evaluated}  ·  Coincidencias con el modelo {_agreements}");

        void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }

        public void GoToMenu() => SceneNav.Load(SceneNav.Menu);
    }
}
