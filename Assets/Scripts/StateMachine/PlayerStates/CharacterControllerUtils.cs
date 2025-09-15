using UnityEngine;

namespace Core
{
    public static class CharacterControllerUtils
    {
        public static Vector3 GetNormalWithSphereCast(CharacterController characterController,
            LayerMask layerMask = default)
        {
            var normal = Vector3.up;
            var center = characterController.transform.position + characterController.center;
            var distance = characterController.height / 2f + characterController.stepOffset + 0.01f;


            RaycastHit hit;
            if (Physics.SphereCast(center, characterController.radius, Vector3.down, out hit, distance, layerMask))
            {
                normal = hit.normal;
            }

            return normal;
        }
    }
}