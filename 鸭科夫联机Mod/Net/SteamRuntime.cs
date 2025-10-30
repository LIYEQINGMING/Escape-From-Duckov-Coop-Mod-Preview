// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team
//
// Runtime Steamworks availability/probing helpers for a mod environment.
// This file intentionally avoids any direct compile-time reference to Steamworks
// to keep the project buildable without Steam SDK assemblies. All checks are
// performed via reflection.

using System;
using System.Linq;
using System.Reflection;

namespace 鸭科夫联机Mod
{
    internal static class SteamRuntime
    {
        // Detect whether Steamworks API seems available in the current process.
        public static bool IsAvailable()
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                // Look for common Steamworks C# bindings
                bool hasSteamworksNet = asms.Any(a => SafeName(a).StartsWith("Steamworks", StringComparison.OrdinalIgnoreCase));
                bool hasFacepunch = asms.Any(a => SafeName(a).IndexOf("Facepunch.Steamworks", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!hasSteamworksNet && !hasFacepunch) return false;

                // Try to detect initialized states via reflection (best-effort; non-fatal)
                if (hasSteamworksNet)
                {
                    // Steamworks.NET: class SteamAPI / SteamClient
                    var tSteamAPI = FindType("Steamworks.SteamAPI");
                    if (tSteamAPI != null)
                    {
                        var miIsSteamRunning = tSteamAPI.GetMethod("IsSteamRunning", BindingFlags.Public | BindingFlags.Static);
                        if (miIsSteamRunning != null)
                        {
                            var ok = (bool) (miIsSteamRunning.Invoke(null, null) ?? false);
                            if (!ok) return false; // Steam client not running
                        }
                    }
                }

                if (hasFacepunch)
                {
                    var tClient = FindType("Steamworks.SteamClient");
                    if (tClient != null)
                    {
                        var piValid = tClient.GetProperty("IsValid", BindingFlags.Public | BindingFlags.Static);
                        if (piValid != null)
                        {
                            var ok = (bool) (piValid.GetValue(null, null) ?? false);
                            if (!ok) return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeName(Assembly a)
        {
            try { return a.GetName().Name ?? string.Empty; } catch { return string.Empty; }
        }

        private static Type FindType(string fullName)
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var t = a.GetType(fullName, throwOnError: false, ignoreCase: false);
                        if (t != null) return t;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }
}
