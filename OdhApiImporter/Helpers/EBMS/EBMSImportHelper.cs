﻿using DataModel;
using EBMS;
using Helper;
using Newtonsoft.Json;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.EBMS
{
    public class EBMSImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;

        public EBMSImportHelper(ISettings settings, QueryFactory queryfactory)
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
        }

        #region EBMS Helpers

        public async Task<string> ImportEbmsEventsToDB()
        {
            try
            {
                var resulttuple = ImportEBMSData.GetEbmsEvents(settings.EbmsConfig.User, settings.EbmsConfig.Password);
                var resulttuplesorted = resulttuple.OrderBy(x => x.Item1.StartDate);

                var currenteventshort = await GetAllEventsShort(DateTime.Now);

                //var result = resulttuple.Select(x => x.Item1).ToList();
                //var resultsorted = result.OrderBy(x => x.StartDate);

                var updatecounter = 0;
                var newcounter = 0;
                var deletecounter = 0;

                foreach (var (eventshort, eventebms) in resulttuplesorted)
                {
                    bool neweventshort = false;

                    var query =
                       QueryFactory.Query("eventeuracnoi")
                           .Select("data")
                           .Where("id", eventshort.Id);

                    var eventindb = await query.GetFirstOrDefaultAsObject<EventShort>();

                    //currenteventshort.Where(x => x.EventId == eventshort.EventId).FirstOrDefault();

                    var changedonDB = DateTime.Now;

                    //Fields to not overwrite
                    var imagegallery = new List<ImageGallery>();
                    var eventTextDE = "";
                    var eventTextIT = "";
                    var eventTextEN = "";
                    var videourl = "";
                    Nullable<bool> activeweb = null;
                    List<string> technologyfields = null;
                    List<string> customtagging = null;
                    var webadress = "";
                    List<DocumentPDF> eventdocument = new List<DocumentPDF>();
                    bool? soldout = false;
                    bool? externalorganizer = false;

                    if (eventindb != null)
                    {
                        changedonDB = eventindb.ChangedOn;
                        imagegallery = eventindb.ImageGallery;
                        eventTextDE = eventindb.EventTextDE;
                        eventTextIT = eventindb.EventTextIT;
                        eventTextEN = eventindb.EventTextEN;
                        activeweb = eventindb.ActiveWeb;
                        videourl = eventindb.VideoUrl;
                        technologyfields = eventindb.TechnologyFields;
                        customtagging = eventindb.CustomTagging;
                        webadress = eventindb.WebAddress;
                        externalorganizer = eventindb.ExternalOrganizer;

                        eventdocument = eventindb.EventDocument;
                        soldout = eventindb.SoldOut;
                    }
                    else
                    {
                        neweventshort = true;
                    }


                    if (changedonDB != eventshort.ChangedOn)
                    {

                        eventshort.ImageGallery = imagegallery;
                        eventshort.EventTextDE = eventTextDE;
                        eventshort.EventTextIT = eventTextIT;
                        eventshort.EventTextEN = eventTextEN;
                        eventshort.ActiveWeb = activeweb;
                        eventshort.VideoUrl = videourl;
                        eventshort.TechnologyFields = technologyfields;
                        eventshort.CustomTagging = customtagging;
                        if (!String.IsNullOrEmpty(webadress))
                            eventshort.WebAddress = webadress;

                        eventshort.SoldOut = soldout;
                        eventshort.EventDocument = eventdocument;
                        eventshort.ExternalOrganizer = externalorganizer;

                        //New If CompanyName is Noi - blablabla assign TechnologyField automatically and Write to Display5 if not empty "NOI"
                        if (eventshort.CompanyName.StartsWith("NOI - "))
                        {
                            if (String.IsNullOrEmpty(eventshort.Display5))
                                eventshort.Display5 = "NOI";

                            eventshort.TechnologyFields = AssignTechnologyfieldsautomatically(eventshort.CompanyName, eventshort.TechnologyFields);
                        }

                        int queryresult = 0;

                        //Setting LicenseInfo
                        eventshort.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<EventShort>(eventshort, Helper.LicenseHelper.GetLicenseforEventShort);


                        if (neweventshort)
                        {
                            queryresult = await QueryFactory.Query("eventeuracnoi")
                                //.InsertAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                                .InsertAsync(new JsonBDataRaw() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort), raw = JsonConvert.SerializeObject(eventebms) });

                            newcounter++;
                        }
                        else
                        {
                            //var query = await QueryFactory.Query("alpinebits").UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(input) });

                            //TODO CHECK IF THIS WORKS     
                            queryresult = await QueryFactory.Query("eventeuracnoi").Where("id", eventshort.Id)
                                //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                                .UpdateAsync(new JsonBDataRaw() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort), raw = JsonConvert.SerializeObject(eventebms) });

                            updatecounter++;
                        }

                        //if (queryresult != "1")
                        //    Constants.tracesource.TraceEvent(TraceEventType.Error, 0, "EventShort save error: " + eventshort.Id);

                        //Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine("EventShort imported: " + eventshort.Id);
                        //Constants.tracesource.TraceEvent(TraceEventType.Information, 0, "EventShort imported: " + eventshort.Id);                        
                    }
                }

                if (resulttuple.Select(x => x.Item1).Count() > 0)
                    deletecounter = await DeleteDeletedEvents(resulttuple.Select(x => x.Item1).ToList(), currenteventshort.ToList());

                return String.Format("Events Updated {0} New {1} Deleted {2}", updatecounter.ToString(), newcounter.ToString(), deletecounter.ToString());
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static List<string> AssignTechnologyfieldsautomatically(string companyname, List<string> technologyfields)
        {
            if (technologyfields == null)
                technologyfields = new List<string>();

            //Digital, Alpine, Automotive/Automation, Food, Green

            AssignTechnologyFields(companyname, "Digital", "Digital", technologyfields);
            AssignTechnologyFields(companyname, "Alpine", "Alpine", technologyfields);
            AssignTechnologyFields(companyname, "Automotive", "Automotive/Automation", technologyfields);
            AssignTechnologyFields(companyname, "Food", "Food", technologyfields);
            AssignTechnologyFields(companyname, "Green", "Green", technologyfields);

            if (technologyfields.Count == 0)
                return null;
            else
                return technologyfields;
        }

        private static void AssignTechnologyFields(string companyname, string tocheck, string toassign, List<string> automatictechnologyfields)
        {
            if (companyname.Contains(tocheck))
                if (!automatictechnologyfields.Contains(toassign))
                    automatictechnologyfields.Add(toassign);
        }

        private async Task<int> DeleteDeletedEvents(List<EventShort> eventshortfromnow, List<EventShort> eventshortinDB)
        {
            //TODO CHECK if Event is in list, if not, DELETE!
            //TODO CHECK IF THIS IS WORKING CORRECTLY


            var idsonListinDB = eventshortinDB.Select(x => x.EventId).ToList();
            var idsonService = eventshortfromnow.Select(x => x.EventId).ToList();

            var deletecounter = 0;

            var idstodelete = idsonListinDB.Where(p => !idsonService.Any(p2 => p2 == p));

            if (idstodelete.Count() > 0)
            {
                foreach (var idtodelete in idstodelete)
                {
                    //Set to inactive or delete?

                    var eventshorttodeactivate = eventshortinDB.Where(x => x.EventId == idtodelete).FirstOrDefault();

                    //TODO CHECK IF IT WORKS
                    if (eventshorttodeactivate != null)
                    {
                        await QueryFactory.Query("eventeuracnoi").Where("id", eventshorttodeactivate.Id.ToLower()).DeleteAsync();
                        deletecounter++;
                    }
                }
            }

            return deletecounter;
        }

        private async Task<IEnumerable<EventShort>> GetAllEventsShort(DateTime now)
        {
            var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

            var query =
                         QueryFactory.Query("eventeuracnoi")
                             .Select("data")
                             .WhereRaw("(((to_date(data->> 'EndDate', 'YYYY-MM-DD') >= '" + String.Format("{0:yyyy-MM-dd}", today) + "'))) AND(data#>>'\\{Source\\}' = ?)", "EBMS");

            return await query.GetAllAsObject<EventShort>();
        }

        #endregion

    }
}