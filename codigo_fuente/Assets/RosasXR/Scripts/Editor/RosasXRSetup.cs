using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RosasXR.EditorTools
{
    /// <summary>
    /// Menú "Rosas XR" en la barra de Unity:
    ///  0. Tema de marca (logo y colores).
    ///  1. Crea los catálogos: estados de corte, color y tamaño.
    ///  2. Importa las fotos de Assets/RosasXR/Fotos/ y sus etiquetas (etiquetas.csv) al dataset.
    ///  3. Construye prefabs y las tres escenas (Menu, Simulador, PracticaYOLO) ya cableadas.
    /// </summary>
    public static partial class RosasXRSetup
    {
        const string Root = "Assets/RosasXR";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string MatDir = Root + "/Materials";
        const string PhotoDir = Root + "/Fotos";
        const string CatalogPath = DataDir + "/RoseClassCatalog.asset";
        const string DatasetPath = DataDir + "/SimulatorDataset.asset";
        const string ColorCatalogPath = DataDir + "/ColorCatalog.asset";
        const string SizeCatalogPath = DataDir + "/SizeCatalog.asset";
        const string LengthCatalogPath = DataDir + "/LengthCatalog.asset";
        const string LabelsCsvPath = PhotoDir + "/etiquetas.csv";

        const string ThemePath = DataDir + "/BrandTheme.asset";

        static BrandTheme _theme;
        static BrandTheme Theme => _theme != null ? _theme : (_theme = LoadOrCreateTheme());
        static Color PanelBg => Theme.panelBackground;
        static Color Neutral => Theme.neutral;
        static Color Light => Theme.light;
        static Color Primary => Theme.primary;
        static Color Secondary => Theme.secondary;
        static readonly Color TextDark = new Color(0.08f, 0.08f, 0.08f);

        // ───────────────────────── 0. Marca ─────────────────────────

        [MenuItem("Rosas XR/0. Crear o editar tema de marca (logo y colores)", priority = 0)]
        public static void ThemeMenu() => Selection.activeObject = LoadOrCreateTheme();

        static BrandTheme LoadOrCreateTheme()
        {
            var t = AssetDatabase.LoadAssetAtPath<BrandTheme>(ThemePath);
            if (t != null) return t;
            EnsureFolder(DataDir);
            t = ScriptableObject.CreateInstance<BrandTheme>();
            AssetDatabase.CreateAsset(t, ThemePath);
            AssetDatabase.SaveAssets();
            return t;
        }

        const string LogoPath = Root + "/Branding/logo_ght.png";

        /// <summary>Si existe Branding/logo_ght.png, lo importa como Sprite y lo pone en el tema de marca.</summary>
        static void AssignBrandLogo()
        {
            if (!File.Exists(LogoPath)) return;
            AssetDatabase.ImportAsset(LogoPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(LogoPath) is TextureImporter ti &&
                (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            if (sprite != null && Theme.logo != sprite)
            {
                Theme.logo = sprite;
                EditorUtility.SetDirty(Theme);
                AssetDatabase.SaveAssets();
            }
        }

        static void AddLogo(Transform parent, float height)
        {
            if (Theme.logo == null) return;
            var rt = NewUI("Logo", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.logo;
            img.preserveAspect = true;
            img.raycastTarget = false;
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        // ───────────────────────── 1. Catálogo ─────────────────────────

        [MenuItem("Rosas XR/1. Crear catálogos (corte, color y tamaño)", priority = 1)]
        public static void CreateDefaultCatalogMenu()
        {
            LoadOrCreateColorCatalog();
            LoadOrCreateSizeCatalog();
            LoadOrCreateLengthCatalog();
            Selection.activeObject = LoadOrCreateCatalog();
            EditorUtility.DisplayDialog("Rosas XR",
                "Catálogos creados en Assets/RosasXR/Data:\n\n" +
                "· RoseClassCatalog: estados de corte (Etapa 2 y modelo YOLO)\n" +
                "· ColorCatalog: colores (Etapa 1)\n" +
                "· SizeCatalog: tamaños (Etapa 1)\n" +
                "· LengthCatalog: largos de corte (Etapa 2)\n\n" +
                "Ajústalos con el agrónomo antes de etiquetar las fotos.", "OK");
        }

        static AttributeCatalog LoadOrCreateColorCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttributeCatalog>(ColorCatalogPath);
            if (existing != null) return existing;
            EnsureFolder(DataDir);
            var cat = ScriptableObject.CreateInstance<AttributeCatalog>();
            cat.attributeName = "Color";
            cat.question = "¿De qué color es esta rosa?";
            cat.options = new List<AttributeOption>
            {
                new AttributeOption { id = "rojo", displayName = "Rojo", color = new Color(0.80f, 0.15f, 0.18f),
                    explanation = "[Ajustar] Rojo intenso en todo el botón, sin tonos rosados en el borde de los pétalos." },
                new AttributeOption { id = "rosado", displayName = "Rosado", color = new Color(0.96f, 0.62f, 0.72f),
                    explanation = "[Ajustar] Tono rosa uniforme; más claro que el rojo y sin vetas de otro color." },
                new AttributeOption { id = "blanco", displayName = "Blanco / crema", color = new Color(0.96f, 0.95f, 0.90f),
                    explanation = "[Ajustar] Pétalos blancos o crema; el centro puede ser levemente más cálido." },
                new AttributeOption { id = "amarillo", displayName = "Amarillo", color = new Color(0.98f, 0.86f, 0.30f),
                    explanation = "[Ajustar] Amarillo en todo el botón." },
                new AttributeOption { id = "naranja", displayName = "Naranja / durazno", color = new Color(0.97f, 0.62f, 0.35f),
                    explanation = "[Ajustar] Tonos naranja o durazno; no confundir con amarillo bajo luz cálida." },
                new AttributeOption { id = "bicolor", displayName = "Bicolor", color = new Color(0.75f, 0.62f, 0.85f),
                    explanation = "[Ajustar] Dos colores claramente distintos (borde y centro, o vetas)." },
            };
            AssetDatabase.CreateAsset(cat, ColorCatalogPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        static AttributeCatalog LoadOrCreateLengthCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttributeCatalog>(LengthCatalogPath);
            if (existing != null) return existing;
            EnsureFolder(DataDir);
            var cat = ScriptableObject.CreateInstance<AttributeCatalog>();
            cat.attributeName = "Largo de corte";
            cat.question = "¿A qué largo de tallo cortas?";
            cat.options = new List<AttributeOption>();
            var tonos = new[] { 0.80f, 0.72f, 0.64f, 0.56f, 0.48f, 0.40f };
            int i = 0;
            foreach (var cm in new[] { 40, 50, 60, 70, 80, 90 })
            {
                cat.options.Add(new AttributeOption
                {
                    id = cm.ToString(),
                    displayName = cm + " cm",
                    color = new Color(tonos[i] * 0.75f, tonos[i], tonos[i] * 0.8f),
                    explanation = "[Validar con experto] El tallo debía cortarse a " + cm + " cm, por encima de la hoja de cinco foliolos, " +
                                  "dejando yemas sanas para el siguiente brote. Mide desde el punto de corte hasta la base del botón."
                });
                i++;
            }
            AssetDatabase.CreateAsset(cat, LengthCatalogPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        static AttributeCatalog LoadOrCreateSizeCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttributeCatalog>(SizeCatalogPath);
            if (existing != null) return existing;
            EnsureFolder(DataDir);
            var cat = ScriptableObject.CreateInstance<AttributeCatalog>();
            cat.attributeName = "Tamaño del botón";
            cat.question = "¿De qué tamaño es el botón?";
            cat.options = new List<AttributeOption>
            {
                new AttributeOption { id = "pequeno", displayName = "Pequeño", color = new Color(0.80f, 0.88f, 0.80f),
                    explanation = "[Ajustar con medida real] Botón angosto; compáralo con el ancho de la mano o una referencia fija." },
                new AttributeOption { id = "mediano", displayName = "Mediano", color = new Color(0.62f, 0.80f, 0.66f),
                    explanation = "[Ajustar con medida real] Tamaño estándar de la variedad." },
                new AttributeOption { id = "grande", displayName = "Grande", color = new Color(0.44f, 0.68f, 0.52f),
                    explanation = "[Ajustar con medida real] Botón ancho y largo; suele ir a los grados más altos." },
            };
            AssetDatabase.CreateAsset(cat, SizeCatalogPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        static RoseClassCatalog LoadOrCreateCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RoseClassCatalog>(CatalogPath);
            if (existing != null) return existing;

            EnsureFolder(DataDir);
            var cat = ScriptableObject.CreateInstance<RoseClassCatalog>();
            // Tres estados ordinales del modelo del equipo (MobileNetV3-Small + umbrales).
            // Los id coinciden con las carpetas del dataset de entrenamiento (datos/train/0_insuficiente...),
            // así las mismas fotos sirven para el simulador.
            cat.classes = new List<RoseClass>
            {
                new RoseClass
                {
                    id = "0_insuficiente", displayName = "Insuficiente", modelClassIndex = 0, shouldCut = false,
                    color = new Color(0.62f, 0.80f, 0.55f), recommendation = "ESPERAR",
                    explanation = "[Validar con experto] Botón con silueta mayormente triangular o cerrada. Todavía no está en punto: se deja en la planta."
                },
                new RoseClass
                {
                    id = "1_optima", displayName = "Óptima", modelClassIndex = 1, shouldCut = true,
                    color = new Color(0.98f, 0.82f, 0.45f), recommendation = "EN PUNTO: verificar color y localizar corte",
                    explanation = "[Validar con experto] Copa moderadamente abierta: es el punto comercial para exportación a Estados Unidos."
                },
                new RoseClass
                {
                    id = "2_excesiva", displayName = "Excesiva", modelClassIndex = 2, shouldCut = false,
                    color = new Color(0.95f, 0.60f, 0.50f), recommendation = "NO CORTAR COMO ÓPTIMA",
                    explanation = "[Validar con experto] Pétalos demasiado desplegados para el objetivo de exportación."
                },
            };
            AssetDatabase.CreateAsset(cat, CatalogPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        // ───────────────────────── 2. Fotos ─────────────────────────

        [MenuItem("Rosas XR/2. Importar fotos al dataset del simulador", priority = 2)]
        public static void ImportPhotos()
        {
            // Las fotos suelen copiarse desde fuera del editor: refrescar para que Unity las importe.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = LoadOrCreateCatalog();
            var colors = LoadOrCreateColorCatalog();
            var sizes = LoadOrCreateSizeCatalog();
            var lengths = LoadOrCreateLengthCatalog();
            EnsureFolder(PhotoDir);
            foreach (var c in catalog.classes) EnsureFolder(PhotoDir + "/" + c.id);
            EnsureFolder(PhotoDir + "/" + UnlabeledFolder);

            // 1. Recorrer las fotos. La carpeta indica el estado de corte (Etapa 2);
            //    las fotos en _sin_estado solo sirven para la Etapa 1.
            var files = new List<(string rel, string assetPath, string classId)>();
            foreach (var dir in Directory.GetDirectories(PhotoDir))
            {
                var folder = Path.GetFileName(dir);
                string classId = catalog.GetById(folder) != null ? folder : "";
                foreach (var file in Directory.GetFiles(dir))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") continue;
                    files.Add((folder + "/" + Path.GetFileName(file), file.Replace('\\', '/'), classId));
                }
            }

            // 2. Leer o crear la planilla de etiquetas de color y tamaño.
            var labels = ReadLabels(out bool csvExisted);
            int added = WriteLabelsTemplate(files.ConvertAll(f => f.rel), labels);

            // 3. Construir el dataset.
            var dataset = LoadOrCreateDataset();
            dataset.samples.Clear();
            int withClass = 0, withColor = 0, withSize = 0, withLength = 0;
            var badValues = new HashSet<string>();

            foreach (var f in files)
            {
                if (AssetImporter.GetAtPath(f.assetPath) is TextureImporter ti && ti.maxTextureSize != 512)
                {
                    // 512 px basta para el visor y ahorra mucha memoria en el Quest.
                    ti.maxTextureSize = 512;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(f.assetPath);
                if (tex == null) continue;

                labels.TryGetValue(f.rel, out var lab);
                string colorId = Clean(lab.color), sizeId = Clean(lab.size), lengthId = Clean(lab.length).Replace("cm", "").Trim();
                if (colorId != "" && colors.GetById(colorId) == null) { badValues.Add("color '" + colorId + "'"); colorId = ""; }
                if (sizeId != "" && sizes.GetById(sizeId) == null) { badValues.Add("tamaño '" + sizeId + "'"); sizeId = ""; }
                if (lengthId != "" && lengths.GetById(lengthId) == null) { badValues.Add("largo '" + lengthId + "'"); lengthId = ""; }

                dataset.samples.Add(new RoseSample
                {
                    sampleId = Path.ChangeExtension(f.rel, null),
                    photo = tex,
                    classId = f.classId,
                    colorId = colorId,
                    sizeId = sizeId,
                    lengthId = lengthId
                });
                if (f.classId != "") withClass++;
                if (colorId != "") withColor++;
                if (sizeId != "") withSize++;
                if (lengthId != "") withLength++;
            }

            int models3D = ImportRoseModels(dataset, catalog);

            EditorUtility.SetDirty(dataset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(LabelsCsvPath);

            var msg = $"Fotos importadas: {dataset.samples.Count}\n" +
                      $"· Con estado de corte (Etapa 2): {withClass}\n" +
                      $"· Con color (Etapa 1): {withColor}\n" +
                      $"· Con tamaño (Etapa 1): {withSize}\n" +
                      $"· Con largo de corte (Etapa 2): {withLength}\n" +
                      $"· Rosas 3D (Etapa 2): {models3D}";
            if (dataset.samples.Count == 0)
                msg += $"\n\nPon las fotos recortadas en {PhotoDir}/<estado>/ (o en {UnlabeledFolder}/ si solo son para la Etapa 1) y vuelve a ejecutar.";
            else if (!csvExisted || added > 0)
                msg += $"\n\n{(csvExisted ? "Agregué " + added + " fotos nuevas a" : "Creé")} {LabelsCsvPath}. " +
                       "Ábrela en Excel, llena las columnas color, tamano y largo (en cm: 40, 50, 60...) con los id de los catálogos, guárdala y vuelve a ejecutar este paso.";
            if (badValues.Count > 0)
                msg += "\n\nValores que no existen en los catálogos (se ignoraron): " + string.Join(", ", badValues);
            EditorUtility.DisplayDialog("Rosas XR", msg, "OK");
        }

        const string UnlabeledFolder = "_sin_estado";
        const string ModelsDir = Root + "/Modelos3D";

        /// <summary>
        /// Agrega al dataset los modelos 3D de Assets/RosasXR/Modelos3D. El estado se deduce del nombre del archivo:
        /// "inmaduro"/"insuficiente" → 0, "punto_corte"/"optima" → 1, "pasado"/"excesiva" → 2.
        /// </summary>
        static int ImportRoseModels(SimulatorDataset dataset, RoseClassCatalog catalog)
        {
            if (!AssetDatabase.IsValidFolder(ModelsDir)) return 0;
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                int idx = name.Contains("inmaduro") || name.Contains("insuficiente") ? 0
                        : name.Contains("punto") || name.Contains("optima") ? 1
                        : name.Contains("pasado") || name.Contains("excesiva") ? 2 : -1;
                var rc = catalog.GetByModelIndex(idx);
                if (rc == null) continue;

                if (AssetImporter.GetAtPath(path) is ModelImporter mi && (mi.importAnimation || mi.addCollider || mi.isReadable))
                {
                    mi.importAnimation = false; mi.addCollider = false; mi.isReadable = false;
                    mi.SaveAndReimport();
                }
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                dataset.samples.Add(new RoseSample
                {
                    sampleId = "3D/" + Path.GetFileNameWithoutExtension(path),
                    model = go,
                    classId = rc.id,
                    colorId = "rojo",
                    // Largo de corte correcto para la rosa en punto: ajustar con el agrónomo en Data/SimulatorDataset
                    lengthId = rc.shouldCut ? "50" : ""
                });
                count++;
            }
            return count;
        }

        static string Clean(string v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim().ToLowerInvariant();

        /// <summary>Lee Fotos/etiquetas.csv (separador ; o ,). Columnas: archivo;color;tamano.</summary>
        static Dictionary<string, (string color, string size, string length)> ReadLabels(out bool existed)
        {
            var result = new Dictionary<string, (string, string, string)>();
            existed = File.Exists(LabelsCsvPath);
            if (!existed) return result;

            var lines = File.ReadAllLines(LabelsCsvPath);
            if (lines.Length == 0) return result;
            char sep = lines[0].Contains(";") ? ';' : ',';
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(sep);
                if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0])) continue;
                var key = parts[0].Trim().Trim('"').Replace('\\', '/');
                result[key] = (parts.Length > 1 ? parts[1].Trim().Trim('"') : "",
                               parts.Length > 2 ? parts[2].Trim().Trim('"') : "",
                               parts.Length > 3 ? parts[3].Trim().Trim('"') : "");
            }
            return result;
        }

        /// <summary>Agrega a la planilla las fotos que aún no aparecen. Devuelve cuántas agregó.</summary>
        static int WriteLabelsTemplate(List<string> files, Dictionary<string, (string color, string size, string length)> labels)
        {
            bool exists = File.Exists(LabelsCsvPath);
            var sb = new System.Text.StringBuilder();
            if (!exists) sb.AppendLine("archivo;color;tamano;largo");
            int added = 0;
            foreach (var rel in files)
            {
                if (labels.ContainsKey(rel)) continue;
                sb.AppendLine(rel + ";;;");
                labels[rel] = ("", "", "");
                added++;
            }
            if (!exists)
                File.WriteAllText(LabelsCsvPath, sb.ToString(), new System.Text.UTF8Encoding(true));
            else if (added > 0)
                File.AppendAllText(LabelsCsvPath, sb.ToString(), new System.Text.UTF8Encoding(false));
            return added;
        }

        static SimulatorDataset LoadOrCreateDataset()
        {
            var ds = AssetDatabase.LoadAssetAtPath<SimulatorDataset>(DatasetPath);
            if (ds != null) return ds;
            EnsureFolder(DataDir);
            ds = ScriptableObject.CreateInstance<SimulatorDataset>();
            AssetDatabase.CreateAsset(ds, DatasetPath);
            return ds;
        }

        // ───────────────────────── 3. Escenas ─────────────────────────

        [MenuItem("Rosas XR/3. Construir escenas y prefabs", priority = 3)]
        public static void BuildAll()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("Rosas XR",
                    "Primero importa TextMeshPro:\nWindow > TextMeshPro > Import TMP Essential Resources.\n\nLuego ejecuta este paso de nuevo.",
                    "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            _theme = LoadOrCreateTheme();
            AssignBrandLogo();
            PlayerSettings.companyName = "GHT";
            PlayerSettings.productName = "Rosas XR GHT";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "co.ght.rosasxr");
            var catalog = LoadOrCreateCatalog();
            var colors = LoadOrCreateColorCatalog();
            var sizes = LoadOrCreateSizeCatalog();
            var lengths = LoadOrCreateLengthCatalog();
            var dataset = LoadOrCreateDataset();
            EnsureFolder(PrefabDir);
            EnsureFolder(SceneDir);
            EnsureFolder(MatDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var classBtn = BuildClassButtonPrefab();
            var card = BuildRoseCardPrefab();
            var label = BuildDetectionLabelPrefab();

            var menu = BuildHomeScene();
            var e1 = BuildStage1Scene();
            var cosecha = BuildHarvestScene();
            var prac = BuildPracticeScene(catalog, classBtn, label);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menu, true),
                new EditorBuildSettingsScene(e1, true),
                new EditorBuildSettingsScene(cosecha, true),
                new EditorBuildSettingsScene(prac, true),
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(menu);
            EditorUtility.DisplayDialog("Rosas XR",
                "Listo: escenas Menu, Etapa1, Cosecha y PracticaYOLO creadas y agregadas al Build.\n\n" +
                "Prueba con Play en el editor: clic izquierdo para pulsar, clic derecho + mouse para mirar.",
                "OK");
        }

        // ── Prefabs ──

        static Button BuildClassButtonPrefab()
        {
            var b = AddButton(null, "ClassButton", "Clase", Light, 560, 90, 40);
            return SavePrefab<Button>(b.gameObject, PrefabDir + "/ClassButton.prefab");
        }

        static RoseSpecimen BuildRoseCardPrefab()
        {
            var rt = NewUI("RoseCard", null);
            rt.sizeDelta = new Vector2(220, 270);
            var frame = rt.gameObject.AddComponent<Image>();
            frame.color = new Color(0.93f, 0.92f, 0.86f);

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = frame;
            var colors = b.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.6f);
            colors.disabledColor = Color.white; // para que se vea el color de acierto/error
            b.colors = colors;

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220;
            le.preferredHeight = 270;

            var ph = NewUI("Foto", rt);
            Stretch(ph, 10);
            var raw = ph.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;

            var spec = rt.gameObject.AddComponent<RoseSpecimen>();
            spec.photo = raw;
            spec.frame = frame;
            spec.correctColor = Theme.correct;
            spec.wrongColor = Theme.wrong;
            return SavePrefab<RoseSpecimen>(rt.gameObject, PrefabDir + "/RoseCard.prefab");
        }

        static DetectionLabel BuildDetectionLabelPrefab()
        {
            var canvas = AddWorldCanvas("DetectionLabel", new Vector2(380, 80), Vector3.zero, Quaternion.identity, null);
            var bg = canvas.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;
            var t = AddText(canvas.transform, "Texto", "clase 00%", 36, TextDark, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 6);
            t.raycastTarget = false;
            var dl = canvas.gameObject.AddComponent<DetectionLabel>();
            dl.label = t;
            dl.background = bg;
            return SavePrefab<DetectionLabel>(canvas.gameObject, PrefabDir + "/DetectionLabel.prefab");
        }

        // ── Escena: Menú ──

        static string BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(true);

            var canvas = AddWorldCanvas("Menu", new Vector2(1000, Theme.logo != null ? 1200 : 1050), new Vector3(0, 1.45f, 1.2f), Quaternion.identity, cam);
            var p = AddPanel(canvas.transform, PanelBg, new RectOffset(40, 40, 40, 40), 22);

            AddLogo(p, 130);
            AddText(p, "Titulo", "Entrenamiento de corte de rosas", 58, Color.white, TextAlignmentOptions.Center, 90).fontStyle = FontStyles.Bold;
            AddText(p, "Subtitulo", "Elige el operario y el modo de práctica", 34, new Color(0.8f, 0.85f, 0.8f), TextAlignmentOptions.Center, 60);

            var row = NewUI("Operario", p);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            var prev = AddButton(row, "Anterior", "<", Neutral, 120, 100, 50);
            var lbl = AddText(row, "Etiqueta", "Operario: OP-01", 42, Color.white, TextAlignmentOptions.Center);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var next = AddButton(row, "Siguiente", ">", Neutral, 120, 100, 50);

            var e1 = AddButton(p, "Etapa1", "Etapa 1 · Identificar color y tamaño\n<size=70%>Simulador con rosas de referencia</size>",
                Primary, 900, 150, 42);
            var e2 = AddButton(p, "Etapa2", "Etapa 2 · Decidir el corte\n<size=70%>Simulador con rosas de referencia</size>",
                Primary, 900, 150, 42);
            var m2 = AddButton(p, "Practica", "Práctica con rosas reales\n<size=70%>Compara tu criterio con el modelo YOLO</size>",
                Secondary, 900, 150, 42);

            var mm = canvas.gameObject.AddComponent<MainMenu>();
            mm.operatorLabel = lbl;
            UnityEventTools.AddPersistentListener(prev.onClick, new UnityAction(mm.PreviousOperator));
            UnityEventTools.AddPersistentListener(next.onClick, new UnityAction(mm.NextOperator));
            UnityEventTools.AddPersistentListener(e1.onClick, new UnityAction(mm.StartStage1));
            UnityEventTools.AddPersistentListener(e2.onClick, new UnityAction(mm.StartStage2));
            UnityEventTools.AddPersistentListener(m2.onClick, new UnityAction(mm.StartPractice));

            var path = SceneDir + "/Menu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ── Escena: Simulador ──

        static string BuildSimulatorScene(RoseClassCatalog catalog, AttributeCatalog colors, AttributeCatalog sizes,
            AttributeCatalog lengths, SimulatorDataset dataset, Button classBtn, RoseSpecimen card)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(false);

            var beds = BuildGreenhouse(cam);

            var dp = BuildDecisionPanel(cam, catalog, classBtn);
            var fb = BuildFeedbackPanel(cam);

            var ctl = new GameObject("SimulatorController").AddComponent<SimulatorController>();
            ctl.catalog = catalog;
            ctl.colorCatalog = colors;
            ctl.sizeCatalog = sizes;
            ctl.lengthCatalog = lengths;
            ctl.dataset = dataset;
            ctl.cardPrefab = card;
            ctl.beds = beds;
            ctl.roseSlots = _roseSlots;
            ctl.decisionPanel = dp;
            ctl.feedbackPanel = fb;

            var hud = BuildHud(cam, new Vector3(0, 1.2f, 1.0f), Quaternion.Euler(25, 0, 0),
                ("Reiniciar", Neutral, new UnityAction(ctl.Restart)),
                ("Menú", Neutral, new UnityAction(ctl.GoToMenu)));
            ctl.statusText = hud.status;
            var follow = hud.canvas.gameObject.AddComponent<FollowInView>();
            follow.distance = 1.0f; follow.heightOffset = -0.5f; follow.tiltDegrees = 30f;

            var path = SceneDir + "/Simulador.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ── Escena: Práctica YOLO ──

        static string BuildPracticeScene(RoseClassCatalog catalog, Button classBtn, DetectionLabel labelPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = SetupBase(true);

            var ext = new GameObject("DeteccionesYOLO (modelo real)").AddComponent<ExternalDetectionSource>();
            var mock = new GameObject("DeteccionesSimuladas (solo Editor)").AddComponent<MockDetectionSource>();
            mock.catalog = catalog;

            // Cámara + mira + clasificador
            // Fuentes de imagen: passthrough de Meta en el visor, webcam en el editor.
            var camRoot = new GameObject("Camara");
            var metaGo = new GameObject("PassthroughCamera (visor)");
            metaGo.transform.SetParent(camRoot.transform);
            var access = metaGo.AddComponent<Meta.XR.PassthroughCameraAccess>();
            var metaFeed = metaGo.AddComponent<MetaPassthroughFeed>();
            metaFeed.cameraAccess = access;
            var webGo = new GameObject("Webcam (solo editor)");
            webGo.transform.SetParent(camRoot.transform);
            var webFeed = webGo.AddComponent<WebCamFeed>();
            var feed = camRoot.AddComponent<FirstReadyFeed>();
            feed.feeds = new List<CameraFeed> { metaFeed, webFeed };
            var capture = camRoot.AddComponent<CropCapture>();
            capture.feed = feed;
            var reticle = BuildReticle(cam);
            var classifier = AddByName("RosasXR.RoseClassifier", "ClasificadorRosas (asignar .onnx)");
            if (classifier != null)
            {
                SetField(classifier, "feed", feed);
                SetField(classifier, "output", ext);
                SetField(classifier, "cropPreview", reticle.preview);
                var onnx = AssetDatabase.LoadMainAssetAtPath(Root + "/Modelos/modelo_rosas.onnx");
                if (onnx != null) SetField(classifier, "model", onnx);
                // El modelo del equipo normaliza por dentro ((x-0.5)/0.5): se le entrega RGB crudo en 0..1
                SetField(classifier, "mean", Vector3.zero);
                SetField(classifier, "std", Vector3.one);
            }

            var dp = BuildDecisionPanel(cam, catalog, classBtn);
            var fb = BuildFeedbackPanel(cam);

            var ctl = new GameObject("PracticeController").AddComponent<PracticeController>();
            ctl.catalog = catalog;
            ctl.sources = new List<DetectionSource> { ext, mock };
            ctl.decisionPanel = dp;
            ctl.feedbackPanel = fb;
            ctl.reticle = reticle;
            ctl.capture = capture;
            ctl.labelPrefab = labelPrefab;

            // Solo un botón "Menú" en la esquina superior derecha de la vista; el resultado va bajo la mira
            ctl.liveResultMode = true;
            var back = AddWorldCanvas("BotonMenu", new Vector2(260, 100), new Vector3(0.4f, 1.85f, 1.0f), Quaternion.identity, cam);
            var backBtn = AddButton(back.transform, "Menu", "< Menú", Neutral, 260, 100, 40);
            Stretch((RectTransform)backBtn.transform);
            UnityEventTools.AddPersistentListener(backBtn.onClick, new UnityAction(ctl.GoToMenu));
            var followB = back.gameObject.AddComponent<FollowInView>();
            followB.distance = 1.1f; followB.heightOffset = 0.32f; followB.sideOffset = 0.42f;
            followB.tiltDegrees = -12f; followB.recenterAngle = 25f;

            var path = SceneDir + "/PracticaYOLO.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ── Invernadero industrial ──

        static List<Transform> _roseSlots;

        static List<Transform> BuildGreenhouse(Camera cam)
        {
            var env = new GameObject("Invernadero industrial").transform;

            var soil = Mat("Suelo", new Color(0.33f, 0.27f, 0.20f));
            var walk = Mat("Camino", new Color(0.50f, 0.47f, 0.40f));
            var bedSoil = Mat("CamaTierra", new Color(0.23f, 0.17f, 0.11f));
            var leafA = Mat("FollajeA", new Color(0.15f, 0.34f, 0.13f));
            var leafB = Mat("FollajeB", new Color(0.19f, 0.40f, 0.16f));
            var stem = Mat("Tallo", new Color(0.24f, 0.42f, 0.17f));
            var budR = Mat("BotonRojo", new Color(0.70f, 0.07f, 0.13f));
            var budP = Mat("BotonRosado", new Color(0.93f, 0.55f, 0.66f));
            var budW = Mat("BotonBlanco", new Color(0.95f, 0.93f, 0.86f));
            var steel = Mat("Acero", new Color(0.66f, 0.68f, 0.70f));
            var roof = Mat("PlasticoTecho", new Color(0.93f, 0.95f, 0.93f));
            var wire = Mat("Alambre", new Color(0.80f, 0.80f, 0.76f));
            var board = Mat("Tablero", new Color(0.30f, 0.22f, 0.14f));

            const float bedLen = 30f, bedW = 0.8f, pathW = 0.7f, pitch = bedW + pathW;
            const float zStart = -1.5f, gutterH = 4.0f, ridgeH = 1.3f;
            float zc = zStart + bedLen / 2f;

            // Suelo y camino central
            Box("Suelo", env, new Vector3(0, -0.05f, zc), new Vector3(30f, 0.1f, bedLen + 12f), soil);
            Box("CaminoCentral", env, new Vector3(0, 0.003f, zc), new Vector3(pathW, 0.01f, bedLen), walk);

            // Camas elevadas con rosal, postes y alambres de tutorado
            var budMats = new[] { budR, budR, budP, budW };
            var rng = new System.Random(7);
            for (int k = -5; k < 5; k++)
            {
                float x = (k + 0.5f) * pitch;
                var bed = new GameObject($"Cama_{k + 6:00}").transform;
                bed.SetParent(env, false);

                Box("Tierra", bed, new Vector3(x, 0.12f, zc), new Vector3(bedW, 0.24f, bedLen), bedSoil);
                Box("Follaje", bed, new Vector3(x, 0.78f, zc), new Vector3(bedW * 0.82f, 1.05f, bedLen), (k % 2 == 0) ? leafA : leafB);

                for (float z = zStart; z <= zStart + bedLen + 0.01f; z += 3f)
                    foreach (float side in new[] { -1f, 1f })
                        Cyl("Poste", bed, new Vector3(x + side * bedW / 2f, 0.85f, z), 0.025f, 1.7f, steel);

                foreach (float h in new[] { 0.75f, 1.25f })
                    foreach (float side in new[] { -1f, 1f })
                        Box("Alambre", bed, new Vector3(x + side * bedW / 2f, h, zc), new Vector3(0.006f, 0.006f, bedLen), wire);

                // Matas de follaje sobre la cama para romper la superficie plana
                for (float z = zStart + 0.1f; z < zStart + bedLen; z += (Mathf.Abs(x) < 1f ? 0.25f : 0.6f))
                {
                    float ox = ((float)rng.NextDouble() - 0.5f) * bedW * 0.6f;
                    float w = 0.18f + (float)rng.NextDouble() * 0.14f;
                    var clump = Ball("Mata", bed, new Vector3(x + ox, 1.30f + (float)rng.NextDouble() * 0.06f, z), w,
                                     rng.Next(2) == 0 ? leafA : leafB);
                    clump.transform.localScale = new Vector3(w, w * 0.45f, w * 1.2f);
                }

                // Detalle de tallos y botones: completo en las camas junto al camino, ralo en las demás
                bool near = Mathf.Abs(x) < 1f;
                float step = near ? 0.16f : 0.45f;
                float zEnd = near ? 14f : 10f;
                for (float z = zStart + 0.2f; z < zEnd; z += step)
                {
                    int perRow = near ? 3 : 1;
                    for (int j = 0; j < perRow; j++)
                    {
                        float ox = ((float)rng.NextDouble() - 0.5f) * bedW * 0.7f;
                        float h = 1.30f + (float)rng.NextDouble() * 0.30f;
                        if (near) Cyl("Tallo", bed, new Vector3(x + ox, h - 0.18f, z), 0.008f, 0.36f, stem);
                        var b = Ball("Boton", bed, new Vector3(x + ox, h + 0.02f, z), 0.030f + (float)rng.NextDouble() * 0.015f,
                             budMats[rng.Next(budMats.Length)]);
                        b.transform.localScale = new Vector3(b.transform.localScale.x, b.transform.localScale.y * 1.5f, b.transform.localScale.z);
                    }
                }
            }

            // Estructura: columnas, canaletas, cerchas y techo a dos aguas (naves de 6 m)
            var frame = new GameObject("Estructura").transform;
            frame.SetParent(env, false);
            float[] lines = { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
            // columnas sobre caminos (no en el camino central): x = ±1.5 cae en el camino entre camas 1 y 2
            for (int i = 0; i < lines.Length; i++)
            {
                float lx = lines[i];
                for (float z = zStart; z <= zStart + bedLen + 0.01f; z += 5f)
                    Box("Columna", frame, new Vector3(lx, gutterH / 2f, z), new Vector3(0.08f, gutterH, 0.08f), steel);
                Box("Canaleta", frame, new Vector3(lx, gutterH, zc), new Vector3(0.22f, 0.12f, bedLen), steel);
            }
            for (int i = 0; i < lines.Length - 1; i++)
            {
                float x0 = lines[i], x1 = lines[i + 1], mid = (x0 + x1) / 2f, half = (x1 - x0) / 2f;
                float slope = Mathf.Atan2(ridgeH, half) * Mathf.Rad2Deg;
                float panelW = Mathf.Sqrt(half * half + ridgeH * ridgeH);
                Box("TechoIzq", frame, new Vector3(mid - half / 2f, gutterH + ridgeH / 2f, zc), new Vector3(panelW, 0.02f, bedLen),
                    roof, Quaternion.Euler(0, 0, slope));
                Box("TechoDer", frame, new Vector3(mid + half / 2f, gutterH + ridgeH / 2f, zc), new Vector3(panelW, 0.02f, bedLen),
                    roof, Quaternion.Euler(0, 0, -slope));
                for (float z = zStart; z <= zStart + bedLen + 0.01f; z += 5f)
                {
                    Box("Cercha", frame, new Vector3(mid, gutterH, z), new Vector3(x1 - x0, 0.06f, 0.06f), steel);
                    Box("Pendolon", frame, new Vector3(mid, gutterH + ridgeH / 2f, z), new Vector3(0.05f, ridgeH, 0.05f), steel);
                }
            }

            // Cerramiento de plástico (laterales y culatas)
            var wall = Mat("PlasticoPared", new Color(0.88f, 0.91f, 0.88f));
            Box("ParedIzq", frame, new Vector3(lines[0] - 0.05f, gutterH / 2f, zc), new Vector3(0.02f, gutterH, bedLen), wall);
            Box("ParedDer", frame, new Vector3(lines[lines.Length - 1] + 0.05f, gutterH / 2f, zc), new Vector3(0.02f, gutterH, bedLen), wall);
            float width = lines[lines.Length - 1] - lines[0];
            Box("CulataFondo", frame, new Vector3(0, gutterH / 2f, zStart + bedLen + 0.3f), new Vector3(width, gutterH, 0.02f), wall);
            Box("CulataEntrada", frame, new Vector3(0, gutterH / 2f, zStart - 0.6f), new Vector3(width, gutterH, 0.02f), wall);
            Box("ZocaloIzq", frame, new Vector3(lines[0], 0.25f, zc), new Vector3(0.06f, 0.5f, bedLen), steel);
            Box("ZocaloDer", frame, new Vector3(lines[lines.Length - 1], 0.25f, zc), new Vector3(0.06f, 0.5f, bedLen), steel);

            // Letrero de bloque colgado sobre el camino
            var sign = AddWorldCanvas("Letrero bloque", new Vector2(900, 160), new Vector3(0, 2.7f, 7f), Quaternion.identity, cam, env);
            sign.gameObject.AddComponent<Image>().color = new Color(0.0f, 0.19f, 0.11f, 0.95f);
            var st = AddText(sign.transform, "Texto", "Bloque 3  ·  Camas 01 a 10  ·  Variedad Freedom", 52, Color.white, TextAlignmentOptions.Center);
            Stretch(st.rectTransform, 10);
            Object.DestroyImmediate(sign.GetComponent<GraphicRaycaster>());

            // Ambiente: luz difusa bajo plástico y bruma leve para dar profundidad
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.75f, 0.70f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.03f;
            RenderSettings.fogColor = new Color(0.86f, 0.89f, 0.86f);

            // Estaciones de evaluación junto al camino, a la altura de los botones
            var stations = new List<Transform>();
            var specs = new[]
            {
                (pos: new Vector3(-0.36f, 1.2f, 1.4f), dir: new Vector3(-1f, 0f, 0.45f)),
                (pos: new Vector3( 0.36f, 1.2f, 2.6f), dir: new Vector3( 1f, 0f, 0.45f)),
                (pos: new Vector3(-0.36f, 1.2f, 3.8f), dir: new Vector3(-1f, 0f, 0.45f)),
            };
            for (int i = 0; i < specs.Length; i++)
            {
                var rot = Quaternion.LookRotation(specs[i].dir.normalized);
                var holder = new GameObject($"Estacion_{i + 1}").transform;
                holder.SetParent(env, false);
                holder.position = specs[i].pos;
                holder.rotation = rot;
                Box("Tablero", holder, new Vector3(0, 0, 0.02f), new Vector3(1.04f, 0.34f, 0.02f), board).isStatic = false;
                Cyl("Estaca", holder, new Vector3(-0.45f, -0.6f, 0.03f), 0.02f, 1.2f, steel).isStatic = false;
                Cyl("Estaca", holder, new Vector3(0.45f, -0.6f, 0.03f), 0.02f, 1.2f, steel).isStatic = false;

                var c = AddWorldCanvas("Rosas", new Vector2(1000, 300), Vector3.zero, Quaternion.identity, cam, holder);
                var hl = c.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.padding = new RectOffset(12, 12, 12, 12);
                hl.spacing = 10;
                hl.childAlignment = TextAnchor.MiddleCenter;
                hl.childControlWidth = true; hl.childControlHeight = true;
                hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
                stations.Add(c.transform);
            }
            // Puntos de siembra de rosas 3D frente al follaje, a ambos lados del camino (Etapa 2)
            _roseSlots = new List<Transform>();
            var slotsRoot = new GameObject("RosasEtapa2").transform;
            slotsRoot.SetParent(env, false);
            for (int i = 0; i < 12; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                var slot = new GameObject($"Punto_{i + 1:00}").transform;
                slot.SetParent(slotsRoot, false);
                slot.localPosition = new Vector3(side * 0.37f, 0.42f, 1.0f + (i / 2) * 0.55f + (side > 0 ? 0.27f : 0f));
                _roseSlots.Add(slot);
            }
            return stations;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material m, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (rot.HasValue) g.transform.localRotation = rot.Value;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.isStatic = true;
            return g;
        }

        static GameObject Cyl(string name, Transform parent, Vector3 pos, float diameter, float height, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(diameter, height / 2f, diameter);
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.isStatic = true;
            return g;
        }

        static GameObject Ball(string name, Transform parent, Vector3 pos, float diameter, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = Vector3.one * diameter;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.isStatic = true;
            return g;
        }

        // ── Paneles compartidos ──

        static DecisionPanel BuildDecisionPanel(Camera cam, RoseClassCatalog catalog, Button classBtn)
        {
            var canvas = AddWorldCanvas("Panel_Decision", new Vector2(760, 1000), new Vector3(0, 1.4f, 1.0f), Quaternion.identity, cam);
            var p = AddPanel(canvas.transform, PanelBg, new RectOffset(30, 30, 30, 30), 18);

            var prompt = AddText(p, "Pregunta", "¿En qué estado está esta rosa?", 44, Color.white, TextAlignmentOptions.Center, 110);

            var holder = NewUI("VistaContenedor", p);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = 320;
            var prev = NewUI("Vista", holder);
            var raw = prev.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var fit = prev.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1f;

            var buttons = NewUI("Opciones", p);
            buttons.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            // Opciones en 2 columnas para que hasta 6 respuestas quepan sin salirse de la vista.
            var grid = buttons.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(340, 92);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            var dp = canvas.gameObject.AddComponent<DecisionPanel>();
            dp.catalog = catalog;
            dp.promptText = prompt;
            dp.previewRoot = holder.gameObject;
            dp.preview = raw;
            dp.previewFitter = fit;
            dp.buttonContainer = buttons;
            dp.classButtonPrefab = classBtn;
            dp.distance = 1.0f;

            canvas.gameObject.SetActive(false);
            return dp;
        }

        static FeedbackPanel BuildFeedbackPanel(Camera cam)
        {
            var canvas = AddWorldCanvas("Panel_Retroalimentacion", new Vector2(760, 700), new Vector3(0, 1.4f, 1.0f), Quaternion.identity, cam);
            var p = AddPanel(canvas.transform, PanelBg, new RectOffset(0, 0, 0, 24), 16);

            var bar = NewUI("Encabezado", p);
            var barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = Neutral;
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            var head = AddText(bar, "Titulo", "", 52, Color.white, TextAlignmentOptions.Center);
            head.fontStyle = FontStyles.Bold;
            Stretch(head.rectTransform, 10);

            var body = AddText(p, "Detalle", "", 34, Color.white, TextAlignmentOptions.TopLeft);
            body.margin = new Vector4(30, 10, 30, 10);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            var btnRow = NewUI("FilaBoton", p);
            btnRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var h = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(30, 30, 0, 0);
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var btn = AddButton(btnRow, "Continuar", "Continuar", Light, 600, 90, 40);

            var fb = canvas.gameObject.AddComponent<FeedbackPanel>();
            fb.headline = head;
            fb.body = body;
            fb.headerBar = barImg;
            fb.continueButton = btn;
            fb.continueLabel = btn.GetComponentInChildren<TMP_Text>();
            fb.neutralColor = Neutral;
            fb.correctColor = Theme.correct;
            fb.wrongColor = Theme.wrong;
            fb.distance = 1.0f;

            canvas.gameObject.SetActive(false);
            return fb;
        }

        static (Canvas canvas, TextMeshProUGUI status, List<Button> buttons) BuildHud(
            Camera cam, Vector3 pos, Quaternion rot, params (string label, Color color, UnityAction action)[] items)
        {
            float width = Mathf.Max(1200, items.Length * 340 + 48);
            var canvas = AddWorldCanvas("HUD", new Vector2(width, Theme.logo != null ? 330 : 260), pos, rot, cam);
            var p = AddPanel(canvas.transform, PanelBg, new RectOffset(24, 24, 20, 20), 14);
            AddLogo(p, 56);
            var status = AddText(p, "Estado", "", 36, Color.white, TextAlignmentOptions.Center, 80);

            var row = NewUI("Botones", p);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;

            var buttons = new List<Button>();
            foreach (var it in items)
            {
                var b = AddButton(row, "Btn_" + it.label, it.label, it.color, 360, 110, 36);
                UnityEventTools.AddPersistentListener(b.onClick, it.action);
                buttons.Add(b);
            }
            return (canvas, status, buttons);
        }

        // ── Mira y clasificador ──

        static AimReticle BuildReticle(Camera cam)
        {
            var canvas = AddWorldCanvas("Mira", new Vector2(260, 260), new Vector3(0, 1.6f, 0.6f), Quaternion.identity, cam);
            var root = (RectTransform)canvas.transform;
            var parts = new List<Graphic>();
            // Cuatro bordes del recuadro
            parts.Add(Edge(root, "Arriba", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 6)));
            parts.Add(Edge(root, "Abajo", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 6)));
            parts.Add(Edge(root, "Izquierda", new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 0)));
            parts.Add(Edge(root, "Derecha", new Vector2(1, 0), new Vector2(1, 1), new Vector2(6, 0)));

            var hint = AddText(root, "Indicacion", "Encuadra el botón completo", 26, Color.white, TextAlignmentOptions.Center);
            hint.raycastTarget = false;
            var hr = hint.rectTransform;
            hr.anchorMin = new Vector2(0.5f, 0); hr.anchorMax = new Vector2(0.5f, 0);
            hr.pivot = new Vector2(0.5f, 1); hr.sizeDelta = new Vector2(520, 90); hr.anchoredPosition = new Vector2(0, -8);

            var pv = NewUI("VistaPreviaModelo", root);
            pv.anchorMin = new Vector2(1, 1); pv.anchorMax = new Vector2(1, 1);
            pv.pivot = new Vector2(0, 1); pv.sizeDelta = new Vector2(110, 110); pv.anchoredPosition = new Vector2(14, 0);
            var raw = pv.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;

            var r = canvas.gameObject.AddComponent<AimReticle>();
            r.frameParts = parts.ToArray();
            r.hint = hint;
            r.preview = raw;
            return r;
        }

        static Graphic Edge(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 size)
        {
            var rt = NewUI(name, parent);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Agrega un componente por nombre. Así este script compila aunque falte el paquete
        /// de inferencia (RoseClassifier depende de él).
        /// </summary>
        static Component AddByName(string typeName, string goName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeName);
                if (t != null) return new GameObject(goName).AddComponent(t);
            }
            Debug.LogWarning($"[RosasXR] No encontré {typeName}. ¿Está instalado el paquete Inference Engine (com.unity.ai.inference)?");
            return null;
        }

        static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field);
            if (f != null) f.SetValue(target, value);
        }

        // ── Base de escena ──

        const string CameraRigPrefab = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        static Transform _rayTransform;

        const string HandPrefab = "Packages/com.meta.xr.sdk.core/Prefabs/OVRHandPrefab.prefab";

        /// <summary>Mano rastreada visible (malla de Meta) + OVRHand para pellizco y pose de puntero.</summary>
        static void AddHand(Transform anchor, int handType, string name)
        {
            if (anchor == null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefab);
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, anchor);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(anchor, false);
                go.AddComponent<OVRHand>();
            }
            var hand = go.GetComponent<OVRHand>();
            var so = new SerializedObject(hand);
            var prop = so.FindProperty("HandType");
            if (prop != null) { prop.intValue = handType; so.ApplyModifiedPropertiesWithoutUndo(); }
            hand.OnValidate(); // ajusta esqueleto y malla al lado de la mano
        }

        static Camera SetupBase(bool passthroughStyle)
        {
            Camera cam = null;
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab);
            if (rigPrefab != null)
            {
                // Camera Rig de Meta: seguimiento de cabeza y controles en el visor.
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.name = "OVRCameraRig";
                rig.transform.position = Vector3.zero;
                var mgr = rig.GetComponent<OVRManager>();
                if (mgr == null) mgr = rig.AddComponent<OVRManager>();
                mgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
                if (passthroughStyle)
                {
                    mgr.isInsightPassthroughEnabled = true;
                    var layer = rig.AddComponent<OVRPassthroughLayer>();
                    layer.overlayType = OVROverlay.OverlayType.Underlay;
                }

                var eye = rig.transform.Find("TrackingSpace/CenterEyeAnchor");
                cam = eye.GetComponent<Camera>();
                eye.tag = "MainCamera";
                cam.nearClipPlane = 0.05f;
                if (passthroughStyle)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
                // Apuntar a objetos 3D (rosas): mouse en el editor, rayo de Meta en el visor
                eye.gameObject.AddComponent<PhysicsRaycaster>();
                rig.AddComponent<OVRPhysicsRaycaster>(); // requiere estar junto a OVRCameraRig

                var look = rig.AddComponent<DesktopLook>();   // solo editor: mouse + altura de ojos
                look.editorEyeHeight = 1.6f;

                // Manos: OVRHand en cada mano (pose de puntero oficial + pellizco como clic).
                AddHand(rig.transform.Find("TrackingSpace/RightHandAnchor"), 1, "ManoDerecha (OVRHand)");
                AddHand(rig.transform.Find("TrackingSpace/LeftHandAnchor"), 0, "ManoIzquierda (OVRHand)");
                _rayTransform = rig.transform.Find("TrackingSpace/RightControllerAnchor");
                if (_rayTransform == null) _rayTransform = rig.transform.Find("TrackingSpace/RightHandAnchor");
            }
            else
            {
                Debug.LogWarning("[RosasXR] No encontré OVRCameraRig; uso una cámara simple (sin seguimiento en el visor).");
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.05f;
                camGo.AddComponent<AudioListener>();
                camGo.AddComponent<DesktopLook>();
                camGo.transform.position = new Vector3(0, 1.6f, 0);
                if (passthroughStyle)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.15f, 0.15f, 0.17f);
                }
            }

            var es = new GameObject("EventSystem (mouse en editor, puntero Meta en visor)", typeof(EventSystem));
            var ovr = es.AddComponent<OVRInputModule>();
            ovr.rayTransform = _rayTransform;
            ovr.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger | OVRInput.Button.One;
            ovr.enabled = false;
            var setup = es.AddComponent<XRInputSetup>();
            es.AddComponent<FingerPoke>(); // pulsar botones tocándolos con el índice
            setup.ovrModule = ovr;

            // Línea del puntero
            var laserGo = new GameObject("Puntero", typeof(LineRenderer));
            var lr = laserGo.GetComponent<LineRenderer>();
            lr.startWidth = 0.004f; lr.endWidth = 0.002f;
            lr.material = Mat("Puntero", new Color(1f, 1f, 1f, 0.8f), true);
            var dotGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotGo.name = "PuntoPuntero";
            Object.DestroyImmediate(dotGo.GetComponent<Collider>());
            dotGo.transform.SetParent(laserGo.transform, false);
            dotGo.transform.localScale = Vector3.one * 0.012f;
            dotGo.GetComponent<Renderer>().sharedMaterial = Mat("PuntoPuntero", new Color(0.11f, 0.64f, 0.42f), true);
            var cursor = laserGo.AddComponent<XRLaserCursor>();
            cursor.dot = dotGo.transform;
            ovr.m_Cursor = cursor;
            _rayTransform = null;
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            var lightGo = new GameObject("Directional Light");
            var l = lightGo.AddComponent<UnityEngine.Light>();
            l.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            return cam;
        }

        // ── Utilidades UI ──

        static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        static Canvas AddWorldCanvas(string name, Vector2 size, Vector3 pos, Quaternion rot, Camera cam, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);

            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3;

            rt.sizeDelta = size;
            rt.localScale = Vector3.one * 0.001f; // 1000 px = 1 m
            if (parent != null) { rt.localPosition = pos; rt.localRotation = rot; }
            else { rt.position = pos; rt.rotation = rot; }
            return c;
        }

        static RectTransform AddPanel(Transform parent, Color bg, RectOffset padding, float spacing)
        {
            var rt = NewUI("Panel", parent);
            Stretch(rt);
            var pimg = rt.gameObject.AddComponent<Image>();
            pimg.color = bg;
            Round(pimg, 1.4f);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = padding;
            v.spacing = spacing;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return rt;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align, float preferredHeight = -1)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.richText = true;
            if (preferredHeight > 0) rt.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return t;
        }

        static Button AddButton(Transform parent, string name, string label, Color color, float w, float h, float fontSize)
        {
            var rt = NewUI(name, parent);
            rt.sizeDelta = new Vector2(w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            Round(img, 1f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;

            float lum = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            var t = AddText(rt, "Label", label, fontSize, lum < 0.5f ? Color.white : TextDark, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            Stretch(t.rectTransform, 8);
            return b;
        }

        const string RoundedSpritePath = Root + "/Branding/esquinas_redondeadas.png";
        static Sprite _rounded;

        /// <summary>Esquinas redondeadas para paneles y botones (sprite de 9 partes generado).</summary>
        static void Round(Image img, float radiusScale)
        {
            var sp = RoundedSprite();
            if (sp == null) return;
            img.sprite = sp;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, radiusScale);
        }

        static Sprite RoundedSprite()
        {
            if (_rounded != null) return _rounded;
            if (!File.Exists(RoundedSpritePath))
            {
                EnsureFolder(Root + "/Branding");
                const int S = 96; const float R = 30f;
                var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, R, S - R), cy = Mathf.Clamp(y + 0.5f, R, S - R);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(R - d + 0.5f));
                    }
                t.SetPixels(px);
                File.WriteAllBytes(RoundedSpritePath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(RoundedSpritePath) is TextureImporter ti &&
                (ti.textureType != TextureImporterType.Sprite || ti.spriteBorder.x < 1f))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.spriteBorder = new Vector4(32, 32, 32, 32);
                ti.SaveAndReimport();
            }
            _rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            return _rounded;
        }

        static Material Mat(string name, Color color, bool unlit = false)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            Shader sh = unlit ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = unlit ? Shader.Find("Unlit/Color") : Shader.Find("Standard");
            m = new Material(sh) { color = color, enableInstancing = true };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static T SavePrefab<T>(GameObject go, string path) where T : Component
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset.GetComponent<T>();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            if (Directory.Exists(path))
            {
                // Existe en disco pero Unity aún no la registró (copiada desde fuera del editor).
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.IsValidFolder(path)) return;
            }
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
