using UnityEngine;

public class LookAtTest : MonoBehaviour {
    public Transform objectToRotate; // 回転させるオブジェクト
    public Transform targetObject; // ターゲットオブジェクト

    void Update() {
        if (objectToRotate != null && targetObject != null) {
            // ターゲットオブジェクトの方向を向く
            Vector3 directionToTarget = targetObject.position - objectToRotate.position;
            Quaternion lookAtRotation = Quaternion.LookRotation(directionToTarget);
            objectToRotate.rotation = lookAtRotation;
        } else {
            if (objectToRotate == null) {
                Debug.LogWarning("Object to Rotate is not set.");
            }
            if (targetObject == null) {
                Debug.LogWarning("Target Object is not set.");
            }
        }
    }
}