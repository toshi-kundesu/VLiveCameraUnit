#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private void CallPublic(string methodName)
        {
            serializedObject.ApplyModifiedProperties();

            MethodInfo mi = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                Undo.RecordObject(target, methodName);
                mi.Invoke(target, null);
                EditorUtility.SetDirty(target);
            }
        }

        private void InvokeNonPublic(Object obj, string methodName)
        {
            serializedObject.ApplyModifiedProperties();

            MethodInfo mi = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (mi != null)
            {
                Undo.RecordObject(obj, methodName);
                mi.Invoke(obj, null);
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
#endif