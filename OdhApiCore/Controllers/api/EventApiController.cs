﻿using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Events Api (data provided by LTS) SOME DATA Available as OPENDATA 
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class EventController : OdhController
    {
        // Only for test purposes

        public EventController(IWebHostEnvironment env, ISettings settings, ILogger<ActivityController> logger, QueryFactory queryFactory)
            : base(env, settings, logger, queryFactory)
        {
        }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Event List
        /// </summary>
        /// <param name="pagenumber">Pagenumber, (default:1)</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = No Filter), (default:'null')</param>        
        /// <param name="rancfilter">Rancfilter (Ranc 0-5 possible)</param>
        /// <param name="typefilter">Typefilter (Type of Event: not used yet)</param>
        /// <param name="topicfilter">Topic ID Filter (Filter by Topic ID) BITMASK</param>
        /// <param name="orgfilter">Organization Filter (Filter by Organizer RID)</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'api/ODHTag?validforentity=event'), (default:'null')</param>        
        /// <param name="begindate">BeginDate of Events (Format: yyyy-MM-dd)</param>
        /// <param name="sort">Sorting of Events ('desc': Descending, default, 'asc': Ascending)</param>
        /// <param name="enddate">EndDate of Events (Format: yyyy-MM-dd)</param>
        /// <param name="active">Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events, (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Events Filter (Refers to field SmgActive) Events Filter (possible Values: 'true' only published Events, 'false' only not published Events, (default:'null')</param>                
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed)</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="updatefrom">Date from Format (yyyy-MM-dd) (all GBActivityPoi with LastChange >= datefrom are passed), (default: null)</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null)</param>
        /// <returns>Collection of Event Objects</returns>         /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,ActivityReader")]
        //[Authorize]
        [HttpGet, Route("api/Event")]
        public async Task<IActionResult> GetEventList(
            string? language = null,
            uint pagenumber = 1,
            uint pagesize = 10,
            string? idlist = null,
            string? locfilter = null,
            string? rancfilter = null,
            string? typefilter = null,
            string? topicfilter = null,
            string? orgfilter = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? begindate = null,
            string? enddate = null,
            string? sort = null,
            string? lastchange = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))]
            string[]? fields = null,
            string? searchfilter = null,
            CancellationToken cancellationToken = default)
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(latitude, longitude, radius);

            return await GetFiltered(
                    fields: fields ?? Array.Empty<string>(), language: language, pagenumber: pagenumber,
                    pagesize: pagesize, typefilter: typefilter, idfilter: idlist, rancfilter: rancfilter,
                    searchfilter: searchfilter, locfilter: locfilter, topicfilter: topicfilter, orgfilter: orgfilter,
                    begindate: begindate, enddate: enddate, sort: sort, active: active,
                    smgactive: odhactive, smgtags: odhtagfilter, seed: seed, lastchange: lastchange,
                    geosearchresult: geosearchresult, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// GET Event Single 
        /// </summary>
        /// <param name="id">ID of the Event</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed)</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <returns>Event Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        /// //[Authorize(Roles = "DataReader,ActivityReader")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("api/Event/{id}")]
        public async Task<IActionResult> GetEventSingle(
            string id, 
            string? language, 
            [ModelBinder(typeof(CommaSeparatedArrayBinder))]
            string[]? fields = null, 
            CancellationToken cancellationToken = default)
        {
            return await GetSingle(id, language, fields: fields ?? Array.Empty<string>(), cancellationToken);
        }

        /// <summary>
        /// GET Event Topic List
        /// </summary>
        /// <returns>Collection of EventTypes Object</returns>                
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        //[CacheOutputUntilToday(23, 59)]
        [ProducesResponseType(typeof(IEnumerable<EventTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,ActivityReader")]
        [HttpGet, Route("api/EventTopics")]
        public async Task<IActionResult> GetAllEventTopicListAsync(CancellationToken cancellationToken)
        {
            return await GetEventTopicListAsync(cancellationToken);
        }

        /// <summary>
        /// GET Event Topic Single
        /// </summary>
        /// <returns>EventTypes Object</returns>                
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        //[CacheOutputUntilToday(23, 59)]
        [ProducesResponseType(typeof(EventTypes), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,ActivityReader")]
        [HttpGet, Route("api/EventTopics/{id}")]
        public async Task<IActionResult> GetAllEventTopicSingleAsync(string id, CancellationToken cancellationToken)
        {
            return await GetEventTopicSingleAsync(id, cancellationToken);
        }

        #endregion

        #region GETTER

            private Task<IActionResult> GetFiltered(
            string[] fields, string? language, uint pagenumber, uint pagesize, string? typefilter, string? idfilter,
            string? rancfilter, string? searchfilter, string? locfilter, string? orgfilter, string? topicfilter, string? begindate, string? enddate,
            string? sort, bool? active, bool? smgactive, string? smgtags, string? seed, string? lastchange, PGGeoSearchResult geosearchresult, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                EventHelper myeventhelper = await EventHelper.CreateAsync(
                    QueryFactory, idfilter, locfilter, rancfilter, typefilter, topicfilter, orgfilter, begindate, enddate,
                    active, smgactive, smgtags, lastchange,
                    cancellationToken);


                string sortifseednull = "data #>>'\\{Shortname\\}' ASC";

                if(sort != null)
                {
                    if (sort.ToLower() == "asc")
                        sortifseednull = "nextbegindate ASC";
                    else
                        sortifseednull = "nextbegindate DESC";

                    //Set seed to null
                    seed = null;
                }

                var query =
                    QueryFactory.Query()
                        .SelectRaw("data")
                        .From("events")
                        .EventWhereExpression(
                            idlist: myeventhelper.idlist, typelist: myeventhelper.typeidlist,
                            ranclist: myeventhelper.rancidlist, orglist: myeventhelper.orgidlist,
                            smgtaglist: myeventhelper.smgtaglist, districtlist: myeventhelper.districtlist,
                            municipalitylist: myeventhelper.municipalitylist, tourismvereinlist: myeventhelper.tourismvereinlist,
                            regionlist: myeventhelper.regionlist, topiclist: myeventhelper.topicrids,
                            begindate: myeventhelper.begin, enddate: myeventhelper.end,
                            activefilter: myeventhelper.active, smgactivefilter: myeventhelper.smgactive,
                            searchfilter: searchfilter, language: language, lastchange: myeventhelper.lastchange, languagelist: new List<string>(),
                            filterClosedData: FilterClosedData)
                        .OrderBySeed(ref seed, sortifseednull)
                        .GeoSearchFilterAndOrderby(geosearchresult);

                // Get paginated data
                var data =
                    await query
                        .PaginateAsync<JsonRaw>(
                            page: (int)pagenumber,
                            perPage: (int)pagesize);

                var dataTransformed =
                    data.List.Select(
                        raw => raw.TransformRawData(language, fields, checkCC0: FilterCC0License, filterClosedData: FilterClosedData)
                    );

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                return ResponseHelpers.GetResult(
                    pagenumber,
                    totalpages,
                    totalcount,
                    seed,
                    dataTransformed,
                    Url);
            });
        }

        /// <summary>
        /// GET Single Event
        /// </summary>
        /// <param name="id">ID of the Event</param>
        /// <returns>Event Object</returns>
        private Task<IActionResult> GetSingle(string id, string? language, string[] fields, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                var query =
                    QueryFactory.Query("events")
                        .Select("data")
                        .Where("id", id)
                        .When(FilterClosedData, q => q.FilterClosedData());

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data?.TransformRawData(language, fields, checkCC0: FilterCC0License, filterClosedData: FilterClosedData);
            });
        }

        #endregion

        #region CUSTOM METHODS

        /// <summary>
        /// GET Event Topics List
        /// </summary>
        /// <returns>Collection of EventTypes Object</returns>
        private Task<IActionResult> GetEventTopicListAsync(CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                var query =
                    QueryFactory.Query("eventtypes")
                        .SelectRaw("data")
                        .When(FilterClosedData, q => q.FilterClosedData());

                var data = await query.GetAsync<JsonRaw?>();

                return data;
            });
        }

        /// <summary>
        /// GET Event Topic Single
        /// </summary>
        /// <returns>EventTypes Object</returns>
        private Task<IActionResult> GetEventTopicSingleAsync(string id, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                var query =
                    QueryFactory.Query("eventtypes")
                        .Select("data")
                        .Where("id",id.ToUpper())
                        .When(FilterClosedData, q => q.FilterClosedData());                

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data;
            });
        }

        #endregion
    }
}