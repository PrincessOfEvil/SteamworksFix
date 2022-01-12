using HarmonyLib;
using System.Reflection;
using Verse;
using Verse.Steam;
using Steamworks;
using System.Linq;

namespace SteamworksFix
    {
#pragma warning disable IDE0051 // Remove unused private members
    [StaticConstructorOnStartup]
    static class HarmonyPatches
        {
        static HarmonyPatches()
            {
            var harmony = new Harmony("princess.steamworksfix");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

    [HarmonyPatch(typeof(WorkshopItemHook), "OnDetailsQueryReturned")]
    static class WorkshopItemHook_OnDetailsQueryReturned_Patch
        {
        private static FieldInfo steamAuthor = typeof(WorkshopItemHook).GetField("steamAuthor", AccessTools.all);

        static void Postfix(SteamUGCRequestUGCDetailsResult_t result, WorkshopItemHook __instance)
            {
            if (__instance.MayHaveAuthorNotCurrentUser)
                {
                var handle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published,
                    EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderAsc,
                    SteamUtils.GetAppID(), SteamUtils.GetAppID(), 1);
                SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(handle);

                CallResult<SteamUGCQueryCompleted_t>.Create((query, ioFail) =>
                {
                    if (query.m_unNumResultsReturned < query.m_unTotalMatchingResults)
                        {
                        Log.Warning("Workshop query result returned more than 50 mods published. " +
                            "(Returned: " + query.m_unNumResultsReturned + ", total amount: " + query.m_unTotalMatchingResults + ") " +
                            "As fixing this would create a massive recursion i am entirely unequipped to create, i will just allow uploads to all mods. " +
                            "This is not incorrect behaviour, as Steam runs its own checks.");

                        steamAuthor.SetValue(__instance, SteamUser.GetSteamID());
                        }
                    else if (query.m_unNumResultsReturned > 0)
                        {
                        for (uint i = 0; i < query.m_unNumResultsReturned; i++)
                            {
                            SteamUGCDetails_t deets;
                            SteamUGC.GetQueryUGCResult(query.m_handle, i, out deets);

                            Log.Message(deets.m_rgchTitle + ":" + result.m_details.m_rgchTitle);

                            if (deets.m_rgchTitle == result.m_details.m_rgchTitle)
                                {
                                steamAuthor.SetValue(__instance, SteamUser.GetSteamID());
                                }
                            }
                        }

                    SteamUGC.ReleaseQueryUGCRequest(handle);
                    }).Set(hAPICall);
                }
            }
        }
    }
