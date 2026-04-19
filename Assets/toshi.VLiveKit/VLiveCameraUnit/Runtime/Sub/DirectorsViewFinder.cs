// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.
// last update: 2024/11/16

using System;
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Photography
{
    [ExecuteAlways] 
	[RequireComponent(typeof(Camera))]
	public class DirectorsViewFinder : MonoBehaviour
	{
		#if UNITY_EDITOR
		public enum Mode
		{
			OFF,
			ON,
		}

		private Mode currentMode = Mode.ON;

		private Vector3 previousDirectorPosition = Vector3.zero;
		private Vector3 previousViewPosition = Vector3.zero;
		private Quaternion previousDirectorRotation = Quaternion.identity;
		private Quaternion previousViewRotation = Quaternion.identity;

		private void OnEnable()
		{
			UnityEditor.EditorApplication.update += UpdateView;
		}

		private void OnDisable()
		{
			UnityEditor.EditorApplication.update -= UpdateView;
		}

		void UpdateView()
		{
			var activeSceneView = SceneView.lastActiveSceneView;
			var viewCameraTransform = activeSceneView.camera.transform;

			switch (currentMode)
			{
				case Mode.ON:
					TransferSceneToGame(transform, viewCameraTransform);
					break;
				case Mode.OFF:
					// Do nothing
					break;
			}

			previousDirectorPosition = transform.position;
			previousViewPosition = viewCameraTransform.position;
			previousDirectorRotation = transform.rotation;
			previousViewRotation = viewCameraTransform.rotation;
		}

		public static void TransferSceneToGame(Transform directorCamera, Transform viewCamera)
		{
			directorCamera.position = viewCamera.position;
			directorCamera.rotation = viewCamera.rotation;
		}
		#endif
	}
} // namespace toshi.VLiveKit.Photography
