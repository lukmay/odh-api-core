﻿using DataModel;
using SqlKata.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using DSS;
using System.Collections.Generic;
using DSS.Parser;
using System.Globalization;
using Helper;
using Newtonsoft.Json;
using System.Linq;

namespace OdhApiImporter.Helpers.DSS
{
    public class DSSImportHelper : ImportHelper, IImportHelper
    {
        //private readonly QueryFactory QueryFactory;
        //private readonly ISettings settings;

        //public DSSImportHelper(ISettings settings, QueryFactory queryfactory)
        //{
        //    this.QueryFactory = queryfactory;
        //    this.settings = settings;
        //}

        public DSSImportHelper(ISettings settings, QueryFactory queryfactory, string table) : base(settings, queryfactory, table)
        {

        }

        public List<DSSRequestType> requesttypelist { get; set; }
        public string entitytype { get; set; }

        public async Task<UpdateDetail> SaveDataToODH(DateTime? lastchanged = null, CancellationToken cancellationToken = default)
        {
            requesttypelist = new List<DSSRequestType>();

            if (entitytype.ToLower() == "lift")
            {
                requesttypelist.Add(DSSRequestType.liftbase);
                requesttypelist.Add(DSSRequestType.liftstatus);
            }
            else if (entitytype.ToLower() == "slope")
            {
                requesttypelist.Add(DSSRequestType.slopebase);
                requesttypelist.Add(DSSRequestType.slopestatus);
            }

            List<dynamic> dssdata = new List<dynamic>();

            foreach (var requesttype in requesttypelist)
            {
                //Get DSS data
                dssdata.Add(await GetDSSData.GetDSSDataAsync(requesttype, settings.DSSConfig.User, settings.DSSConfig.Password, settings.DSSConfig.ServiceUrl));
            }

            var updateresult = await ImportData(dssdata, cancellationToken);

            return updateresult;
        }

        public async Task<UpdateDetail> ImportData(List<dynamic> dssinput, CancellationToken cancellationToken)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;

            List<string> idlistdssinterface = new List<string>();

            if (dssinput != null && dssinput.Count > 0)
            {
                string lastupdatestr = dssinput[0].lastUpdate;
                //interface lastupdate
                DateTime.TryParseExact(lastupdatestr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastupdate);

                var areaquery = QueryFactory.Query()
                            .SelectRaw("data")
                            .From("areas");

                // Get all Areas
                var arealist =
                    await areaquery
                        .GetAllAsObject<AreaLinked>();

                var validforentity = new List<string>();

                if (entitytype.ToLower() == "lift")
                {
                    validforentity.Add("anderes");
                }
                else if (entitytype.ToLower() == "slope")
                {
                    validforentity.Add("winter");
                }

                var validcategories = new List<ODHTagLinked>();

                // Get all valid categories

                var subcategories = await QueryFactory.Query()
                            .SelectRaw("data")
                            .From("smgtags")
                            .ODHTagValidForEntityFilter(validforentity)
                            .ODHTagMainEntityFilter(new List<string>() { "smgpoi" })
                            .GetAllAsObject<ODHTagLinked>();

                //Temporary get winter/anderes tag and add it to validcategories
                var maincategories = await QueryFactory.Query()
                            .SelectRaw("data")
                            .From("smgtags")
                            .IdIlikeFilter(new List<string>() { "winter","anderes" })
                            .GetAllAsObject<ODHTagLinked>();

                validcategories.AddRange(maincategories);
                validcategories.AddRange(subcategories);

                //loop trough dss items
                foreach (var item in dssinput[0].items)
                {
                    //Parse DSS Data
                    ODHActivityPoiLinked parsedobject = await ParseDSSDataToODHActivityPoi(item);

                    //Add to list
                    idlistdssinterface.Add(parsedobject.Id);

                    if (parsedobject != null)
                    {
                        //Add the LocationInfo
                        //TODO if Area can be mapped return locationinfo
                        if (parsedobject.GpsInfo != null && parsedobject.GpsInfo.Count > 0)
                        {
                            if (parsedobject.GpsInfo.FirstOrDefault().Latitude != 0 && parsedobject.GpsInfo.FirstOrDefault().Longitude != 0)
                            {
                                var district = await GetLocationInfo.GetNearestDistrictbyGPS(QueryFactory, parsedobject.GpsInfo.FirstOrDefault().Latitude, parsedobject.GpsInfo.FirstOrDefault().Longitude, 30000);

                                if (district != null)
                                {
                                    var locinfo = await GetLocationInfo.GetTheLocationInfoDistrict(QueryFactory, district.Id);

                                    parsedobject.LocationInfo = locinfo;
                                    parsedobject.TourismorganizationId = locinfo.TvInfo.Id;
                                }
                            }
                        }

                        //Add AreaInfo from DSS skiarea
                        var dssskiarearid = (int?)item["skiresort"]["pid"];
                        if (dssskiarearid != null)
                        {
                            //TODO Select Area which has the mapping to dss/rid and fill AreaId Array and LocationInfo.Area
                            var area = arealist.Where(x => x.Mapping.ContainsKey("dss") && x.Mapping["dss"].ContainsKey("pid") && x.Mapping["dss"]["pid"] == dssskiarearid.ToString()).FirstOrDefault();

                            if (area != null)
                            {
                                parsedobject.AreaId = new List<string>() { area.Id };
                                if (parsedobject.LocationInfo == null)
                                    parsedobject.LocationInfo = new LocationInfoLinked();

                                Dictionary<string, string> areanames = new Dictionary<string, string>();
                                areanames.Add("de", area.Shortname);
                                areanames.Add("it", area.Shortname);
                                areanames.Add("en", area.Shortname);
                                areanames.Add("nl", area.Shortname);
                                areanames.Add("cs", area.Shortname);
                                areanames.Add("pl", area.Shortname);
                                areanames.Add("fr", area.Shortname);
                                areanames.Add("ru", area.Shortname);

                                parsedobject.LocationInfo.AreaInfo = new AreaInfoLinked() { Id = area.Id, Name = areanames };

                                //Use RegionId, TVId from Area
                                if (parsedobject.LocationInfo.RegionInfo == null)
                                    if (!String.IsNullOrEmpty(area.RegionId))
                                        parsedobject.LocationInfo.RegionInfo = new RegionInfoLinked() { Id = area.RegionId, Name = null };
                                
                                if (parsedobject.LocationInfo.TvInfo == null)
                                    if (!String.IsNullOrEmpty(area.TourismvereinId))
                                    {
                                        parsedobject.LocationInfo.TvInfo = new TvInfoLinked() { Id = area.TourismvereinId, Name = null };
                                        parsedobject.TourismorganizationId = area.TourismvereinId;
                                    }
                                        

                                if (parsedobject.LocationInfo.MunicipalityInfo == null)
                                    if (!String.IsNullOrEmpty(area.MunicipalityId))
                                        parsedobject.LocationInfo.MunicipalityInfo = new MunicipalityInfoLinked() { Id = area.MunicipalityId, Name = null };

                            }
                        }

                        //Setting Categorization by Valid Tags
                        var currentcategories = validcategories.Where(x => parsedobject.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower()));

                        foreach (var languagecategory in parsedobject.HasLanguage)
                        {
                            if (parsedobject.AdditionalPoiInfos == null)
                                parsedobject.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                            //Set MainType, SubType, PoiType
                            var additionalpoiinfo = new AdditionalPoiInfos();
                            additionalpoiinfo.Language = languagecategory;

                            var maintypeobj = validcategories.Where(x => x.Id == parsedobject.Type.ToLower()).FirstOrDefault();
                            var subtypeobj = validcategories.Where(x => x.Id == parsedobject.SubType.ToLower()).FirstOrDefault();

                            additionalpoiinfo.MainType = maintypeobj != null && maintypeobj.TagName != null && maintypeobj.TagName.ContainsKey(languagecategory) ? maintypeobj.TagName[languagecategory] : "";
                            additionalpoiinfo.SubType = subtypeobj != null && subtypeobj.TagName != null && subtypeobj.TagName.ContainsKey(languagecategory) ? subtypeobj.TagName[languagecategory] : "";

                            //Add the AdditionalPoi Info (include Novelty)
                            if (entitytype.ToLower() == "lift")
                                additionalpoiinfo.Novelty = (string)item["info-text"][languagecategory];
                            if (entitytype.ToLower() == "slope")
                                additionalpoiinfo.Novelty = (string)item["info-text-winter"][languagecategory];

                            foreach (var smgtagtotranslate in currentcategories.Where(x => x.DisplayAsCategory == true))
                            {
                                if (additionalpoiinfo.Categories == null)
                                    additionalpoiinfo.Categories = new List<string>();

                                if (smgtagtotranslate.TagName.ContainsKey(languagecategory) && !additionalpoiinfo.Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                                    additionalpoiinfo.Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                            }

                            parsedobject.AdditionalPoiInfos.TryAddOrUpdate(languagecategory, additionalpoiinfo);
                        }

                        //Set shortname
                        if (!String.IsNullOrEmpty(parsedobject.Detail["de"].Title))
                            parsedobject.Shortname = parsedobject.Detail["de"].Title;
                        else if (!String.IsNullOrEmpty(parsedobject.Detail["it"].Title))
                            parsedobject.Shortname = parsedobject.Detail["it"].Title;
                        else if (!String.IsNullOrEmpty(parsedobject.Detail["en"].Title))
                            parsedobject.Shortname = parsedobject.Detail["en"].Title;

                        ODHTagHelper.SetMainCategorizationForODHActivityPoi(parsedobject);

                        //Save parsedobject to DB + Save Rawdata to DB
                        var pgcrudresult = await InsertDataToDB(parsedobject, new KeyValuePair<string, dynamic>((string)item.pid, item));

                        newcounter = newcounter + pgcrudresult.created.Value;
                        updatecounter = updatecounter + pgcrudresult.updated.Value;

                        WriteLog.LogToConsole(parsedobject.Id, "dataimport", "single.dss" + entitytype, new ImportLog() { sourceid = parsedobject.Id, sourceinterface = "dss." + entitytype + "base", success = true, error = "" });
                    }
                    else
                    {
                        WriteLog.LogToConsole(parsedobject.Id, "dataimport", "single.dss" + entitytype, new ImportLog() { sourceid = parsedobject.Id, sourceinterface = "dss." + entitytype + "base", success = false, error = entitytype + " could not be parsed" });
                    }
                }
            }

            //Begin SetDataNotinListToInactive
            var idlistdb = await GetAllDSSDataByInterface(new List<string>() { "dss" + entitytype + "base" });

            var idstodelete = idlistdb.Where(p => !idlistdssinterface.Any(p2 => p2 == p));

            foreach (var idtodelete in idstodelete)
            {
                var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                if (deletedisableresult.Item1 > 0)
                    WriteLog.LogToConsole(idtodelete, "dataimport", "single.dss" + entitytype, new ImportLog() { sourceid = idtodelete, sourceinterface = "mobility.culture", success = true, error = "" });
                else if (deletedisableresult.Item2 > 0)
                    WriteLog.LogToConsole(idtodelete, "dataimport", "single.dss" + entitytype, new ImportLog() { sourceid = idtodelete, sourceinterface = "mobility.culture", success = true, error = "" });


                deletecounter = deletecounter + deletedisableresult.Item1 + deletedisableresult.Item2;
            }


            return new UpdateDetail() { created = newcounter, updated = updatecounter, deleted = deletecounter };
        }

        //Parse the dss interface content
        public async Task<ODHActivityPoiLinked> ParseDSSDataToODHActivityPoi(dynamic dssinput)
        {
            //id
            string odhdssid = "dss_" + dssinput.pid;

            //Get the ODH Item
            var mydssquery = QueryFactory.Query(table)
              .Select("data")
              .Where("id", odhdssid);

            var odhactivitypoiindb = await mydssquery.GetFirstOrDefaultAsObject<ODHActivityPoiLinked>();
            var odhactivitypoi = default(ODHActivityPoiLinked);

            if (entitytype.ToLower() == "lift")
            {
                odhactivitypoi = ParseDSSToODHActivityPoi.ParseDSSLiftDataToODHActivityPoi(odhactivitypoiindb, dssinput);
            }
            else if (entitytype.ToLower() == "slope")
            {
                odhactivitypoi = ParseDSSToODHActivityPoi.ParseDSSSlopeDataToODHActivityPoi(odhactivitypoiindb, dssinput);
            }

            //TODOS all of this stuff, Tags, Categories etc....

            return odhactivitypoi;
        }

        private async Task<PGCRUDResult> InsertDataToDB(ODHActivityPoiLinked odhactivitypoi, KeyValuePair<string, dynamic> dssdata)
        {
            try
            {
                odhactivitypoi.Id = odhactivitypoi.Id?.ToLower();

                //Set LicenseInfo
                odhactivitypoi.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoi>(odhactivitypoi, Helper.LicenseHelper.GetLicenseforOdhActivityPoi);

                var rawdataid = await InsertInRawDataDB(dssdata);
                
                var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(odhactivitypoi, table, rawdataid);

                //Hack insert also in Activity table
                await InsertInLegacyActivityTable(odhactivitypoi);

                return pgcrudresult;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, dynamic> dssdata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                        new RawDataStore()
                        {
                            datasource = "dss",
                            importdate = DateTime.Now,
                            raw = JsonConvert.SerializeObject(dssdata.Value),
                            sourceinterface = entitytype + "base",
                            sourceid = dssdata.Key,
                            sourceurl = "http://dss.dev.tinext.net/.rest/json-export/export/",
                            type = "odhactivitypoi-" + entitytype  
                        });
        }

        private async Task<List<string>> GetAllDSSDataByInterface(List<string> syncsourceinterfacelist)
        {

            var query =
               QueryFactory.Query(table)
                   .Select("id")
                   .SourceFilter_GeneratedColumn(syncsourceinterfacelist);

            var idlist = await query.GetAsync<string>();

            return idlist.ToList();
        }

        #region ActivityHackDestinationdata

        //TODO before each import clear activity table data

        private async Task<PGCRUDResult> InsertInLegacyActivityTable(ODHActivityPoiLinked odhactivitypoi)
        {

            //Transform to LTSActivityLinked
            var activity = TransformODHActivityPoiToActivity(odhactivitypoi);
            
            //Insert in Table
            var pgcrudresult = await QueryFactory.UpsertData<LTSActivityLinked>(activity, "activities");

            return pgcrudresult;
        }
     
        private LTSActivityLinked TransformODHActivityPoiToActivity(ODHActivityPoiLinked odhactivitypoi)
        {
            //TODO Transform class
            LTSActivityLinked myactivity = CastODHActivityTOLTSActivity(odhactivitypoi);

            //Update Categorization
            if (odhactivitypoi.SyncSourceInterface == "dssliftbase")
            {
                myactivity.Type = "Aufstiegsanlagen";

                var types = GetSubTypeAndPoiTypeFromFlagDescription(myactivity.SmgTags.ToList());

                myactivity.SubType = types.Item1;
                myactivity.PoiType = types.Item2;

                myactivity.AdditionalPoiInfos.TryAddOrUpdate("de", new AdditionalPoiInfos() { Language = "de", MainType = "Aufstiegsanlagen", SubType = "" });
                myactivity.AdditionalPoiInfos.TryAddOrUpdate("it", new AdditionalPoiInfos() { Language = "it", MainType = "lifts", SubType = "" });
                myactivity.AdditionalPoiInfos.TryAddOrUpdate("en", new AdditionalPoiInfos() { Language = "en", MainType = "ascensioni", SubType = "" });

            }
            else if(odhactivitypoi.SyncSourceInterface == "dssslopebase")
            {
                myactivity.Type = "Piste";                
                myactivity.SubType = "Ski Alpin";

                if(myactivity.Difficulty == "2")
                    myactivity.PoiType = "blau";
                if (myactivity.Difficulty == "4")
                    myactivity.PoiType = "rot";
                if (myactivity.Difficulty == "6")
                    myactivity.PoiType = "schwarz";

                myactivity.AdditionalPoiInfos.TryAddOrUpdate("de", new AdditionalPoiInfos() { Language = "de", MainType = "Ski alpin", SubType = "Piste" });
                myactivity.AdditionalPoiInfos.TryAddOrUpdate("it", new AdditionalPoiInfos() { Language = "it", MainType = "Sci alpino", SubType = "piste" });
                myactivity.AdditionalPoiInfos.TryAddOrUpdate("en", new AdditionalPoiInfos() { Language = "en", MainType = "Ski alpin", SubType = "slopes" });
            }

            //Type to Tag
            if (!String.IsNullOrEmpty(myactivity.Type) && !myactivity.SmgTags.Contains(myactivity.Type.ToLower()))
                myactivity.SmgTags.Add(myactivity.Type.ToLower());
            if (!String.IsNullOrEmpty(myactivity.SubType) && !myactivity.SmgTags.Contains(myactivity.SubType.ToLower()))
                myactivity.SmgTags.Add(myactivity.SubType.ToLower());
            if (!String.IsNullOrEmpty(myactivity.PoiType) && !myactivity.SmgTags.Contains(myactivity.PoiType.ToLower()))
                myactivity.SmgTags.Add(myactivity.PoiType.ToLower());

            if (myactivity.SmgTags.Contains("anderes"))
                myactivity.SmgTags.Remove("anderes");
            if (myactivity.SmgTags.Contains("winter"))
                myactivity.SmgTags.Remove("winter");
            if (myactivity.SmgTags.Contains("skirundtouren pisten"))
                myactivity.SmgTags.Remove("skirundtouren pisten");
            if (myactivity.SmgTags.Contains("activity"))
                myactivity.SmgTags.Remove("activity");
            if (myactivity.SmgTags.Contains("poi"))
                myactivity.SmgTags.Remove("poi");

            //Update GPS points position/valleystation/mountainstation
            if (odhactivitypoi.GpsInfo != null)
            {
                foreach (var gpsinfo in odhactivitypoi.GpsInfo)
                {
                    var gpsresult = ReturnGpsInfoActivityKey(gpsinfo);

                    myactivity.GpsInfo.Add(gpsresult.Item2);
                    myactivity.GpsPoints.TryAddOrUpdate(gpsresult.Item1, gpsresult.Item2);
                }
            }           

            return myactivity;
        }

        private Tuple<string,string> GetSubTypeAndPoiTypeFromFlagDescription(List<string> odhtags)
        {
            List<string> validtags = new List<string>();

            List<string> dsslifttypes = new List<string>()
            {
                "Seilbahn","Kabinenbahn","Unterirdische Bahn","Sessellift","Sessellift","Skilift","Schrägaufzug","Klein-Skilift","Telemix","Standseilbahn/Zahnradbahn",
                "Skibus","Zug","Sessellift","Sessellift","Sessellift","Förderband","4er Sessellift kuppelbar","6er Sessellift kuppelbar","8er Sessellift kuppelbar"
            };

            foreach(var odhtag in odhtags)
            {
                if(odhtag != "activity" && odhtag != "poi" &&
                    odhtag != "anderes" && odhtag != "aufstiegsanlagen" && odhtag != "weitere aufstiegsanlagen" && 
                    odhtag != "winter" && odhtag != "skirundtouren pisten" && odhtag != "pisten" && odhtag != "ski alpin" && odhtag != "piste" && odhtag != "weitere pisten")
                {
                    validtags.Add(odhtag);
                }
            }

            string subtype = "";
            string poitype = "";

            if (validtags.Count > 0)
            {
                subtype = dsslifttypes.Where(x => x.ToLower() == validtags[0]).FirstOrDefault();

                if (validtags.Count > 1)
                {
                    poitype = dsslifttypes.Where(x => x.ToLower() == validtags[1]).FirstOrDefault();
                }
            }

            return Tuple.Create(subtype, poitype);
        }

        private LTSActivityLinked CastODHActivityTOLTSActivity(ODHActivityPoiLinked odhactivitypoi)
        {
            var myactivity = new LTSActivityLinked();

            myactivity.Active = odhactivitypoi.Active;
            //myactivity.AdditionalPoiInfos = odhactivitypoi.AdditionalPoiInfos;
            myactivity.AltitudeDifference = odhactivitypoi.AltitudeDifference;
            myactivity.AltitudeHighestPoint = odhactivitypoi.AltitudeHighestPoint;
            myactivity.AltitudeLowestPoint = odhactivitypoi.AltitudeLowestPoint;
            myactivity.AltitudeSumDown = odhactivitypoi.AltitudeSumDown;
            myactivity.AltitudeSumUp = odhactivitypoi.AltitudeSumUp;
            myactivity.AreaId = odhactivitypoi.AreaId;
            myactivity.ContactInfos = odhactivitypoi.ContactInfos;
            myactivity.CopyrightChecked = odhactivitypoi.CopyrightChecked;
            myactivity.BikeTransport = odhactivitypoi.BikeTransport;
            myactivity.ChildPoiIds = odhactivitypoi.ChildPoiIds;
            myactivity.Detail = odhactivitypoi.Detail;
            myactivity.Difficulty = odhactivitypoi.Difficulty;
            myactivity.DistanceDuration = odhactivitypoi.DistanceDuration;
            myactivity.DistanceInfo = odhactivitypoi.DistanceInfo;
            myactivity.DistanceLength = odhactivitypoi.DistanceLength;
            myactivity.Exposition = odhactivitypoi.Exposition;
            myactivity.FeetClimb = odhactivitypoi.FeetClimb;
            myactivity.FirstImport  = odhactivitypoi.FirstImport;
            myactivity.GpsInfo = new List<GpsInfo>();
            //myactivity.GpsPoints = odhactivitypoi.GpsPoints;
            myactivity.GpsTrack = odhactivitypoi.GpsTrack;
            myactivity.HasFreeEntrance = odhactivitypoi.HasFreeEntrance;
            myactivity.HasLanguage = odhactivitypoi.HasLanguage;
            myactivity.HasRentals = odhactivitypoi.HasRentals;
            myactivity.Highlight = odhactivitypoi.Highlight;
            myactivity.Id = odhactivitypoi.Id.ToUpper();
            myactivity.ImageGallery = odhactivitypoi.ImageGallery;
            myactivity.IsOpen = odhactivitypoi.IsOpen;
            myactivity.IsPrepared = odhactivitypoi.IsPrepared;
            myactivity.IsWithLigth = odhactivitypoi.IsWithLigth;
            myactivity.LastChange = odhactivitypoi.LastChange;
            myactivity.LiftAvailable = odhactivitypoi.LiftAvailable;
            myactivity.LicenseInfo = odhactivitypoi.LicenseInfo;
            myactivity.LocationInfo = odhactivitypoi.LocationInfo;
            myactivity.LTSTags = odhactivitypoi.LTSTags;
            myactivity.Mapping = odhactivitypoi.Mapping;
            myactivity.MasterPoiIds = odhactivitypoi.MasterPoiIds;
            myactivity.Number = odhactivitypoi.Number;
            myactivity.OperationSchedule = odhactivitypoi.OperationSchedule;
            myactivity.OutdooractiveElevationID = odhactivitypoi.OutdooractiveElevationID;
            myactivity.OutdooractiveID = odhactivitypoi.OutdooractiveID;
            myactivity.OwnerRid = odhactivitypoi.OwnerRid;
            myactivity.PublishedOn = odhactivitypoi.PublishedOn;
            myactivity.Ratings = odhactivitypoi.Ratings;
            myactivity.RunToValley = odhactivitypoi.RunToValley;
            myactivity.Shortname = odhactivitypoi.Shortname;
            myactivity.SmgActive = odhactivitypoi.SmgActive;
            myactivity.SmgId = odhactivitypoi.SmgId;
            myactivity.SmgTags = odhactivitypoi.SmgTags;
            myactivity.Source = odhactivitypoi.Source;
            myactivity.TourismorganizationId = odhactivitypoi.TourismorganizationId;
            myactivity.WayNumber = odhactivitypoi.WayNumber;            

            return myactivity;
        }

        private Tuple<string, GpsInfo> ReturnGpsInfoActivityKey(GpsInfo gpsinfo)
        {

            string activitygpstypevalue = "";
            string gpstypekey = "";

            switch(gpsinfo.Gpstype)
            {
                case "valleystationpoint": activitygpstypevalue = "Talstation"; gpstypekey = "position"; break;
                case "middlestationpoint" : activitygpstypevalue = "Mittelstation"; gpstypekey = "middleposition"; break;
                case "mountainstationpoint" : activitygpstypevalue = "Bergstation"; gpstypekey = "endposition"; break;
                case "position": activitygpstypevalue = "Startpunkt"; gpstypekey = "position"; break;                
            };

            GpsInfo gpstoreturn = new GpsInfo() { Gpstype = activitygpstypevalue, Altitude = gpsinfo.Altitude, AltitudeUnitofMeasure = gpsinfo.AltitudeUnitofMeasure, Latitude = gpsinfo.Latitude, Longitude = gpsinfo.Longitude };

            return Tuple.Create(gpstypekey, gpstoreturn);
        }

        #endregion
    }
}
