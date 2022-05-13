﻿using DataModel;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helper;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// This class is used for different update operations on the data
    /// </summary>
    public class CustomDataOperation
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;

        public CustomDataOperation(ISettings settings, QueryFactory queryfactory)
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
        }

        public async Task<int> UpdateAllEventShortstonewDataModel()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query()
                   .SelectRaw("data")
                   .From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                if (eventshort.LastChange == null)
                    eventshort.LastChange = eventshort.ChangedOn;

                //Setting MetaInfo
                eventshort._Meta = MetadataHelper.GetMetadataobject<EventShortLinked>(eventshort, MetadataHelper.GetMetadataforEventShort);
                eventshort._Meta.LastUpdate = eventshort.LastChange;

                //Save tp DB
                //TODO CHECK IF THIS WORKS     
                var queryresult = await QueryFactory.Query("eventeuracnoi").Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(new JsonBData() { id = eventshort.Id?.ToLower() ?? "", data = new JsonRaw(eventshort) });

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllSTAVendingpoints()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query()
                   .SelectRaw("data")
                   .From("smgpois")
                   .Where("gen_source", "sta");

            var data = await query.GetObjectListAsync<ODHActivityPoiLinked>();
            int i = 0;

            foreach (var stapoi in data)
            {
                //Setting MetaInfo
                stapoi._Meta.Reduced = false;
                stapoi.Source = "sta";

                //Save tp DB
                //TODO CHECK IF THIS WORKS     
                var queryresult = await QueryFactory.Query("smgpois").Where("id", stapoi.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(new JsonBData() { id = stapoi.Id?.ToLower() ?? "", data = new JsonRaw(stapoi) });

                i++;
            }

            return i;
        }

        public async Task<int> FillDBWithDummyNews()
        {
            int i = 0;

            //for (int i=0; i )
            //{
            //    //Setting MetaInfo
            //    stapoi._Meta.Reduced = false;
            //    stapoi.Source = "sta";

            //    //Save tp DB
            //    //TODO CHECK IF THIS WORKS     
            //    var queryresult = await QueryFactory.Query("smgpois").Where("id", stapoi.Id)
            //        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
            //        .UpdateAsync(new JsonBData() { id = stapoi.Id?.ToLower() ?? "", data = new JsonRaw(stapoi) });

            //    i++;
            //}

            return i;
        }

        
    }
}
