
// using System.Collections.Concurrent;
// using System.Threading.Tasks;
// using System.Text;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using NAK;

public class AssetInstancerGUI : EditorWindow {

	#region UI Declarations
	private static GUIContent iconToolbarMinus;
	private static GUIStyle preButton;
	#endregion

	#region Automated Declarations
	private static Vector2 scroll;
	private static ReorderableList ignoreList;
	private static ReorderableList ignoreList2;
	#endregion

	#region Input Variables
	public Object oldAsset;
	public List < DefaultAsset > ignoreFolders = new List < DefaultAsset > ();
	public List < Object > displayDependencies = new List < Object > ();
	#endregion
	
	[MenuItem("Tools/Asset Instancer", false, 20)]
	public static void ShowWindow()
	{
		GetWindow<AssetInstancerGUI>(false, "Asset Instancer", true);
	}
	private void OnGUI() {

		scroll = EditorGUILayout.BeginScrollView(scroll);
		GUIStyle box = GUI.skin.GetStyle("box");
		preButton = "RL FooterButton";
		
		//old asset input
		EditorGUIUtility.labelWidth = 100;
		oldAsset = EditorGUILayout.ObjectField("Asset:", oldAsset, typeof(Object), true) as Object;
		EditorGUIUtility.labelWidth = 0;

		//instance button
		using (new EditorGUI.DisabledScope(oldAsset == null))
			if (GUILayout.Button("Instance"))
			NAK.AssetInstancer.InstanceAsset(oldAsset, ignoreFolders);
			
		// //panic button if i fuck up and hang unity in editing state
		if (GUILayout.Button("Show Dependencies")) {
			Debug.Log( Floor( (decimal)11 % 10) + 48 );
		}
			
		// 	// AssetDatabase.StopAssetEditing();
		// 	// AssetDatabase.SaveAssets();
		// 	// AssetDatabase.Refresh();
		// 	string path = AssetDatabase.GetAssetPath(oldAsset);
		// 	var dependencies = AssetDatabase.GetDependencies(path, true);

			

		// 	foreach (var toAdd in dependencies) {
		// 		displayDependencies.Add( AssetDatabase.LoadAssetAtPath<Object>(toAdd) );
		// 	}

		// }

		//ignore folder list
		using(new GUILayout.VerticalScope(box))
			ignoreList.DoLayoutList();

		// //ignore folder list
		// using(new GUILayout.VerticalScope(box))
		// 	ignoreList2.DoLayoutList();

		EditorGUILayout.EndScrollView();
	}

	//all of the reorderable list shit is below (DREADRITH)

	private void OnEnable() {
		iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove element from list");
		ignoreList = new ReorderableList(ignoreFolders, typeof(DefaultAsset), true, false, true, false) {
			drawHeaderCallback = DrawHeaderIL,
			onAddCallback = AddElementIL,
			drawElementCallback = DrawElementIL,
		};		
		ignoreList2 = new ReorderableList(displayDependencies, typeof(Object), true, false, true, false) {
			drawHeaderCallback = DrawHeaderDD,
			onAddCallback = AddElementDD,
			drawElementCallback = DrawElementDD,
		};

		//add null list object by default to QOL drag and drop
		if (ignoreFolders.Count == 0) ignoreFolders.Add(null);
	}

	private void OnDisable() {
	}

	//Ignore List Code
	private static void DrawHeaderIL(Rect rect) {
		GUI.Label(rect, "Ignore Folders:");
	}
	private void AddElementIL(ReorderableList list) {
		ignoreFolders.Add(null);
	}
	private void DrawElementIL(Rect rect, int index, bool active, bool focused) {
		if (! (index < ignoreFolders.Count && index >= 0)) return;

		#region Rects
		rect.y += 1;
		rect.height = 18;
		rect.width -= 44;

		Rect removeRect = new Rect(rect.width + 36, rect.y, 32, 20);
		#endregion

		EditorGUI.BeginChangeCheck();

		ignoreFolders[index] = EditorGUI.ObjectField(rect, ignoreFolders[index], typeof(DefaultAsset), false) as DefaultAsset;

		if (Event.current.type == EventType.DragExited && rect.Contains(Event.current.mousePosition)) {
			ignoreFolders.InsertRange(index, DragAndDrop.objectReferences.Where(o =>o is DefaultAsset && ignoreFolders[index] != o).Cast < DefaultAsset > ());
			FilterObjectList();
		}

		if (EditorGUI.EndChangeCheck()) {
			FilterObjectList();
		}

		if (GUI.Button(removeRect, iconToolbarMinus, preButton)) ignoreFolders.RemoveAt(index);
		//add null list object if there are no more items in list to QOL drag and drop
		if (ignoreFolders.Count == 0) ignoreFolders.Add(null);
	}

	private void FilterObjectList() {
		var filteredFolders = ignoreFolders.Distinct().ToList();
		ignoreFolders.Clear();
		foreach (var toAdd in filteredFolders) {
			ignoreFolders.Add(toAdd);
		}
	}

	//Display Dependencies Code
	private static void DrawHeaderDD(Rect rect) {
		GUI.Label(rect, "Dependencies:");
	}
	private void AddElementDD(ReorderableList list) {
		displayDependencies.Add(null);
	}
	private void DrawElementDD(Rect rect, int index, bool active, bool focused) {
		if (! (index < displayDependencies.Count && index >= 0)) return;

		#region Rects
		rect.y += 1;
		rect.height = 18;
		rect.width -= 44;

		Rect removeRect = new Rect(rect.width + 36, rect.y, 32, 20);
		#endregion

		// EditorGUI.BeginChangeCheck();

		displayDependencies[index] = EditorGUI.ObjectField(rect, displayDependencies[index], typeof(Object), false) as Object;

		// if (EditorGUI.EndChangeCheck()) {
			// FilterObjectList();
		// }

		if (GUI.Button(removeRect, iconToolbarMinus, preButton)) displayDependencies.RemoveAt(index);
		//add null list object if there are no more items in list to QOL drag and drop
		if (displayDependencies.Count == 0) displayDependencies.Add(null);
	}
}