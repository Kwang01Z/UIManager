#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using TMPro;
using System.Collections.Generic;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine.TextCore;

namespace Game.EditorTools
{
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
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string newPath = System.IO.Path.Combine(folder, name + ".asset").Replace("\\", "/");

            if (System.IO.File.Exists(newPath))
            {
                if (EditorUtility.DisplayDialog("Cảnh báo", $"File {name}.asset đã tồn tại.\nBạn có muốn Cập Nhật (Sync) dữ liệu từ Atlas mà vẫn GIỮ NGUYÊN các Offset đã cấu hình không?", "Cập nhật Sync", "Hủy"))
                {
                    TMP_SpriteAsset existingTarget = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(newPath);
                    UpdateExistingSpriteAsset(existingTarget, targetAtlas);
                }
                return;
            }

            // Pack atlas để lấy texture chuẩn
            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { targetAtlas }, EditorUserBuildSettings.activeBuildTarget);

            int count = targetAtlas.spriteCount;
            if (count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Sprite Atlas không chứa sprite nào!", "OK");
                return;
            }

            Sprite[] sprites = new Sprite[count];
            targetAtlas.GetSprites(sprites);

            // Nhóm sprite theo texture (đề phòng Atlas bị chia thành nhiều pages)
            Dictionary<Texture2D, List<Sprite>> textureGroups = new Dictionary<Texture2D, List<Sprite>>();
            foreach (Sprite s in sprites)
            {
                if (s == null) continue;
                Texture2D tex = s.texture;
                if (tex == null) continue;

                if (!textureGroups.ContainsKey(tex))
                    textureGroups[tex] = new List<Sprite>();

                textureGroups[tex].Add(s);
            }

            TMP_SpriteAsset mainAsset = null;
            List<TMP_SpriteAsset> fallbacks = new List<TMP_SpriteAsset>();

            int index = 0;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            foreach (var kvp in textureGroups)
            {
                Texture2D tex = kvp.Key;
                List<Sprite> texSprites = kvp.Value;

                TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                // Đặt tên fallback sub-asset khớp với tên của Packed Texture (thứ xuất hiện ở field "Sprite Atlas" của TMP)
                string fallbackName = string.IsNullOrEmpty(tex.name) ? $"sactx-{index}-{tex.width}x{tex.height}" : tex.name;
                asset.name = index == 0 ? name : fallbackName;
                
                typeof(TMP_SpriteAsset).GetField("m_Version", flags)?.SetValue(asset, "1.1.0");
                asset.spriteSheet = tex;

                Shader shader = Shader.Find("TextMeshPro/Sprite");
                if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Sprite");

                Material mat = null;
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.name = asset.name + " Material";
                    mat.SetTexture(ShaderUtilities.ID_MainTex, tex);
                    asset.material = mat;
                }

                var glyphTable = new List<TMP_SpriteGlyph>();
                var charTable = new List<TMP_SpriteCharacter>();

                for (int i = 0; i < texSprites.Count; i++)
                {
                    Sprite s = texSprites[i];
                    string cleanName = s.name.EndsWith("(Clone)") ? s.name.Substring(0, s.name.Length - 7).TrimEnd() : s.name;

                    Rect r = s.textureRect;
                    GlyphRect glyphRect = new GlyphRect((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                    
                    float width = r.width;
                    float height = r.height;
                    
                    // Default metrics cho TMP
                    float bearingX = 0;
                    float bearingY = height;
                    float advance = width + 5f; // Thêm 1 chút khoảng trống mặc định

                    GlyphMetrics metrics = new GlyphMetrics(width, height, bearingX, bearingY, advance);

                    TMP_SpriteGlyph glyph = new TMP_SpriteGlyph((uint)i, metrics, glyphRect, 1.0f, 0, s);
                    glyphTable.Add(glyph);

                    uint unicode = (uint)(0x2000 + i); 
                    TMP_SpriteCharacter character = new TMP_SpriteCharacter(unicode, glyph);
                    character.name = cleanName;
                    character.scale = 1.0f;
                    charTable.Add(character);
                }

                typeof(TMP_SpriteAsset).GetField("m_SpriteGlyphTable", flags)?.SetValue(asset, glyphTable);
                typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", flags)?.SetValue(asset, charTable);
                asset.UpdateLookupTables();

                if (index == 0)
                {
                    mainAsset = asset;
                    AssetDatabase.CreateAsset(mainAsset, newPath);
                    if (mat != null) AssetDatabase.AddObjectToAsset(mat, mainAsset);
                }
                else
                {
                    fallbacks.Add(asset);
                    AssetDatabase.AddObjectToAsset(asset, mainAsset);
                    if (mat != null) AssetDatabase.AddObjectToAsset(mat, mainAsset);
                }

                index++;
            }

            if (mainAsset != null)
            {
                mainAsset.fallbackSpriteAssets = fallbacks;
                EditorUtility.SetDirty(mainAsset);
                AssetDatabase.SaveAssets();

                EditorGUIUtility.PingObject(mainAsset);
                Selection.activeObject = mainAsset;
                Debug.Log($"Đã tạo thành công {mainAsset.name}.asset. BẠN CÓ THỂ MỞ FILE LÊN ĐỂ CHỈNH SỬA OFFSET!");
            }
        }

        // ============================================
        // 2. CHỨC NĂNG SYNC (Giữ nguyên cấu hình Offset cũ)
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
            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { targetAtlas }, EditorUserBuildSettings.activeBuildTarget);

            if (targetAtlas.spriteCount == 0) return;

            Sprite[] sprites = new Sprite[targetAtlas.spriteCount];
            targetAtlas.GetSprites(sprites);

            // Gom nhóm tất cả file cũ (main + list fallback)
            List<TMP_SpriteAsset> allValidAssets = new List<TMP_SpriteAsset>();
            allValidAssets.Add(mainAsset);
            if (mainAsset.fallbackSpriteAssets != null)
                allValidAssets.AddRange(mainAsset.fallbackSpriteAssets);

            // Lookup Offset & Scale cũ thông qua Object Name
            Dictionary<string, SavedMetrics> oldMetrics = new Dictionary<string, SavedMetrics>();
            uint maxUsedUnicode = 0x2000;

            foreach (var ast in allValidAssets)
            {
                if (ast == null || ast.spriteCharacterTable == null) continue;
                foreach (var chara in ast.spriteCharacterTable)
                {
                    if (chara.glyph == null) continue;
                    oldMetrics[chara.name] = new SavedMetrics()
                    {
                        bearingX = chara.glyph.metrics.horizontalBearingX,
                        bearingY = chara.glyph.metrics.horizontalBearingY,
                        advance = chara.glyph.metrics.horizontalAdvance,
                        scale = chara.scale,
                        unicode = chara.unicode
                    };
                    if (chara.unicode > maxUsedUnicode) 
                        maxUsedUnicode = chara.unicode;
                }
            }

            // --- Tiếp theo rebuild lại --- 
            // (Đơn giản nhất: Ta lấy tất cả sub-assets hiện tại và override glyph của nó, 
            // xoá những cái thừa. Việc quản lý nhiều texture pages khá phức tạp, ta sẽ xoá sub-asset cũ và tạo lại 
            // để đảm bảo đúng texture page).

            string assetPath = AssetDatabase.GetAssetPath(mainAsset);
            
            // Xoá tất cả sub-assets cũ bên trong file
            var objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in objs)
            {
                if (obj != null && !AssetDatabase.IsMainAsset(obj))
                {
                    Object.DestroyImmediate(obj, true);
                }
            }

            // Xây dựng lại
            Dictionary<Texture2D, List<Sprite>> textureGroups = new Dictionary<Texture2D, List<Sprite>>();
            foreach (Sprite s in sprites)
            {
                if (s == null) continue;
                Texture2D tex = s.texture;
                if (tex == null) continue;

                if (!textureGroups.ContainsKey(tex))
                    textureGroups[tex] = new List<Sprite>();
                textureGroups[tex].Add(s);
            }

            List<TMP_SpriteAsset> fallbacks = new List<TMP_SpriteAsset>();
            int index = 0;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            foreach (var kvp in textureGroups)
            {
                Texture2D tex = kvp.Key;
                List<Sprite> texSprites = kvp.Value;

                TMP_SpriteAsset asset = index == 0 ? mainAsset : ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                
                if (index > 0)
                {
                    string fallbackName = string.IsNullOrEmpty(tex.name) ? $"sactx-{index}-{tex.width}x{tex.height}" : tex.name;
                    asset.name = fallbackName;
                    typeof(TMP_SpriteAsset).GetField("m_Version", flags)?.SetValue(asset, "1.1.0");
                }

                asset.spriteSheet = tex;

                Material mat = null;
                Shader shader = Shader.Find("TextMeshPro/Sprite");
                if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Sprite");
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.name = (index == 0 ? mainAsset.name : asset.name) + " Material";
                    mat.SetTexture(ShaderUtilities.ID_MainTex, tex);
                    asset.material = mat;
                }

                var glyphTable = new List<TMP_SpriteGlyph>();
                var charTable = new List<TMP_SpriteCharacter>();

                for (int i = 0; i < texSprites.Count; i++)
                {
                    Sprite s = texSprites[i];
                    string cleanName = s.name.EndsWith("(Clone)") ? s.name.Substring(0, s.name.Length - 7).TrimEnd() : s.name;

                    Rect r = s.textureRect;
                    GlyphRect glyphRect = new GlyphRect((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                    
                    float width = r.width;
                    float height = r.height;

                    // Phục hồi cấu hình người dùng if exist
                    float bearingX = 0;
                    float bearingY = height;
                    float advance = width + 5f;
                    float spScale = 1.0f;
                    uint unicode;

                    if (oldMetrics.TryGetValue(cleanName, out SavedMetrics mem))
                    {
                        bearingX = mem.bearingX;
                        bearingY = mem.bearingY;
                        advance = mem.advance;
                        spScale = mem.scale;
                        unicode = mem.unicode;
                    }
                    else
                    {
                        maxUsedUnicode++; // Cấp phát unicode mới chưa từng dùng
                        unicode = maxUsedUnicode;
                    }

                    GlyphMetrics metrics = new GlyphMetrics(width, height, bearingX, bearingY, advance);

                    TMP_SpriteGlyph glyph = new TMP_SpriteGlyph((uint)i, metrics, glyphRect, 1.0f, 0, s);
                    glyphTable.Add(glyph);

                    TMP_SpriteCharacter character = new TMP_SpriteCharacter(unicode, glyph);
                    character.name = cleanName;
                    character.scale = spScale;
                    charTable.Add(character);
                }

                typeof(TMP_SpriteAsset).GetField("m_SpriteGlyphTable", flags)?.SetValue(asset, glyphTable);
                typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", flags)?.SetValue(asset, charTable);
                asset.UpdateLookupTables();

                if (index > 0)
                {
                    fallbacks.Add(asset);
                    AssetDatabase.AddObjectToAsset(asset, mainAsset);
                }
                
                if (mat != null) AssetDatabase.AddObjectToAsset(mat, mainAsset);

                index++;
            }

            mainAsset.fallbackSpriteAssets = fallbacks;
            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                EditorUtility.DisplayDialog("Thành công", "Đã đồng bộ lại TMP Sprite Asset từ Sprite Atlas!\nCác thay đổi Offset/Scale của bạn vẫn được giữ nguyên.", "OK");
            else
                Debug.Log($"[TMP Sprite Asset] Đã tự động đồng bộ file {mainAsset.name}.asset do Sprite Atlas liên quan có thay đổi.");
        }

        private struct SavedMetrics
        {
            public float bearingX;
            public float bearingY;
            public float advance;
            public float scale;
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
            foreach (string str in importedAssets)
            {
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
}
#endif
