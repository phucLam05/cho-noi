/**
 * BoatPhysicMaterialCreator: Editor helper tạo PhysicsMaterial cho ghe & môi trường.
 * [Chức năng]: Menu "ChoNoi > Create Boat PhysicMaterial" tạo sẵn 1 asset PhysicsMaterial với
 *              Bounciness = 0 (không nảy) và ma sát thấp, để ghe đâm bờ thì TRƯỢT DỌC theo bờ
 *              thay vì nảy ngược như quả bóng (Phase 4 - Collision Polish).
 * [Dependencies]: UnityEditor, UnityEngine. (Unity 6 đổi tên PhysicMaterial -> PhysicsMaterial.)
 */

using UnityEngine;
using UnityEditor;

namespace ChoNoi.Editor
{
    public static class BoatPhysicMaterialCreator
    {
        private const string AssetPath = "Assets/_Project/BoatPhysicsMaterial.physicMaterial";

        [MenuItem("ChoNoi/Physics/Create Boat PhysicMaterial")]
        public static void Create()
        {
            // Bounciness = 0: không nảy. Combine = Minimum: lấy giá trị nhỏ nhất giữa 2 vật
            // -> chỉ cần 1 bên có bounciness thấp là cả va chạm không nảy.
            var material = new PhysicsMaterial("BoatPhysicsMaterial")
            {
                bounciness = 0f,
                dynamicFriction = 0.2f,
                staticFriction = 0.2f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            AssetDatabase.CreateAsset(material, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Chọn asset vừa tạo trong Project window cho dễ kéo thả.
            Selection.activeObject = material;
            EditorGUIUtility.PingObject(material);

            Debug.Log($"[BoatPhysicMaterialCreator] Da tao PhysicsMaterial tai: {AssetPath}\n" +
                      "Keo material nay vao Collider cua ghe VA tuong/bo song de ghe khong nay khi dam.");
        }
    }
}
