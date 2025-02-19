﻿using DataModel;
using Helper;
using Newtonsoft.Json;
using NINJA;
using NINJA.Parser;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class NINJAImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;

        public NINJAImportHelper(ISettings settings, QueryFactory queryfactory)
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
        }

        #region NINJA Helpers

        public async Task<UpdateDetail> SaveDataToODH(DateTime? lastchanged = null, CancellationToken cancellationToken = default)
        {
            //Import the data from Mobility Api
            var culturelist = await ImportList(cancellationToken);
            //Parse the data and save it to DB
            var result = await SaveEventsToPG(culturelist.Item1.data, culturelist.Item2.data);

            return result;
        }

        private async Task<Tuple<NinjaObject<NinjaEvent>,NinjaObject<NinjaPlaceRoom>> > ImportList(CancellationToken cancellationToken)
        {
            var responseevents = await GetNinjaData.GetNinjaEvent();
            var responseplaces = await GetNinjaData.GetNinjaPlaces();

            WriteLog.LogToConsole("", "dataimport", "list.mobilityculture", new ImportLog() { sourceid = "", sourceinterface = "mobility.culture", success = true, error = "" });

            return Tuple.Create(responseevents, responseplaces);
        }

        private async Task<UpdateDetail> SaveEventsToPG(ICollection<NinjaData<NinjaEvent>> ninjadataarr, ICollection<NinjaData<NinjaPlaceRoom>> ninjaplaceroomarr)
        {

            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            List<string> idlistspreadsheet = new List<string>();
            List<string> sourcelist = new List<string>();

            foreach (var ninjadata in ninjadataarr.Select(x => x.tmetadata))
            {
                foreach (KeyValuePair<string, NinjaEvent> kvp in ninjadata)
                {
                    if (!String.IsNullOrEmpty(kvp.Key))
                    {
                        var place = ninjaplaceroomarr.Where(x => x.sname == kvp.Value.place).FirstOrDefault();
                        var room = ninjaplaceroomarr.Where(x => x.sname == kvp.Value.room).FirstOrDefault();

                        var eventtosave = ParseNinjaData.ParseNinjaEventToODHEvent(kvp.Key, kvp.Value, place, room);

                        if(eventtosave != null)
                        {
                            //Setting Location Info
                            //Location Info (by GPS Point)
                            if (eventtosave.Latitude != 0 && eventtosave.Longitude != 0)
                            {
                                await SetLocationInfo(eventtosave);
                            }

                            eventtosave.Active = true;
                            eventtosave.SmgActive = false;

                            var idtocheck = kvp.Key;

                            if (idtocheck.Length > 50)
                                idtocheck = idtocheck.Substring(0, 50);

                            var result = await InsertDataToDB(eventtosave, kvp);

                            newimportcounter = newimportcounter + result.created.Value;
                            updateimportcounter = updateimportcounter + result.updated.Value;
                            errorimportcounter = errorimportcounter + result.error.Value;

                            idlistspreadsheet.Add(idtocheck.ToUpper());
                            if (!sourcelist.Contains(eventtosave.Source))
                                sourcelist.Add(eventtosave.Source);

                            WriteLog.LogToConsole(idtocheck.ToUpper(), "dataimport", "single.mobilityculture", new ImportLog() { sourceid = idtocheck.ToUpper(), sourceinterface = "mobility.culture", success = true, error = "" });
                        }                        
                        else
                        {
                            WriteLog.LogToConsole(kvp.Key, "dataimport", "single.mobilityculture", new ImportLog() { sourceid = kvp.Key, sourceinterface = "mobility.culture", success = false, error = "Event could not be parsed" });
                        }
                    }
                }               
            }

            //Begin SetDataNotinListToInactive
            var idlistdb = await GetAllEventsBySource(sourcelist);

            var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

            foreach (var idtodelete in idstodelete)
            {
                var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                if(deletedisableresult.Item1 > 0)
                    WriteLog.LogToConsole(idtodelete, "dataimport", "single.mobilityculture", new ImportLog() { sourceid = idtodelete, sourceinterface = "mobility.culture", success = true, error = "" });
                else if (deletedisableresult.Item2 > 0)
                    WriteLog.LogToConsole(idtodelete, "dataimport", "single.mobilityculture", new ImportLog() { sourceid = idtodelete, sourceinterface = "mobility.culture", success = true, error = "" });


                deleteimportcounter = deleteimportcounter + deletedisableresult.Item1 + deletedisableresult.Item2;
            }

            return new UpdateDetail() { updated = updateimportcounter, created = newimportcounter, deleted = deleteimportcounter };
        }

        //Check if logic can be moved here
        //private async Task<UpdateDetail> SetDataNotinListToInactive()
        //{
            
        //}

        private async Task<PGCRUDResult> InsertDataToDB(EventLinked eventtosave, KeyValuePair<string, NinjaEvent> ninjaevent)
        {
            try
            {
                eventtosave.Id = eventtosave.Id?.ToUpper();

                //Set LicenseInfo
                eventtosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<Event>(eventtosave, Helper.LicenseHelper.GetLicenseforEvent);

                var rawdataid = await InsertInRawDataDB(ninjaevent);

                return await QueryFactory.UpsertData<EventLinked>(eventtosave, "events", rawdataid);                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, NinjaEvent> ninjaevent)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                        new RawDataStore()
                        {
                            datasource = "ninja",
                            importdate = DateTime.Now,
                            raw = JsonConvert.SerializeObject(ninjaevent.Value),
                            sourceinterface = "culture",
                            sourceid = ninjaevent.Key,
                            sourceurl = "https://mobility.api.opendatahub.bz.it/v2/flat/Culture/",
                            type = "event_centrotrevi-drin"
                        });
        }        
          
        private async Task<Tuple<int,int>> DeleteOrDisableData(string eventid, bool delete)
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory.Query("events").Where("id", eventid)
                    .DeleteAsync();
            }
            else
            {
                var query =
               QueryFactory.Query("events")
                   .Select("data")
                   .Where("id", eventid);

                var data = await query.GetFirstOrDefaultAsObject<EventLinked>();

                if (data != null)                
                {
                    if (data.Active != false || data.SmgActive != false)
                    {
                        data.Active = false;
                        data.SmgActive = false;

                        updateresult = await QueryFactory.Query("events").Where("id", eventid)
                                        .UpdateAsync(new JsonBData() { id = eventid, data = new JsonRaw(data) });
                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }

        #endregion

        #region CUSTOM Ninja Import

        private async Task<List<string>> GetAllEventsBySource(List<string> sourcelist)
        {

            var query =
               QueryFactory.Query("events")
                   .Select("id")
                   .SourceFilter_GeneratedColumn(sourcelist);

            var eventids = await query.GetAsync<string>();

            return eventids.ToList();
        }

        private async Task SetLocationInfo(EventLinked myevent)
        {
            var district = await GetLocationInfo.GetNearestDistrictbyGPS(QueryFactory, myevent.Latitude, myevent.Longitude, 30000);

            if (district == null)
                return;

            myevent.DistrictId = district.Id;
            myevent.DistrictIds = new List<string>() { district.Id };

            var locinfo = await GetLocationInfo.GetTheLocationInfoDistrict(QueryFactory, district.Id);
            if (locinfo != null)
            {
                LocationInfoLinked locinfolinked = new LocationInfoLinked
                {
                    DistrictInfo = new DistrictInfoLinked
                    {
                        Id = locinfo.DistrictInfo?.Id,
                        Name = locinfo.DistrictInfo?.Name
                    },
                    MunicipalityInfo = new MunicipalityInfoLinked
                    {
                        Id = locinfo.MunicipalityInfo?.Id,
                        Name = locinfo.MunicipalityInfo?.Name
                    },
                    TvInfo = new TvInfoLinked
                    {
                        Id = locinfo.TvInfo?.Id,
                        Name = locinfo.TvInfo?.Name
                    },
                    RegionInfo = new RegionInfoLinked
                    {
                        Id = locinfo.RegionInfo?.Id,
                        Name = locinfo.RegionInfo?.Name
                    }
                };

                myevent.LocationInfo = locinfolinked;
            }
        }

        #endregion

    }
}
