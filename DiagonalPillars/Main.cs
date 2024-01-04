using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Steamworks;
using HMLLibrary;

namespace DiagonalPillars
{
    public class Main : Mod
    {
        static Dictionary<Item_Base, List<Item_Base>> addedItems;
        static HashSet<Item_Base> createdItems = new HashSet<Item_Base>();
        static List<Sprite> createdSprites = new List<Sprite>();
        static Transform prefabParent;
        public override bool CanUnload(ref string message)
        {
            if (SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
            {
                message = "Mod cannot be unloaded while in a multiplayer";
                return false;
            }
            return base.CanUnload(ref message);
        }
        Harmony harmony;
        static bool loaded = false;
        public void Start()
        {
            if (SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
            {
                Debug.LogError($"[{name}]: This cannot be loaded in multiplayer");
                modlistEntry.modinfo.unloadBtn.GetComponent<Button>().onClick.Invoke();
                return;
            }
            loaded = true;
            harmony = new Harmony("com.aidanamite.DiagonalPillars");
            addedItems = new Dictionary<Item_Base, List<Item_Base>>();
            prefabParent = new GameObject("prefabParent").transform;
            prefabParent.gameObject.SetActive(false);
            DontDestroyOnLoad(prefabParent.gameObject);
            var index = 4200;
            foreach (var i in new[] {
            ItemManager.GetItemByIndex(84),
            ItemManager.GetItemByIndex(146),
            ItemManager.GetItemByIndex(399),
            ItemManager.GetItemByIndex(400) })
            {
                var n = new List<Item_Base>();
                addedItems.Add(i, n);
                var newItem = i.Clone(index++, i.UniqueName + "_Diagonal");
                n.Add(newItem);
                ChangeUpgrades(newItem, i, 0);
                var t = newItem.settings_Inventory.Sprite.texture.GetReadable(newItem.settings_Inventory.Sprite.rect);
                var ot = t.GetReadable(newItem.settings_Inventory.Sprite.rect);
                for (int x = 0; x < t.width; x++)
                    for (int y = 0; y < t.height; y++)
                        t.SetPixel(x, y, ot.GetPixel((x + (y - t.height / 2) / 2).Mod(t.width), y));
                t.Apply();
                var t2 = new Texture2D(t.width, t.height, t.format, false);
                t2.SetPixels(t.GetPixels(0));
                t2.Apply(true, true);
                newItem.settings_Inventory.Sprite = t2.ToSprite();
                createdSprites.Add(newItem.settings_Inventory.Sprite);
                Destroy(t);
                var prefabs = new List<Block>(newItem.settings_buildable.GetBlockPrefabs()).ToArray();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    prefabs[j] = Instantiate(prefabs[j], prefabParent);
                    foreach (Transform c in prefabs[j].transform)
                    {
                        var scale = c.localScale;
                        scale.Scale(new Vector3(1, new Vector2(BlockCreator.HalfFloorHeight, BlockCreator.BlockSize).magnitude / BlockCreator.HalfFloorHeight, 1));
                        c.localScale = scale;
                        c.RotateAround(Vector3.zero, Vector3.right, Mathf.Atan(BlockCreator.BlockSize / (BlockCreator.HalfFloorHeight - 0.1f)) / Mathf.PI * 180);
                    }
                    prefabs[j].colliderPrefab = Instantiate(prefabs[j].colliderPrefab, prefabParent);

                    prefabs[j].ReplaceValues(i, newItem);
                    foreach (var quad in prefabs[j].colliderPrefab.GetComponentsInChildren<BlockQuad>(true))
                        quad.transform.localPosition = quad.transform.localPosition.offsetZByY();
                    /*if (modified.TryGetValue(prefabs[j].blockCollisionMask, out var v))
                        prefabs[j].blockCollisionMask = v;
                    else {
                        prefabs[j].blockCollisionMask = Instantiate(prefabs[j].blockCollisionMask);
                        prefabs[j].blockCollisionMask.exceptSelf = false;
                    }*/
                    /*foreach (var r in prefabs[j].GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh = new Mesh();
                        var v = new List<Vector3>();
                        r.mesh.GetVertices(v);
                        mesh.vertices = v.ToArray();
                        var p = new List<int>();
                        r.mesh.GetTriangles(p, 0);
                        mesh.triangles = p.ToArray();
                        var u = new List<Vector2>();
                        r.mesh.GetUVs(0, u);
                        mesh.uv = u.ToArray();
                        for (int k = 0; k < mesh.vertices.Length; k++)
                            mesh.vertices[k] = mesh.vertices[k].offsetByY();
                        mesh.RecalculateBounds();
                        r.sharedMesh = mesh;
                        createdMesh.Add(mesh);
                    }*/
                }
                Traverse.Create(newItem.settings_buildable).Field("blockPrefabs").SetValue(prefabs);
                RAPI.RegisterItem(newItem);
                createdItems.Add(newItem);
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                    if (type.AcceptsBlock(i))
                        Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(newItem);
                foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                    if (q.IgnoresBlock(i))
                        Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(newItem);

                // ---------------------------

                newItem = i.Clone(index++, i.UniqueName + "_Diagonal2");
                n.Add(newItem);
                ChangeUpgrades(newItem, i, 1);
                t = newItem.settings_Inventory.Sprite.texture.GetReadable(newItem.settings_Inventory.Sprite.rect);
                for (int x = 0; x < t.width; x++)
                    for (int y = 0; y < t.height; y++)
                        t.SetPixel(x, y, ot.GetPixel((x + (y - t.height / 2) / 2).Mod(t.width), y));
                t.Apply();
                t2 = new Texture2D(t.width, t.height, t.format, false);
                t2.SetPixels(t.GetPixels(0));
                t2.Apply(true, true);
                newItem.settings_Inventory.Sprite = t2.ToSprite();
                createdSprites.Add(newItem.settings_Inventory.Sprite);
                Destroy(t);
                Destroy(ot);
                prefabs = new List<Block>(newItem.settings_buildable.GetBlockPrefabs()).ToArray();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    prefabs[j] = Instantiate(prefabs[j], prefabParent);
                    foreach (Transform c in prefabs[j].transform)
                    {
                        var scale = c.localScale;
                        scale.Scale(new Vector3(1, new Vector3(BlockCreator.BlockSize, BlockCreator.HalfFloorHeight, BlockCreator.BlockSize).magnitude / BlockCreator.HalfFloorHeight, 1));
                        c.localScale = scale;
                        c.RotateAround(Vector3.zero, Vector3.right, Mathf.Atan(Mathf.Sqrt(Mathf.Pow(BlockCreator.BlockSize, 2) * 2) / (BlockCreator.HalfFloorHeight - 0.1f)) / Mathf.PI * 180);
                        c.RotateAround(Vector3.zero, Vector3.up, 45);
                    }
                    prefabs[j].colliderPrefab = Instantiate(prefabs[j].colliderPrefab, prefabParent);

                    prefabs[j].ReplaceValues(i, newItem);
                    foreach (var quad in prefabs[j].colliderPrefab.GetComponentsInChildren<BlockQuad>(true))
                        quad.transform.localPosition = quad.transform.localPosition.offsetZByY().offsetXByY();
                }
                Traverse.Create(newItem.settings_buildable).Field("blockPrefabs").SetValue(prefabs);
                RAPI.RegisterItem(newItem);
                createdItems.Add(newItem);
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                    if (type.AcceptsBlock(i))
                        Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(newItem);
                foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                    if (q.IgnoresBlock(i))
                        Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(newItem);
            }
            foreach (var i in new[] {
            ItemManager.GetItemByIndex(541),
            ItemManager.GetItemByIndex(542),
            ItemManager.GetItemByIndex(543),
            ItemManager.GetItemByIndex(544) })
            {
                var n = new List<Item_Base>();
                addedItems.Add(i, n);

                var newItem = i.Clone(index++, i.UniqueName + "_Diagonal");
                n.Add(newItem);
                ChangeUpgrades(newItem, i, 0);
                var t = newItem.settings_Inventory.Sprite.texture.GetReadable(newItem.settings_Inventory.Sprite.rect);
                var ot = t.GetReadable(newItem.settings_Inventory.Sprite.rect);
                for (int x = 0; x < t.width; x++)
                    for (int y = 0; y < t.height; y++)
                        t.SetPixel(x, y, ot.GetPixel(x, (y + (x - t.width / 2) / 2).Mod(t.height)));
                t.Apply();
                var t2 = new Texture2D(t.width,t.height,t.format,false);
                t2.SetPixels(t.GetPixels(0));
                t2.Apply(true, true);
                newItem.settings_Inventory.Sprite = t2.ToSprite();
                createdSprites.Add(newItem.settings_Inventory.Sprite);
                Destroy(t);
                Destroy(ot);
                var prefabs = new List<Block>(newItem.settings_buildable.GetBlockPrefabs()).ToArray();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    prefabs[j] = Instantiate(prefabs[j], prefabParent);
                    foreach (Transform c in prefabs[j].transform)
                    {
                        var scale = c.localScale;
                        scale.Scale(new Vector3(1, new Vector3(BlockCreator.BlockSize, 0, BlockCreator.BlockSize).magnitude / BlockCreator.BlockSize, 1));
                        c.localScale = scale;
                        c.RotateAround(Vector3.zero, Vector3.up, 45);
                    }
                    prefabs[j].colliderPrefab = Instantiate(prefabs[j].colliderPrefab, prefabParent);

                    prefabs[j].ReplaceValues(i, newItem);
                        foreach (var quad in prefabs[j].colliderPrefab.GetComponentsInChildren<BlockQuad>(true))
                            quad.transform.localPosition += new Vector3(0,0,(i.UniqueName.ToLower().Contains("half") ? 1 : 2) * -BlockCreator.BlockSize);
                    
                }
                Traverse.Create(newItem.settings_buildable).Field("blockPrefabs").SetValue(prefabs);
                RAPI.RegisterItem(newItem);
                createdItems.Add(newItem);
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                    if (type.AcceptsBlock(i))
                        Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(newItem);
                foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                    if (q.IgnoresBlock(i))
                        Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(newItem);

            }


            ModUtils_ReloadBuildMenu();

            Log("Mod has been loaded!");
        }

        public static void ChangeUpgrades(Item_Base newItem, Item_Base oldItem, int itemIndex)
        {
            if (oldItem.settings_buildable?.Upgrades == null)
                return;
            Traverse.Create(newItem.settings_buildable).Field("upgrades").SetValue(new ItemInstance_Buildable.Upgrade());
            FieldInfo selfUpgrade = null;
            var targetFields = oldItem.settings_buildable.Upgrades.FindFieldsMatch<Item_Base>(x => x);
            foreach (var f in targetFields)
            {
                if (selfUpgrade == null)
                    selfUpgrade = (f.GetValue(oldItem.settings_buildable.Upgrades) as Item_Base).settings_buildable.Upgrades.FindFieldsMatch<Item_Base>(x => x?.UniqueIndex == oldItem.UniqueIndex).FirstOrDefault();
                else
                    break;
            }
            foreach (var f in targetFields)
            {
                var l = addedItems.FirstOrDefault(x => x.Key.UniqueIndex == (f.GetValue(oldItem.settings_buildable.Upgrades) as Item_Base).UniqueIndex).Value;
                if (l == null || l.Count <= itemIndex)
                    f.SetValue(newItem.settings_buildable.Upgrades, f.GetValue(oldItem.settings_buildable.Upgrades));
                else
                {
                    f.SetValue(newItem.settings_buildable.Upgrades, l[itemIndex]);
                    if (selfUpgrade != null)
                        selfUpgrade.SetValue(l[itemIndex].settings_buildable.Upgrades, newItem);
                }
            }
        }

        public void OnModUnload()
        {
            harmony?.UnpatchAll(harmony.Id);
            if (!loaded)
                return;
            loaded = false;
            ModUtils_ReloadBuildMenu();
            var items = ItemManager.GetAllItems();
            items.RemoveAll((x) => createdItems.Contains(x));
            foreach (var i in createdItems)
            {
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                    if (type.AcceptsBlock(i))
                        Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().RemoveAll((x) => x.UniqueName == i.UniqueName);
                foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                    if (q.IgnoresBlock(i))
                        Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().RemoveAll((x) => x.UniqueName == i.UniqueName);
                Destroy(i);
            }
            createdItems.Clear();
            foreach (var s in createdSprites)
            {
                if (!s)
                    continue;
                if (s.texture)
                    Destroy(s.texture);
                Destroy(s);
            }
            createdSprites.Clear();
            Destroy(prefabParent.gameObject);
            Log("Mod has been unloaded!");
        }


        List<(Item_Base, Item_Base)> ModUtils_BuildMenuItems()
        {
            if (!loaded) return null;
            var l = new List<(Item_Base, Item_Base)>();
            foreach (var i in addedItems)
                foreach (var j in i.Value)
                    l.Add((i.Key, j));
            return l;
        }

        void ModUtils_ReloadBuildMenu() { }
    }


    static class ExtentionMethods
    {
        public static Y Join<X, Y>(this IEnumerable<X> collection, System.Func<X, Y> converter, System.Func<Y, Y, Y> joiner) => (collection as IEnumerable).Join(converter, joiner);
        public static Y Join<X, Y>(this IEnumerable collection, System.Func<X, Y> converter, System.Func<Y, Y, Y> joiner)
        {
            bool first = false;
            Y r = default(Y);
            foreach (X v in collection)
                if (first)
                    r = joiner(r, converter(v));
                else
                {
                    r = converter(v);
                    first = true;
                }
            return r;
        }
        public static bool Exists<T>(this IEnumerable<T> collection, System.Predicate<T> match) => (collection as IEnumerable).Exists(match);
        public static bool Exists<T>(this IEnumerable collection, System.Predicate<T> match)
        {
            foreach (T v in collection)
                if (match(v))
                    return true;
            return false;
        }
        public static Sprite ToSprite(this Texture2D t, Rect? rect = null, Vector2? pivot = null) => Sprite.Create(t, rect ?? new Rect(0, 0, t.width, t.height), pivot ?? new Vector2(0.5f, 0.5f));

        public static Texture2D GetReadable(this Texture2D source, Rect? copyArea = null, RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, TextureFormat? targetFormat = null, bool mipChain = true)
        {
            var temp = RenderTexture.GetTemporary(source.width, source.height, 0, format, readWrite);
            Graphics.Blit(source, temp);
            temp.filterMode = FilterMode.Point;
            var prev = RenderTexture.active;
            RenderTexture.active = temp;
            var area = copyArea ?? new Rect(0, 0, temp.width, temp.height);
            var texture = new Texture2D((int)area.width, (int)area.height, targetFormat ?? TextureFormat.RGBA32, mipChain);
            texture.ReadPixels(area, 0, 0);
            texture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(temp);
            return texture;
        }
        public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
        {
            Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
            item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
            item.settings_buildable = source.settings_buildable.Clone();
            item.settings_consumeable = source.settings_consumeable.Clone();
            item.settings_cookable = source.settings_cookable.Clone();
            item.settings_equipment = source.settings_equipment.Clone();
            item.settings_Inventory = source.settings_Inventory.Clone();
            item.settings_recipe = source.settings_recipe.Clone();
            item.settings_usable = source.settings_usable.Clone();
            return item;
        }

        public static void SetRecipe(this ItemInstance_Recipe item, CostMultiple[] cost, int amountToCraft = 1)
        {
            Traverse.Create(item).Field("amountToCraft").SetValue(amountToCraft);
            item.NewCost = cost;
        }
        public static int Mod(this int a, int b) => a % b + (b < 0 == a < 0 ? 0 : a);
        public static Vector3 offsetZByY(this Vector3 v) => new Vector3(v.x, v.y, v.z + v.y / BlockCreator.HalfFloorHeight * BlockCreator.BlockSize);
        public static Vector3 offsetXByY(this Vector3 v) => new Vector3(v.x + v.y / BlockCreator.HalfFloorHeight * BlockCreator.BlockSize, v.y, v.z);

        public static List<FieldInfo> FindAllFields(this object obj)
        {
            var t = obj.GetType();
            var l = new List<FieldInfo>();
            do
            {
                foreach (var f in t.GetFields((BindingFlags)(-1)))
                    if (!f.IsStatic)
                        l.Add(f);
                t = t.BaseType;
            } while (t != typeof(object));
            return l;
        }
        public static void ReplaceValues(this Component value, object original, object replacement)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement);
        }
        public static void ReplaceValues(this GameObject value, object original, object replacement)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement);
        }

        public static void ReplaceValues(this object value, object original, object replacement)
        {
            var t = value.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && f.GetValue(value) == original)
                        f.SetValue(value, replacement);
                t = t.BaseType;
            }
        }

        public static void CopyFieldsOf(this object value, object source)
        {
            var t1 = value.GetType();
            var t2 = source.GetType();
            while (!t1.IsAssignableFrom(t2))
                t1 = t1.BaseType;
            while (t1 != typeof(Object) && t1 != typeof(object))
            {
                foreach (var f in t1.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(value, f.GetValue(source));
                t1 = t1.BaseType;
            }
        }
        public static FieldInfo[] FindFieldsMatch<T>(this object obj, System.Predicate<T> predicate)
        {
            var fs = new List<FieldInfo>();
            var t = obj.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && typeof(T).IsAssignableFrom(f.FieldType) && (predicate == null || predicate((T)f.GetValue(obj))))
                        fs.Add(f);
                t = t.BaseType;
            }
            return fs.ToArray();
        }
    }
}