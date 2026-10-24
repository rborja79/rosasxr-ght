using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una decisión del entrenamiento (etapa 1 o 2) con las columnas de la especificación.</summary>
    public class TrainingDecision
    {
        public int etapa;
        public int nivel;
        public string item;            // id_rosa o id_foto
        public string pedido;
        public int gradoReal;
        public string estadoReal;
        public int gradoPredicho = -1; // etapa 2
        public float confianza = -1f;
        public string veredicto = "";  // CORTAR | NO CORTAR | DUDA | SIN SISTEMA
        public string decisionUsuario = "";
        public bool corto;
        public string coincidioConSistema = ""; // "1" | "0" | "" si no aplica
        public bool acerto;
        public float largoTalloCm = -1f;
        public int ms = -1;
        public bool ParaRevision => corto && (veredicto == "NO CORTAR" || veredicto == "DUDA");
    }

    /// <summary>
    /// Registro por usuario en persistentDataPath/registros/[usuario].csv
    /// (en el Quest: /sdcard/Android/data/co.ght.rosasxr/files/registros). Sirve como evidencia para la presentación.
    /// </summary>
    public static class TrainingLog
    {
        const string Header = "hora,usuario,etapa,nivel,id_item,pedido,grado_real,estado_real,grado_predicho,confianza," +
                              "veredicto,decision_usuario,corto,coincidio_con_sistema,acerto,largo_tallo_cm,tiempo_ms,para_revision";

        public static string PathFor(string user) =>
            Path.Combine(Application.persistentDataPath, "registros", user + ".csv");

        public static void Log(TrainingDecision d)
        {
            var ci = CultureInfo.InvariantCulture;
            var path = PathFor(OperatorSession.OperatorId);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (!File.Exists(path)) File.WriteAllText(path, Header + "\n", Encoding.UTF8);
                var line = string.Join(",",
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", ci),
                    OperatorSession.OperatorId,
                    d.etapa.ToString(ci),
                    d.nivel.ToString(ci),
                    Esc(d.item),
                    Esc(d.pedido),
                    d.gradoReal.ToString(ci),
                    Esc(d.estadoReal),
                    d.gradoPredicho < 0 ? "" : d.gradoPredicho.ToString(ci),
                    d.confianza < 0 ? "" : d.confianza.ToString("0.00", ci),
                    Esc(d.veredicto),
                    Esc(d.decisionUsuario),
                    d.corto ? "1" : "0",
                    d.coincidioConSistema,
                    d.acerto ? "1" : "0",
                    d.largoTalloCm < 0 ? "" : d.largoTalloCm.ToString("0.0", ci),
                    d.ms < 0 ? "" : d.ms.ToString(ci),
                    d.ParaRevision ? "1" : "0");
                File.AppendAllText(path, line + "\n", Encoding.UTF8);
            }
            catch (Exception e) { Debug.LogError("[RosasXR] No se pudo escribir el registro: " + e.Message); }
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
