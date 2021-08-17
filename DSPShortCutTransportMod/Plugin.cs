using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;



namespace DSPShortCutTransportMod
{

    [BepInPlugin(GUID,NAME,VERSION)]
    [BepInProcess(GAME_PROCESS)]
    public class Plugin:BaseUnityPlugin
    {
        public const String GUID = "com.lby.dsp.shortcuttransport";
        public const String NAME = "lby";
        public const String VERSION = "1.0";
        private const String GAME_PROCESS = "DSPGAME.exe";
        public static int StarCount = 64;
        public static int[,] starDistance = null;
        public static GalacticTransport instanceGalacticTransport;
        public static bool firstUse = true;
        void Start()
        {
            var harmony = new Harmony(GUID); 
            harmony.PatchAll();
            Logger.LogInfo("plugin start" + GUID);
        }

        [HarmonyPatch(typeof(GalacticTransport), "RefreshTraffic")]
        class Patch
        {            
            static void Postfix(GalacticTransport __instance)
            {
                Console.WriteLine("patch run");
                if (firstUse)
                {
                    Console.WriteLine("First Run Pass");
                    firstUse = false;
                    return;
                }
                instanceGalacticTransport = __instance;
                initStarDistanceArray();

                Console.WriteLine("Start Sort,Count is " + __instance.stationCursor);
                for (int index = 1; index < __instance.stationCursor; ++index)
                {
                    if (__instance.stationPool[index] != null && __instance.stationPool[index].gid == index)
                    {
                        try
                        {
                            Console.WriteLine("Sorting " + index);
                            sortRemotePairs(__instance.stationPool[index]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            Console.WriteLine(e.StackTrace);
                            //throw;
                        }
                    }
                }
                Console.WriteLine("Sort Over");
            }
        
            static void sortRemotePairs(StationComponent stationComponent)
            {
                SupplyDemandPair[] remotePairs = new SupplyDemandPair[stationComponent.remotePairs.Length];
                stationComponent.remotePairs.CopyTo(remotePairs, 0);
                int[] distancelist = new int[stationComponent.remotePairCount];
                SortedSet<int> distanceset = new SortedSet<int>();
                int nowmax = 0;
                bool needsort=false;
                for(int i = 0; i < stationComponent.remotePairCount; i++)
                {
                    int distance= getDistance(remotePairs[i].supplyId, remotePairs[i].demandId);
                    distancelist[i] = distance;
                    distanceset.Add(distance);
                    if (nowmax <= distance)
                    {
                        nowmax = distance;
                    }
                    else
                    {
                        needsort = true;
                    }
                    Console.WriteLine($"distance={distance},SID={remotePairs[i].supplyId},supplyIndex={remotePairs[i].supplyIndex},DID={remotePairs[i].demandId},demandIndex={remotePairs[i].demandIndex}");
                }
                if (!needsort)
                {
                    Console.WriteLine("No Sort");
                    return;
                }
                Console.WriteLine($"Begin Sort RemotePairs,RemotePairs count is {stationComponent.remotePairCount},{stationComponent.remotePairs.Length}");
                lock (stationComponent.remotePairs)
                {
                    stationComponent.ClearRemotePairs();
                    foreach (int item in distanceset)
                    {
                        for (int i=0;i< distancelist.Length; i++)
                        {
                            if (distancelist[i] == item)
                            {
                                SupplyDemandPair temp = remotePairs[i];
                                stationComponent.AddRemotePair(temp.supplyId, temp.supplyIndex, temp.demandId, temp.demandIndex);
                                Console.WriteLine($"distance={item},SID={remotePairs[i].supplyId},supplyIndex={remotePairs[i].supplyIndex},DID={remotePairs[i].demandId},demandIndex={remotePairs[i].demandIndex}");
                            }
                        }
                    }     
                }
                Console.WriteLine($"After Sort RemotePairs,RemotePairs count is {stationComponent.remotePairCount},{stationComponent.remotePairs.Length}");
            }

            static int getDistance(int sid,int did)
            {
                int sStarId = stationidToStarid(sid);
                int dStarId = stationidToStarid(did);
                return starDistance[sStarId - 1, dStarId - 1];
            }

            static int stationidToStarid(int stationgid)
            {
                StationComponent stationComponent = instanceGalacticTransport.stationPool[stationgid];
                int plantid = stationComponent.planetId;
                GalaxyData galaxyData = instanceGalacticTransport.gameData.galaxy;
                PlanetData planetData = galaxyData.PlanetById(plantid);
                StarData starData = planetData.star;
                return starData.id;
            }

            static void initStarDistanceArray()
            {
                Console.WriteLine("initStarDistanceArray(GalacticTransport __instance)");       
                if (starDistance == null)
                {
                    Console.WriteLine("initStarDistanceArray");
                    Console.WriteLine("starDistance == null");
                    GalaxyData galaxyData = instanceGalacticTransport.gameData.galaxy;
                    StarData[] stars = galaxyData.stars;
                    int starCount = stars.Length;
                    starDistance = new int[starCount, starCount];
                    for (int i = 0; i < starCount; i++)
                    {
                        for (int j = i + 1; j < starCount; j++)
                        {                              
                            StarData starDatai = stars[i];
                            VectorLF3 vectorLF3Stari = starDatai.position;
                            StarData starDataj = stars[j];
                            VectorLF3 vectorLF3Starj = starDataj.position;
                            int distance = (int)vectorLF3Stari.Distance(vectorLF3Starj);
                            starDistance[i, j] = distance;
                            starDistance[j, 1] = distance;
                            //Console.WriteLine(i + "<>" + j +"is"+distance);
                        }
                    }                    
                    Console.WriteLine("initStarDistanceArray over");
                }                
            }
        }
    }
}
