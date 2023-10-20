using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NAK {
    public class AssetInstancer : MonoBehaviour
    {
        //Unity NativeFormatImporter whitelist https://docs.unity3d.com/Manual/BuiltInImporters.html
		public static HashSet<string> NFIWhitelist = new HashSet<string> {
            ".anim",
            ".animset",
            ".asset",
            ".blendtree",
            ".buildreport",
            ".colors",
            ".controller",
            ".cubemap",
            ".curves",
            ".curvesNormalized",
            ".flare",
            ".fontsettings",
            ".giparams",
            ".gradients",
            ".guiskin",
            ".ht",
            ".mask",
            ".mat",
            ".mesh",
            ".mixer",
            ".overrideController",
            ".particleCurves",
            ".particleCurvesSigned",
            ".particleDoubleCurves",
            ".particleDoubleCurvesSigned",
            ".physicMaterial",
            ".physicsMaterial2D",
            ".playable",
            ".preset",
            ".renderTexture",
            ".shadervariants",
            ".spriteatlas",
            ".state",
            ".statemachine",
            ".texture2D",
            ".transition",
            ".webCamTexture",
            ".brush",
            ".terrainlayer",
            ".signal",
            //added
            ".prefab",
            ".unity",
            ".shader",
        };

        //things that we should not duplicate
		public static HashSet<string> DupeBlacklist = new HashSet<string> {
            "VRCSDK/", 
            // ".shader", 
            // ".cs",
            // ".meta",
            // ".exr",
            ".dll",
        };

		#region Hardcoded Variables
		private const string DEFAULTPATH = "Assets/Asset Instances";
		#endregion

		#region Input Variables
		private static Object oldAsset;
		private static List<DefaultAsset> ignoredFolders;
		private static List < string > dependenciesFiltered = new List < string > ();
		private static List < string > duplicatePaths = new List < string > ();
		#endregion

        public static void InstanceAsset(Object inputAsset, List<DefaultAsset> inputFolders)
        {
            Debug.Log("Running Asset Instancer!");

			oldAsset = inputAsset;
			ignoredFolders = inputFolders;

            RunScript();
        }

        private static void RunScript()
		{

			//setup
			RunFilter();

			CreateNewFolders();
			
			// part 1
			AssetDatabase.StartAssetEditing();
			
			DuplicateAssets();
			
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			// part 2
			AssetDatabase.StartAssetEditing();
			
			FixNewAssets();
			
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			dependenciesFiltered.Clear();
			duplicatePaths.Clear();

			EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath($"{DEFAULTPATH}/{oldAsset.name}") );
		}
		
		private static bool CheckBlacklist(string curDependency)
		{
			bool isFlagged = false;
			
			foreach (var toFilter in DupeBlacklist) {
				if ( curDependency.Contains(toFilter) ) {
					isFlagged = true;
					// Debug.Log("FLAGGED BLACKLIST");
					// Debug.Log(curDependency);
					break;
				}
			}

			if(ignoredFolders.Count() != 0){
				foreach (var toFilter in ignoredFolders) {
					if ( curDependency.Contains( AssetDatabase.GetAssetPath(toFilter) ) ) {
						isFlagged = true;
						// Debug.Log("FLAGGED BLACKLIST");
						// Debug.Log(curDependency);
						break;
					}
				}
			}

			return isFlagged;
		}
		
		private static bool CheckWhitelist(string curDependency)
		{
			bool isFlagged = false;
			
			foreach (var toFilter in NFIWhitelist) {
				if ( curDependency.Contains(toFilter) ) {
					isFlagged = true;
					// Debug.Log("FLAGGED WHITELIST");
					// Debug.Log(curDependency);
					break;
				}
			}
			return isFlagged;
		}

		private static void RunFilter() 
		{

			string path = AssetDatabase.GetAssetPath(oldAsset);

            if (AssetDatabase.IsValidFolder(path))
            {
                FilterFolders();
				Debug.Log("FILTER FOLDER");
            }
            else
            {
                FilterAssets();
				Debug.Log("FILTER ASSETS");
            }
        }

		//why tf u no work... u worked like 5 minutes ago
		private static void FilterFolders()
		{

			string path = AssetDatabase.GetAssetPath(oldAsset);
			var dependencies = System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories);

			// Debug.Log(path);

			// dependenciesFiltered.Add(path);

			//find and filter the paths
			
			var countFiltered = 0;
			var countToFilter = dependencies.Count();
			
			foreach (var curDependency in dependencies)
			{
				countFiltered++; 
				
				if ( CheckBlacklist(curDependency) ) { 
					continue; 
				}
				
				dependenciesFiltered.Add(curDependency);

				countFiltered++;
				EditorUtility.DisplayProgressBar($"Filtering Paths", curDependency, (float) countFiltered / countToFilter);
			}

			// Debug.Log(dependenciesFiltered.Count());
			EditorUtility.ClearProgressBar();
		}

		private static void FilterAssets()
		{
			string path = AssetDatabase.GetAssetPath(oldAsset);
			var dependencies = AssetDatabase.GetDependencies(path, true);

			// Debug.Log(path);

			dependenciesFiltered.Add(path);

			//find and filter the paths
			
			var countFiltered = 0;
			var countToFilter = dependencies.Count();
			
			foreach (var curDependency in dependencies)
			{
				countFiltered++; 
				
				if ( CheckBlacklist(curDependency) ) { 
					continue; 
				}
				
				//sometimes GetDependencies returns the original asset as its own dependency :shrug:
				if(curDependency != path){
					dependenciesFiltered.Add(curDependency);
				}

				countFiltered++;
				EditorUtility.DisplayProgressBar($"Filtering Paths", curDependency, (float) countFiltered / countToFilter);
			}

			// Debug.Log(dependenciesFiltered.Count());
			EditorUtility.ClearProgressBar();
		}
		
		private static void CreateNewFolders()
		{
			var countFiltered = 0;
			var countToFilter = dependenciesFiltered.Count();
			foreach (var curDependency in dependenciesFiltered) {
				var dirPath = $"{DEFAULTPATH}/{oldAsset.name}/{Path.GetExtension(curDependency).Replace(".", "")}";
				if(!Directory.Exists( dirPath )) {
					Directory.CreateDirectory(dirPath);
					EditorUtility.DisplayProgressBar($"Creating Folders", dirPath, (float) countFiltered / countToFilter);
				}
			}
			EditorUtility.ClearProgressBar();
		}
		
		private static void DuplicateAssets()
		{
			//duplicate stuff into PATH folder
			var countFiltered = 0;
			var countToFilter = dependenciesFiltered.Count();
			foreach (var curDependency in dependenciesFiltered) {
				
				var dirPath = $"{DEFAULTPATH}/{oldAsset.name}/{Path.GetExtension(curDependency).Replace(".", "")}";
				string assetDupePath = ($"{dirPath}/{Path.GetFileName(curDependency)}");

				//check if new duped file would conflict with something else - dirPath/{Name}{RandomNum}{Extension} (used countFiltered cause lazy)
				if(File.Exists(assetDupePath)) {
					assetDupePath = ($"{dirPath}/{Path.GetFileNameWithoutExtension(curDependency)}{countFiltered}{Path.GetExtension(curDependency)}");
				}
				
				AssetDatabase.CopyAsset(curDependency, assetDupePath);
				duplicatePaths.Add(assetDupePath);
				// Debug.Log(assetDupePath);
				
				countFiltered++;
				EditorUtility.DisplayProgressBar($"Duplicating Assets", curDependency, (float) countFiltered / countToFilter);
			}
			EditorUtility.ClearProgressBar();
		}
		
		private static void FixNewAssets()
		{
			// replace old guids with their new guid counterpart
			
			var countFiltered = 0;
			var countToFilter = duplicatePaths.Count();
			
			foreach (var curPaths in duplicatePaths) 
			{	
				countFiltered++;
				
				// Debug.Log(curPaths);
				if ( !CheckWhitelist(curPaths) ) { 
					continue; 
				}
				
				// load the file in text format
                var contents = File.ReadAllText(curPaths);
				
				EditorUtility.DisplayProgressBar($"Finding Old References", curPaths, (float) countFiltered / countToFilter);
				
				// Debug.Log(contents);
				// replace every old reference with its new one for this item
				for (var i = 0; i < dependenciesFiltered.Count; i++) {
					
					 var oldGUID = AssetDatabase.AssetPathToGUID(dependenciesFiltered[i]);
					 var newGUID = AssetDatabase.AssetPathToGUID(duplicatePaths[i]);
					
					EditorUtility.DisplayProgressBar($"Comparing Old References...", curPaths, (float) i / dependenciesFiltered.Count);

					// if ( contents.Contains(oldGUID) ) {
                        contents = contents.Replace(oldGUID, newGUID);
						EditorUtility.DisplayProgressBar($"Found Reference! Replacing...", curPaths, (float) i / dependenciesFiltered.Count);
					// }
				}
				// write our changes to disk
				File.WriteAllText(curPaths, contents);
			}
			
			EditorUtility.ClearProgressBar();
		}
    }
}