using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Unity.Editor;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class TestScript : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		SetupGPGS();
		SetupFirebase();
		
		Debug.Log("Attempting to log in using GPGS");
		PlayGamesPlatform.Instance.Authenticate(OnAuthenticateCompleted);

	}

	private void SetupFirebase()
	{
		Debug.Log("Setting up Firebase");

		FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://project-swing-75126199.firebaseio.com/");
	}

	private void SetupGPGS()
	{
		Debug.Log("Setting up GPGS");

		PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
			.RequestIdToken()
			.RequestEmail()
			.Build();

		PlayGamesPlatform.InitializeInstance(config);
		PlayGamesPlatform.DebugLogEnabled = false;

		PlayGamesPlatform.Activate();

		Debug.Log("PlayGames Platform activated.");
	}

	private void OnAuthenticateCompleted(bool success)
	{
		if (success)
		{
			StartCoroutine(WaitForTokenAndAuthenticateFirebase());
		}
		else
		{
			Debug.LogError("Failed to authenticate using Google Play");
		}
	}

	private IEnumerator WaitForTokenAndAuthenticateFirebase()
	{
		Debug.Log("Waiting for Google Token");
		while (PlayGamesPlatform.Instance.GetIdToken() == null)
		{
			yield return new WaitForSeconds(1);
		}

		var auth = FirebaseAuth.DefaultInstance;
		var token = PlayGamesPlatform.Instance.GetIdToken();
		var credential = GoogleAuthProvider.GetCredential(token, null);
		auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
			{
				if (task.IsCanceled)
				{
					Debug.LogError("SignInWithCredentialAsync was canceled.");
					return;
				}
				else if (task.IsFaulted)
				{
					Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
					return;
				}
				else if (task.IsCompleted)
				{
					var user = task.Result;
					Debug.LogFormat("User signed in to Firebase successfully: {0} ({1})",
						user.DisplayName, user.UserId);

					LoadUserData(user.UserId);

					Debug.Log("Attempting to load user data");
				}
			});
	}

	private IEnumerator WaitToLoadUserData()
	{
		yield return new WaitForSeconds(5);
	}

	private void LoadUserData(string userId)
	{
		FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(userId).GetValueAsync().ContinueWith(
			task =>
			{
				if (task.IsCompleted)
				{
					Debug.Log("Successfully read user data " + task.Result.GetRawJsonValue());
				}
				else
				{
					Debug.LogError("Failed to read user data: " + task.Exception);
				}
			});
	}
}
