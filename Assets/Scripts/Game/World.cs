using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>Câmera presa ao jogador, com tremor de tela opcional.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        float shakeAmount;
        float shakeTime;

        public void Shake(float amount, float duration)
        {
            if (!GameSettings.ScreenShake) return;
            shakeAmount = Mathf.Max(shakeAmount, amount);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        void LateUpdate()
        {
            Vector3 p = Target != null ? Target.position : transform.position;
            p.z = -10f;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                if (Time.timeScale > 0f) p += (Vector3)(Random.insideUnitCircle * shakeAmount);
                if (shakeTime <= 0f) shakeAmount = 0f;
            }
            transform.position = p;
        }

        /// <summary>Configura a câmera principal (ou cria uma) para o jogo 2D.</summary>
        public static Camera SetupMainCamera(float orthoSize)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.1f, 0.09f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            // sprites mais abaixo na tela ficam na frente
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }
    }

    /// <summary>Piso infinito: a imagem de ladrilhos repetida, acompanhando a câmera.</summary>
    public class GroundTiler : MonoBehaviour
    {
        /// <summary>Tamanho de um ladrilho no mundo (a imagem tem 3 x 2 ladrilhos).</summary>
        public const float TileWorld = 1.5f;
        const int TilesAcross = 3;

        SpriteRenderer sr;
        Vector2 period;
        float scale;

        void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Floor;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.sortingOrder = -1000;
            Vector2 spriteSize = sr.sprite.bounds.size;
            scale = TilesAcross * TileWorld / spriteSize.x;
            transform.localScale = new Vector3(scale, scale, 1f);
            period = spriteSize * scale;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            var world = new Vector2((Mathf.Ceil(w / period.x) + 2f) * period.x, (Mathf.Ceil(h / period.y) + 2f) * period.y);
            sr.size = world / scale;
            Vector3 c = cam.transform.position;
            transform.position = new Vector3(Mathf.Round(c.x / period.x) * period.x, Mathf.Round(c.y / period.y) * period.y, 5f);
        }
    }
}
