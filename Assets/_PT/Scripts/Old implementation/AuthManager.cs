//
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
//
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Security.Cryptography;
// using System.Threading.Tasks;
// using TwitchSDK;
// using TwitchSDK.Interop;
// using UnityEngine;
// using System.Text.RegularExpressions;
// using UnityEngine.Windows;
// using System.Linq;
// using UnityEngine.Events;
// namespace TwitchEventsubUnity
// {
//     public class AuthManager : MonoBehaviour
//     {
//         public string twitchClientId;
//
//         public TwitchSDKSettings twitchSettings;
//
//         private void OnValidate()
//         {
//             if(twitchSettings!=null)
//             {
//                 twitchSettings.ClientId = twitchClientId;
//             }
//         }
//
//         #region PUBLIC_API
//         public static bool isConnected { get; set; }
//         public static AuthManager singleton { get; private set; }
//
//         public TwitchChatReader chatReader { get; private set; }
//
//         public TwitchUser autheticatedTwitchUser { get; set; }
//         #endregion
//         #region PUBLIC_VARIABLES_FOR_INSPECTOR
//         public TwitchOAuthScopeEnum[] scopes = new TwitchOAuthScopeEnum[] {
//             TwitchOAuthScopeEnum.ChannelManagePredictions,
//             TwitchOAuthScopeEnum.ChannelManageRedemptions,
//             TwitchOAuthScopeEnum.ChatRead,
//
//         };
//
//         public bool connectAtStart;
//         #endregion
//
//         #region PRIVATE_VARIABLES
//         bool m_ConnectedLastFrame;
//
//         List<WebRequestResult> m_WebRequestResults = new List<WebRequestResult>();
//         Coroutine m_AuthInfoTaskCoroutine = null;
//         GameTask<AuthenticationInfo> m_AuthInfoTask;
//         GameTask<AuthState> m_CurAuthState;
//
//         string m_DataPath;
//
//         const float REFRESH_SECOND = 1;
//         #endregion
//         #region UNITY_EVENTS
//         private void Awake()
//         {
//             m_ConnectedLastFrame = false;
//             singleton = this;
//             m_DataPath = Application.persistentDataPath;
//         }
//
//         void Start()
//         {
//             isConnected = false;
//             if (connectAtStart)
//                 Connect();
//         }
//
//         private void OnDestroy()
//         {
//             if (chatReader != null)
//                 chatReader.Dispose();
//         }
//
//         private void Update()
//         {
//             if (isConnected)
//                 return;
//
//             if (m_WebRequestResults.Count == 0)
//                 return;
//
//             var res = m_WebRequestResults[0];
//             m_WebRequestResults.RemoveAt(0);
//
//             try
//             {
//                 var data = JObject.Parse(res.ResponseBody);
//                 var error = data["error"];
//                 if (error != null)
//                 {
//                     m_AuthInfoTask = null;
//                     DeleteToken();
//                     Reconnect();
//                     //restart auth process
//                     return;
//                 }
//             }
//             catch
//             {
//
//             }
//
//
//             var rootObject = JsonConvert.DeserializeObject<TwitchRootObject>(res.ResponseBody);
//             if (rootObject != null && rootObject.Data != null)
//             {
//                 var user = rootObject.Data[0];
//
//                 OnConnected(user);
//
//             }
//
//
//
//         }
//
//
//
//         #endregion
//         #region PRIVATE_API
//
//         public UnityEvent<TwitchUser> onConnect;
//
//         private void OnConnected(TwitchUser user)
//         {
//             isConnected = true;
//             autheticatedTwitchUser = user;
//
//             onConnect?.Invoke(autheticatedTwitchUser);
//
//             var modules = GetComponentsInChildren<TwitchChatModule>();
//             for (int i = 0; i < modules.Length; i++)
//             {
//                 if (modules[i].authenticated)
//                     continue;
//
//                 modules[i].authenticated = true;
//                 if (modules[i].enabled)
//                     modules[i].Authenticate(GetAccessToken(), user.DisplayName.ToLower());
//             }
//             //            if (chatReader == null)
//             //              chatReader = new TwitchChatReader(GetAccessToken(), user.DisplayName.ToLower());
//         }
//
//         /// <summary>
//         /// Thread for the connection to twitch API
//         /// </summary>
//         /// <returns></returns>
//         private void ConnectionToApplicationAPI()
//         {
//             if (m_CurAuthState == null)
//                 return;
//             switch (m_CurAuthState.MaybeResult.Status)
//             {
//                 case AuthStatus.LoggedIn:
//                     break;
//
//                 case AuthStatus.WaitingForCode:
//                     {
//                         m_AuthInfoTask = null;
//                         Reconnect();
//
//                         break;
//                     }
//                 case AuthStatus.LoggedOut:
//                 case AuthStatus.Loading:
//                     //Remake a connection
//                     Reconnect();
//                     break;
//             }
//         }
//
//
//         double ReadActiveExpireTime()
//         {
//             try
//             {
//                 return (double)PlayerPrefs.GetFloat("token-expire-time-cache", -1);
//
//             }
//             catch
//             {
//                 return -1;
//             }
//         }
//
//         void Reconnect()
//         {
//             if (m_AuthInfoTask == null)
//                 m_AuthInfoTask = Twitch.API.GetAuthenticationInfo(scopes.Select(x => x.GetScope()).ToArray());
//
//             Task.Factory.StartNew(async () => await m_AuthInfoTask);
//
//             if (m_AuthInfoTaskCoroutine == null)
//                 m_AuthInfoTaskCoroutine = StartCoroutine(CheckAuthInfoTask());
//
//         }
//
//
//         /// <summary>
//         /// Thread to open a web page for logging the user
//         /// </summary>
//         /// <returns></returns>
//         IEnumerator CheckAuthInfoTask()
//         {
//             while (true)
//             {
//                 if (m_AuthInfoTask == null)
//                 {
//                     yield return new WaitForSeconds(REFRESH_SECOND);
//                     continue;
//                 }
//
//                 switch (m_AuthInfoTask.Task.Status)
//                 {
//                     case TaskStatus.Created:
//                     case TaskStatus.WaitingForActivation:
//                     case TaskStatus.WaitingToRun:
//                     case TaskStatus.Running:
//                     case TaskStatus.WaitingForChildrenToComplete:
//                         break;
//                     case TaskStatus.RanToCompletion:
//                         try
//                         {
//                             if (m_AuthInfoTask.Task.Result == null)
//                             {
//                                 //                            DeleteToken();
//                                 //     AuthInfoTask = Twitch.API.GetAuthenticationInfo(SCOPES);
//
//                                 break;
//                             }
//                             var foo = m_AuthInfoTask.Task.Result.Uri;
//                             Debug.Log(foo);
//                             Application.OpenURL(m_AuthInfoTask.Task.Result.Uri);
//
//                             m_AuthInfoTaskCoroutine = null;
//                             yield break;
//
//                         }
//                         catch (System.Exception ex)
//                         {
//                             Debug.LogException(ex);
//                         }
//                         break;
//                     case TaskStatus.Canceled:
//                         {
//                             m_AuthInfoTaskCoroutine = null;
//                             yield break;
//                         }
//                     case TaskStatus.Faulted:
//                         {
//                             m_AuthInfoTaskCoroutine = null;
//                             yield break;
//                         }
//                 }
//
//                 yield return new WaitForSeconds(REFRESH_SECOND);
//             }
//         }
//
//
//
//
//         static double GetTotalSeconds(DateTime time)
//         {
//             DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
//             double totalSeconds = (time.ToUniversalTime() - epoch).TotalSeconds;
//             return totalSeconds;
//         }
//
//         void EnsureNotInvalidToken()
//         {
//             var expireTime = ReadActiveExpireTime();
//
//             if (expireTime == -1)
//                 return;
//
//             double nowSeconds = GetTotalSeconds(DateTime.Now);//DateTime.Now.TimeOfDay.TotalSeconds;
//             if (nowSeconds > expireTime)//expireDuration)
//             {
//                 DeleteToken();
//             }
//         }
//         void AuthPipe()
//         {
//             UpdateAuthState();
//             ConnectionToApplicationAPI();
//
//         }
//         #endregion
//         #region PUBLIC_API
//         public static void StoreExpirationTime()
//         {
//             int expireDuration = 13922;
//
//             var time = DateTime.Now;
//             time = time.AddSeconds(expireDuration);
//
//             PlayerPrefs.SetFloat("token-expire-time-cache", (float)GetTotalSeconds(time));
//             PlayerPrefs.Save();
//         }
//
//         public void Connect()
//         {
//             EnsureNotInvalidToken();
//
//             UnityTwitch.UnityPAL.captureRequests.Clear();
//             UnityTwitch.UnityPAL.captureRequests.Add("https://api.twitch.tv/helix/eventsub/subscriptions", (res) =>
//             {
//                 bool success = res.HttpStatus == 202 || res.HttpStatus == 200;
//                 //if (success)
//                 //  isConnected = true;
//
//             });
//             UnityTwitch.UnityPAL.captureRequests.Add("https://api.twitch.tv/helix/users", (res) =>
//             {
//
//                 m_WebRequestResults.Add(res);
//
//                 //   MonoBehaviour.print("captured:::::" + res.ResponseBody);
//             });//Clear();
//
//             AuthPipe();
//         }
//
//
//
//
//
//         public string GetAuthFile()
//         {
//             var tokenFile = "twitch-r66-authorization.json";
//             var file = $"{m_DataPath}/{tokenFile}";
//             return file;
//         }
//
//         public string GetAccessToken()
//         {
//             var authFile = System.IO.File.ReadAllText(GetAuthFile());
//             return JObject.Parse(authFile)["access_token"].ToString();
//         }
//
//         [ContextMenu("delete token")]
//         public void DeleteToken()
//         {
//             var file = GetAuthFile();
//             // Application.persistentDataPath
//             if (!System.IO.File.Exists(file))
//                 return;
//
//             System.IO.File.Delete(file);
//             MonoBehaviour.print("auth file deleted!!!!");
//         }
//
//         public void UpdateAuthState()
//         {
//             if (Twitch.API == null)
//             {
//                 Debug.LogError("twcihapi is null???");
//                 return;
//             }
//             m_CurAuthState = Twitch.API.GetAuthState();
//             if (m_CurAuthState.MaybeResult.Status == AuthStatus.LoggedIn)
//             {
//                 // user is logged in
//             }
//             if (m_CurAuthState.MaybeResult.Status == AuthStatus.LoggedOut)
//             {
//                 // user is logged out, do something
//                 // In this example you could also call GetAuthInformation() to retrigger login
//             }
//             if (m_CurAuthState.MaybeResult.Status == AuthStatus.WaitingForCode)
//             {
//                 /*
//
//                 // Waiting for code
//                 var RequiredScopes = new List<TwitchOAuthScope>();
//                 RequiredScopes.Add(TwitchOAuthScope.Channel.ManageRedemptions);
//                 var UserAuthInfo = Twitch.API.GetAuthenticationInfo(RequiredScopes.ToArray()).MaybeResult;
//                 if (UserAuthInfo == null)
//                 {
//                     // User is still loading
//                 }*/
//                 // We have reached the state where we can ask the user to login
//                 //    Application.OpenURL($"{UserAuthInfo.Uri}{UserAuthInfo.UserCode}");
//             }
//         }
//         #endregion
//     }
// }