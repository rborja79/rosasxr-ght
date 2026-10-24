using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RosasXR
{
    public struct DecisionRecord
    {
        public string mode;            // "simulador" | "practica_yolo"
        public string itemId;          // foto o detección evaluada
        public string operatorAnswer;  // id de clase que eligió el operario
        public string reference;       // id de clase de referencia
        public string referenceSource; // "experto" | "modelo"
        public float modelConfidence;  // -1 si no aplica
        public bool match;
        public int responseMs;
    }

    /// <summary>
    /// Escribe un CSV por sesión en Application.persistentDataPath/sesiones.
    /// En el Quest queda en /sdcard/Android/data/[paquete]/files/sesiones.
    /// </summary>
    public static class SessionLogger
    {
        const string Header =
            "timestamp,operario,modo,item,respuesta_operario,referencia,fuente_referencia,confianza_modelo,coincide,tiempo_ms";

        public static string CurrentPath { get; private set; }

        public static void BeginSession(string mode)
        {
            var dir = Path.Combine(Application.persistentDataPath, "sesiones");
            Directory.CreateDirectory(dir);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            CurrentPath = Path.Combine(dir, $"{OperatorSession.OperatorId}_{mode}_{stamp}.csv");
            File.WriteAllText(CurrentPath, Header + "\n", Encoding.UTF8);
            Debug.Log($"[RosasXR] Registro de sesión: {CurrentPath}");
        }

        public static void Log(DecisionRecord r)
        {
            if (string.IsNullOrEmpty(CurrentPath)) BeginSession(r.mode ?? "sin_modo");

            var ci = CultureInfo.InvariantCulture;
            var line = string.Join(",",
                Esc(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", ci)),
                Esc(OperatorSession.OperatorId),
                Esc(r.mode),
                Esc(r.itemId),
                Esc(r.operatorAnswer),
                Esc(r.reference),
                Esc(r.referenceSource),
                r.modelConfidence < 0 ? "" : r.modelConfidence.ToString("0.000", ci),
                r.match ? "1" : "0",
                r.responseMs.ToString(ci));

            try { File.AppendAllText(CurrentPath, line + "\n", Encoding.UTF8); }
            catch (Exception e) { Debug.LogError($"[RosasXR] No se pudo escribir el registro: {e.Message}"); }
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool needsQuotes = s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0;
            return needsQuotes ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
