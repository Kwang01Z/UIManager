#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using TMPro;
using System.Collections.Generic;
using UnityEngine.TextCore;

namespace Game.EditorTools
{
    public static class TMPSpriteAssetFromAtlasGenerator
    {
        // ============================================
        // 1. CHỨC NĂNG TẠO MỚI (Từ SpriteAtlas)
        // ============================================
        [MenuItem("Assets/Create/TextMeshPro/Sprite Asset (.asset) From Atlas", false, 10)]
        public static void CreateTmpSpriteAssetFromAtlas()
        {
            Object selected = Selection.activeObject;
            if (selected == null || !(selected is SpriteAtlas))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn một Sprite Atlas!", "OK");
                return;
            }

            SpriteAtlas targetAtlas = (SpriteAtlas)selected;
            string assetPath = AssetDatabase.GetAssetPath(selected);
            string folder = System.IO.Path.GetDirectoryName(assetPath);
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();
            string newPath = System.IO.Path.Combine(folder, name + ".asset").Replace("\\", "/");

            if (System.IO.File.Exists(newPath))
            {
                if (EditorUtility.DisplayDialog("Cảnh báo", $"File {name}.asset đã tồn tại.\nBạn có muốn Cập Nhật (Sync) dữ liệu từ Atlas mà vẫn GIỮ NGUYÊN các cấu hình đã chỉnh sửa không?", "Cập nhật Sync", "Hủy"))
                {
                    TMP_SpriteAsset existingTarget = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(newPath);
                    UpdateExistingSpriteAsset(existingTarget, targetAtlas);
                }
                return;
            }

            TMP_SpriteAsset mainAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            mainAsset.name = name;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(TMP_SpriteAsset).GetField("m_Version", flags)?.SetValue(mainAsset, "1.1.0");

            AssetDatabase.CreateAsset(mainAsset, newPath);
            UpdateExistingSpriteAsset(mainAsset, targetAtlas, true);

            EditorGUIUtility.PingObject(mainAsset);
            Selection.activeObject = mainAsset;
            Debug.Log($"Đã tạo thành công {mainAsset.name}.asset từ SpriteAtlas.");
        }

        // ============================================
        // 2. CHỨC NĂNG SYNC (Giữ nguyên cấu hình cũ)
        // ============================================
        [MenuItem("Assets/Sync TMP Sprite Asset From Atlas", true)]
        public static bool ValidateSync()
        {
            return Selection.activeObject is TMP_SpriteAsset || Selection.activeObject is SpriteAtlas;
        }

        [MenuItem("Assets/Sync TMP Sprite Asset From Atlas", false, 11)]
        public static void SyncTmpSpriteAsset()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            string path = AssetDatabase.GetAssetPath(selected);
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            
            string assetPath = System.IO.Path.Combine(folder, name + ".asset").Replace("\\", "/");
            string atlasPath = System.IO.Path.Combine(folder, name + ".spriteatlasv2").Replace("\\", "/");

            TMP_SpriteAsset mainAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);
            SpriteAtlas targetAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            // Fallback sang v1 nếu không tìm thấy v2
            if (targetAtlas == null)
            {
                atlasPath = System.IO.Path.Combine(folder, name + ".spriteatlas").Replace("\\", "/");
                targetAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            }

            if (targetAtlas == null)
            {
                EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy SpriteAtlas tên '{name}.spriteatlasv2' (hoặc v1) ở cùng thư mục.", "OK");
                return;
            }

            if (mainAsset == null)
            {
                EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy TMP Sprite Asset tên '{name}.asset' ở cùng thư mục.\nVui lòng Generate trước.", "OK");
                return;
            }

            UpdateExistingSpriteAsset(mainAsset, targetAtlas, false);
        }

        public static void UpdateExistingSpriteAsset(TMP_SpriteAsset mainAsset, SpriteAtlas targetAtlas, bool silent = false)
        {
            // Bắt buộc pack atlas trước để cập nhật dữ liệu texture và UV
            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { targetAtlas }, EditorUserBuildSettings.activeBuildTarget);

            // Lấy danh sách Sprite ĐÃ PACK (chứa toạ độ textureRect chính xác trong Packed Texture)
            Sprite[] sprites = GetAtlasPackedSprites(targetAtlas);
            if (sprites == null || sprites.Length == 0)
            {
                if (!silent) EditorUtility.DisplayDialog("Lỗi", "Không lấy được danh sách sprite từ Sprite Atlas!", "OK");
                return;
            }

            // Gom nhóm tất cả file cũ (main + list fallback nếu có trước đó) để lấy metrics đã chỉnh sửa
            List<TMP_SpriteAsset> allValidAssets = new List<TMP_SpriteAsset>();
            allValidAssets.Add(mainAsset);
            if (mainAsset.fallbackSpriteAssets != null)
                allValidAssets.AddRange(mainAsset.fallbackSpriteAssets);

            // Lookup đầy đủ cấu hình cũ thông qua Character Name
            Dictionary<string, SavedMetrics> oldMetrics = new Dictionary<string, SavedMetrics>();
            uint maxUsedUnicode = 0x1999; // Bắt đầu từ dải 0x2000

            for (int i = 0; i < allValidAssets.Count; i++)
            {
                var ast = allValidAssets[i];
                if (ast == null || ast.spriteCharacterTable == null) continue;
                for (int j = 0; j < ast.spriteCharacterTable.Count; j++)
                {
                    var chara = ast.spriteCharacterTable[j];
                    if (chara == null || chara.glyph == null) continue;
                    
                    oldMetrics[chara.name] = new SavedMetrics()
                    {
                        width = chara.glyph.metrics.width,
                        height = chara.glyph.metrics.height,
                        bearingX = chara.glyph.metrics.horizontalBearingX,
                        bearingY = chara.glyph.metrics.horizontalBearingY,
                        advance = chara.glyph.metrics.horizontalAdvance,
                        charScale = chara.scale,
                        glyphScale = chara.glyph.scale,
                        unicode = chara.unicode
                    };
                    
                    if (chara.unicode > maxUsedUnicode) 
                        maxUsedUnicode = chara.unicode;
                }
            }

            // --- Xây dựng lại --- 
            string assetPath = AssetDatabase.GetAssetPath(mainAsset);
            
            // Xoá tất cả sub-assets cũ (như các fallback TMP_SpriteAsset cũ) ngoại trừ Material chính
            var objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (obj != null && !AssetDatabase.IsMainAsset(obj) && !(obj is Material))
                {
                    Object.DestroyImmediate(obj, true);
                }
            }

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(TMP_SpriteAsset).GetField("m_Version", flags)?.SetValue(mainAsset, "1.1.0");

            // Lấy Packed Texture từ SpriteAtlas
            Texture2D packedTex = GetAtlasPackedTexture(targetAtlas, sprites);
            mainAsset.spriteSheet = packedTex;

            // Xử lý Material
            Material mat = mainAsset.material;
            if (mat == null)
            {
                Shader shader = Shader.Find("TextMeshPro/Sprite");
                if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Sprite");
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.name = mainAsset.name + " Material";
                }
            }

            if (mat != null)
            {
                if (packedTex != null)
                {
                    mat.SetTexture(ShaderUtilities.ID_MainTex, packedTex);
                }
                mainAsset.material = mat;
                if (!AssetDatabase.Contains(mat))
                {
                    AssetDatabase.AddObjectToAsset(mat, mainAsset);
                }
            }

            var glyphTable = new List<TMP_SpriteGlyph>();
            var charTable = new List<TMP_SpriteCharacter>();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite s = sprites[i];
                if (s == null) continue;

                string cleanName = GetCleanName(s.name);
                Rect r = s.rect;
                try
                {
                    r = s.textureRect;
                }
                catch
                {
                    r = s.rect;
                }

                GlyphRect glyphRect = new GlyphRect((int)r.x, (int)r.y, (int)r.width, (int)r.height);

                float width = r.width;
                float height = r.height;
                float bearingX = 0;
                float bearingY = height;
                float advance = width + 5f;
                float charScale = 1.0f;
                float glyphScale = 1.0f;
                uint unicode;

                // KIỂM TRA NẾU SPRITE ĐÃ TỒN TẠI THÌ GIỮ NGUYÊN TOÀN BỘ METRICS
                if (oldMetrics.TryGetValue(cleanName, out SavedMetrics mem))
                {
                    if (mem.width > 0) width = mem.width;
                    if (mem.height > 0) height = mem.height;
                    bearingX = mem.bearingX;
                    bearingY = mem.bearingY != 0 ? mem.bearingY : height;
                    advance = mem.advance != 0 ? mem.advance : (width + 5f);
                    charScale = mem.charScale;
                    glyphScale = mem.glyphScale;
                    unicode = mem.unicode;
                }
                else
                {
                    maxUsedUnicode++;
                    unicode = maxUsedUnicode;
                }

                GlyphMetrics metrics = new GlyphMetrics(width, height, bearingX, bearingY, advance);

                TMP_SpriteGlyph glyph = new TMP_SpriteGlyph((uint)i, metrics, glyphRect, glyphScale, 0, s);
                glyphTable.Add(glyph);

                TMP_SpriteCharacter character = new TMP_SpriteCharacter(unicode, glyph);
                character.name = cleanName;
                character.scale = charScale;
                charTable.Add(character);
            }

            typeof(TMP_SpriteAsset).GetField("m_SpriteGlyphTable", flags)?.SetValue(mainAsset, glyphTable);
            typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", flags)?.SetValue(mainAsset, charTable);
            mainAsset.UpdateLookupTables();

            mainAsset.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                EditorUtility.DisplayDialog("Thành công", "Đã đồng bộ lại TMP Sprite Asset từ Sprite Atlas!\nToạ độ và kích thước Sprite trong Atlas đã được cập nhật chính xác.", "OK");
            else
                Debug.Log($"[TMP Sprite Asset] Đã tự động đồng bộ file {mainAsset.name}.asset. Bảo toàn metrics cho các sprite cũ.");
        }

        private static Sprite[] GetAtlasPackedSprites(SpriteAtlas targetAtlas)
        {
            if (targetAtlas == null) return new Sprite[0];

            // Dùng method nội bộ GetPackedSprites của Unity Editor để lấy Sprite với textureRect chuẩn trong Packed Texture
            var extType = typeof(UnityEditor.U2D.SpriteAtlasExtensions);
            var getPackedSpritesMethod = extType.GetMethod("GetPackedSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (getPackedSpritesMethod != null)
            {
                Sprite[] packed = getPackedSpritesMethod.Invoke(null, new object[] { targetAtlas }) as Sprite[];
                if (packed != null && packed.Length > 0)
                {
                    return packed;
                }
            }

            // Fallback
            int count = targetAtlas.spriteCount;
            Sprite[] fallbackSprites = new Sprite[count];
            targetAtlas.GetSprites(fallbackSprites);
            return fallbackSprites;
        }

        private static Texture2D GetAtlasPackedTexture(SpriteAtlas atlas, Sprite[] packedSprites)
        {
            if (atlas == null) return null;

            // 1. Thử lấy từ sub-assets của file .spriteatlasv2
            string atlasPath = AssetDatabase.GetAssetPath(atlas);
            if (!string.IsNullOrEmpty(atlasPath))
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is Texture2D tex)
                    {
                        return tex;
                    }
                }
            }

            // 2. Thử lấy từ GetPreviewTextures của SpriteAtlasExtensions
            var extType = typeof(UnityEditor.U2D.SpriteAtlasExtensions);
            var getPreviewTexturesMethod = extType.GetMethod("GetPreviewTextures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (getPreviewTexturesMethod != null)
            {
                Texture2D[] textures = getPreviewTexturesMethod.Invoke(null, new object[] { atlas }) as Texture2D[];
                if (textures != null && textures.Length > 0 && textures[0] != null)
                {
                    return textures[0];
                }
            }

            // 3. Fallback từ packed sprites
            if (packedSprites != null && packedSprites.Length > 0 && packedSprites[0] != null)
            {
                return packedSprites[0].texture;
            }

            return null;
        }

        private static string GetCleanName(string rawName)
        {
            string clean = rawName.EndsWith("(Clone)") ? rawName.Substring(0, rawName.Length - 7).TrimEnd() : rawName;
            return clean.ToLower();
        }

        private struct SavedMetrics
        {
            public float width;
            public float height;
            public float bearingX;
            public float bearingY;
            public float advance;
            public float charScale;
            public float glyphScale;
            public uint unicode;
        }
    }

    // ============================================
    // 3. TỰ ĐỘNG ĐỒNG BỘ KHI SPRITE ATLAS CẬP NHẬT
    // ============================================
    public class TMPSpriteAssetAtlasPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string str = importedAssets[i];
                if (str.EndsWith(".spriteatlasv2") || str.EndsWith(".spriteatlas"))
                {
                    AutoSyncSpriteAsset(str);
                }
            }
        }

        private static void AutoSyncSpriteAsset(string atlasPath)
        {
            string folder = System.IO.Path.GetDirectoryName(atlasPath);
            string name = System.IO.Path.GetFileNameWithoutExtension(atlasPath);
            string assetPath = System.IO.Path.Combine(folder, name + ".asset").Replace("\\", "/");

            if (System.IO.File.Exists(assetPath))
            {
                EditorApplication.delayCall += () =>
                {
                    TMP_SpriteAsset mainAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);
                    SpriteAtlas targetAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

                    if (mainAsset != null && targetAtlas != null)
                    {
                        TMPSpriteAssetFromAtlasGenerator.UpdateExistingSpriteAsset(mainAsset, targetAtlas, true);
                    }
                };
            }
        }
    }
}
#endif
