using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RosasXR.EditorTools
{
    /// <summary>Escenas de la versión gamificada: inicio con perfil, Etapa 1 (fotos reales) y Cosecha con el carro.</summary>
    public static partial class RosasXRSetup
    {
        const string GenDir = Root + "/Generado";

        // ───────────────────────── Inicio ─────────────────────────

        static string BuildHomeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(true);

            var canvas = AddWorldCanvas("Inicio", new Vector2(1100, 1300), new Vector3(0, 1.45f, 1.3f), Quaternion.identity, cam);
            var p = AddPanel(canvas.transform, PanelBg, new RectOffset(40, 40, 34, 34), 16);
            AddLogo(p, 110);
            AddText(p, "Titulo", "Entrenamiento de corte de rosas", 54, Color.white, TextAlignmentOptions.Center, 76).fontStyle = FontStyles.Bold;

            var row = NewUI("Usuario", p);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            var prev = AddButton(row, "Anterior", "<", Neutral, 120, 90, 50);
            var lbl = AddText(row, "Etiqueta", "Usuario: OP-01", 42, Color.white, TextAlignmentOptions.Center);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var next = AddButton(row, "Siguiente", ">", Neutral, 120, 90, 50);

            var prog = AddText(p, "Progreso", "", 28, new Color(0.85f, 0.9f, 0.85f), TextAlignmentOptions.Center, 80);

            var e1 = AddButton(p, "Etapa1", "Etapa 1 · Aprende a ver\n<size=70%>Fotos reales de botones de rosa</size>", Primary, 1000, 130, 42);
            var s1 = AddText(p, "EstadoEtapa1", "", 26, new Color(0.8f, 0.86f, 0.8f), TextAlignmentOptions.Center, 44);
            var e2 = AddButton(p, "Etapa2", "Etapa 2 · Cosecha con el carro\n<size=70%>Invernadero y cámara robótica</size>", Primary, 1000, 130, 42);
            var s2 = AddText(p, "EstadoEtapa2", "", 26, new Color(0.8f, 0.86f, 0.8f), TextAlignmentOptions.Center, 90);
            var pr = AddButton(p, "Practica", "Práctica con rosas reales\n<size=70%>Clasificador del equipo en el visor</size>", Secondary, 1000, 120, 40);

            var row2 = NewUI("Opciones", p);
            row2.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var h2 = row2.gameObject.AddComponent<HorizontalLayoutGroup>();
            h2.spacing = 16; h2.childControlWidth = true; h2.childControlHeight = true;
            h2.childForceExpandWidth = true; h2.childForceExpandHeight = true;
            var hand = AddButton(row2, "Lateralidad", "Diestro (tijera derecha)", Neutral, 330, 80, 26);
            var jury = AddButton(row2, "Jurado", "Modo jurado: NO", Neutral, 330, 80, 26);
            var reset = AddButton(row2, "Reiniciar", "Reiniciar perfil", Neutral, 330, 80, 26);
            var msg = AddText(p, "Mensaje", "", 28, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Center, 70);

            var hm = canvas.gameObject.AddComponent<HomeMenu>();
            hm.operatorLabel = lbl;
            hm.progressText = prog;
            hm.stage1Status = s1;
            hm.stage2Status = s2;
            hm.stage2Button = e2.GetComponent<Image>();
            hm.juryLabel = jury.GetComponentInChildren<TMP_Text>();
            hm.messageText = msg;
            hm.unlockedColor = Primary;
            UnityEventTools.AddPersistentListener(prev.onClick, new UnityAction(hm.PreviousOperator));
            UnityEventTools.AddPersistentListener(next.onClick, new UnityAction(hm.NextOperator));
            UnityEventTools.AddPersistentListener(e1.onClick, new UnityAction(hm.StartStage1));
            UnityEventTools.AddPersistentListener(e2.onClick, new UnityAction(hm.StartStage2));
            UnityEventTools.AddPersistentListener(pr.onClick, new UnityAction(hm.StartPractice));
            UnityEventTools.AddPersistentListener(jury.onClick, new UnityAction(hm.ToggleJury));
            UnityEventTools.AddPersistentListener(reset.onClick, new UnityAction(hm.ResetProfile));
            UnityEventTools.AddPersistentListener(hand.onClick, new UnityAction(hm.ToggleHandedness));
            hm.handLabel = hand.GetComponentInChildren<TMP_Text>();

            var path = SceneDir + "/Menu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ───────────────────────── Etapa 1 ─────────────────────────

        static string BuildStage1Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(true); // passthrough: espacio tranquilo, el usuario ve su entorno

            var canvas = AddWorldCanvas("Etapa1", new Vector2(1500, 1000), new Vector3(0, 1.4f, 1.5f), Quaternion.identity, cam);
            var root = AddPanel(canvas.transform, PanelBg, new RectOffset(28, 28, 20, 24), 12);

            // Barra superior: logo, pedido y menú
            var top = NewUI("Superior", root);
            top.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var th = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 18; th.childControlWidth = true; th.childControlHeight = true;
            th.childForceExpandWidth = false; th.childForceExpandHeight = true;
            if (Theme.logo != null)
            {
                var lg = NewUI("Logo", top);
                var li = lg.gameObject.AddComponent<Image>();
                li.sprite = Theme.logo; li.preserveAspect = true; li.raycastTarget = false;
                lg.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            }
            var order = AddText(top, "Pedido", "Pedido activo", 32, Color.white, TextAlignmentOptions.MidlineLeft);
            order.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            order.fontStyle = FontStyles.Bold;
            var menuTop = AddButton(top, "Menu", "< Menú", Neutral, 200, 80, 32);

            var stats = AddText(root, "Estado", "", 28, new Color(0.85f, 0.9f, 0.85f), TextAlignmentOptions.Center, 44);

            // Contenido: foto a la izquierda, pregunta / retroalimentación a la derecha
            var content = NewUI("Contenido", root);
            content.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var ch = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 24; ch.childControlWidth = true; ch.childControlHeight = true;
            ch.childForceExpandWidth = false; ch.childForceExpandHeight = true;

            var card = NewUI("TarjetaFoto", content);
            card.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.92f);
            card.gameObject.AddComponent<LayoutElement>().preferredWidth = 640;
            var holder = NewUI("Marco", card);
            Stretch(holder, 12);
            var ph = NewUI("Foto", holder);
            var raw = ph.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fit = ph.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 0.75f;
            var hl = NewUI("Resaltado", ph);
            Stretch(hl);
            var hlColor = new Color(1f, 0.85f, 0.1f);
            foreach (var e in new[] { Edge(hl, "A", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 8)),
                                      Edge(hl, "B", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 8)),
                                      Edge(hl, "I", new Vector2(0, 0), new Vector2(0, 1), new Vector2(8, 0)),
                                      Edge(hl, "D", new Vector2(1, 0), new Vector2(1, 1), new Vector2(8, 0)) })
                e.color = hlColor;
            var loading = AddText(card, "Cargando", "Cargando…", 34, TextDark, TextAlignmentOptions.Center);
            Stretch(loading.rectTransform, 20);
            loading.raycastTarget = false;

            // Pregunta
            var q = NewUI("Pregunta", content);
            q.gameObject.AddComponent<LayoutElement>().preferredWidth = 780;
            var qv = q.gameObject.AddComponent<VerticalLayoutGroup>();
            qv.spacing = 22; qv.padding = new RectOffset(10, 10, 40, 10);
            qv.childControlWidth = true; qv.childControlHeight = true; qv.childForceExpandWidth = true; qv.childForceExpandHeight = false;
            var qText = AddText(q, "Texto", "¿Se corta para este pedido?", 46, Color.white, TextAlignmentOptions.Center, 140);
            qText.fontStyle = FontStyles.Bold;
            var answers = new List<Button>();
            for (int i = 0; i < 3; i++) answers.Add(AddButton(q, "Respuesta" + i, "Opción", i == 0 ? Primary : Light, 760, 130, 44));

            // Retroalimentación
            var f = NewUI("Retroalimentacion", content);
            f.gameObject.AddComponent<LayoutElement>().preferredWidth = 780;
            var fv = f.gameObject.AddComponent<VerticalLayoutGroup>();
            fv.spacing = 12; fv.padding = new RectOffset(0, 0, 0, 0);
            fv.childControlWidth = true; fv.childControlHeight = true; fv.childForceExpandWidth = true; fv.childForceExpandHeight = false;
            var bar = NewUI("Encabezado", f);
            var barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = Theme.correct;
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var head = AddText(bar, "Titulo", "", 50, Color.white, TextAlignmentOptions.Center);
            head.fontStyle = FontStyles.Bold;
            Stretch(head.rectTransform, 8);
            var body = AddText(f, "Detalle", "", 30, Color.white, TextAlignmentOptions.TopLeft);
            body.margin = new Vector4(16, 6, 16, 6);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var refRoot = NewUI("Referencia", f);
            refRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 250;
            var refH = refRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            refH.spacing = 14; refH.childControlWidth = true; refH.childControlHeight = true;
            refH.childForceExpandWidth = false; refH.childForceExpandHeight = true;
            var refHolder = NewUI("FotoRef", refRoot);
            refHolder.gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
            var refImgRt = NewUI("Imagen", refHolder);
            var refRaw = refImgRt.gameObject.AddComponent<RawImage>();
            refRaw.raycastTarget = false;
            var refFit = refImgRt.gameObject.AddComponent<AspectRatioFitter>();
            refFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            refFit.aspectRatio = 0.75f;
            var refCap = AddText(refRoot, "Texto", "Referencia", 28, new Color(0.85f, 0.9f, 0.85f), TextAlignmentOptions.MidlineLeft);
            refCap.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var cont = AddButton(f, "Continuar", "Continuar", Light, 760, 100, 40);

            // Resumen
            var s = NewUI("Resumen", content);
            s.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var sv = s.gameObject.AddComponent<VerticalLayoutGroup>();
            sv.spacing = 16; sv.padding = new RectOffset(40, 40, 20, 10);
            sv.childControlWidth = true; sv.childControlHeight = true; sv.childForceExpandWidth = true; sv.childForceExpandHeight = false;
            var sText = AddText(s, "Texto", "", 34, Color.white, TextAlignmentOptions.Top);
            sText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var sRow = NewUI("Botones", s);
            sRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
            var sh = sRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            sh.spacing = 20; sh.childControlWidth = true; sh.childControlHeight = true;
            sh.childForceExpandWidth = true; sh.childForceExpandHeight = true;
            var again = AddButton(sRow, "Otra", "Otra lección", Primary, 600, 110, 40);
            var menu = AddButton(sRow, "Menu", "Menú principal", Neutral, 600, 110, 40);

            var ctl = canvas.gameObject.AddComponent<Stage1Controller>();
            ctl.orderText = order; ctl.statsText = stats; ctl.questionText = qText;
            ctl.photoCard = card.gameObject; ctl.photo = raw; ctl.photoFitter = fit; ctl.highlightBox = hl; ctl.loadingText = loading;
            ctl.questionRoot = q.gameObject; ctl.answerButtons = answers.ToArray();
            ctl.feedbackRoot = f.gameObject; ctl.feedbackBar = barImg; ctl.feedbackHeadline = head; ctl.feedbackBody = body;
            ctl.referenceRoot = refRoot.gameObject; ctl.referencePhoto = refRaw; ctl.referenceFitter = refFit; ctl.referenceCaption = refCap;
            ctl.continueButton = cont; ctl.summaryRoot = s.gameObject; ctl.summaryText = sText;
            ctl.nextLessonButton = again; ctl.menuButton = menu; ctl.menuButtonTop = menuTop;
            ctl.correctColor = Theme.correct; ctl.wrongColor = Theme.wrong;

            AddSounds(null, null);
            var path = SceneDir + "/Etapa1.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ───────────────────────── Etapa 2: Cosecha ─────────────────────────

        static string BuildHarvestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(false);
            EnsureFolder(GenDir);
            var rig = Object.FindFirstObjectByType<OVRCameraRig>();
            Transform rightAnchor = rig != null ? rig.transform.Find("TrackingSpace/RightControllerAnchor") : null;
            Transform leftAnchor = rig != null ? rig.transform.Find("TrackingSpace/LeftControllerAnchor") : null;

            const float len = 24f, pathW = 0.7f, bedW = 0.8f;
            BuildCorridor(len, pathW, bedW, cam);
            var roses = PlantRoses(len, pathW);

            // ── Carro ──
            var cartGo = new GameObject("Carro");
            cartGo.transform.position = new Vector3(0, 0, 0.75f);
            var cartT = cartGo.transform;
            var blue = TexMatLit("CanastaAzulTex", BlueTex(), new Vector2(2f, 2f), 0.35f, 0f);
            var metal = TexMatLit("AceroCarroTex", MetalTex(), new Vector2(1f, 1f), 0.55f, 0.6f);
            var dark = Mat("NegroCarro", new Color(0.08f, 0.08f, 0.09f));
            Dyn(RBox("Base", cartT, new Vector3(0, 0.22f, 0), new Vector3(0.45f, 0.03f, 0.9f), metal, 0.012f));
            foreach (var wx in new[] { -0.2f, 0.2f })
                foreach (var wz in new[] { -0.38f, 0.38f })
                {
                    var w = Dyn(Cyl("Rueda", cartT, new Vector3(wx, 0.08f, wz), 0.14f, 0.04f, dark));
                    w.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            foreach (var px in new[] { -0.2f, 0.2f })
                foreach (var pz in new[] { -0.42f, 0.42f })
                    Dyn(Cyl("Pata", cartT, new Vector3(px, 0.15f, pz), 0.025f, 0.15f, metal));
            var basket = new GameObject("Canasta").transform;
            basket.SetParent(cartT, false);
            basket.localPosition = new Vector3(0, 0.25f, 0.05f);
            Dyn(RBox("Fondo", basket, new Vector3(0, 0.0f, 0), new Vector3(0.42f, 0.02f, 0.7f), blue, 0.008f));
            Dyn(RBox("LadoI", basket, new Vector3(-0.21f, 0.14f, 0), new Vector3(0.02f, 0.28f, 0.7f), blue, 0.009f));
            Dyn(RBox("LadoD", basket, new Vector3(0.21f, 0.14f, 0), new Vector3(0.02f, 0.28f, 0.7f), blue, 0.009f));
            Dyn(RBox("Frente", basket, new Vector3(0, 0.14f, 0.35f), new Vector3(0.42f, 0.28f, 0.02f), blue, 0.009f));
            Dyn(RBox("Atras", basket, new Vector3(0, 0.14f, -0.35f), new Vector3(0.42f, 0.28f, 0.02f), blue, 0.009f));
            // Manija (el usuario empuja desde atrás)
            Dyn(Cyl("PostManijaI", cartT, new Vector3(-0.2f, 0.6f, -0.45f), 0.025f, 0.75f, metal));
            Dyn(Cyl("PostManijaD", cartT, new Vector3(0.2f, 0.6f, -0.45f), 0.025f, 0.75f, metal));
            var handle = Dyn(Cyl("Manija", cartT, new Vector3(0, 0.97f, -0.47f), 0.035f, 0.46f, dark));
            handle.transform.localRotation = Quaternion.Euler(0, 0, 90);

            // Poste delantero: pantalla y semáforo
            Dyn(Cyl("PosteDisplay", cartT, new Vector3(0, 0.97f, 0.4f), 0.04f, 1.5f, metal));
            // Mástil (~1,5 m) junto a la manija, al lado del operario, con brazo corto y cámara en pan-tilt de 2 servos.
            // Va atrás para que la tijera (a los lados del operario) quede dentro del alcance de ±100° del pan.
            const float camX = 0.21f, camZ = -0.42f;
            Dyn(Cyl("Mastil", cartT, new Vector3(camX, 0.83f, camZ), 0.035f, 1.25f, metal));
            Dyn(Tube("BrazoCorto", cartT, new Vector3(camX, 1.46f, camZ), new Vector3(camX, 1.46f, camZ + 0.16f), 0.03f, metal));
            var servoM = Mat("Servo", new Color(0.12f, 0.12f, 0.14f));
            var hornM = Mat("CuernoServo", new Color(0.92f, 0.92f, 0.92f));
            var panJ = new GameObject("ServoPan").transform;
            panJ.SetParent(cartT, false);
            panJ.localPosition = new Vector3(camX, 1.5f, camZ + 0.16f);
            panJ.localScale = Vector3.one * 1.6f; // servos y cámara bien visibles
            Dyn(RBox("CuerpoServoPan", panJ, new Vector3(0, -0.012f, 0), new Vector3(0.04f, 0.036f, 0.02f), servoM, 0.005f));
            Dyn(Cyl("CuernoPan", panJ, new Vector3(0, 0.008f, 0), 0.025f, 0.004f, hornM));
            var tiltJ = new GameObject("ServoTilt").transform;
            tiltJ.SetParent(panJ, false);
            tiltJ.localPosition = new Vector3(0, 0.04f, 0);
            Dyn(Box("SoporteU", tiltJ, new Vector3(0, -0.015f, 0), new Vector3(0.06f, 0.006f, 0.03f), metal));
            Dyn(RBox("CuerpoServoTilt", tiltJ, new Vector3(0.035f, 0, 0), new Vector3(0.02f, 0.036f, 0.04f), servoM, 0.005f));
            Dyn(RBox("CuerpoCamara", tiltJ, new Vector3(0, 0.005f, 0.01f), new Vector3(0.045f, 0.035f, 0.05f), dark, 0.008f));
            var lens = new GameObject("Lente").transform;
            lens.SetParent(tiltJ, false);
            lens.localPosition = new Vector3(0, 0.005f, 0.04f);
            Dyn(Cyl("Objetivo", lens, new Vector3(0, 0, -0.004f), 0.022f, 0.012f, Mat("Vidrio", new Color(0.1f, 0.15f, 0.25f)))).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var feedCam = new GameObject("CamaraBrazo").AddComponent<Camera>();
            feedCam.transform.SetParent(lens, false);
            feedCam.fieldOfView = 55f; feedCam.nearClipPlane = 0.03f; feedCam.farClipPlane = 4f;
            feedCam.clearFlags = CameraClearFlags.SolidColor; feedCam.backgroundColor = new Color(0.85f, 0.88f, 0.86f);
            feedCam.cullingMask = ~(1 << 5); // sin la interfaz
            feedCam.enabled = false;

            // Semáforo al costado del mástil, hacia el usuario
            var lights = new Renderer[3];
            string[] ln = { "Rojo", "Ambar", "Verde" };
            Dyn(RBox("CajaSemaforo", cartT, new Vector3(0.07f, 1.32f, 0.37f), new Vector3(0.075f, 0.24f, 0.035f), dark, 0.012f));
            for (int i = 0; i < 3; i++)
            {
                var b = Dyn(Ball("Luz" + ln[i], cartT, new Vector3(0.07f, 1.40f - i * 0.075f, 0.35f), 0.055f, Mat("Luz" + ln[i], Color.black, true)));
                lights[i] = b.GetComponent<Renderer>();
            }

            // Pantalla del carro: vista de la cámara del brazo + pedido, contador y tutorial
            var screen = AddWorldCanvas("PantallaCarro", new Vector2(860, 380), new Vector3(-0.04f, 0.96f, 0.16f), Quaternion.Euler(28, 0, 0), cam, cartT);
            var sp = NewUI("Panel", screen.transform);
            Stretch(sp);
            sp.gameObject.AddComponent<Image>().color = PanelBg;
            var sh = sp.gameObject.AddComponent<HorizontalLayoutGroup>();
            sh.padding = new RectOffset(14, 14, 14, 14); sh.spacing = 14;
            sh.childControlWidth = true; sh.childControlHeight = true; sh.childForceExpandWidth = false; sh.childForceExpandHeight = true;
            var camView = NewUI("VistaCamara", sp);
            camView.gameObject.AddComponent<LayoutElement>().preferredWidth = 352;
            var camImg = camView.gameObject.AddComponent<RawImage>();
            camImg.raycastTarget = false;
            var camLbl = AddText(camView, "Etiqueta", "Cámara del brazo", 18, Color.white, TextAlignmentOptions.TopLeft);
            Stretch(camLbl.rectTransform, 6);
            var col = NewUI("Texto", sp);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var cv = col.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 4; cv.childControlWidth = true; cv.childControlHeight = true; cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;
            var status = AddText(col, "EstadoCamara", "Cámara en reposo", 20, new Color(1f, 0.9f, 0.6f), TextAlignmentOptions.Center, 96);
            var info = AddText(col, "Pantalla", "", 18, Color.white, TextAlignmentOptions.TopLeft);
            info.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            Object.DestroyImmediate(screen.GetComponent<GraphicRaycaster>());

            // Marcador grande arriba, al fondo del carro: buenas, malas, altura, puntos, tallos y últimos cortes
            var board = AddWorldCanvas("Marcador", new Vector2(820, 330), new Vector3(0, 1.88f, 0.42f), Quaternion.Euler(-8, 0, 0), cam, cartT);
            var bp = AddPanel(board.transform, new Color(0f, 0.12f, 0.07f, 0.95f), new RectOffset(16, 16, 12, 12), 6);
            var brow1 = NewUI("Cifras", bp);
            brow1.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            var bh1 = brow1.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh1.spacing = 10; bh1.childControlWidth = true; bh1.childControlHeight = true; bh1.childForceExpandWidth = true; bh1.childForceExpandHeight = true;
            TMP_Text Cell(string title, Color c)
            {
                var cell = NewUI(title, brow1);
                cell.gameObject.AddComponent<Image>().color = new Color(c.r * 0.25f, c.g * 0.25f, c.b * 0.25f, 0.9f);
                var v = cell.gameObject.AddComponent<VerticalLayoutGroup>();
                v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                v.padding = new RectOffset(4, 4, 6, 4);
                AddText(cell, "Titulo", title, 22, c, TextAlignmentOptions.Center, 30).fontStyle = FontStyles.Bold;
                var n = AddText(cell, "Valor", "0", 72, Color.white, TextAlignmentOptions.Center, 96);
                n.fontStyle = FontStyles.Bold;
                return n;
            }
            var bGood = Cell("BUENAS", new Color(0.3f, 0.95f, 0.5f));
            var bBad = Cell("MALAS", new Color(1f, 0.35f, 0.3f));
            var bHeight = Cell("ALTURA MAL", new Color(1f, 0.75f, 0.2f));
            var bPts = Cell("PUNTOS", new Color(0.85f, 0.9f, 1f));
            var brow2 = NewUI("Fila2", bp);
            brow2.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var bh2 = brow2.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh2.spacing = 8; bh2.childControlWidth = true; bh2.childControlHeight = true; bh2.childForceExpandWidth = false; bh2.childForceExpandHeight = true;
            var counter = AddText(brow2, "Contador", "Tallos: 0 / 8", 34, Color.white, TextAlignmentOptions.MidlineLeft);
            counter.fontStyle = FontStyles.Bold;
            counter.gameObject.AddComponent<LayoutElement>().preferredWidth = 270;
            AddText(brow2, "Ultimos", "Últimos cortes:", 22, new Color(0.8f, 0.85f, 0.8f), TextAlignmentOptions.MidlineRight).gameObject.AddComponent<LayoutElement>().preferredWidth = 170;
            var dots = new List<Image>();
            for (int i = 0; i < 10; i++)
            {
                var d = NewUI("Corte" + i, brow2);
                var le = d.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 26; le.preferredHeight = 26;
                var img = d.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0.12f); img.raycastTarget = false;
                dots.Add(img);
            }
            var points = AddText(bp, "Ultimo", "", 24, new Color(0.9f, 0.95f, 0.9f), TextAlignmentOptions.Center, 50);
            Object.DestroyImmediate(board.GetComponent<GraphicRaycaster>());

            // Menú desplegable: ícono en la esquina superior derecha de la vista
            var fm = BuildFloatingMenu(cam, "Menú principal", "Recentrar", "< Corredor izq.", "Corredor der. >", "Terminar ronda", "Reiniciar ronda",
                "Mano de tijera: DERECHA", "Pantalla cámara: SÍ", "Simular falla", "Depuración: NO", "Sostener: OBLIGATORIO", "Repetir tutorial", "Voz guía: SÍ");
            var bMenu = fm[0]; var bRecenter = fm[1]; var bLeft = fm[2]; var bRight = fm[3]; var bFinish = fm[4]; var bRestart = fm[5];
            var bHand = fm[6]; var bCam = fm[7]; var bFail = fm[8]; var bDbg = fm[9]; var bHold = fm[10]; var bReplay = fm[11]; var bVoice = fm[12];

            var cart = cartGo.AddComponent<HarvestCart>();
            cart.handle = handle.transform;
            cart.handleRenderer = handle.GetComponent<Renderer>();
            cart.rig = rig != null ? rig.transform : null;
            new GameObject("EntradaXR (controles y manos)").AddComponent<XRInputBridge>();
            cart.railMinZ = 0.75f; cart.railMaxZ = len - 1.2f;
            cart.aisles = new[] { -(pathW + bedW), 0f, pathW + bedW }; cart.aisle = 1;

            for (int i = 0; i < 3; i++)
            {
                var sgo = GameObject.Find($"LetreroCorredor{i + 1}");
                var sbtn = sgo != null ? sgo.GetComponent<Button>() : null;
                if (sbtn != null) UnityEventTools.AddIntPersistentListener(sbtn.onClick, new UnityAction<int>(cart.ChangeToAisle), i);
            }

            var feed = cartGo.AddComponent<CameraFeedScreen>();
            feed.feedCamera = feedCam; feed.screen = camImg; feed.screenRoot = camView.gameObject;

            // ── Tijeras (MANO DE TIJERA): con guante; se abren y cierran ──
            var tracking = rig != null ? rig.transform.Find("TrackingSpace") : null;
            var scissors = new GameObject("Tijeras").transform;
            scissors.SetParent(tracking, false);
            scissors.localScale = Vector3.one * 1.4f;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(scissors, false);
            var steelM = Mat("AceroTijera", new Color(0.80f, 0.82f, 0.85f));
            var gripM = Mat("MangoTijera", new Color(0.85f, 0.12f, 0.10f));
            var gloveM = Mat("GuanteBlanco", new Color(0.95f, 0.95f, 0.93f));
            Transform Blade(string n, float side)
            {
                var piv = new GameObject(n).transform;
                piv.SetParent(visual, false);
                piv.localPosition = new Vector3(0, 0, 0.02f);
                var bl = Dyn(Box("Hoja", piv, new Vector3(side * 0.004f, side * 0.0015f, 0.05f), new Vector3(0.012f, 0.003f, 0.1f), steelM));
                bl.transform.localRotation = Quaternion.Euler(0, -side * 2f, 0);
                return piv;
            }
            var bladeA = Blade("HojaA", 1f);
            var bladeB = Blade("HojaB", -1f);
            Dyn(Cyl("Eje", visual, new Vector3(0, 0, 0.02f), 0.012f, 0.012f, steelM));
            Dyn(Box("Mango1", visual, new Vector3(0.016f, -0.004f, -0.03f), new Vector3(0.018f, 0.016f, 0.09f), gripM));
            Dyn(Box("Mango2", visual, new Vector3(-0.016f, -0.004f, -0.03f), new Vector3(0.018f, 0.016f, 0.09f), gripM));
            Dyn(Box("Guante", visual, new Vector3(0, -0.02f, -0.045f), new Vector3(0.07f, 0.03f, 0.08f), gloveM));
            var ledGo = Dyn(Ball("LED", visual, new Vector3(0, 0.012f, -0.035f), 0.016f, Mat("LEDTijeras", Color.black, true)));
            var tip = new GameObject("ZonaCorte").transform;
            tip.SetParent(scissors, false);
            tip.localPosition = new Vector3(0, 0, 0.075f);
            var tool = scissors.gameObject.AddComponent<ScissorsTool>();
            tool.visual = visual.gameObject; tool.bladeA = bladeA; tool.bladeB = bladeB; tool.tip = tip; tool.led = ledGo.transform;
            scissors.gameObject.AddComponent<EditorHandDriver>();

            // ── Mano de sostén ──
            var glove = new GameObject("ManoSosten").transform;
            glove.SetParent(tracking, false);
            glove.localPosition = new Vector3(-0.2f, 1.0f, 0.2f);
            var gloveVis = Dyn(Box("Guante", glove, new Vector3(0, -0.01f, 0.0f), new Vector3(0.085f, 0.035f, 0.1f), gloveM));
            glove.gameObject.AddComponent<HandAttach>().gloveVisual = gloveVis;

            var flash = Dyn(Ball("DestelloCorte", null, Vector3.zero, 0.02f, Mat("Destello", new Color(1f, 0.95f, 0.5f), true)));

            // ── Pan-tilt, clasificador y controlador ──
            var pt = cartGo.AddComponent<PanTiltCamera>();
            pt.panJoint = panJ; pt.tiltJoint = tiltJ; pt.lens = lens; pt.target = ledGo.transform;
            // Pan amplio: el operario corta a los lados y un poco atrás del mástil; con ±100° la tijera queda fuera
            pt.panLimits = new Vector2(-170f, 170f);
            var dbgRay = new GameObject("RayoDepuracion", typeof(LineRenderer)).GetComponent<LineRenderer>();
            dbgRay.startWidth = dbgRay.endWidth = 0.003f;
            dbgRay.material = Mat("RayoDepuracion", new Color(0.2f, 0.6f, 1f), true);
            dbgRay.positionCount = 2;
            dbgRay.gameObject.SetActive(false);
            pt.debugRay = dbgRay;

            var clf = new GameObject("ClasificadorSimulado").AddComponent<SimulatedClassifier>();

            var assoc = new GameObject("LineaAsociacion", typeof(LineRenderer)).GetComponent<LineRenderer>();
            assoc.startWidth = assoc.endWidth = 0.003f;
            assoc.material = Mat("LineaAsociacion", new Color(1f, 0.9f, 0.2f), true);
            assoc.positionCount = 2;

            // Resumen de ronda
            var sum = AddWorldCanvas("ResumenRonda", new Vector2(960, 720), new Vector3(0, 1.5f, 1.5f), Quaternion.identity, cam);
            var sumP = AddPanel(sum.transform, PanelBg, new RectOffset(36, 36, 30, 30), 16);
            AddLogo(sumP, 70);
            var sumText = AddText(sumP, "Texto", "", 32, Color.white, TextAlignmentOptions.Top);
            sumText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var srow = NewUI("Botones", sumP);
            srow.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var srh = srow.gameObject.AddComponent<HorizontalLayoutGroup>();
            srh.spacing = 16; srh.childControlWidth = true; srh.childControlHeight = true; srh.childForceExpandWidth = true; srh.childForceExpandHeight = true;
            var sAgain = AddButton(srow, "Otra", "Otra ronda", Primary, 420, 100, 38);
            var sMenu = AddButton(srow, "Menu", "Menú principal", Neutral, 420, 100, 38);

            var ctl = new GameObject("CosechaController").AddComponent<HarvestController>();
            ctl.cart = cart; ctl.panTilt = pt; ctl.classifier = clf;
            ctl.scissorsTip = tip; ctl.led = ledGo.GetComponent<Renderer>();
            ctl.scissors = tool; ctl.cameraFeed = feed; ctl.cutFlash = flash.transform;
            ctl.freeHand = glove; ctl.basket = basket;
            ctl.lightRed = lights[0]; ctl.lightAmber = lights[1]; ctl.lightGreen = lights[2];
            ctl.counterText = counter; ctl.screenText = info; ctl.pointsText = points; ctl.statusText = status;
            ctl.associationLine = assoc;
            ctl.summaryPanel = sum.gameObject; ctl.summaryText = sumText;
            ctl.againButton = sAgain; ctl.menuButton = sMenu; ctl.menuButtonCart = bMenu;
            ctl.failureButton = bFail; ctl.debugButton = bDbg;
            ctl.recenterButton = bRecenter; ctl.restartButton = bRestart;
            ctl.aisleLeftButton = bLeft; ctl.aisleRightButton = bRight; ctl.finishButton = bFinish;
            ctl.handednessButton = bHand; ctl.cameraButton = bCam;
            ctl.holdModeButton = bHold; ctl.holdModeLabel = bHold.GetComponentInChildren<TMP_Text>();
            ctl.replayTutorialButton = bReplay;
            ctl.voiceButton = bVoice; ctl.voiceLabel = bVoice.GetComponentInChildren<TMP_Text>();
            // Panel del tutorial: delante del usuario (no sobre el carro), con Siguiente y Saltar
            var tut = AddWorldCanvas("PanelTutorial", new Vector2(760, 400), new Vector3(0, 1.3f, 1f), Quaternion.identity, cam);
            tut.transform.localScale = Vector3.one * 0.0007f;
            var tp = AddPanel(tut.transform, PanelBg, new RectOffset(24, 24, 18, 18), 10);
            var tText = AddText(tp, "Texto", "", 30, Color.white, TextAlignmentOptions.TopLeft);
            tText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var trow = NewUI("Botones", tp);
            trow.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var trh = trow.gameObject.AddComponent<HorizontalLayoutGroup>();
            trh.spacing = 14; trh.childControlWidth = true; trh.childControlHeight = true; trh.childForceExpandWidth = true; trh.childForceExpandHeight = true;
            var bNext = AddButton(trow, "Siguiente", "Siguiente paso >", Primary, 340, 80, 30);
            var bSkip = AddButton(trow, "Saltar", "Saltar tutorial", Neutral, 340, 80, 30);
            var tf = tut.gameObject.AddComponent<FollowInView>();
            tf.distance = 0.62f; tf.heightOffset = -0.2f; tf.sideOffset = -0.12f; tf.tiltDegrees = 18f; tf.recenterAngle = 40f;
            ctl.tutorialPanel = tut.gameObject; ctl.tutorialText = tText;
            ctl.nextStepButton = bNext; ctl.skipTutorialButton = bSkip;
            ctl.boardGood = bGood; ctl.boardBad = bBad; ctl.boardHeight = bHeight; ctl.boardPoints = bPts;
            ctl.historyDots = dots.ToArray();
            ctl.basketRenderers = basket.GetComponentsInChildren<Renderer>();
            ctl.handednessLabel = bHand.GetComponentInChildren<TMP_Text>();
            ctl.cameraLabel = bCam.GetComponentInChildren<TMP_Text>();
            var ring = new GameObject("AnilloBoton", typeof(LineRenderer)).GetComponent<LineRenderer>();
            ring.startWidth = ring.endWidth = 0.004f; ring.positionCount = 25; ring.loop = false;
            ring.material = Mat("AnilloBoton", Color.white, true);
            ring.enabled = false;
            ctl.budRing = ring;
            var band = new GameObject("ZonaCorteCorrecta", typeof(LineRenderer)).GetComponent<LineRenderer>();
            band.startWidth = band.endWidth = 0.012f; band.positionCount = 2;
            band.material = Mat("ZonaCorte", new Color(0.2f, 1f, 0.45f), true);
            band.startColor = band.endColor = new Color(0.2f, 1f, 0.45f);
            band.enabled = false;
            ctl.cutBand = band;
            ctl.failureLabel = bFail.GetComponentInChildren<TMP_Text>();
            AddSounds(cart, new[] { scissors, glove });
            ctl.debugLabel = bDbg.GetComponentInChildren<TMP_Text>();

            // Viñeta de confort pegada a la vista
            var vig = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vig.name = "VinetaConfort";
            Object.DestroyImmediate(vig.GetComponent<Collider>());
            vig.transform.SetParent(cam.transform, false);
            vig.transform.localPosition = new Vector3(0, 0, 0.12f);
            vig.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            vig.GetComponent<Renderer>().sharedMaterial = VignetteMaterial();
            var cv2 = vig.AddComponent<ComfortVignette>();
            cv2.cart = cart; cv2.quad = vig.GetComponent<Renderer>();

            // La interfaz va en la capa UI para que la cámara del brazo no la vea
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var t in c.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;

            Debug.Log($"[RosasXR] Cosecha: {roses} rosas interactivas");
            var path = SceneDir + "/Cosecha.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static GameObject Dyn(GameObject g) { g.isStatic = false; return g; }

        /// <summary>Cilindro (tubo) entre dos puntos locales.</summary>
        static GameObject Tube(string name, Transform parent, Vector3 from, Vector3 to, float diameter, Material m)
        {
            var g = Cyl(name, parent, (from + to) * 0.5f, diameter, Vector3.Distance(from, to), m);
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            return g;
        }

        /// <summary>Caja con bordes redondeados (malla generada y guardada por tamaño).</summary>
        static GameObject RBox(string name, Transform parent, Vector3 pos, Vector3 size, Material m, float radius)
        {
            var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.GetComponent<MeshFilter>().sharedMesh = RoundedBoxMesh(size, radius);
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            g.isStatic = true;
            return g;
        }

        static Mesh RoundedBoxMesh(Vector3 size, float radius)
        {
            var key = $"RBuv_{size.x:0.###}_{size.y:0.###}_{size.z:0.###}_{radius:0.###}".Replace(',', '.');
            var path = $"{GenDir}/{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            EnsureFolder(GenDir);
            var h = size * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.999f);
            var inner = new Vector3(h.x - r, h.y - r, h.z - r);
            const int N = 6;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>(); var uvs = new List<Vector2>();
            var faces = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in faces)
            {
                var u = n == Vector3.up || n == Vector3.down ? Vector3.right : Vector3.Cross(Vector3.up, n);
                var v = Vector3.Cross(n, u);
                int b = verts.Count;
                for (int j = 0; j <= N; j++)
                    for (int i = 0; i <= N; i++)
                    {
                        var p = n + u * (2f * i / N - 1f) + v * (2f * j / N - 1f);
                        p = Vector3.Scale(p, h);
                        var c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        var d = (p - c); var dn = d.sqrMagnitude > 1e-10f ? d.normalized : n;
                        verts.Add(c + dn * r); norms.Add(dn); uvs.Add(new Vector2((float)i / N, (float)j / N));
                    }
                for (int j = 0; j < N; j++)
                    for (int i = 0; i < N; i++)
                    {
                        int a = b + j * (N + 1) + i, a1 = a + 1, c0 = a + N + 1, c1 = c0 + 1;
                        tris.AddRange(new[] { a, c0, a1, a1, c0, c1 });
                    }
            }
            mesh = new Mesh { name = key };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            // Asegurar que las caras miran hacia afuera
            var t0 = mesh.triangles; var vv = mesh.vertices;
            var e = Vector3.Cross(vv[t0[1]] - vv[t0[0]], vv[t0[2]] - vv[t0[0]]);
            if (Vector3.Dot(e, norms[t0[0]]) < 0f)
            {
                for (int k = 0; k < t0.Length; k += 3) { var tmp = t0[k + 1]; t0[k + 1] = t0[k + 2]; t0[k + 2] = tmp; }
                mesh.triangles = t0;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ── Pasillo realista (referencia: fotos de finca) ──

        static void BuildCorridor(float len, float pathW, float bedW, Camera cam)
        {
            var env = new GameObject("Invernadero").transform;
            var soil = TexMat("SueloTierra", SoilTex(), new Vector2(8f, 15f), false, Color.white);
            var bedSoil = TexMatLit("TierraCamaTex", SoilTex(), new Vector2(1f, 12f), 0.02f, 0f);
            // Follaje de fondo con textura tomada de las fotos reales de la finca (si existe)
            var fotoFollaje = GenTexture("follaje_foto", false);
            var leafDark = fotoFollaje != null
                ? TexMatLit("FollajeFotoTex", fotoFollaje, new Vector2((len - 1.4f) / 1.4f, 1.1f), 0.06f, 0f)
                : Mat("FollajeFondo", new Color(0.07f, 0.17f, 0.06f));
            var wood = TexMatLit("MaderaTex", WoodTex(), new Vector2(1f, 2f), 0.08f, 0f);
            var wire = Mat("AlambreTutor", new Color(0.85f, 0.85f, 0.8f));
            var steel = TexMatLit("AceroTex", MetalTex(), new Vector2(1f, 3f), 0.5f, 0.55f);
            var roof = TexMat("TechoPlastico", RoofTex(), new Vector2(1f, len / 3f), true, Color.white);
            var pipe = Mat("MangueraRiego", new Color(0.05f, 0.05f, 0.05f));
            var leafMat = LeafMaterial();
            var cluster = ClusterMesh();
            var budMesh = SpindleMesh();
            var budMat = Mat("BotonSimple", new Color(0.55f, 0.04f, 0.10f));
            var rng = new System.Random(11);
            float R() => (float)rng.NextDouble();

            float zc = len / 2f;
            Box("Suelo", env, new Vector3(0, -0.05f, zc), new Vector3(16f, 0.1f, len + 6f), soil).AddComponent<BoxCollider>();

            float pitch = pathW + bedW;
            const float bedStart = 1.4f;               // cabecera libre para cruzar entre corredores
            float bedLen = len - bedStart, bzc = bedStart + bedLen / 2f;
            var aisles = new[] { -pitch, 0f, pitch };  // corredores con rosas para cosechar
            for (int k = -3; k <= 3; k++)
            {
                if (k == 0) continue;
                float side = Mathf.Sign(k);
                float x = side * (pathW / 2f + bedW / 2f + (Mathf.Abs(k) - 1) * pitch);
                var bed = new GameObject($"Cama_{k}").transform;
                bed.SetParent(env, false);
                // Caras de la cama que dan a un corredor de cosecha
                var faces = new List<float>();
                foreach (float d in new[] { -1f, 1f })
                {
                    float ax = x + d * (bedW / 2f + pathW / 2f);
                    foreach (var a2 in aisles) if (Mathf.Abs(ax - a2) < 0.01f) faces.Add(d);
                }
                bool near = faces.Count > 0;
                Box("Tierra", bed, new Vector3(x, 0.12f, bzc), new Vector3(bedW, 0.24f, bedLen), bedSoil);
                var core = Box("FollajeBase", bed, new Vector3(x, 1.0f, bzc + 0.25f), new Vector3(bedW * 0.3f, 1.55f, bedLen - 0.5f), leafDark);
                if (near) core.AddComponent<BoxCollider>(); // oclusión para la cámara del carro
                Tube("Manguera", bed, new Vector3(x - side * bedW * 0.3f, 0.26f, bedStart), new Vector3(x - side * bedW * 0.3f, 0.26f, len), 0.02f, pipe);

                // Postes de madera y alambres de tutorado
                for (float z = bedStart; z <= len; z += 3f)
                    foreach (float s2 in new[] { -1f, 1f })
                        Cyl("Poste", bed, new Vector3(x + s2 * bedW * 0.45f, 1.3f, z), 0.06f, 2.6f, wood);
                foreach (float hgt in new[] { 1.0f, 1.6f, 2.15f })
                    foreach (float s2 in new[] { -1f, 1f })
                        Tube("Alambre", bed, new Vector3(x + s2 * bedW * 0.45f, hgt, bedStart), new Vector3(x + s2 * bedW * 0.45f, hgt, len), 0.005f, wire);

                // Racimos de hojas: un poco atrás del borde para que los tallos y botones se vean desde el corredor
                float step = near ? 0.22f : 0.55f;
                for (float z = bedStart + 0.1f; z < len; z += step)
                {
                    foreach (var d in faces)
                        foreach (float hgt in new[] { 0.45f, 0.85f, 1.25f, 1.6f })
                            Cluster(bed, cluster, leafMat, new Vector3(x + d * (bedW * 0.5f - 0.13f - R() * 0.1f), hgt + (R() - 0.5f) * 0.14f, z + (R() - 0.5f) * 0.1f),
                                    0.32f + R() * 0.14f, rng);
                    int across = near ? 3 : 2;
                    for (int a2 = 0; a2 < across; a2++)
                        Cluster(bed, cluster, leafMat, new Vector3(x + (a2 - (across - 1) / 2f) * 0.2f + (R() - 0.5f) * 0.08f, 1.72f + R() * 0.15f, z + R() * step),
                                0.34f + R() * 0.14f, rng);
                    // Botones decorativos (no interactivos) dentro de la cama
                    if (false)
                    {
                        var bb = new GameObject("BotonFondo", typeof(MeshFilter), typeof(MeshRenderer));
                        bb.transform.SetParent(bed, false);
                        bb.transform.localPosition = new Vector3(x + (R() - 0.5f) * 0.3f, 1.75f + R() * 0.25f, z + R() * step);
                        bb.transform.localScale = new Vector3(0.018f, 0.05f, 0.018f) * (0.8f + R() * 0.5f);
                        bb.GetComponent<MeshFilter>().sharedMesh = budMesh;
                        bb.GetComponent<MeshRenderer>().sharedMaterial = budMat;
                        bb.isStatic = true;
                    }
                }
            }
            // Letreros de corredor en la cabecera
            for (int i = 0; i < aisles.Length; i++)
            {
                var sign = AddWorldCanvas($"LetreroCorredor{i + 1}", new Vector2(420, 110), new Vector3(aisles[i], 2.45f, bedStart - 0.1f), Quaternion.identity, cam, env);
                sign.gameObject.AddComponent<Image>().color = new Color(0f, 0.19f, 0.11f, 0.95f);
                var st = AddText(sign.transform, "Texto", $"Corredor {i + 1}", 56, Color.white, TextAlignmentOptions.Center);
                Stretch(st.rectTransform, 6);
                // Clicable: apuntar y pellizcar (o gatillo) lleva el carro a ese corredor
                var sb = sign.gameObject.AddComponent<Button>();
                sb.targetGraphic = sign.GetComponent<Image>();
                var cb = sb.colors; cb.highlightedColor = new Color(0.6f, 1f, 0.7f); sb.colors = cb;
            }

            // Techo de plástico difuso con canaletas (varias naves)
            var frame = new GameObject("Estructura").transform;
            frame.SetParent(env, false);
            var wallEndArc = TexMat("PlasticoCulataArco", PlasticTex(), new Vector2(1f, 0.4f), true, Color.white);
            const float gutter = 4.0f, ridge = 1.2f;
            // Columnas sobre el eje de las camas (x = ±2,25) y junto a las paredes: nunca dentro de un corredor
            float[] lines = { -6.75f, -2.25f, 2.25f, 6.75f };
            const float W = 6.85f;
            foreach (var lx in lines)
            {
                for (float z = 0f; z <= len; z += 5f)
                    Cyl("Columna", frame, new Vector3(lx, gutter / 2f, z), 0.08f, gutter, steel);
                Box("Canaleta", frame, new Vector3(lx, gutter, zc), new Vector3(0.25f, 0.12f, len), steel);
            }
            for (int i = 0; i < lines.Length - 1; i++)
            {
                float x0 = lines[i], x1 = lines[i + 1], mid = (x0 + x1) / 2f, half = (x1 - x0) / 2f;
                // Techo en arco (curvo) hecho de tramos: plástico, arcos de acero cada 2,5 m y cierre en las culatas
                const int segs = 12;
                Vector3 ArcPt(float t) => new Vector3(x0 + t * (x1 - x0), gutter + ridge * Mathf.Sin(Mathf.PI * t), 0f);
                for (int k = 0; k < segs; k++)
                {
                    var a = ArcPt((float)k / segs); var b = ArcPt((float)(k + 1) / segs);
                    var c = (a + b) * 0.5f; var d = b - a;
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    Box("TechoArco", frame, new Vector3(c.x, c.y, zc), new Vector3(d.magnitude + 0.01f, 0.02f, len), roof, Quaternion.Euler(0, 0, ang));
                    for (float z = 0f; z <= len; z += 2.5f)
                        Box("ArcoAcero", frame, new Vector3(c.x, c.y - 0.03f, z), new Vector3(d.magnitude + 0.01f, 0.035f, 0.035f), steel, Quaternion.Euler(0, 0, ang));
                    foreach (float ez in new[] { -1.5f, len + 1.5f })
                        Box("CierreArco", frame, new Vector3(c.x, gutter + (c.y - gutter) / 2f, ez), new Vector3(d.x + 0.01f, c.y - gutter, 0.02f), wallEndArc);
                }
                for (float z = 0f; z <= len; z += 2.5f)
                    Tube("Tensor", frame, new Vector3(x0, gutter, z), new Vector3(x1, gutter, z), 0.025f, steel);
            }
            // Cerramiento: plástico con pliegues y polvo abajo, zócalo, marco de acero y puerta
            var wallSide = TexMat("ParedPlastico", PlasticTex(), new Vector2((len + 3f) / 4f, 1f), true, Color.white);
            var wallEnd = TexMat("PlasticoCulata", PlasticTex(), new Vector2(15f / 4f, 1f), true, Color.white);
            var plinth = TexMatLit("ConcretoTex", ConcreteTex(), new Vector2(12f, 1f), 0.05f, 0f);
            foreach (float wx in new[] { -W, W })
            {
                Box("Pared", frame, new Vector3(wx, gutter / 2f, zc), new Vector3(0.02f, gutter, len + 3f), wallSide);
                Box("Zocalo", frame, new Vector3(wx - Mathf.Sign(wx) * 0.03f, 0.3f, zc), new Vector3(0.06f, 0.6f, len + 3f), plinth);
                for (float z = -1.5f; z <= len + 1.5f; z += 2.5f)
                    Cyl("Parante", frame, new Vector3(wx - Mathf.Sign(wx) * 0.05f, gutter / 2f, z), 0.06f, gutter, steel);
                foreach (float hy in new[] { 1.2f, 2.4f, 3.4f })
                    Box("Correa", frame, new Vector3(wx - Mathf.Sign(wx) * 0.06f, hy, zc), new Vector3(0.04f, 0.05f, len + 3f), steel);
            }
            foreach (float ez in new[] { -1.5f, len + 1.5f })
            {
                float inward = ez < 0 ? 1f : -1f;
                Box("Culata", frame, new Vector3(0, gutter / 2f, ez), new Vector3(2f * W, gutter, 0.02f), wallEnd);
                Box("ZocaloCulata", frame, new Vector3(0, 0.3f, ez + inward * 0.03f), new Vector3(2f * W, 0.6f, 0.06f), plinth);
                for (float x = -6.75f; x <= 6.76f; x += 1.5f)
                    Cyl("ParanteCulata", frame, new Vector3(x, gutter / 2f, ez + inward * 0.05f), 0.06f, gutter, steel);
                foreach (float hy in new[] { 1.2f, 2.4f, 3.4f })
                    Box("CorreaCulata", frame, new Vector3(0, hy, ez + inward * 0.06f), new Vector3(2f * W, 0.05f, 0.04f), steel);
                // Puerta corrediza al final del pasillo central
                Box("MarcoPuertaI", frame, new Vector3(-1.1f, 1.25f, ez + inward * 0.08f), new Vector3(0.08f, 2.5f, 0.08f), steel);
                Box("MarcoPuertaD", frame, new Vector3(1.1f, 1.25f, ez + inward * 0.08f), new Vector3(0.08f, 2.5f, 0.08f), steel);
                Box("DinteLPuerta", frame, new Vector3(0, 2.5f, ez + inward * 0.08f), new Vector3(2.3f, 0.08f, 0.08f), steel);
            }
            // Correas del techo a lo largo de las naves
            for (int i = 0; i < lines.Length - 1; i++)
            {
                float mid = (lines[i] + lines[i + 1]) / 2f;
                float x0c = lines[i], x1c = lines[i + 1];
                foreach (float t in new[] { 0.25f, 0.5f, 0.75f })
                {
                    float xx = x0c + t * (x1c - x0c), yy = gutter + ridge * Mathf.Sin(Mathf.PI * t) - 0.05f;
                    Tube("CorreaTecho", frame, new Vector3(xx, yy, 0f), new Vector3(xx, yy, len), 0.035f, steel);
                }
            }

            // Luz de día nublado de sabana: ambiente difuso, sin sombras en tiempo real (rendimiento)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.97f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.fogColor = new Color(0.88f, 0.90f, 0.88f);
            var sun = Object.FindFirstObjectByType<UnityEngine.Light>();
            if (sun != null) { sun.intensity = 0.55f; sun.shadows = LightShadows.None; sun.color = new Color(1f, 0.98f, 0.95f); sun.transform.rotation = Quaternion.Euler(70, 20, 0); }
            cam.backgroundColor = new Color(0.9f, 0.92f, 0.92f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        static GameObject Cluster(Transform parent, Mesh mesh, Material mat, Vector3 pos, float size, System.Random rng)
        {
            var g = new GameObject("Hojas", typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 30f - 15f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 20f - 10f);
            g.transform.localScale = Vector3.one * size;
            g.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.isStatic = true;
            return g;
        }

        // ── Rosas interactivas ──

        static int PlantRoses(float len, float pathW)
        {
            var meshes = BuildRoseMeshes();
            var root = new GameObject("Rosas").transform;
            var petal = Mat("RosaPetalo", new Color(0.62f, 0.05f, 0.12f));
            var inner = Mat("RosaPetaloInterno", new Color(0.45f, 0.03f, 0.09f));
            var green = Mat("RosaVerde", new Color(0.24f, 0.40f, 0.15f));
            var leaf = Mat("RosaHoja", new Color(0.15f, 0.33f, 0.12f));
            foreach (var m in new[] { petal, inner }) { if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f); }
            var lodBud = SpindleMesh();
            var lodBudMat = Mat("BotonSimple", new Color(0.55f, 0.04f, 0.10f));
            var stemMat = Mat("TalloSimple", new Color(0.24f, 0.40f, 0.15f));
            var leafMat = LeafMaterial();
            var cluster = ClusterMesh();
            var rng = new System.Random(23);
            float R() => (float)rng.NextDouble();
            int n = 0;
            float pitchR = pathW + 0.8f;
            foreach (float aisleX in new[] { -pitchR, 0f, pitchR })
            foreach (float side in new[] { -1f, 1f })
            {
                for (float z = 1.9f + (side > 0 ? 0.25f : 0f); z < len - 0.8f; z += 0.55f + R() * 0.15f)
                {
                    // Grado: ~35% en punto (3–4) para que haya decisiones en ambos sentidos
                    float u = R();
                    int grade = u < 0.12f ? 1 : u < 0.32f ? 2 : u < 0.50f ? 3 : u < 0.68f ? 4 : u < 0.86f ? 5 : 6;
                    float scale = 0.95f + R() * 0.2f;
                    float budTop = 1.9f + R() * 0.35f; // plantas de ~2,0–2,3 m: botones a la altura de la cabeza o por encima
                    var go = new GameObject($"Rosa_{n:000}");
                    go.transform.SetParent(root, false);
                    // En el borde de la cama, asomando al corredor
                    go.transform.localPosition = new Vector3(aisleX + side * (pathW / 2f - 0.01f + R() * 0.05f), budTop - 0.62f * scale, z);
                    go.transform.localRotation = Quaternion.Euler((R() - 0.5f) * 12f, R() * 360f, (R() - 0.5f) * 12f + side * -4f);
                    go.transform.localScale = Vector3.one * scale;

                    var lod0 = new GameObject("Detalle", typeof(MeshFilter), typeof(MeshRenderer));
                    lod0.transform.SetParent(go.transform, false);
                    lod0.GetComponent<MeshFilter>().sharedMesh = meshes[grade - 1];
                    var mr0 = lod0.GetComponent<MeshRenderer>();
                    mr0.sharedMaterials = new[] { petal, inner, green, leaf };
                    mr0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    var lod1 = new GameObject("Simple");
                    lod1.transform.SetParent(go.transform, false);
                    var st = Cyl("Tallo", lod1.transform, new Vector3(0, 0.28f, 0), 0.008f, 0.56f, stemMat);
                    st.isStatic = false;
                    var bud = new GameObject("Boton", typeof(MeshFilter), typeof(MeshRenderer));
                    bud.transform.SetParent(lod1.transform, false);
                    bud.transform.localPosition = new Vector3(0, 0.56f, 0);
                    float r = grade <= 2 ? 0.014f : grade <= 4 ? 0.019f : 0.03f;
                    bud.transform.localScale = new Vector3(r, grade >= 5 ? 0.045f : 0.055f, r);
                    bud.GetComponent<MeshFilter>().sharedMesh = lodBud;
                    bud.GetComponent<MeshRenderer>().sharedMaterial = lodBudMat;

                    var lg = go.AddComponent<LODGroup>();
                    lg.SetLODs(new[]
                    {
                        new LOD(0.16f, new Renderer[] { mr0 }),
                        new LOD(0.01f, new Renderer[] { st.GetComponent<Renderer>(), bud.GetComponent<Renderer>() }),
                    });
                    lg.RecalculateBounds();

                    var hr = go.AddComponent<HarvestRose>();
                    hr.id = $"R{n:000}";
                    hr.grade = grade;
                    hr.stemLengthCm = Mathf.Round(50 + R() * 30);
                    hr.budHeight = 0.59f * scale;
                    hr.budBase = 0.555f * scale;
                    hr.occluded = R() < 0.2f;
                    if (hr.occluded)
                        Cluster(go.transform, cluster, leafMat, go.transform.InverseTransformPoint(go.transform.position + go.transform.up * 0.57f * scale + Vector3.right * -side * 0.06f), 0.15f / scale, rng).isStatic = false;
                    n++;
                }
            }
            return n;
        }

        // ── Malla de la rosa: los 3 OBJ comparten topología → formas intermedias por grado ──

        static Mesh[] BuildRoseMeshes()
        {
            var paths = new[]
            {
                ModelsDir + "/rosa_1_triangulo_inmaduro.obj",
                ModelsDir + "/rosa_2_cuadrado_punto_corte.obj",
                ModelsDir + "/rosa_3_copa_pasado.obj",
            };
            var objs = new List<ObjData>();
            foreach (var p in paths) objs.Add(ReadObj(p));
            // Grado 1–6 como mezcla de las 3 formas (inmaduro → punto → pasado)
            var blends = new (int a, int b, float t)[] { (0, 1, 0f), (0, 1, 0.45f), (0, 1, 0.85f), (1, 2, 0.25f), (1, 2, 0.65f), (1, 2, 1f) };
            var result = new Mesh[6];
            for (int g = 0; g < 6; g++)
            {
                var path = $"{GenDir}/RosaGrado{g + 1}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing != null) { result[g] = existing; continue; }
                var (a, b, t) = blends[g];
                var A = objs[a]; var B = objs[b];
                int nv = A.v.Count;
                var verts = new Vector3[nv];
                var norms = new Vector3[nv];
                for (int i = 0; i < nv; i++)
                {
                    var pa = A.v[i]; var pb = B.v[i];
                    var p = Vector3.Lerp(pa, pb, t);
                    verts[i] = new Vector3(-p.x, p.y, p.z); // OBJ (mano derecha) → Unity
                    var na = i < A.n.Count ? A.n[i] : Vector3.up;
                    var nb = i < B.n.Count ? B.n[i] : na;
                    var nn = Vector3.Lerp(na, nb, t).normalized;
                    norms[i] = new Vector3(-nn.x, nn.y, nn.z);
                }
                var mesh = new Mesh { name = $"RosaGrado{g + 1}" };
                mesh.indexFormat = nv > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.vertices = verts;
                mesh.normals = norms;
                mesh.subMeshCount = 4;
                for (int s = 0; s < 4; s++) mesh.SetTriangles(A.tris[s], s);
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
                result[g] = mesh;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        class ObjData
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector3> n = new List<Vector3>();
            public List<int>[] tris = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        }

        static int SubmeshFor(string name)
        {
            name = (name ?? "").ToLowerInvariant();
            if (name.Contains("interno")) return 1;
            if (name.Contains("petalo")) return 0;
            if (name.Contains("hoja")) return 3;
            return 2; // tallo, sépalo, espina
        }

        static ObjData ReadObj(string path)
        {
            var d = new ObjData();
            var ci = CultureInfo.InvariantCulture;
            int sub = 2;
            foreach (var raw in File.ReadLines(path))
            {
                if (raw.Length < 2) continue;
                var line = raw.Trim();
                if (line.StartsWith("v "))
                {
                    var p = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    d.v.Add(new Vector3(float.Parse(p[1], ci), float.Parse(p[2], ci), float.Parse(p[3], ci)));
                }
                else if (line.StartsWith("vn "))
                {
                    var p = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    d.n.Add(new Vector3(float.Parse(p[1], ci), float.Parse(p[2], ci), float.Parse(p[3], ci)));
                }
                else if (line.StartsWith("o ") || line.StartsWith("usemtl "))
                    sub = SubmeshFor(line.Substring(line.IndexOf(' ') + 1));
                else if (line.StartsWith("f "))
                {
                    var p = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    var idx = new List<int>();
                    for (int k = 1; k < p.Length; k++)
                        idx.Add(int.Parse(p[k].Split('/')[0], ci) - 1);
                    for (int k = 1; k + 1 < idx.Count; k++)
                    {
                        // invertir el orden por el cambio de mano en X
                        d.tris[sub].Add(idx[0]); d.tris[sub].Add(idx[k + 1]); d.tris[sub].Add(idx[k]);
                    }
                }
            }
            return d;
        }

        // ── Follaje: textura de hojas generada + racimo de 3 planos cruzados ──

        static Material LeafMaterial()
        {
            // Preferir hojas reales recortadas de las fotos de la finca; si no existen, las generadas
            var foto = GenTexture("hojas_foto", true);
            var matPath = MatDir + (foto != null ? "/HojasFotoRacimo.mat" : "/HojasRacimo.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m != null) return m;
            var tex = foto != null ? foto : LeafTexture();
            // El proyecto usa el pipeline integrado: Standard en modo Cutout (URP Lit si existiera)
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            var sh = urp != null ? urp : Shader.Find("Standard");
            m = new Material(sh) { enableInstancing = true };
            m.mainTexture = tex;
            m.color = Color.white;
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            if (urp != null) { m.SetTexture("_BaseMap", tex); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cull", 0f); }
            else { m.SetFloat("_Mode", 1f); m.SetFloat("_Glossiness", 0.1f); m.SetOverrideTag("RenderType", "TransparentCutout"); }
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            AssetDatabase.CreateAsset(m, matPath);
            return m;
        }

        static Texture2D LeafTexture()
        {
            EnsureFolder(GenDir);
            var path = GenDir + "/hojas.png";
            if (!File.Exists(path))
            {
                const int S = 512;
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var px = new Color32[S * S];
                var rng = new System.Random(5);
                float R() => (float)rng.NextDouble();
                var greens = new[] { new Color(0.16f, 0.36f, 0.12f), new Color(0.22f, 0.46f, 0.16f), new Color(0.11f, 0.27f, 0.09f), new Color(0.28f, 0.50f, 0.20f) };
                var reddish = new Color(0.36f, 0.16f, 0.11f); // brotes nuevos rojizos (pocos)
                for (int leafI = 0; leafI < 70; leafI++)
                {
                    float cx = 50 + R() * (S - 100), cy = 50 + R() * (S - 100);
                    float L = 45 + R() * 45, W = L * (0.42f + R() * 0.1f), ang = R() * Mathf.PI * 2f;
                    var col = R() < 0.06f ? reddish : greens[rng.Next(greens.Length)];
                    float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                    int r = Mathf.CeilToInt(L) + 2;
                    for (int y = (int)cy - r; y <= cy + r; y++)
                        for (int x = (int)cx - r; x <= cx + r; x++)
                        {
                            if (x < 0 || y < 0 || x >= S || y >= S) continue;
                            float dx = x - cx, dy = y - cy;
                            float u = (dx * ca + dy * sa) / L;      // a lo largo (-1..1)
                            float v = (-dx * sa + dy * ca);          // a lo ancho (px)
                            if (u < -1f || u > 1f) continue;
                            float w = W * Mathf.Pow(Mathf.Max(0f, 1f - u * u), 0.75f) * (u > 0 ? 1f - u * 0.25f : 1f);
                            if (Mathf.Abs(v) > w) continue;
                            float shade = 0.82f + 0.35f * (1f - Mathf.Abs(v) / Mathf.Max(1f, w));
                            if (Mathf.Abs(v) < 1.3f) shade *= 0.8f;                     // nervadura
                            if (((int)(u * 9f) % 2 == 0) && Mathf.Abs(v) < w * 0.15f) shade *= 0.95f;
                            var c = col * shade;
                            px[y * S + x] = new Color(c.r, c.g, c.b, 1f);
                        }
                }
                t.SetPixels32(px);
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && !ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 512;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Mesh ClusterMesh()
        {
            var path = GenDir + "/RacimoHojas.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m != null) return m;
            EnsureFolder(GenDir);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var tr = new List<int>();
            for (int q = 0; q < 3; q++)
            {
                float a = q * 60f * Mathf.Deg2Rad;
                var right = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.5f;
                int b = v.Count;
                v.Add(-right + Vector3.down * 0.5f); v.Add(right + Vector3.down * 0.5f);
                v.Add(right + Vector3.up * 0.5f); v.Add(-right + Vector3.up * 0.5f);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int i = 0; i < 4; i++) n.Add(Vector3.up); // normales hacia arriba: luz suave en ambas caras
                tr.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                tr.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); // cara trasera (Standard no es de doble cara)
            }
            m = new Mesh { name = "RacimoHojas" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(n); m.SetTriangles(tr, 0);
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static List<Button> BuildFloatingMenu(Camera cam, params string[] options)
        {
            var c = AddWorldCanvas("MenuFlotante", new Vector2(110, 110), new Vector3(0.4f, 1.8f, 1f), Quaternion.identity, cam);
            c.transform.localScale = Vector3.one * 0.0007f; // más pequeño: va cerca, al alcance del dedo
            var icon = AddButton(c.transform, "Icono", "", Neutral, 110, 110, 10);
            Stretch((RectTransform)icon.transform);
            for (int i = 0; i < 3; i++)
            {
                var bar = NewUI("Raya" + i, icon.transform);
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(62, 9);
                bar.anchoredPosition = new Vector2(0, 20 - i * 20);
                var img = bar.gameObject.AddComponent<Image>();
                img.color = Color.white; img.raycastTarget = false;
            }
            var panel = NewUI("Opciones", c.transform);
            panel.anchorMin = panel.anchorMax = new Vector2(1, 0);
            panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = new Vector2(0, -12);
            int rows = (options.Length + 2) / 2;
            panel.sizeDelta = new Vector2(640, 28 + rows * 72);
            panel.gameObject.AddComponent<Image>().color = PanelBg;
            var grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(14, 14, 14, 14); grid.spacing = new Vector2(8, 8);
            grid.cellSize = new Vector2(302, 64);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            var list = new List<Button>();
            foreach (var o in options) list.Add(AddButton(panel, o, o, Primary, 302, 64, 24));
            var close = AddButton(panel, "Cerrar", "Cerrar", Light, 302, 64, 24);
            var fmc = c.gameObject.AddComponent<FloatingMenu>();
            fmc.iconButton = icon; fmc.panel = panel.gameObject; fmc.closeButton = close;
            var f = c.gameObject.AddComponent<FollowInView>();
            // Al alcance de la mano (para tocarlo con el dedo), arriba a la derecha
            f.distance = 0.55f; f.heightOffset = 0.12f; f.sideOffset = 0.32f; f.tiltDegrees = -15f; f.recenterAngle = 35f;
            return list;
        }

        static Material VignetteMaterial()
        {
            var path = MatDir + "/VinetaConfort.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            EnsureFolder(GenDir);
            var tpath = GenDir + "/vineta.png";
            if (!File.Exists(tpath))
            {
                const int S = 256;
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = (x - S / 2f) / (S / 2f), dy = (y - S / 2f) / (S / 2f);
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        px[y * S + x] = new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.95f, r)));
                    }
                t.SetPixels(px);
                File.WriteAllBytes(tpath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(tpath, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(tpath) is TextureImporter ti) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
            }
            m = new Material(Shader.Find("Sprites/Default")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tpath), color = new Color(1, 1, 1, 0) };
            m.renderQueue = 4000;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        const string AudioDir = Root + "/Audio";

        static void AddSounds(HarvestCart cart, Transform[] hands)
        {
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{n}.wav");
            var fx = new GameObject("Sonido").AddComponent<SoundFx>();
            fx.click = A("clic"); fx.good = A("bien"); fx.bad = A("mal"); fx.deposit = A("deposito");
            if (cart != null)
            {
                fx.ambient = A("ambiente"); fx.cartLoop = A("carro"); fx.leaves = A("hojas"); fx.step = A("paso");
                fx.cart = cart; fx.handsForLeaves = hands;
            }
        }

        // ── Texturas procedurales del invernadero ──

        static Texture2D ProcTexture(string name, int w, int h, System.Func<int, int, Color> f)
        {
            EnsureFolder(GenDir);
            var path = $"{GenDir}/{name}.png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(w, h, TextureFormat.RGB24, false);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
                t.SetPixels(px);
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti) { ti.wrapMode = TextureWrapMode.Repeat; ti.maxTextureSize = 512; ti.SaveAndReimport(); }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material TexMat(string name, Texture2D tex, Vector2 tiling, bool unlit, Color tint)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = unlit ? Shader.Find("Unlit/Texture") : Shader.Find("Standard");
            m = new Material(sh) { enableInstancing = true, mainTexture = tex, mainTextureScale = tiling };
            if (!unlit) { m.color = tint; m.SetFloat("_Glossiness", 0.05f); }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Textura de la carpeta Generado (p. ej. sacada de las fotos de la finca), con ajustes de importación.</summary>
        static Texture2D GenTexture(string name, bool alpha)
        {
            var path = $"{GenDir}/{name}.png";
            if (!File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && (ti.maxTextureSize != 1024 || ti.alphaIsTransparency != alpha))
            {
                ti.maxTextureSize = 1024;
                ti.alphaIsTransparency = alpha;
                ti.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material TexMatLit(string name, Texture2D tex, Vector2 tiling, float gloss, float metallic)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Standard")) { enableInstancing = true, mainTexture = tex, mainTextureScale = tiling, color = Color.white };
            m.SetFloat("_Glossiness", gloss);
            m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Texture2D WoodTex() => ProcTexture("madera", 128, 256, (x, y) =>
        {
            float warp = Mathf.PerlinNoise(x * 0.03f, y * 0.004f) * 6f;
            float grain = 0.5f + 0.5f * Mathf.Sin(x * 0.35f + warp * 2.2f);
            float fine = Mathf.PerlinNoise(x * 0.4f, y * 0.02f);
            float knot = Mathf.PerlinNoise(x * 0.06f + 40f, y * 0.03f) > 0.8f ? -0.12f : 0f;
            float v = 0.78f + grain * 0.14f + fine * 0.08f + knot;
            return new Color(0.62f * v, 0.48f * v, 0.33f * v);
        });

        static Texture2D MetalTex() => ProcTexture("acero", 128, 128, (x, y) =>
        {
            float brushed = Mathf.PerlinNoise(x * 0.02f, y * 0.9f) * 0.12f;
            float spots = Mathf.PerlinNoise(x * 0.08f + 7f, y * 0.08f) > 0.75f ? -0.06f : 0f;
            float v = 0.66f + brushed + spots;
            return new Color(v * 0.98f, v, v * 1.02f);
        });

        static Texture2D ConcreteTex() => ProcTexture("concreto", 128, 128, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.4f;
            float pit = Mathf.PerlinNoise(x * 0.9f + 3f, y * 0.9f) > 0.82f ? -0.12f : 0f;
            float v = 0.38f + n * 0.18f + pit;
            return new Color(v, v, v * 0.98f);
        });

        static Texture2D BlueTex() => ProcTexture("plastico_azul", 128, 128, (x, y) =>
        {
            float rib = (x % 32) < 3 ? -0.08f : 0f;
            float n = (Mathf.PerlinNoise(x * 0.07f, y * 0.07f) - 0.5f) * 0.08f;
            float v = 1f + rib + n;
            return new Color(0.06f * v, 0.32f * v, 0.82f * v);
        });

        static Texture2D PlasticTex() => ProcTexture("plastico", 256, 256, (x, y) =>
        {
            float fold = 0.04f * Mathf.Sin(x / 256f * Mathf.PI * 2f * 6f + Mathf.PerlinNoise(x * 0.02f, 3f) * 3f);
            float cloud = (Mathf.PerlinNoise(x * 0.015f, y * 0.015f) - 0.5f) * 0.08f;
            float dirt = y < 50 ? (50 - y) / 50f * 0.25f * Mathf.PerlinNoise(x * 0.08f, y * 0.08f) : 0f;
            float v = 0.93f + fold + cloud;
            return new Color(v * 0.97f - dirt * 0.5f, v - dirt * 0.6f, v * 0.95f - dirt * 0.8f);
        });

        static Texture2D RoofTex() => ProcTexture("techo", 256, 256, (x, y) =>
        {
            float seam = (y % 128) < 3 ? -0.18f : 0f;
            float cloud = (Mathf.PerlinNoise(x * 0.01f, y * 0.01f) - 0.5f) * 0.07f;
            float v = 0.95f + cloud + seam;
            return new Color(v, v, v * 0.97f);
        });

        static Texture2D SoilTex() => ProcTexture("tierra", 256, 256, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f + Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.4f;
            float pebble = Mathf.PerlinNoise(x * 0.6f + 9f, y * 0.6f) > 0.78f ? 0.12f : 0f;
            float v = 0.12f + n * 0.12f + pebble;
            return new Color(v * 1.25f, v * 1.0f, v * 0.8f);
        });

        /// <summary>Botón simple de bajo costo (huso de 8 lados) para LOD lejano y botones decorativos.</summary>
        static Mesh SpindleMesh()
        {
            var path = GenDir + "/BotonSimple.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m != null) return m;
            EnsureFolder(GenDir);
            const int sides = 8;
            float[] ys = { 0f, 0.25f, 0.6f, 0.85f, 1f };
            float[] rs = { 0.25f, 0.9f, 1f, 0.7f, 0.05f };
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int r = 0; r < ys.Length; r++)
                for (int s = 0; s < sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    v.Add(new Vector3(Mathf.Cos(a) * rs[r], ys[r], Mathf.Sin(a) * rs[r]));
                }
            for (int r = 0; r < ys.Length - 1; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a0 = r * sides + s, a1 = r * sides + (s + 1) % sides, b0 = a0 + sides, b1 = a1 + sides;
                    tr.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
            m = new Mesh { name = "BotonSimple" };
            m.SetVertices(v); m.SetTriangles(tr, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
