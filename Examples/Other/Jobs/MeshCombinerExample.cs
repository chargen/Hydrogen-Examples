﻿using UnityEngine;
using System.Collections;
using System.Threading;
using System.Collections.Generic;

public class MeshCombinerExample : MonoBehaviour
{
		Hydrogen.Threading.Jobs.MeshCombiner _meshCombiner;
		public Transform TargetMeshes;

		/// <summary>
		/// Process meshFilters in Unity's main thread, as we are required to by Unity. At least we've rigged it as a 
		/// coroutine! Right? OK I know I really wish we could have used mesh data in a thread but properties die as well.
		/// </summary>
		/// <returns>IEnumartor aka Coroutine</returns>
		/// <remarks>
		/// For the sake of the demo we are going to need to roll over the "Target" to find all the 
		/// meshes that we need to look at, but in theory you could do this without having to load the
		/// object by simply having raw mesh data, or any other means of accessing it.
		/// </remarks>
		public IEnumerator PreProcess ()
		{
				// Create a new MeshCombiner (we dont want any old data kicking around)
				_meshCombiner = new Hydrogen.Threading.Jobs.MeshCombiner ();

				// Yes We Hate This - There Are Better Implementations
				MeshFilter[] meshFilters = TargetMeshes.GetComponentsInChildren<MeshFilter> ();
				yield return new WaitForEndOfFrame ();

				// have static function taht determines if its on a different material ... loops one material at a time
				// Our data array
				var meshes = new Hydrogen.Threading.Jobs.MeshCombiner.MeshDescription[meshFilters.Length];

				// Loop through all of our mesh filters and add them to the combiner to be combined.
				for (int x = 0; x < meshFilters.Length; x++) {

						if (meshFilters [x].gameObject.activeSelf) {

								_meshCombiner.AddMesh (meshFilters [x].mesh, meshFilters [x].transform);
						}
						meshFilters [x].gameObject.SetActive (false);
						yield return new WaitForEndOfFrame ();
				}

				// Start the threaded love
				_meshCombiner.Combine (System.Threading.ThreadPriority.Normal, null, ThreadCallback);
		}

		/// <summary>
		/// Process the MeshDescription data sent back from the Combiner and make it appear!
		/// </summary>
		/// <param name="hash">Instance Hash.</param>
		/// <param name="meshDescriptions">MeshDescriptions.</param>
		/// <param name="materials">Materials.</param>
		public IEnumerator PostProcess (int hash, 
		                                Hydrogen.Threading.Jobs.MeshCombiner.MeshDescription[] meshDescriptions, 
		                                Material[] materials)
		{
				// Create our dummy list of meshes
				var meshes = new List<Mesh> ();


				for (int x = 0; x <= meshDescriptions.Length; x++) {
						var newMesh = Hydrogen.Threading.Jobs.MeshCombiner.CreateMesh (meshDescriptions [x]);

						// Add to list
						meshes.Add (newMesh);

						// Fake Unity Threading
						yield return new WaitForEndOfFrame ();
				}

				var go = new GameObject ("Combined Meshes");
				go.transform.position = TargetMeshes.position;
				go.transform.rotation = TargetMeshes.rotation;

				// Show Them
				for (int y = 0; y <= meshes.Count; y++) {
						var meshObject = new GameObject ();
						meshObject.name = hash + "_" + meshes [y].name;
						meshObject.transform.parent = go.transform;
						meshObject.transform.position = Vector3.zero;
						meshObject.transform.rotation = Quaternion.identity;
						meshObject.AddComponent<MeshFilter> ().mesh = meshes [y];
						//meshObject.AddComponent<MeshRenderer> ().material = defaultMaterial;
				}

				// Destroy the mesh combiner (forcing data wipe);
				yield return new WaitForEndOfFrame ();
				_meshCombiner = null;
		}

		/// <summary>
		/// This function is called in the example after the MeshCombiner has processed the meshes, it starts a Coroutine 
		/// to create the actual meshes based on the flat data. This is the most optimal way to do this sadly as we cannot
		/// create or touch Unity based meshes outside of the main thread.
		/// </summary>
		/// <param name="hash">Instance Hash.</param>
		/// <param name="meshDescriptions">MeshDescriptions.</param>
		/// <param name="materials">Materials.</param>
		public void ThreadCallback (int hash, Hydrogen.Threading.Jobs.MeshCombiner.MeshDescription[] meshDescriptions, Material[] materials)
		{
				// This is just a dirty way to see if we can squeeze jsut a bit more performance out of Unity when 
				// making all of the meshes for us (instead of it being done in one call, we use a coroutine with a loop.
				StartCoroutine (PostProcess (hash, meshDescriptions, materials));
		}

		/// <summary>
		/// Unity's LateUpdate Event
		/// </summary>
		void LateUpdate ()
		{
				// If we have a MeshCombiner lets run the Check()
				if (_meshCombiner != null) {
						// Funny thing about this method of doing this; lots of Thread based solutions in Unity have an
						// elaborate manager that does this for you ... just saying.
						_meshCombiner.Check ();
				}
		}

		/// <summary>
		/// Unity's OnGUI Event
		/// </summary>
		void OnGUI ()
		{
				// A clever little trick to only show the button when nothing is going on.
				// Obviously, in a real world setting this would probably not look like this.
				if (_meshCombiner == null) {

						if (GUI.Button (new Rect (5, 5, 200, 35), "Rock & || Possibly Roll!")) {
								StartCoroutine (PreProcess ());
						}

				}

				// Some debug output, helpful for determining if we block the main thread much.
				GUI.color = Color.black;
				GUI.Label (new Rect (210, 12, 100, 35), Time.time.ToString ());
		}
}