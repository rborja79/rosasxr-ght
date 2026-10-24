using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RosasXR
{
    /// <summary>
    /// Prueba automática en el editor (sin visor): pone la tijera en el tallo de varias rosas y registra
    /// qué ve la cámara del brazo, el veredicto, el color del LED y el estado del semáforo.
    /// Se activa con el menú "Rosas XR > 8. Prueba automática (Cosecha)".
    /// </summary>
    public class HarvestSelfTest : MonoBehaviour
    {
        static string Flag => Path.Combine(Application.dataPath, "../selftest.flag");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!Application.isEditor || !File.Exists(Flag)) return;
            if (SceneManager.GetActiveScene().name != SceneNav.Harvest) return;
            new GameObject("PruebaAutomatica").AddComponent<HarvestSelfTest>();
        }

        static string ColorName(Color c)
        {
            if (c.r > 0.8f && c.g < 0.3f) return "ROJO";
            if (c.g > 0.8f && c.r < 0.3f && c.b < 0.5f) return "VERDE";
            if (c.r > 0.8f && c.g > 0.5f && c.b < 0.2f) return "AMBAR";
            if (c.b > 0.8f && c.g > 0.7f) return "CELESTE";
            if (c.b > 0.8f) return "AZUL";
            if (c.b > 0.3f) return "AZUL_TENUE";
            return "APAGADO";
        }

        IEnumerator Start()
        {
            var log = new StringBuilder();
            void L(string s) { Debug.Log("[SelfTest] " + s); log.AppendLine(s); }
            yield return new WaitForSeconds(1.5f);
            var ctl = FindFirstObjectByType<HarvestController>();
            var tool = FindFirstObjectByType<ScissorsTool>();
            var drv = tool != null ? tool.GetComponent<EditorHandDriver>() : null;
            if (drv != null) drv.enabled = false;
            if (ctl == null || tool == null) { L("FALTA controlador o tijera"); yield break; }
            var cart = ctl.cart;
            var roses = FindObjectsByType<HarvestRose>(FindObjectsSortMode.None);
            System.Array.Sort(roses, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            var order = HarvestOrder.Current;
            int n = 0;
            foreach (var r in roses)
            {
                if (n >= 8) break;
                if (Mathf.Abs(r.transform.position.x - cart.transform.position.x) > 0.7f) continue;
                if (r.transform.position.z < cart.transform.position.z - 1.2f || r.transform.position.z > cart.transform.position.z + 2.5f) continue;
                foreach (var cm in new[] { 42f, 20f })
                {
                    float along = Mathf.Max(0.03f, r.budBase - cm / 100f);
                    var tipPos = r.transform.position + r.transform.up * along;
                    var toStem = tipPos - new Vector3(cart.transform.position.x, tipPos.y, tipPos.z); toStem.y = 0f;
                    tool.transform.rotation = Quaternion.LookRotation(toStem.sqrMagnitude > 1e-4f ? toStem.normalized : Vector3.right, Vector3.up);
                    var off = tool.tip.position - tool.transform.position;
                    tool.transform.position = tipPos - off;
                    yield return new WaitForSeconds(1.6f);
                    L($"rosa {r.id} grado {r.grade} (pedido {(order.Accepts(r.grade) ? "SI" : "NO")}) corte a {cm} cm | camara ve: {ctl.panTilt.SeesTarget} ({ctl.panTilt.Reason}) pan {ctl.panTilt.Pan:0} tilt {ctl.panTilt.Tilt:0} | " +
                      $"asociada: {(ctl.Associated == r ? "esta" : ctl.Associated != null ? "OTRA " + ctl.Associated.id : "ninguna")} | veredicto: {ctl.Verdict} | en tallo: {ctl.OnStem} {ctl.CutCm:0} cm | LED: {ColorName(ctl.LedColor)} | semaforo: {ctl.SemaphoreState}");
                }
                n++;
            }
            L(n == 0 ? "No encontré rosas cerca del carro" : "FIN");
            try { File.WriteAllText(Path.Combine(Application.dataPath, "../selftest_resultado.txt"), log.ToString()); File.Delete(Flag); } catch { }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
