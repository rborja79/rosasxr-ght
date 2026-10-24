"""
Exporta el modelo de rosas (MobileNetV3-Small + umbrales ordinales) a ONNX para Unity / Meta Quest.

Uso (en la misma carpeta que rosas_ordinal_hackaton.py):
    python exportar_onnx.py --checkpoint modelo_rosas.pt --salida modelo_rosas.onnx

Prueba de integración SIN modelo entrenado (misma arquitectura, cabeza al azar):
    python exportar_onnx.py --sin-entrenar --salida modelo_prueba.onnx
  Sirve para validar HOY en el Quest: que Unity importa el ONNX, que corre en el visor,
  cuánto tarda y que la interfaz muestra las 3 probabilidades. Las predicciones no significan nada.

El ONNX resultante:
  - entrada  "input":  (1, 3, 224, 224), RGB normalizado con MEAN/STD de ImageNet
  - salida   "probs":  (1, 3) = [P(insuficiente), P(óptima), P(excesiva)]

La conversión ordinal (q = sigmoid(logits); p = [1-q1, q1-q2, q2]) queda DENTRO del ONNX,
así Unity recibe directamente las tres probabilidades y no hay que replicar fórmulas.
(Unity también acepta el modelo sin esta envoltura: con outputType = Ordinal convierte los 2 logits.)
"""
import argparse

import torch
import torch.nn as nn

# Reutiliza la construcción del modelo del script del equipo.
from rosas_ordinal_hackaton import build_model


class ProbabilidadesOrdinales(nn.Module):
    def __init__(self, modelo):
        super().__init__()
        self.modelo = modelo

    def forward(self, x):
        q = torch.sigmoid(self.modelo(x))           # (N, 2): P(Y>=1), P(Y>=2)
        p0 = 1 - q[:, 0]
        p1 = torch.clamp(q[:, 0] - q[:, 1], min=0)
        p2 = q[:, 1]
        return torch.stack([p0, p1, p2], dim=1)     # (N, 3)


def cargar_pesos(modelo, ruta):
    ckpt = torch.load(ruta, map_location="cpu")
    if isinstance(ckpt, dict):
        for clave in ("model", "model_state", "model_state_dict", "state_dict"):
            if clave in ckpt and isinstance(ckpt[clave], dict):
                ckpt = ckpt[clave]
                break
    modelo.load_state_dict(ckpt)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--checkpoint", default="modelo_rosas.pt")
    ap.add_argument("--salida", default="modelo_rosas.onnx")
    ap.add_argument("--imgsz", type=int, default=224)
    ap.add_argument("--opset", type=int, default=15)
    ap.add_argument("--sin-entrenar", action="store_true",
                    help="Exporta la arquitectura con pesos ImageNet y cabeza sin entrenar (solo para probar la integración).")
    args = ap.parse_args()

    if args.sin_entrenar:
        modelo = build_model(pretrained=True, freeze_features=False)
        print("AVISO: modelo SIN entrenar. Solo para probar la integración en Unity / Quest.")
    else:
        modelo = build_model(pretrained=False, freeze_features=False)
        cargar_pesos(modelo, args.checkpoint)
    exportable = ProbabilidadesOrdinales(modelo).eval()

    ejemplo = torch.randn(1, 3, args.imgsz, args.imgsz)
    with torch.inference_mode():
        p = exportable(ejemplo)
    print("Prueba en PyTorch:", p.numpy().round(3), "suma =", float(p.sum()))

    torch.onnx.export(
        exportable, ejemplo, args.salida,
        input_names=["input"], output_names=["probs"],
        opset_version=args.opset, do_constant_folding=True,
    )
    print(f"Listo: {args.salida}  (arrastrarlo a Unity y asignarlo en ClasificadorRosas > model)")

    # Verificación opcional con onnxruntime (pip install onnxruntime)
    try:
        import numpy as np
        import onnxruntime as ort
        sess = ort.InferenceSession(args.salida)
        out = sess.run(None, {"input": ejemplo.numpy()})[0]
        print("Prueba en ONNX Runtime:", out.round(3), "diferencia máx =", float(np.abs(out - p.numpy()).max()))
    except ImportError:
        print("(Instala onnxruntime para verificar que el ONNX da lo mismo que PyTorch.)")


if __name__ == "__main__":
    main()
