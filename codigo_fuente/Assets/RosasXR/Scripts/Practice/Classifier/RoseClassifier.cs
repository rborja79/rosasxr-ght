using System;
using Unity.InferenceEngine; // Si el proyecto usa Sentis 2.1 en vez de Inference Engine, cambiar por: using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// Conecta el modelo CLASIFICADOR (ONNX) con la interfaz.
    ///
    ///   cámara (CameraFeed) → recorte central (la mira) → redimensión → tensor → modelo
    ///   → probabilidades por clase → ExternalDetectionSource → PracticeController
    ///
    /// Para conectar el modelo de sus compañeros:
    ///   1. Arrastrar el .onnx a Unity (se importa como ModelAsset) y asignarlo en "model".
    ///   2. Ajustar inputSize, layout y normalización a como se entrenó (ver README).
    ///   3. En RoseClassCatalog, modelClassIndex = índice de salida de cada clase.
    /// </summary>
    public class RoseClassifier : MonoBehaviour
    {
        public enum Layout { NCHW, NHWC }

        /// <summary>Cómo interpretar la salida del modelo.</summary>
        public enum OutputType
        {
            Auto,          // 2 valores → Ordinal; si ya suman 1 → Probabilidades; si no → Softmax
            Probabilities, // la salida ya son probabilidades por clase
            Logits,        // logits por clase → softmax
            Ordinal        // 2 logits ordinales (CORAL / umbrales): q = sigmoid; p = [1-q1, q1-q2, q2]
        }

        [Header("Modelo")]
        public ModelAsset model;
        [Tooltip("Lado de la imagen de entrada (YOLO-cls de Ultralytics: 224 por defecto).")]
        public int inputSize = 224;
        public Layout layout = Layout.NCHW;
        [Tooltip("Media por canal RGB (0..1). MobileNetV3 con pesos ImageNet: 0.485, 0.456, 0.406. YOLO-cls: 0,0,0.")]
        public Vector3 mean = new Vector3(0.485f, 0.456f, 0.406f);
        [Tooltip("Desviación por canal RGB. MobileNetV3 con pesos ImageNet: 0.229, 0.224, 0.225. YOLO-cls: 1,1,1.")]
        public Vector3 std = new Vector3(0.229f, 0.224f, 0.225f);
        [Tooltip("Ordinal = modelo MobileNetV3 + umbrales del equipo (2 logits). Auto lo detecta por el tamaño de la salida.")]
        public OutputType outputType = OutputType.Auto;
        public BackendType backend = BackendType.GPUCompute;

        [Header("Entrada")]
        public CameraFeed feed;
        [Tooltip("Fracción del lado corto de la imagen que se recorta en el centro (debe coincidir con la mira).")]
        [Range(0.1f, 1f)] public float cropFraction = 0.45f;
        [Tooltip("Invertir verticalmente al leer la GPU. Si la vista previa sale de cabeza, cambiar este valor.")]
        public bool flipVertical = true;

        [Header("Salida")]
        public ExternalDetectionSource output;
        [Tooltip("Clasificaciones por segundo. 3-5 es suficiente y cuida la batería del visor.")]
        public float inferencesPerSecond = 4f;
        [Tooltip("Índices de clase que significan 'no hay rosa' (ej. fondo). Con esas clases no se publica nada.")]
        public int[] ignoreClassIndices = new int[0];
        [Tooltip("Distancia a la que se ubica la rosa clasificada frente a la mirada (para etiquetas en vivo).")]
        public float aimDistance = 0.6f;

        [Header("Depuración")]
        [Tooltip("Muestra lo que ve el modelo (el recorte). Útil para calibrar la mira y para la demo.")]
        public RawImage cropPreview;

        public int LastClass { get; private set; } = -1;
        /// <summary>Tiempo de la última inferencia (desde que se programa hasta que se leen los resultados).</summary>
        public float LastInferenceMs { get; private set; }
        public float LastConfidence { get; private set; }
        public float[] LastProbabilities { get; private set; }
        public event Action<int, float, float[]> Classified;

        Worker _worker;
        RenderTexture _crop;
        float[] _input;
        float _next;
        bool _pending;
        int _runs;

        void Start()
        {
            if (model == null)
            {
                Debug.LogWarning("[RosasXR] RoseClassifier sin modelo: asigna el archivo .onnx en el campo 'model'.");
                if (cropPreview != null) cropPreview.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            if (output == null) output = ExternalDetectionSource.Instance;
            if (output == null) output = FindFirstObjectByType<ExternalDetectionSource>();

            _worker = new Worker(ModelLoader.Load(model), backend);
            _crop = new RenderTexture(inputSize, inputSize, 0, RenderTextureFormat.ARGB32);
            _crop.Create();
            _input = new float[3 * inputSize * inputSize];
            if (cropPreview != null) cropPreview.texture = _crop;
        }

        void Update()
        {
            if (_pending || feed == null || !feed.IsReady || Time.time < _next) return;
            _next = Time.time + 1f / Mathf.Max(0.5f, inferencesPerSecond);
            Capture();
        }

        void Capture()
        {
            var src = feed.Current;
            if (src == null) return;

            // Recorte cuadrado centrado + redimensión en GPU.
            float side = cropFraction * Mathf.Min(src.width, src.height);
            var scale = new Vector2(side / src.width, side / src.height);
            var offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);
            Graphics.Blit(src, _crop, scale, offset);

            _pending = true;
            AsyncGPUReadback.Request(_crop, 0, TextureFormat.RGBA32, OnReadback);
        }

        void OnReadback(AsyncGPUReadbackRequest req)
        {
            if (this == null || _worker == null) return;
            if (req.hasError) { _pending = false; return; }

            var data = req.GetData<Color32>();
            int n = inputSize * inputSize;
            for (int y = 0; y < inputSize; y++)
            {
                int dstRow = flipVertical ? inputSize - 1 - y : y;
                for (int x = 0; x < inputSize; x++)
                {
                    var c = data[y * inputSize + x];
                    float r = (c.r / 255f - mean.x) / std.x;
                    float g = (c.g / 255f - mean.y) / std.y;
                    float b = (c.b / 255f - mean.z) / std.z;
                    int p = dstRow * inputSize + x;
                    if (layout == Layout.NCHW)
                    {
                        _input[p] = r; _input[n + p] = g; _input[2 * n + p] = b;
                    }
                    else
                    {
                        int k = p * 3;
                        _input[k] = r; _input[k + 1] = g; _input[k + 2] = b;
                    }
                }
            }
            RunModel();
        }

        async void RunModel()
        {
            try
            {
                var shape = layout == Layout.NCHW
                    ? new TensorShape(1, 3, inputSize, inputSize)
                    : new TensorShape(1, inputSize, inputSize, 3);

                float t0 = Time.realtimeSinceStartup;
                using var input = new Tensor<float>(shape, _input);
                _worker.Schedule(input);
                var outTensor = _worker.PeekOutput() as Tensor<float>;
                if (outTensor == null) return;

                using var cpu = await outTensor.ReadbackAndCloneAsync();
                if (this == null) return;
                LastInferenceMs = (Time.realtimeSinceStartup - t0) * 1000f;
                if (++_runs % 20 == 1) Debug.Log($"[RosasXR] Inferencia: {LastInferenceMs:0} ms (corrida {_runs})");
                Publish(cpu.DownloadToArray());
            }
            catch (Exception e)
            {
                Debug.LogError($"[RosasXR] Error al ejecutar el clasificador: {e.Message}");
            }
            finally
            {
                _pending = false;
            }
        }

        void Publish(float[] scores)
        {
            if (scores == null || scores.Length == 0) return;
            var probs = ToProbabilities(scores);

            int best = 0;
            for (int i = 1; i < probs.Length; i++) if (probs[i] > probs[best]) best = i;

            LastClass = best;
            LastConfidence = probs[best];
            LastProbabilities = probs;
            Classified?.Invoke(best, probs[best], probs);

            if (output == null) return;
            output.BeginFrame();
            if (Array.IndexOf(ignoreClassIndices, best) < 0)
            {
                var cam = Camera.main;
                if (cam != null) output.Add(best, probs[best], cam.transform.position + cam.transform.forward * aimDistance, probs);
                else output.Add(best, probs[best], new Vector2(0.5f, 0.5f), probs);
            }
            output.EndFrame();
        }

        float[] ToProbabilities(float[] scores)
        {
            var type = outputType;
            if (type == OutputType.Auto)
                type = scores.Length == 2 ? OutputType.Ordinal
                     : LooksLikeProbabilities(scores) ? OutputType.Probabilities
                     : OutputType.Logits;

            switch (type)
            {
                case OutputType.Ordinal: return Ordinal(scores);
                case OutputType.Logits: return Softmax(scores);
                default: return scores;
            }
        }

        /// <summary>
        /// Umbrales ordinales (K-1 logits) → K probabilidades.
        /// q_k = sigmoid(logit_k) = P(Y >= k+1);  P(Y=0) = 1 - q_1;  P(Y=k) = q_k - q_{k+1};  P(Y=K-1) = q_{K-1}.
        /// </summary>
        static float[] Ordinal(float[] logits)
        {
            int k = logits.Length;
            var q = new float[k];
            for (int i = 0; i < k; i++) q[i] = 1f / (1f + Mathf.Exp(-logits[i]));
            var p = new float[k + 1];
            p[0] = 1f - q[0];
            for (int i = 1; i < k; i++) p[i] = Mathf.Max(0f, q[i - 1] - q[i]);
            p[k] = q[k - 1];
            return p;
        }

        static bool LooksLikeProbabilities(float[] v)
        {
            float sum = 0f;
            foreach (var x in v)
            {
                if (x < 0f || x > 1f) return false;
                sum += x;
            }
            return Mathf.Abs(sum - 1f) < 0.02f;
        }

        static float[] Softmax(float[] v)
        {
            float max = float.MinValue;
            foreach (var x in v) max = Mathf.Max(max, x);
            var e = new float[v.Length];
            float sum = 0f;
            for (int i = 0; i < v.Length; i++) { e[i] = Mathf.Exp(v[i] - max); sum += e[i]; }
            for (int i = 0; i < v.Length; i++) e[i] /= sum;
            return e;
        }

        void OnDestroy()
        {
            _worker?.Dispose();
            if (_crop != null) _crop.Release();
        }
    }
}
