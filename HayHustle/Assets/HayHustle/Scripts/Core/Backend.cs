using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HayHustle
{
    /// <summary>
    /// Player accounts with Supabase Auth (email + password, no extra verification step).
    /// Only the PUBLIC publishable key is used here - never put a secret key in the game.
    /// </summary>
    public static class Backend
    {
        public const string Url = "https://mmkktfpeqhweikkyglvp.supabase.co";
        public const string PublishableKey = "sb_publishable_FNufZzQ3mE5gOO1tsJobMQ_xgST2tKu";

        public static bool SignedIn => !string.IsNullOrEmpty(AccessToken);
        public static bool Offline { get; set; }          // "Play offline" chosen on the sign-in screen
        public static bool Busy { get; private set; }
        public static string Email { get; private set; } = "";
        public static string UserId { get; private set; } = "";
        public static string AccessToken { get; private set; } = "";
        static string refreshToken = "";
        static float tokenExpires;   // realtimeSinceStartup when the access token runs out
        static bool remembered;

#pragma warning disable 0649 // fields are filled in by JsonUtility
        [System.Serializable] class Credentials { public string email; public string password; }
        [System.Serializable] class RefreshBody { public string refresh_token; }
        [System.Serializable] class User { public string id; public string email; }
        [System.Serializable]
        class AuthResponse
        {
            public string access_token, refresh_token;
            public int expires_in;
            public User user;
            public string id, email;                          // sign-up without a session returns the user here
            public string error, error_description, msg, message, error_code;
        }

#pragma warning restore 0649

        public static void ResetRuntime()
        {
            Offline = false;
            Busy = false;
        }

        public static string SavedEmail => PlayerPrefs.GetString("hh_email", "");
        public static bool HasSavedSession => !string.IsNullOrEmpty(PlayerPrefs.GetString("hh_refresh", ""));

        // ================= Public API =================

        public static void SignUp(string email, string password, bool remember, System.Action<bool, string> done) =>
            Run(Post("/auth/v1/signup", JsonUtility.ToJson(new Credentials { email = email.Trim(), password = password }), remember, done, true));

        public static void SignIn(string email, string password, bool remember, System.Action<bool, string> done) =>
            Run(Post("/auth/v1/token?grant_type=password", JsonUtility.ToJson(new Credentials { email = email.Trim(), password = password }), remember, done, false));

        /// <summary>"Stay signed in": use the saved refresh token.</summary>
        public static void Resume(System.Action<bool, string> done)
        {
            string saved = PlayerPrefs.GetString("hh_refresh", "");
            if (string.IsNullOrEmpty(saved)) { done(false, ""); return; }
            Run(Post("/auth/v1/token?grant_type=refresh_token", JsonUtility.ToJson(new RefreshBody { refresh_token = saved }), true, done, false));
        }

        public static void SignOut()
        {
            if (SignedIn) Run(Logout(AccessToken));
            AccessToken = refreshToken = UserId = "";
            Offline = false;
            CloudSave.Reset();
            PlayerPrefs.DeleteKey("hh_refresh");
            PlayerPrefs.Save();
        }

        // ================= Requests =================

        static void Run(IEnumerator routine)
        {
            if (GameManager.I) GameManager.I.StartCoroutine(routine);
        }

        static UnityWebRequest Request(string path, string json)
        {
            var req = new UnityWebRequest(Url + path, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json ?? "{}")),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15,
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", PublishableKey);
            return req;
        }

        static IEnumerator Post(string path, string json, bool remember, System.Action<bool, string> done, bool isSignUp)
        {
            Busy = true;
            using (var req = Request(path, json))
            {
                yield return req.SendWebRequest();
                Busy = false;

                if (req.result == UnityWebRequest.Result.ConnectionError)
                {
                    done(false, "No internet connection. Check your connection, or play offline.");
                    yield break;
                }

                AuthResponse r = null;
                try { r = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text); } catch (System.Exception) { }

                if (req.result != UnityWebRequest.Result.Success || r == null)
                {
                    done(false, FriendlyError(r, req.responseCode));
                    yield break;
                }

                if (string.IsNullOrEmpty(r.access_token))
                {
                    // Sign-up worked but Supabase wants the email confirmed first.
                    done(false, isSignUp
                        ? "Account created, but this project still asks for email confirmation. Turn OFF \"Confirm email\" in Supabase (Authentication > Sign In / Providers > Email), or confirm from your inbox."
                        : "Could not sign in. Please try again.");
                    yield break;
                }

                AccessToken = r.access_token;
                refreshToken = r.refresh_token ?? "";
                tokenExpires = Time.realtimeSinceStartup + Mathf.Max(60, r.expires_in > 0 ? r.expires_in : 3600);
                remembered = remember;
                UserId = r.user != null ? r.user.id : "";
                Email = r.user != null && !string.IsNullOrEmpty(r.user.email) ? r.user.email : Email;
                Offline = false;
                PlayerPrefs.SetString("hh_email", Email);
                if (remember && !string.IsNullOrEmpty(refreshToken)) PlayerPrefs.SetString("hh_refresh", refreshToken);
                else PlayerPrefs.DeleteKey("hh_refresh");
                PlayerPrefs.Save();
                done(true, "");
            }
        }

        // ================= Database (cloud save) =================

        /// <summary>Makes sure the access token is still valid (refreshes it a minute before it runs out).</summary>
        public static IEnumerator EnsureToken()
        {
            if (!SignedIn || string.IsNullOrEmpty(refreshToken)) yield break;
            if (Time.realtimeSinceStartup < tokenExpires - 60f) yield break;
            bool finished = false;
            yield return Post("/auth/v1/token?grant_type=refresh_token", JsonUtility.ToJson(new RefreshBody { refresh_token = refreshToken }), remembered,
                (ok, err) => { finished = true; if (!ok) Debug.LogWarning("Hay Hustle: could not refresh sign-in - " + err); }, false);
            if (!finished) Busy = false;
        }

        /// <summary>A request to the Supabase database API as the signed-in player (Row Level Security keeps players apart).</summary>
        public static UnityWebRequest Rest(string method, string pathAndQuery, string json = null)
        {
            var req = new UnityWebRequest(Url + "/rest/v1/" + pathAndQuery, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 20,
            };
            if (json != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", PublishableKey);
            req.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            return req;
        }

        static IEnumerator Logout(string token)
        {
            using (var req = Request("/auth/v1/logout", "{}"))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                yield return req.SendWebRequest();
            }
        }

        static string FriendlyError(AuthResponse r, long code)
        {
            string raw = r == null ? "" : (r.error_description ?? r.msg ?? r.message ?? r.error ?? "");
            string c = r?.error_code ?? "";
            string low = raw.ToLowerInvariant();
            if (c == "email_provider_disabled" || low.Contains("email logins are disabled"))
                return "Email sign-in is switched off in Supabase. Turn ON the Email provider (Authentication > Sign In / Providers > Email).";
            if (c == "signup_disabled" || low.Contains("signups not allowed")) return "New sign-ups are switched off in Supabase (Authentication > Sign In / Providers > Allow new users to sign up).";
            if (low.Contains("invalid login") || c == "invalid_credentials") return "Wrong email or password.";
            if (low.Contains("already registered") || c == "user_already_exists") return "That email already has an account. Sign in instead.";
            if (low.Contains("not confirmed") || c == "email_not_confirmed")
                return "This email is not confirmed yet. Turn OFF \"Confirm email\" in Supabase (Authentication > Sign In / Providers > Email), or confirm from your inbox.";
            if (low.Contains("password") && (low.Contains("least") || low.Contains("short") || c == "weak_password")) return "Password is too short (at least 6 characters).";
            if (low.Contains("invalid") && low.Contains("email") || c == "email_address_invalid") return "That email address is not valid.";
            if (code == 429 || c == "over_request_rate_limit" || c == "over_email_send_rate_limit") return "Too many tries. Wait a minute and try again.";
            if (low.Contains("refresh token")) return "Your session expired. Please sign in again.";
            return string.IsNullOrEmpty(raw) ? $"Something went wrong (error {code}). Please try again." : raw;
        }
    }
}
