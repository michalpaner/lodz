using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SanAndreasUnity.Editor
{
    public static class LodzMapSceneGenerator
    {
        private const double CenterLatitude = 51.7592;
        private const double CenterLongitude = 19.4560;
        private const string GeneratedFolder = "Assets/Generated/Lodz";
        private const string ScenePath = "Assets/Scenes/Lodz.unity";

        private sealed class Place
        {
            public readonly string Name;
            public readonly double Latitude;
            public readonly double Longitude;
            public readonly float Radius;

            public Place(string name, double latitude, double longitude, float radius)
            {
                Name = name;
                Latitude = latitude;
                Longitude = longitude;
                Radius = radius;
            }
        }

        private static readonly Place[] Districts =
        {
            new Place("Śródmieście", 51.7680, 19.4560, 1000f),
            new Place("Bałuty", 51.8020, 19.4450, 1500f),
            new Place("Widzew", 51.7650, 19.5100, 1500f),
            new Place("Polesie", 51.7670, 19.4050, 1400f),
            new Place("Górna", 51.7200, 19.4700, 1500f),
            new Place("Retkinia", 51.7350, 19.3750, 1100f),
            new Place("Teofilów", 51.8080, 19.3650, 1000f),
            new Place("Olechów", 51.7350, 19.5600, 900f),
            new Place("Chojny", 51.7100, 19.4550, 900f),
            new Place("Łagiewniki", 51.8300, 19.4850, 1000f)
        };

        private static readonly Place[] NearbyTowns =
        {
            new Place("Zgierz", 51.8550, 19.4060, 1300f),
            new Place("Aleksandrów Łódzki", 51.8190, 19.3030, 1100f),
            new Place("Konstantynów Łódzki", 51.7470, 19.3260, 1000f),
            new Place("Pabianice", 51.6640, 19.3540, 1500f),
            new Place("Rzgów", 51.6640, 19.4910, 700f),
            new Place("Tuszyn", 51.6090, 19.5310, 800f),
            new Place("Andrespol", 51.7280, 19.6410, 700f),
            new Place("Brzeziny", 51.8010, 19.7520, 900f),
            new Place("Stryków", 51.9020, 19.6050, 850f),
            new Place("Ozorków", 51.9640, 19.2920, 900f),
            new Place("Głowno", 51.9650, 19.7170, 800f)
        };

        private static readonly Place[] Landmarks =
        {
            new Place("Manufaktura", 51.7790, 19.4440, 0f),
            new Place("Piotrkowska", 51.7590, 19.4560, 0f),
            new Place("EC1 Łódź", 51.7680, 19.4690, 0f),
            new Place("Atlas Arena", 51.7590, 19.4250, 0f),
            new Place("Orientarium ZOO", 51.7590, 19.4080, 0f),
            new Place("Księży Młyn", 51.7510, 19.4830, 0f),
            new Place("Las Łagiewnicki", 51.8300, 19.4850, 0f),
            new Place("Lotnisko Łódź", 51.7210, 19.3980, 0f),
            new Place("Dworzec Łódź Fabryczna", 51.7680, 19.4660, 0f),
            new Place("Park na Zdrowiu", 51.7640, 19.4250, 0f)
        };

        [MenuItem("San Andreas Unity/Łódź/Generate Łódź Map Scene")]
        public static void Generate()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Generate Łódź map",
                    "The Łódź scene already exists. Regenerate it?",
                    "Regenerate",
                    "Cancel"))
                return;

            EnsureFolder("Assets/Generated");
            EnsureFolder(GeneratedFolder);

            var groundMaterial = GetMaterial("Ground", new Color(0.39f, 0.50f, 0.39f));
            var buildingMaterial = GetMaterial("Buildings", new Color(0.66f, 0.65f, 0.60f));
            var townMaterial = GetMaterial("Towns", new Color(0.75f, 0.68f, 0.53f));
            var roadMaterial = GetMaterial("Roads", new Color(0.19f, 0.21f, 0.22f));
            var highwayMaterial = GetMaterial("Highways", new Color(0.83f, 0.49f, 0.22f));
            var landmarkMaterial = GetMaterial("Landmarks", new Color(0.79f, 0.25f, 0.20f));
            var districtMaterial = GetMaterial("Districts", new Color(0.91f, 0.83f, 0.60f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Łódź and Central Poland");

            CreateGround(root.transform, groundMaterial);
            CreateUrbanFabric(root.transform, buildingMaterial);
            CreateTownFabric(root.transform, townMaterial);
            CreateRoadNetwork(root.transform, roadMaterial, highwayMaterial);
            CreatePlaceMarkers(root.transform, landmarkMaterial, districtMaterial);
            CreateCamera(root.transform);
            CreateLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Łódź map generated",
                "Open Assets/Scenes/Lodz.unity and press Play. The scene is a geographic concept map; local street geometry is schematic.",
                "OK");
        }

        private static void CreateGround(Transform parent, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Regional map ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -2f, 0f);
            ground.transform.localScale = new Vector3(5000f, 1f, 5000f);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateUrbanFabric(Transform parent, Material material)
        {
            var random = new System.Random(1906);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int halfExtent = 6000;
            const int spacing = 220;

            for (int x = -halfExtent; x <= halfExtent; x += spacing)
            {
                for (int z = -halfExtent; z <= halfExtent; z += spacing)
                {
                    if (random.NextDouble() < 0.22 || x * x + z * z > halfExtent * halfExtent)
                        continue;

                    float width = 70f + (float)random.NextDouble() * 110f;
                    float depth = 70f + (float)random.NextDouble() * 110f;
                    float height = 12f + (float)random.NextDouble() * 75f;
                    float rotation = (float)(random.NextDouble() * 16.0 - 8.0);
                    AddBox(vertices, triangles, new Vector3(x, height * 0.5f, z),
                        new Vector3(width, height, depth), rotation);
                }
            }

            CreateMeshObject(parent, "Łódź urban blocks", vertices, triangles, material, "LodzUrbanBlocks");
        }

        private static void CreateTownFabric(Transform parent, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var random = new System.Random(1991);

            foreach (var town in NearbyTowns)
            {
                var center = ToWorld(town.Latitude, town.Longitude);
                int radius = Mathf.RoundToInt(town.Radius);
                const int spacing = 190;

                for (int x = -radius; x <= radius; x += spacing)
                {
                    for (int z = -radius; z <= radius; z += spacing)
                    {
                        if (x * x + z * z > radius * radius || random.NextDouble() < 0.48)
                            continue;

                        float height = 8f + (float)random.NextDouble() * 32f;
                        float width = 55f + (float)random.NextDouble() * 75f;
                        float depth = 55f + (float)random.NextDouble() * 75f;
                        AddBox(vertices, triangles,
                            new Vector3(center.x + x, height * 0.5f, center.z + z),
                            new Vector3(width, height, depth), (float)random.NextDouble() * 12f - 6f);
                    }
                }
            }

            CreateMeshObject(parent, "Nearby towns", vertices, triangles, material, "LodzNearbyTowns");
        }

        private static void CreateRoadNetwork(Transform parent, Material localRoadMaterial, Material highwayMaterial)
        {
            var localRoads = new List<Vector3>();
            var localTriangles = new List<int>();
            var highways = new List<Vector3>();
            var highwayTriangles = new List<int>();

            for (int offset = -5500; offset <= 5500; offset += 440)
            {
                AddRoadSegment(localRoads, localTriangles,
                    new Vector2(offset, -5500f), new Vector2(offset, 5500f), 18f, 1.5f);
                AddRoadSegment(localRoads, localTriangles,
                    new Vector2(-5500f, offset), new Vector2(5500f, offset), 18f, 1.5f);
            }

            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.430, 51.800),
                new Vector2(19.406, 51.855)
            }, 46f, 2.5f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.390, 51.790),
                new Vector2(19.303, 51.819)
            }, 38f, 2.4f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.400, 51.750),
                new Vector2(19.326, 51.747)
            }, 34f, 2.3f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.410, 51.715),
                new Vector2(19.354, 51.664)
            }, 42f, 2.5f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.490, 51.715),
                new Vector2(19.491, 51.664), new Vector2(19.531, 51.609)
            }, 34f, 2.4f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.540, 51.748),
                new Vector2(19.641, 51.728)
            }, 34f, 2.3f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.456, 51.759), new Vector2(19.540, 51.800),
                new Vector2(19.605, 51.902)
            }, 42f, 2.5f);
            AddPath(highways, highwayTriangles, new[]
            {
                new Vector2(19.405, 51.964), new Vector2(19.480, 51.940),
                new Vector2(19.560, 51.940), new Vector2(19.640, 51.940)
            }, 48f, 2.5f);

            AddPath(localRoads, localTriangles, new[]
            {
                new Vector2(19.365, 51.820), new Vector2(19.400, 51.845),
                new Vector2(19.455, 51.850), new Vector2(19.520, 51.840),
                new Vector2(19.560, 51.805), new Vector2(19.570, 51.755),
                new Vector2(19.550, 51.710), new Vector2(19.500, 51.685),
                new Vector2(19.430, 51.685), new Vector2(19.380, 51.715),
                new Vector2(19.365, 51.765), new Vector2(19.365, 51.820)
            }, 24f, 1.8f);

            CreateMeshObject(parent, "Schematic city street grid", localRoads, localTriangles,
                localRoadMaterial, "LodzLocalRoads");
            CreateMeshObject(parent, "Regional routes", highways, highwayTriangles,
                highwayMaterial, "LodzRegionalRoads");
        }

        private static void CreatePlaceMarkers(Transform parent, Material landmarkMaterial, Material districtMaterial)
        {
            foreach (var district in Districts)
            {
                var point = ToWorld(district.Latitude, district.Longitude);
                CreateMarker(parent, district.Name, point, districtMaterial, 100f, 3f);
                CreateLabel(parent, district.Name, point + new Vector3(0f, 115f, 80f), 44, 13f);
            }

            foreach (var town in NearbyTowns)
            {
                var point = ToWorld(town.Latitude, town.Longitude);
                CreateMarker(parent, town.Name, point, districtMaterial, 70f, 3f);
                CreateLabel(parent, town.Name, point + new Vector3(0f, 85f, 70f), 38, 11f);
            }

            foreach (var landmark in Landmarks)
            {
                var point = ToWorld(landmark.Latitude, landmark.Longitude);
                CreateMarker(parent, landmark.Name, point, landmarkMaterial, 85f, 26f);
                CreateLabel(parent, landmark.Name, point + new Vector3(0f, 52f, 55f), 38, 10f);
            }

            CreateLabel(parent, "ŁÓDŹ", new Vector3(0f, 120f, 950f), 100, 34f);
            CreateLabel(parent, "Mapa koncepcyjna • układ ulic orientacyjny",
                new Vector3(-17500f, 30f, -18500f), 30, 8f);
        }

        private static void CreateMarker(Transform parent, string name, Vector3 position, Material material,
            float diameter, float height)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position + Vector3.up * (height * 0.5f + 3f);
            marker.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position, int fontSize,
            float characterSize)
        {
            var labelObject = new GameObject("Label: " + text);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = fontSize;
            label.characterSize = characterSize;
            label.color = new Color(0.12f, 0.13f, 0.12f);
        }

        private static void CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Map Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 26000f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 23000f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 60000f;
            camera.backgroundColor = new Color(0.39f, 0.50f, 0.39f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<LodzMapCameraController>();
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void AddPath(List<Vector3> vertices, List<int> triangles, Vector2[] points,
            float width, float height)
        {
            for (int i = 0; i < points.Length - 1; i++)
                AddRoadSegment(vertices, triangles, ToWorld(points[i].y, points[i].x),
                    ToWorld(points[i + 1].y, points[i + 1].x), width, height);
        }

        private static void AddRoadSegment(List<Vector3> vertices, List<int> triangles,
            Vector2 start, Vector2 end, float width, float height)
        {
            var direction = (end - start).normalized;
            var side = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            int index = vertices.Count;
            vertices.Add(new Vector3(start.x + side.x, height, start.y + side.y));
            vertices.Add(new Vector3(start.x - side.x, height, start.y - side.y));
            vertices.Add(new Vector3(end.x + side.x, height, end.y + side.y));
            vertices.Add(new Vector3(end.x - side.x, height, end.y - side.y));
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 center,
            Vector3 size, float angleDegrees)
        {
            int index = vertices.Count;
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            float halfZ = size.z * 0.5f;
            var rotation = Quaternion.Euler(0f, angleDegrees, 0f);
            vertices.Add(center + rotation * new Vector3(-halfX, -halfY, -halfZ));
            vertices.Add(center + rotation * new Vector3(halfX, -halfY, -halfZ));
            vertices.Add(center + rotation * new Vector3(halfX, -halfY, halfZ));
            vertices.Add(center + rotation * new Vector3(-halfX, -halfY, halfZ));
            vertices.Add(center + rotation * new Vector3(-halfX, halfY, -halfZ));
            vertices.Add(center + rotation * new Vector3(halfX, halfY, -halfZ));
            vertices.Add(center + rotation * new Vector3(halfX, halfY, halfZ));
            vertices.Add(center + rotation * new Vector3(-halfX, halfY, halfZ));

            int[] boxTriangles =
            {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7
            };
            for (int i = 0; i < boxTriangles.Length; i++)
                triangles.Add(index + boxTriangles[i]);
        }

        private static void CreateMeshObject(Transform parent, string objectName, List<Vector3> vertices,
            List<int> triangles, Material material, string meshName)
        {
            var mesh = new Mesh { name = meshName };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var savedMesh = SaveMesh(mesh, meshName);

            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = savedMesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh SaveMesh(Mesh mesh, string meshName)
        {
            string path = GeneratedFolder + "/" + meshName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
                AssetDatabase.CreateAsset(mesh, path);
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        private static Material GetMaterial(string name, Color color)
        {
            string path = GeneratedFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Vector2 ToWorld(double latitude, double longitude)
        {
            float x = (float)((longitude - CenterLongitude) * 68500.0);
            float z = (float)((latitude - CenterLatitude) * 111000.0);
            return new Vector2(x, z);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                    return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}