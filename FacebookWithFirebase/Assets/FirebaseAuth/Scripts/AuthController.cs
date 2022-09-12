using Facebook.Unity;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
/*using TwitterKit.ThirdParty.MiniJSON;
using TwitterKit.Unity;*/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthController : MonoBehaviour {
	//Panel
	public GameObject PanelSignIn;
	public GameObject PanelSignUp;
	public GameObject PanelSigned;
	public GameObject PanelProfile;
	public GameObject PanelSettings;
	public GameObject PanelLoading;
	public GameObject PanelPopup;
	public GameObject PanelLog;
	public GameObject PanelPhoneSignIn;
	public GameObject PanelChangePassword;
	public GameObject PanelResetPassword;

	//Phone
	public Text txtPhoneNumber;
	public Text txtCodeNumber;
	private string phoneId;
	public Text txtBtnGetCode;

	//SignIn
	public Text txtSignInEmail;
	public InputField ipfSignInPwd;
	public Toggle toggleRememberMe;

	//SignUp
	public Text txtSignUpEmail;
	public InputField ipfSignUpPwd;
	public Text txtSignUpUsername;

	//Signed
	public Text txtSignedUsername;
	public Image Avata;

	//Popup
	public Text PopupTitle;
	public Text PopupMessage;

	//Log
	public Text txtLog;
	static private int cntLog = 0;
	static private List<string> lstLog = new List<string>();
	private string log;
	private bool isWrite = false;

	//Setting
	public GameObject btnLogout;
	public GameObject grbLogout;

	//Profile
	public Sprite defaultSprite;
	private string imageUrl;
	private string displayName;
	private string email;
	private string id;

	//Change Password
	public InputField ipfNewPassword;
	public InputField ipfConfirmPassword;
	public GameObject BtnChangePassword;

	//Reset password
	public InputField ipfResetPassword;

	/*//Google auth variables
	private string webClientId = "639676282029-ua4na1h3ua2mpenvq2i8ccfsgoip742h.apps.googleusercontent.com";
	private GoogleSignInConfiguration configuration;
*/
	//protected Firebase.Auth.FirebaseAuth auth;
	Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
	Firebase.Auth.FirebaseAuth auth;
	Firebase.Auth.FirebaseUser user;
	PhoneAuthProvider provider;

	private bool isToggleClick = false;
	private	bool isSettingFlag = false;

	private int prevAction = 0;

	void Awake()
	{
		//UI Setting for email sign in
		if (PlayerPrefs.GetInt(Utils.REMEMBER_ME) == 1 && PlayerPrefs.GetInt(Utils.LOGGED) == 1) //Keep auth
		{
			PanelSignIn.SetActive(false);
			PanelSigned.SetActive(true);

			PanelLoading.SetActive(true);
		}

		isSettingFlag = false;

		if (PlayerPrefs.GetInt(Utils.REMEMBER_ME) == 1)
		{
			isToggleClick = true;
			toggleRememberMe.isOn = true;
		}
		else
		{
			isToggleClick = false;
			toggleRememberMe.isOn = false;
		}

		/*//Setup for Google Sign In
		configuration = new GoogleSignInConfiguration
		{
			WebClientId = webClientId,
			RequestIdToken = true
		};*/
	}
	
	// Use this for initialization
	void Start () {
		Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
		{
			dependencyStatus = task.Result;
			if (dependencyStatus == Firebase.DependencyStatus.Available)
			{
				WriteLog("Firebase initializing...");
				InitializeFirebase();
			}
			else
			{
				WriteLog("Could not resolve all Firebase dependencies: " + dependencyStatus, "ERR");
			}
		});
	}

	private void Update()
	{
		if(isWrite)
		{
			isWrite = false;

			txtLog.GetComponent<Text>().text = log;
		}
	}

	void InitializeFirebase()
	{
		auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
		auth.StateChanged += AuthStateChanged;
		AuthStateChanged(this, null);

		//Email sign in auto when open app if remember me toggle checked
		if (PlayerPrefs.GetInt(Utils.REMEMBER_ME) == 1 && PlayerPrefs.GetInt(Utils.LOGGED) == 1) //Keep auth
		{
			SigninAsync(PlayerPrefs.GetString("Email"), PlayerPrefs.GetString("Pwd"));
		}

		//Setup for Facebook Sign In
		if (!FB.IsInitialized)
		{
			// Initialize the Facebook SDK
			FB.Init(InitCallback, OnHideUnity);
		}
		else
		{
			// Already initialized, signal an app activation App Event
			FB.ActivateApp();
		}
	}

	// Track state changes of the auth object.
	void AuthStateChanged(object sender, System.EventArgs eventArgs)
	{
		WriteLog("Auth State Changed");

		//if (auth.CurrentUser != user)
		//{
		//	bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

		//	user = auth.CurrentUser;
		//	if (signedIn)
		//	{
		//		txtSignedUsername.text = user.DisplayName;
		//	}
		//}
	}

	void OnDestroy()
	{
		auth.StateChanged -= AuthStateChanged;
		auth = null;
	}

	#region Email SignIn
	public void SignIn_Click()
	{
		if (string.IsNullOrEmpty(txtSignInEmail.text.Trim()) || string.IsNullOrEmpty(ipfSignInPwd.text.Trim()))
		{
			WriteLog("Email or Pwd is null");

			ShowPopup("Error Message", "Email or Password is null");

			return;
		}

		//Show loading panel
		PanelLoading.SetActive(true);

		//Signin
		SigninAsync(txtSignInEmail.text, ipfSignInPwd.text.Trim());
	}

	public Task SigninAsync(string email, string pwd)
	{
		WriteLog("Email: " + email);
		WriteLog("Password: " + pwd);

		return auth.SignInWithEmailAndPasswordAsync(email, pwd)
		.ContinueWithOnMainThread(HandleEmailSigninResult);
	}

	void HandleEmailSigninResult(Task<Firebase.Auth.FirebaseUser> authTask)
	{
		//Show off loading panel
		PanelLoading.SetActive(false);

		if (authTask.IsCanceled)
		{
			WriteLog("SigninAsync was canceled.");
			ShowPopup("Error Message", "SigninAsync was canceled.\n Please check log for more detail.");
			return;
		}
		if (authTask.IsFaulted)
		{
			WriteLog("SigninAsync encountered an error: " + authTask.Exception);
			ShowPopup("Error Message", "SigninAsync encountered an error.\n Please check log for more detail.");
			return;
		}

		PanelSignIn.SetActive(false);
		PanelSigned.SetActive(true);

		PlayerPrefs.SetInt(Utils.LOGGED, Utils.EM);
		PlayerPrefs.Save();

		WriteLog("Email signin done.");

		if (toggleRememberMe.isOn)
		{
			PlayerPrefs.SetString("Email", txtSignInEmail.text);
			PlayerPrefs.SetString("Pwd", ipfSignInPwd.text);
			PlayerPrefs.Save();
		}

		user = auth.CurrentUser;

		if (user != null)
		{
			txtSignedUsername.text = String.Format("Welcome {0}!",user.DisplayName);

			WriteLog("User:" + user.DisplayName);
			WriteLog("PhotoUrl:" + user.PhotoUrl.ToString());

			if (!string.IsNullOrEmpty(user.PhotoUrl.ToString()))
			{
				StartCoroutine(LoadImage(user.PhotoUrl.ToString()));
			}
		}
		else
		{
			WriteLog("User is null.");
		}
	}
	#endregion

	#region Email SignUp
	public void SignUp_Click()
	{
		if (string.IsNullOrEmpty(txtSignUpEmail.text.Trim()) || string.IsNullOrEmpty(ipfSignUpPwd.text.Trim()) || string.IsNullOrEmpty(txtSignUpUsername.text.Trim()))
		{
			WriteLog("Email or Pwd or Username is null");

			ShowPopup("Error Message", "Email or Pwd or Username is null");

			return;
		}

		PanelLoading.SetActive(true);

		CreateUserAsync(txtSignUpEmail.text, ipfSignUpPwd.text.Trim(), txtSignUpUsername.text);
	}

	public void CreateUserAsync(string email, string pwd, string username)
	{
		WriteLog("Create user:");
		WriteLog("Email: " + email);
		WriteLog("Password: " + pwd);

		try
		{
			auth.CreateUserWithEmailAndPasswordAsync(email, pwd).ContinueWithOnMainThread(task =>
			{
				if (task.IsCanceled)
				{
					WriteLog("CreateUserWithEmailAndPasswordAsync was canceled.");
					PanelLoading.SetActive(false);
					return;
				}
				if (task.IsFaulted)
				{
					WriteLog("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
					PanelLoading.SetActive(false);
					return;
				}

				WriteLog("Firebase user created successfully");

				UpdateUserProfileAsync(username);

				PanelLoading.SetActive(false);
				PanelSignIn.SetActive(false);
				PanelSigned.SetActive(true);
			});
		}
		catch (Exception e)
		{
			PanelLoading.SetActive(false);

			WriteLog("Exception:" + e.Message);
		}
		
	}

	void UpdateUserProfileAsync(string username)
	{
		if (auth.CurrentUser == null)
		{
			WriteLog("Not signed in, unable to update user profile");
			return;
		}

		WriteLog("Updating user profile");

		user = auth.CurrentUser;
		if (user != null)
		{
			Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
			{
				DisplayName = username,
				PhotoUrl = user.PhotoUrl,
			};

			user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
				if (task.IsCanceled)
				{
					WriteLog("UpdateUserProfileAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					WriteLog("UpdateUserProfileAsync encountered an error: " + task.Exception);
					return;
				}
				if (task.IsCompleted)
				{
					WriteLog("User profile updated completed");
				}


				PlayerPrefs.SetInt(Utils.LOGGED, Utils.EM);
				PlayerPrefs.Save();

				user = auth.CurrentUser;
				txtSignedUsername.text = String.Format("Welcome {0}!", user.DisplayName);

				//WriteLog("PhotoUrl:" + user.PhotoUrl.ToString());
				//if (!string.IsNullOrEmpty(user.PhotoUrl.ToString()))
				//{
				//	StartCoroutine(LoadImage(user.PhotoUrl.ToString()));
				//}
			});
		}
	}

	//https://firebase.google.com/docs/auth/unity/manage-users#send_a_password_reset_email
	public void SendPasswordResetEmail()
	{
		if(string.IsNullOrEmpty(ipfResetPassword.text.Trim()))
		{
			ShowPopup("Information", "Email can't be empty");
			return;
		}

		auth.SendPasswordResetEmailAsync(ipfResetPassword.text.Trim()).ContinueWithOnMainThread((authTask) =>
		{
			if (authTask.IsCanceled)
			{
				WriteLog("SendPasswordResetEmailAsync was canceled.");
				return;
			}
			if (authTask.IsFaulted)
			{
				WriteLog("SendPasswordResetEmailAsync encountered an error: " + authTask.Exception);
				return;
			}

			WriteLog("Password reset email sent successfully.");
			prevAction = Utils.RESET_PASS;
			ShowPopup("Information", "Password reset email sent successfully.");
		});

		//Firebase.Auth.FirebaseUser user = auth.CurrentUser;

		//if (user != null)
		//{
		//	auth.SendPasswordResetEmailAsync(txtResetPassword.text.Trim()).ContinueWithOnMainThread(task =>
		//	{
		//		if (task.IsCanceled)
		//		{
		//			WriteLog("SendPasswordResetEmailAsync was canceled.");
		//			return;
		//		}
		//		if (task.IsFaulted)
		//		{
		//			WriteLog("SendPasswordResetEmailAsync encountered an error: " + task.Exception);
		//			return;
		//		}

		//		WriteLog("Password reset email sent successfully.");
		//		prevAction = Utils.RESET_PASS;
		//		ShowPopup("Information", "Password reset email sent successfully.");
		//	});
		//}
	}

	//Send a user a verification email
	void SendVerificationEmail()
	{
		Firebase.Auth.FirebaseUser user = auth.CurrentUser;
		if (user != null)
		{
			user.SendEmailVerificationAsync().ContinueWithOnMainThread(task => {
				if (task.IsCanceled)
				{
					WriteLog("SendEmailVerificationAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					WriteLog("SendEmailVerificationAsync encountered an error: " + task.Exception);
					return;
				}

				WriteLog("Email sent successfully.");
			});
		}
	}

	//Re-authenticate a user
	void ReAuthenticate(string email, string password)
	{
		Firebase.Auth.FirebaseUser user = auth.CurrentUser;

		// Get auth credentials from the user for re-authentication. The example below shows
		// email and password credentials but there are multiple possible providers,
		// such as GoogleAuthProvider or FacebookAuthProvider.
		Firebase.Auth.Credential credential =
			Firebase.Auth.EmailAuthProvider.GetCredential(email, password);

		if (user != null)
		{
			user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task => {
				if (task.IsCanceled)
				{
					WriteLog("ReauthenticateAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					WriteLog("ReauthenticateAsync encountered an error: " + task.Exception);
					return;
				}

				WriteLog("User reauthenticated successfully.");
			});
		}
	}

	//Set a user's password
	public void SetPassword()
	{
		if(!ipfNewPassword.text.Equals(ipfConfirmPassword.text))
		{
			ShowPopup("Password Error", "Passwords do not match");
			return;
		}

		Firebase.Auth.FirebaseUser user = auth.CurrentUser;

		if (user != null)
		{
			user.UpdatePasswordAsync(ipfNewPassword.text).ContinueWithOnMainThread(task => {
				if (task.IsCanceled)
				{
					WriteLog("UpdatePasswordAsync was canceled.");
					return;
				}
				if (task.IsFaulted)
				{
					WriteLog("UpdatePasswordAsync encountered an error: " + task.Exception);
					return;
				}

				WriteLog("Password updated successfully.");

				prevAction = Utils.CHANGE_PASS;

				ShowPopup("Information", "Password updated successfully.");
			});
		}
	}
	#endregion

	#region UI Event
	public void GotoSignUp()
	{
		PanelSignIn.SetActive(false);
		PanelSignUp.SetActive(true);
	}

	//Goto SignIn panel from SignUp or after logout from Panel Signed
	public void GotoSignIn()
	{
		PanelSignIn.SetActive(true);
		PanelSignUp.SetActive(false);
		PanelSigned.SetActive(false);
		PanelPhoneSignIn.SetActive(false);
	}

	public void ToggleRememberMe_Click()
	{
		if (!isToggleClick)
		{
			isToggleClick = true;
			return;
		}

		if (toggleRememberMe.isOn)
		{
			PlayerPrefs.SetInt(Utils.REMEMBER_ME, 1);
		}
		else
		{
			PlayerPrefs.SetInt(Utils.REMEMBER_ME, 0);
		}

		PlayerPrefs.Save();
	}

	public void Setting_Click()
	{
		if(PlayerPrefs.GetInt(Utils.LOGGED) != 0)
		{
			btnLogout.SetActive(true);
			grbLogout.SetActive(true);

			if(PlayerPrefs.GetInt(Utils.LOGGED) == 1)
			{
				BtnChangePassword.SetActive(true);
			}
			else
			{
				BtnChangePassword.SetActive(false);
			}
		}
		else
		{
			btnLogout.SetActive(false);
			grbLogout.SetActive(false);
		}

		if (!isSettingFlag)
		{
			isSettingFlag = true;
			PanelProfile.SetActive(false);
			PanelSettings.SetActive(true);
		}
		else
		{
			isSettingFlag = false;
			PanelProfile.SetActive(true);
			PanelSettings.SetActive(false);
		}
	}

	public void ChangePassword_Click()
	{
		PanelChangePassword.SetActive(true);
	}

	public void CancelChangePassword_Click()
	{
		PanelChangePassword.SetActive(false);
	}

	public void ForgotPassword_Click()
	{
		PanelResetPassword.SetActive(true);
	}

	public void CancelResetPassword_Click()
	{
		PanelResetPassword.SetActive(false);
	}
	#endregion

	#region Firebase Logout
	/// <summary>
	/// Logout Firebase + logout thirt party
	/// </summary>
	public void Logout_Click()
	{
		auth.SignOut();

		if (PlayerPrefs.GetInt(Utils.LOGGED) == Utils.FB)
		{
			FB.LogOut();
		}
		/*else if (PlayerPrefs.GetInt(Utils.LOGGED) == Utils.GG)
		{
			GoogleSignIn.DefaultInstance.SignOut();
		}
		else if (PlayerPrefs.GetInt(Utils.LOGGED) == Utils.TW)
		{
			Twitter.LogOut();
		}*/


		PlayerPrefs.SetInt(Utils.LOGGED, Utils.NONE);
		PlayerPrefs.Save();

		PanelProfile.SetActive(true);
		PanelSettings.SetActive(false);
		PanelPhoneSignIn.SetActive(false);
		PanelSigned.SetActive(false);
		PanelSignUp.SetActive(false);
		PanelSignIn.SetActive(true);

		txtSignedUsername.text = "User Name";
		Avata.sprite = defaultSprite;
	}
	#endregion

	/*#region Google SignIn
	/// <summary>
	/// Google Sign-In Click
	/// </summary>
	public void GoogleSignIn_Click()
	{
		//Sign-In with Google as first to get token for Firebase Auth
		OnGoogleSignIn();
	}

	void OnGoogleSignIn()
	{
		GoogleSignIn.Configuration = configuration;
		GoogleSignIn.Configuration.UseGameSignIn = false;
		GoogleSignIn.Configuration.RequestIdToken = true;
		GoogleSignIn.Configuration.RequestEmail = true;

		GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(
		  OnGoogleAuthenticationFinished);
	}

	//Handle when Google Sign In successfully
	void OnGoogleAuthenticationFinished(Task<GoogleSignInUser> task)
	{
		if (task.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator =
					task.Exception.InnerExceptions.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					GoogleSignIn.SignInException error =
							(GoogleSignIn.SignInException)enumerator.Current;
					WriteLog("Got Error: " + error.Status + " " + error.Message, "ERR");
				}
				else
				{
					WriteLog("Got Unexpected Exception?!?" + task.Exception, "ERR");
				}
			}
		}
		else if (task.IsCanceled)
		{
			WriteLog("Canceled");
		}
		else
		{
			WriteLog("Google Sign-In successed");

			WriteLog("IdToken: " +task.Result.IdToken);
			WriteLog("ImageUrl: " + task.Result.ImageUrl.AbsoluteUrlOrEmptyString());

			//Set imageUrl
			imageUrl = task.Result.ImageUrl.AbsoluteUrlOrEmptyString();

			//Start Firebase Auth
			Firebase.Auth.Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
			auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(t =>
			{
				if (t.IsCanceled)
				{
					WriteLog("SignInWithCredentialAsync was canceled.");
					return;
				}
				if (t.IsFaulted)
				{
					WriteLog("SignInWithCredentialAsync encountered an error: " + t.Exception);
					return;
				}

				PlayerPrefs.SetInt(Utils.LOGGED, Utils.GG);
				PlayerPrefs.Save();

				user = auth.CurrentUser;
				txtSignedUsername.text = String.Format("Welcome {0}!", user.DisplayName);

				PanelSignIn.SetActive(false);
				PanelSignUp.SetActive(false);
				PanelSigned.SetActive(true);

				WriteLog("PhotoUrl:" + user.PhotoUrl.AbsoluteUrlOrEmptyString());

				StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.AbsoluteUrlOrEmptyString())));
			});
		}
	}
	#endregion*/

	#region Facebook SignIn
	public void FacebookSignIn_Click()
	{
		OnFacebookSignIn();
	}

	void OnFacebookSignIn()
	{
		FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, OnFacebookAuthenticationFinished);
	}

	void OnFacebookAuthenticationFinished(IResult result)
	{
		if (FB.IsLoggedIn)
		{
			WriteLog("FB Logged In.");

			PlayerPrefs.SetInt(Utils.LOGGED, Utils.FB);
			PlayerPrefs.Save();

			PanelSignIn.SetActive(false);
			PanelSigned.SetActive(true);

			WriteLog("Start Firebase Auth");
			WriteLog("IdToken: " + AccessToken.CurrentAccessToken.TokenString);
			WriteLog("ImageUrl: " + String.Format("https://graph.facebook.com/{0}/picture?type=large&width=100&height=100", AccessToken.CurrentAccessToken.UserId));

			//Set imageUrl
			imageUrl = String.Format("https://graph.facebook.com/{0}/picture?type=large&width=100&height=100", AccessToken.CurrentAccessToken.UserId);

			//Firebase Auth
			FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
		}
		else
		{
			WriteLog("User cancelled login");
		}
	}

	private void FacebookAuth(string accessToken)
	{
		Firebase.Auth.Credential credential =
		Firebase.Auth.FacebookAuthProvider.GetCredential(accessToken);

		auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
		{
			if (task.IsCanceled)
			{
				WriteLog("SignInWithCredentialAsync was canceled.", "ERR");
				return;
			}
			if (task.IsFaulted)
			{
				WriteLog("SignInWithCredentialAsync encountered an error: " + task.Exception, "ERR");
				return;
			}

			user = auth.CurrentUser;

			WriteLog(String.Format("User signed in successfully: {0} ({1})",
				user.DisplayName, user.UserId));

			txtSignedUsername.text = user.DisplayName;

			WriteLog("PhotoUrl:" + user.PhotoUrl.AbsoluteUrlOrEmptyString());

			////It might an error, but with url get from firebase, now we can't get a profile image. So we succgess a solution,
			////we will use profile url from facebook that we get after login.
			//StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.AbsoluteUrlOrEmptyString())));
			StartCoroutine(LoadImage(imageUrl));
		});
	}

	private void InitCallback()
	{
		WriteLog("FB Init done.");

		if (FB.IsLoggedIn)
		{
			WriteLog(String.Format("FB Logged In. TokenString:" + AccessToken.CurrentAccessToken.TokenString));
			WriteLog(AccessToken.CurrentAccessToken.ToString());

			if (PlayerPrefs.GetInt(Utils.LOGGED) == Utils.FB)
			{
				PanelSignIn.SetActive(false);
				PanelSigned.SetActive(true);

				FacebookAuth(AccessToken.CurrentAccessToken.TokenString);
			}
			else
			{
				WriteLog("User not yet loged FB or loged out");
			}
		}
		else
		{
			WriteLog("User cancelled login");
		}
	}

	private void OnHideUnity(bool isGameShown)
	{
		if (!isGameShown)
		{
			Time.timeScale = 0;
		}
		else
		{
			Time.timeScale = 1;
		}
	}
	#endregion

	/*#region Twitter SignIn
	public void TwitterSignIn_Click()
	{
		WriteLog("Start Twitter Sign In");

		Twitter.Init();

		Twitter.LogIn(TwitterLoginComplete, (ApiError error) => {
			WriteLog(error.message);
		});
	}

	public void TwitterLoginComplete(TwitterSession session)
	{
		if (session == null || session.ToString().Length == 0)
		{
			WriteLog("Session null or blank.");
			return;
		}

		WriteLog("Twitter loggin successed.");

		PlayerPrefs.SetInt(Utils.LOGGED, Utils.TW);
		PlayerPrefs.Save();

		PanelSignIn.SetActive(false);
		PanelSigned.SetActive(true);

		WriteLog("Start Firebase Auth");
		WriteLog("Token: " + session.authToken.token);
		WriteLog("Secret: " + session.authToken.secret);

		//------------------Twitter ImageUrl format------------------------
		//https://twitter.com/[screen_name]/profile_image?size=mini
		//https://twitter.com/[screen_name]/profile_image?size=normal
		//https://twitter.com/[screen_name]/profile_image?size=bigger
		//https://twitter.com/[screen_name]/profile_image?size=original
		//-----------------------------------------------------------------
		WriteLog("ImageUrl: " + String.Format("https://twitter.com/{0}/profile_image?size=normal", session.userName));

		//Set imageUrl
		imageUrl = String.Format("https://twitter.com/{0}/profile_image?size=normal", session.userName);

		Firebase.Auth.Credential credential =
		Firebase.Auth.TwitterAuthProvider.GetCredential(session.authToken.token, session.authToken.secret);
		auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task => {
			if (task.IsCanceled)
			{
				WriteLog("SignInWithCredentialAsync was canceled.");
				return;
			}
			if (task.IsFaulted)
			{
				WriteLog(("SignInWithCredentialAsync encountered an error: " + task.Exception));
				return;
			}

			Firebase.Auth.FirebaseUser newUser = task.Result;
			WriteLog(String.Format("User signed in successfully: {0} ({1})",
				newUser.DisplayName, newUser.UserId));

			txtSignedUsername.text = newUser.DisplayName;

			WriteLog("PhotoUrl:" + newUser.PhotoUrl.AbsoluteUrlOrEmptyString());

			StartCoroutine(LoadImage(CheckImageUrl(newUser.PhotoUrl.AbsoluteUrlOrEmptyString())));
		});
	}
	#endregion*/

	/*#region Phone SignIn
	public void PhoneSignIn_Click()
	{
		PanelPhoneSignIn.SetActive(true);
	}

	public void PhoneGetVerificationCode_Click()
	{
		provider = PhoneAuthProvider.GetInstance(auth);
		WriteLog("Verify Phone Number" + txtPhoneNumber.text);

		txtBtnGetCode.text = "Resend Verification Code";

		provider.VerifyPhoneNumber(txtPhoneNumber.text, 60, null, //phoneAuthTimeoutMs = 60
		verificationCompleted: (credential) => {
			// Auto-sms-retrieval or instant validation has succeeded (Android only).
			// There is no need to input the verification code.
			// `credential` can be used instead of calling GetCredential().
			WriteLog("Auto-sms-retrieval or instant validation has succeeded (Android only).");

			OnVerifyCode(credential);
		},
		verificationFailed: (error) => {
			// The verification code was not sent.
			// `error` contains a human readable explanation of the problem.
			WriteLog("The verification code was not sent.");
			WriteLog(error.ToString());
		},
		codeSent: (id, token) => {
			// Verification code was successfully sent via SMS.
			// `id` contains the verification id that will need to passed in with
			// the code from the user when calling GetCredential().
			// `token` can be used if the user requests the code be sent again, to
			// tie the two requests together.
			WriteLog("Verification code was successfully sent via SMS.");
			WriteLog("Id: " + id);
			WriteLog("Token: " + token);
			phoneId = id;

		},
		codeAutoRetrievalTimeOut: (id) => {
			// Called when the auto-sms-retrieval has timed out, based on the given
			// timeout parameter.
			// `id` contains the verification id of the request that timed out.
			WriteLog("Auto-sms-retrieval has timed out");
			WriteLog("Id: " + id);

		});
	}

	public void PhoneVerifyCode_Click()
	{
		Credential credential = provider.GetCredential(phoneId, txtCodeNumber.text);

		OnVerifyCode(credential);
	}

	private void OnVerifyCode(Credential credential)
	{
		auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task => {
			if (task.IsFaulted)
			{
				WriteLog("SignInWithCredentialAsync encountered an error: " + task.Exception);
				return;
			}

			WriteLog("Phone Sign In successed.");

			PlayerPrefs.SetInt(Utils.LOGGED, Utils.PH);
			PlayerPrefs.Save();

			PanelPhoneSignIn.SetActive(false);
			PanelSigned.SetActive(true);


			FirebaseUser newUser = task.Result;

			WriteLog(String.Format("User signed in successfully: {0} (ProviderID = {1})",
				newUser.PhoneNumber, newUser.ProviderId));

			txtSignedUsername.text = newUser.PhoneNumber;
		});
	}
	#endregion*/

	#region Popup
	public void OK_Click()
	{
		PanelPopup.SetActive(false);

		if(prevAction == Utils.CHANGE_PASS)
		{
			prevAction = 0;

			PanelChangePassword.SetActive(false);

			Logout_Click();
		}

		if (prevAction == Utils.RESET_PASS)
		{
			prevAction = 0;

			PanelResetPassword.SetActive(false);
		}
	}

	public void Log_Click()
	{
		PanelPopup.SetActive(false);
		PanelLog.SetActive(true);
	}

	private void ShowPopup(string title,string mes)
	{
		PopupTitle.text = title;
		PopupMessage.text = mes;

		PanelPopup.SetActive(true);
	}
	#endregion

	#region Log
	public void CloseLog_Click()
	{
		PanelLog.SetActive(false);
	}

	public void WriteLog(string mes, string logType = "INF")
	{
		StackFrame frame = new StackFrame(1);
		cntLog++;

		lstLog.Add(cntLog.ToString() + ". [" + frame.GetMethod().Name + "] " + mes + "\n");

		string txt = "";
		foreach (string s in lstLog)
		{
			txt += s;
		}

		log = txt;
		isWrite = true;
	}
	#endregion

	/// <summary>
	/// Return imageUrl from Firebase, if it null, return imageUrl from thirt party(google, facebook,...)
	/// </summary>
	/// <param name="url">imageUrl from Firebase</param>
	/// <returns>imageUrl</returns>
	private string CheckImageUrl(string url)
	{
		if(!string.IsNullOrEmpty(url))
		{
			return url;
		}

		WriteLog("Use imageUrl not from Firebase");
		return imageUrl;
	}

	IEnumerator LoadImage(string imageUri)
	{
		WriteLog("Loading Image");

		WWW www = new WWW(imageUri);
		yield return www;

		WriteLog("Get Image success, width = " + www.texture.width + ", height = " + www.texture.height);
		Avata.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
	}
}
