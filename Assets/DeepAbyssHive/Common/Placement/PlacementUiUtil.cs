using UnityEngine;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 放置 UI 輔助：統一顏色/訊息產生，避免 Placer 與 HUD 分叉。
    /// </summary>
    public static class PlacementUiUtil
    {
        // 統一預覽透明度
        public const float PREVIEW_ALPHA = 0.35f;

        /// <summary>
        /// 依 Result 決定顏色；forPreview=true 會套用固定透明度。
        /// </summary>
        public static Color ColorFor(Result<Bounds> r, bool forPreview)
        {
            Color c;
            if (r == null) c = Color.white;
            else if (r.ok) c = Color.green;
            else
            {
                switch (r.code)
                {
                    case PlaceResultCode.E_PLACE_COLLISION: c = Color.red; break;
                    case PlaceResultCode.E_REQUIRE_CREEP:   c = Color.yellow; break;
                    case PlaceResultCode.E_OUT_OF_BOUNDS:   c = new Color(1f, 0f, 1f); break; // magenta
                    case PlaceResultCode.E_INVALID_TYPE:    c = Color.cyan; break;
                    case PlaceResultCode.E_TERRAIN_TOO_STEEP: c = new Color(1f, 0.5f, 0f); break; // orange
                    default:                                c = Color.white; break;
                }
            }
            if (forPreview) { c.a = PREVIEW_ALPHA; }
            return c;
        }

        /// <summary>
        /// 產生 HUD 顯示字串：CODE + 精簡訊息（去除系統前綴，如 "[Placement] "）。
        /// </summary>
        public static string TextFor(Result<Bounds> r)
        {
            if (r == null) return "(no checks yet)";
            var code = r.code.ToString();
            var msg = Sanitize(r.message);
            if (string.IsNullOrEmpty(msg))
                return $"{code} ok={r.ok}";
            return $"{code} ok={r.ok} {msg}";
        }

        /// <summary>去除常見前綴，讓訊息更乾淨。</summary>
        public static string Sanitize(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return msg;
            if (msg.StartsWith("[Placement] ")) return msg.Substring(12);
            if (msg.StartsWith("[DEV HUD] "))  return msg.Substring(10);
            return msg;
        }
    }
}