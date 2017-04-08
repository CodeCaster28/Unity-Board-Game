using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal interface IBrushGenerator
	{
		CSGOperationType CurrentCSGOperationType { get; set; }
		bool OnShowGUI(bool isSceneGUI);
		void Init();
		bool HotKeyReleased();
		bool UndoRedoPerformed();
		void PerformDeselectAll();
		void HandleEvents(Rect sceneRect);
		
		// unity bug workaround
		void StartGUI();
		void DoCancel();
		void DoCommit();
		void FinishGUI();
	}
}
