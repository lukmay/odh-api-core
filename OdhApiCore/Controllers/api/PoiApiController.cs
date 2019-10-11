﻿using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// Poi Api (data provided by LTS PoiData) SOME DATA Available as OPENDATA 
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class PoiController : OdhController
    {
        public PoiController(ISettings settings, IPostGreSQLConnectionFactory connectionFactory)
            : base(settings, connectionFactory)
        {
        }

        public static bool CheckOpenData(IPrincipal currentuser)
        {          
            List<string> roles = new List<string>() { "DataReader", "PoiReader" };

            return roles.Any(x => currentuser.IsInRole(x));                
        }

        #region SWAGGER Exposed API

        //Standard GETTER
        /// <summary>
        /// GET Poi List
        /// </summary>
        /// <param name="pagenumber">Pagenumber, (default:1)</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="poitype">Type of the Poi ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256), (default:'511' == ALL), REFERENCE TO: GET /api/PoiTypes </param>
        /// <param name="subtype">Subtype of the Poi ('null' = Filter disabled, available Subtypes depends on the poitype BITMASK), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Activity IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), 'null' = No Filter), (default:'null')</param>
        /// <param name="areafilter">AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')</param>
        /// <param name="highlight">Highlight Filter (Show only Highlights possible values: 'true' : show only Highlight Pois, 'null' Filter disabled), (default:'null')</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'api/ODHTag?validforentity=poi'), (default:'null')</param>        
        /// <param name="active">Active Pois Filter (possible Values: 'true' only Active Pois, 'false' only Disabled Pois, (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Pois Filter (Refers to field SmgActive) Pois Filter (possible Values: 'true' only published Pois, 'false' only not published Pois, (default:'null')</param>        
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of LTSPoi Objects</returns>        
        [ProducesResponseType(typeof(IEnumerable<GBLTSPoi>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]        
        [HttpGet, Route("api/Poi")]
        public async Task<IActionResult> GetPoiList(
            string? language = null,
            uint pagenumber = 1,
            uint pagesize = 10,
            string? poitype = "511",
            string? subtype = null,
            string? idlist = null,
            string? areafilter = null,
            bool? highlight = null,
            string? locfilter = null,
            string? odhtagfilter = null,
            bool? active = null,
            bool? odhactive = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))]
            string[]? fields = null,
            CancellationToken cancellationToken = default)
        {
            //TODO
            //CheckOpenData(User);

            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(latitude, longitude, radius);

            return await GetFiltered(
                fields ?? new string[] { }, language, pagenumber, pagesize, poitype, subtype, idlist,
                locfilter, areafilter, highlight, active, odhactive, odhtagfilter, seed,
                geosearchresult, cancellationToken);
        }

        /// <summary>
        /// GET Poi Single 
        /// </summary>
        /// <param name="id">ID of the Poi</param>
        /// <returns>LTSPoi Object</returns>
        [ProducesResponseType(typeof(GBLTSPoi), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,PoiReader")]        
        [HttpGet, Route("api/Poi/{id}")]
        public async Task<IActionResult> GetPoiSingle(string id, string? language, CancellationToken cancellationToken)
        {
            //TODO
            //CheckOpenData(User);

            return await GetSingle(id, language, cancellationToken);
        }

        //Reduced GETTER

        /// <summary>
        /// GET Poi List Reduced
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="poitype">Type of the Poi ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256), (default:'511' == ALL), REFERENCE TO: GET /api/PoiTypes </param>
        /// <param name="subtype">Subtype of the Activity (BITMASK Filter = available SubTypes depends on the selected poiType), (default:'null')</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), 'null' = No Filter), (default:'null')</param>
        /// <param name="areafilter">AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')</param>
        /// <param name="highlight">Hightlight Filter (possible values: 'false' = only Pois with Highlight false, 'true' = only Pois with Highlight true), (default:'null')</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'api/ODHTag?validforentity=poi'), (default:'null')</param>        
        /// <param name="active">Active Pois Filter (possible Values: 'true' only Active Pois, 'false' only Disabled Pois</param>
        /// <param name="odhactive">ODH Active (Published) Pois Filter (Refers to field SmgActive) Pois Filter (possible Values: 'true' only published Pois, 'false' only not published Pois, (default:'null')</param>        
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of Poi Reduced Objects</returns>        
        [ProducesResponseType(typeof(IEnumerable<ActivityPoiReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,PoiReader")]
        [HttpGet, Route("api/PoiReduced")]
        public async Task<IActionResult> GetPoiReduced(
            string? language = "en",
            string? poitype = "511",
            string? subtype = null,
            string? locfilter = null,
            string? areafilter = null,
            bool? highlight = null,
            string? odhtagfilter = null,
            bool? active = null,
            bool? odhactive = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            CancellationToken cancellationToken = default)
        {
            //TODO
            //CheckOpenData(User);

            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(latitude, longitude, radius);

            return await GetReduced(language, poitype, subtype, locfilter, areafilter, highlight, active, odhactive, odhtagfilter, geosearchresult, cancellationToken);
        }

        //Special GETTER

        /// <summary>
        /// GET Poi Types List
        /// </summary>
        /// <returns>Collection of PoiType Object</returns>                
        //[CacheOutputUntilToday(23, 59)]
        [ProducesResponseType(typeof(IEnumerable<PoiTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,PoiReader")]        
        [HttpGet, Route("api/PoiTypes")]
        public async Task<IActionResult> GetAllPoiTypesList(CancellationToken cancellationToken)
        {
            return await GetPoiTypesList(cancellationToken);
        }

        /// <summary>
        /// GET Poi Changed List by Date
        /// </summary>
        /// <param name="pagenumber">Pagenumber, (default:1)</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="updatefrom">Date from Format (yyyy-MM-dd) (all GBActivityPoi with LastChange >= datefrom are passed), (default: DateTime.Now - 1 Day)</param>
        /// <returns>Collection of LTSPoi Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<GBLTSPoi>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,PoiReader")]
        [HttpGet, Route("api/PoiChanged")]
        public async Task<IActionResult> GetAllPoisChanged(
            uint pagenumber = 1,
            uint pagesize = 10,
            string? seed = null,
            string? updatefrom = null,
            CancellationToken cancellationToken = default
            )
        {
            //TODO
            //CheckOpenData(User);

            updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

            return await GetLastChanged(pagenumber, pagesize, updatefrom, seed, cancellationToken);
        }

        #endregion

        #region GETTER
       
        /// <summary>
        /// GET Paged Filtered Pois List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page</param>
        /// <param name="activitytype">Type of the Poi (possible values: STRINGS: 'Ärzte, Apotheken','Geschäfte und Dienstleister','Kultur und Sehenswürdigkeiten','Nachtleben und Unterhaltung','Öffentliche Einrichtungen','Sport und Freizeit','Verkehr und Transport' : BITMASK also possible: 'Ärtze, Apotheken = 1','Geschäfte und Dienstleister = 2','Kultur und Sehenswürdigkeiten = 4','Nachtleben und Unterhaltung = 8','Öffentliche Einrichtungen = 16','Sport und Freizeit = 32','Verkehr und Transport = 64')</param>
        /// <param name="subtypefilter">Subtype of the Poi ('null' = Filter disabled, available Subtypes depends on the activitytype BITMASK)</param>
        /// <param name="idfilter">IDFilter (Separator ',' List of Activity IDs, 'null' = No Filter)</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = No Filter)</param>
        /// <param name="areafilter">AreaFilter (Separator ',' IDList of AreaIDs separated by ',', 'null' : Filter disabled)</param>
        /// <param name="highlightfilter">Highlight Filter (Show only Highlights possible values: 'true' : show only Highlight Pois, 'null' Filter disabled)</param>
        /// <param name="active">Active Filter (possible Values: 'null' Displays all Pois, 'true' only Active Pois, 'false' only Disabled Pois</param>
        /// <param name="smgactive">SMGActive Filter (possible Values: 'null' Displays all Pois, 'true' only SMG Active Pois, 'false' only SMG Disabled Pois</param>
        /// <param name="smgtags">SMGTag Filter (String, Separator ',' more SMGTags possible, 'null' = No Filter)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting</param>
        /// <returns>Result Object with Collection of Pois</returns>        
        private Task<IActionResult> GetFiltered(
            string[] fields, string? language, uint pagenumber, uint pagesize, string? activitytype, string? subtypefilter,
            string? idfilter, string? locfilter, string? areafilter, bool? highlightfilter, bool? active, bool? smgactive,
            string? smgtags, string? seed, PGGeoSearchResult geosearchresult, CancellationToken cancellationToken)
        {

            return DoAsyncReturnString(async connectionFactory =>
            {
                PoiHelper myactivityhelper = await PoiHelper.CreateAsync(
                    connectionFactory, activitytype, subtypefilter, idfilter, locfilter, areafilter,
                    highlightfilter, active, smgactive, smgtags, cancellationToken);

                string select = "*";
                string orderby = "";

                var (whereexpression, parameters) = PostgresSQLWhereBuilder.CreatePoiWhereExpression(
                    myactivityhelper.idlist, myactivityhelper.poitypelist, myactivityhelper.subtypelist,
                    myactivityhelper.smgtaglist, new List<string>(), new List<string>(), myactivityhelper.tourismvereinlist,
                    myactivityhelper.regionlist, myactivityhelper.arealist, myactivityhelper.highlight, myactivityhelper.active,
                    myactivityhelper.smgactive);

                //Build Orderby
                string? myseed = PostgresSQLOrderByBuilder.BuildSeedOrderBy(ref orderby, seed, "data ->>'Shortname' ASC");
                
                PostgresSQLHelper.ApplyGeoSearchWhereOrderby(ref whereexpression, ref orderby, geosearchresult);

                uint pageskip = pagesize * (pagenumber - 1);

                var dataTask = PostgresSQLHelper.SelectFromTableDataAsStringParametrizedAsync(
                    connectionFactory, "pois", select, (whereexpression, parameters), orderby, pagesize, pageskip,
                    cancellationToken);
                var count = await PostgresSQLHelper.CountDataFromTableParametrizedAsync(
                    connectionFactory, "pois", (whereexpression, parameters), cancellationToken);

                uint totalcount = count;
                uint totalpages = PostgresSQLHelper.PGPagingHelper(totalcount, pagesize);

                var data = dataTask.Select(raw => raw.TransformRawData(language, fields, checkCC0: CheckCC0License));

                return PostgresSQLHelper.GetResultJson(
                    pagenumber,
                    totalpages,
                    totalcount,
                    myseed,
                    await data.ToListAsync());
            });
        }

        /// <summary>
        /// GET Single Poi
        /// </summary>
        /// <param name="id">ID of Poi</param>
        /// <returns>Poi Object</returns>
        private Task<IActionResult> GetSingle(string id, string? language, CancellationToken cancellationToken)
        {
            return DoAsyncReturnString(async connectionFactory =>
            {
                var where = PostgresSQLWhereBuilder.CreateIdListWhereExpression(id.ToUpper());
                var data = await PostgresSQLHelper.SelectFromTableDataAsStringParametrizedAsync(
                    connectionFactory, "pois", "*", where, "", 0,
                    null, cancellationToken).ToListAsync();

                var result = data.FirstOrDefault()?.TransformRawData(language, new string[] { }, checkCC0: CheckCC0License);
                return result == null ? null : JsonConvert.SerializeObject(result);
            });
        }

        #endregion

        #region REDUCED GETTER

        /// <summary>
        /// GET Reduced POI List
        /// </summary>
        /// <param name="language">Localization Language</param>
        /// <param name="poitype">Type of the Poi (possible values: STRINGS: 'Ärtze, Apotheken','Geschäfte und Dienstleister','Kultur und Sehenswürdigkeiten','Nachtleben und Unterhaltung','Öffentliche Einrichtungen','Sport und Freizeit','Verkehr und Transport' : BITMASK also possible: 'Ärtze, Apotheken = 1','Geschäfte und Dienstleister = 2','Kultur und Sehenswürdigkeiten = 4','Nachtleben und Unterhaltung = 8','Öffentliche Einrichtungen = 16','Sport und Freizeit = 32','Verkehr und Transport = 64')</param>
        /// <param name="subtypefilter">Subtype of the Poi ('null' = Filter disabled, available Subtypes depends on the activitytype BITMASK)</param>        
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), 'null' = No Filter)</param>
        /// <param name="areafilter">AreaFilter (Separator ',' IDList of AreaIDs separated by ',', 'null' : Filter disabled)</param>
        /// <param name="highlightfilter">Highlight Filter (Show only Highlights possible values: 'true' : show only Highlight Pois, 'null' Filter disabled)</param>
        /// <param name="active">Active Filter (possible Values: 'null' Displays all Pois, 'true' only Active Pois, 'false' only Disabled Pois</param>
        /// <param name="smgactive">SMGActive Filter (possible Values: 'null' Displays all Pois, 'true' only SMG Active Pois, 'false' only SMG Disabled Pois</param>
        /// <param name="smgtags">SMGTag Filter (String, Separator ',' more SMGTags possible, 'null' = No Filter)</param>
        /// <returns>Collection of Reduced Poi Objects</returns>
        private Task<IActionResult> GetReduced(
            string? language, string? poitype, string? subtypefilter, string? locfilter, 
            string? areafilter, bool? highlightfilter, bool? active, bool? smgactive, 
            string? smgtags, PGGeoSearchResult geosearchresult, CancellationToken cancellationToken)
        {
            return DoAsyncReturnString(async connectionFactory =>
            {
                PoiHelper myactivityhelper = await PoiHelper.CreateAsync(
                    connectionFactory, poitype, subtypefilter, null, locfilter, areafilter, 
                    highlightfilter, active, smgactive, smgtags, cancellationToken);
                
                string select = $"data->'Id' as Id, data->'Detail'->' {language} '->'Title' as Name";
                string orderby = "data ->>'Shortname' ASC";

                var (whereexpression, parameters) = PostgresSQLWhereBuilder.CreatePoiWhereExpression(
                    myactivityhelper.idlist, myactivityhelper.poitypelist, myactivityhelper.subtypelist, 
                    myactivityhelper.smgtaglist, new List<string>(), new List<string>(), myactivityhelper.tourismvereinlist, 
                    myactivityhelper.regionlist, myactivityhelper.arealist, myactivityhelper.highlight,
                    myactivityhelper.active, myactivityhelper.smgactive);
                

                PostgresSQLHelper.ApplyGeoSearchWhereOrderby(ref whereexpression, ref orderby, geosearchresult);

                var data = await PostgresSQLHelper.SelectFromTableDataAsJsonParametrizedAsync(
                    connectionFactory, "pois", select, (whereexpression, parameters), orderby, 0, null,
                    new List<string>() { "Id", "Name" }, cancellationToken).ToListAsync();

                return JsonConvert.SerializeObject(data);
            });
        }

        #endregion

        #region CUSTOM METHODS

        /// <summary>
        /// GET Poi Types List (Localized Type Names and Bitmasks)
        /// </summary>
        /// <returns>Collection of PoiTypes Object</returns>
        private Task<IActionResult> GetPoiTypesList(CancellationToken cancellationToken)
        {
            return DoAsyncReturnString(async connectionFactory =>
            {
                List<PoiTypes> mysuedtiroltypeslist = new List<PoiTypes>();

                //Get LTS Tagging Types List
                var ltstaggingtypes = PostgresSQLHelper.SelectFromTableDataAsObjectAsync<LTSTaggingType>(
                    connectionFactory, "ltstaggingtypes", "*", "", "", 0, null, cancellationToken);
                
                foreach (PoiTypeFlag myactivitytype in EnumHelper.GetValues<PoiTypeFlag>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();

                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeFlag>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;
                    
                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }

                //Ärzte, Apotheken Types
                foreach (PoiTypeAerzteApotheken myactivitytype in EnumHelper.GetValues<PoiTypeAerzteApotheken>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();

                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Ärzte, Apotheken";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeAerzteApotheken>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }

                //Geschäfte Types
                foreach (PoiTypeGeschaefte myactivitytype in EnumHelper.GetValues<PoiTypeGeschaefte>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Geschäfte";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofTypeLong<PoiTypeGeschaefte>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Kultur und Sehenswürdigkeiten Types
                foreach (PoiTypeKulturSehenswuerdigkeiten myactivitytype in EnumHelper.GetValues<PoiTypeKulturSehenswuerdigkeiten>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Kultur und Sehenswürdigkeiten";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeKulturSehenswuerdigkeiten>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Nachtleben und Unterhaltung Types
                foreach (PoiTypeNachtlebenUnterhaltung myactivitytype in EnumHelper.GetValues<PoiTypeNachtlebenUnterhaltung>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Nachtleben und Unterhaltung";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeNachtlebenUnterhaltung>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Öffentliche Einrichtungen Types
                foreach (PoiTypeOeffentlicheEinrichtungen myactivitytype in EnumHelper.GetValues<PoiTypeOeffentlicheEinrichtungen>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Öffentliche Einrichtungen";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeOeffentlicheEinrichtungen>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Sport und Freizeit Types
                foreach (PoiTypeSportFreizeit myactivitytype in EnumHelper.GetValues<PoiTypeSportFreizeit>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Sport und Freizeit";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofTypeLong<PoiTypeSportFreizeit>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Verkehr und Transport Types
                foreach (PoiTypeVerkehrTransport myactivitytype in EnumHelper.GetValues<PoiTypeVerkehrTransport>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Verkehr und Transport";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeVerkehrTransport>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Dienstleister Types
                foreach (PoiTypeDienstleister myactivitytype in EnumHelper.GetValues<PoiTypeDienstleister>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Dienstleister";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofTypeLong<PoiTypeDienstleister>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }
                //Kunsthandwerker Types
                foreach (PoiTypeHandwerk myactivitytype in EnumHelper.GetValues<PoiTypeHandwerk>())
                {
                    PoiTypes mysmgpoitype = new PoiTypes();

                    string? id = myactivitytype.GetDescription();
                    mysmgpoitype.Id = id;
                    mysmgpoitype.Type = "PoiSubType"; // +mysuedtiroltype.TypeParent;
                    mysmgpoitype.Parent = "Kunsthandwerker";

                    mysmgpoitype.Bitmask = FlagsHelper.GetFlagofType<PoiTypeHandwerk>(id);

                    mysmgpoitype.TypeDesc = await Helper.LTSTaggingHelper.GetPoiTypeDescAsync(id, ltstaggingtypes) as Dictionary<string, string>;

                    mysuedtiroltypeslist.Add(mysmgpoitype);
                }

                return JsonConvert.SerializeObject(mysuedtiroltypeslist);
            });
        }

        /// <summary>
        /// GET Paged Poi List based on LastChange Date
        /// </summary>        
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page</param>
        /// <param name="updatefrom">Date from (all Activity with LastChange >= datefrom are passed)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting</param>
        /// <returns>Result Object with Collection of Poi Objects</returns>
        private Task<IActionResult> GetLastChanged(
            uint pagenumber, uint pagesize, string updatefrom, 
            string? seed, CancellationToken cancellationToken)
        {        
            return DoAsyncReturnString(async connectionFactory =>
            {
                DateTime updatefromDT = Convert.ToDateTime(updatefrom);

                string select = "*";
                string orderby = "";

                string? myseed = PostgresSQLOrderByBuilder.BuildSeedOrderBy(ref orderby, seed, "data ->>'Shortname' ASC");

                uint pageskip = pagesize * (pagenumber - 1);

                var where = PostgresSQLWhereBuilder.CreateLastChangedWhereExpression(updatefrom);

                var dataTask = PostgresSQLHelper.SelectFromTableDataAsStringParametrizedAsync(
                    connectionFactory, "pois", select, where, orderby, pagesize, pageskip,
                    cancellationToken);
                var count = await PostgresSQLHelper.CountDataFromTableParametrizedAsync(
                    connectionFactory, "pois", where, cancellationToken);

                uint totalcount = count;
                uint totalpages = PostgresSQLHelper.PGPagingHelper(totalcount, pagesize);

                return PostgresSQLHelper.GetResultJson(
                    pagenumber, totalpages, totalcount, -1, myseed, await dataTask.ToListAsync());
            });
        }

        #endregion

        //#region POST PUT DELETE

        ///// <summary>
        ///// POST Insert new Poi
        ///// </summary>
        ///// <param name="poi">Poi Object</param>
        ///// <returns>HttpResponseMessage</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,PoiManager,PoiCreate")]
        //[HttpPost, Route("api/Poi")]
        //public HttpResponseMessage Post([FromBody]GBLTSPoi poi)
        //{
        //    try
        //    {
        //        if (poi != null)
        //        {
        //            using (var conn = new NpgsqlConnection(GlobalPGConnection.PGConnectionString))
        //            {

        //                conn.Open();

        //                PostgresSQLHelper.InsertDataIntoTable(conn, "pois", JsonConvert.SerializeObject(poi), poi.Id.ToUpper());

        //                return Request.CreateResponse(HttpStatusCode.Created, new GenericResult() { Message = "Insert Poi succeeded, Id:" + poi.Id.ToUpper() }, "application/json");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("No Poi Data provided");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new GenericResult() { Message = ex.Message }, "application/json");
        //    }
        //}

        ///// <summary>
        ///// PUT Modify existing Poi
        ///// </summary>
        ///// <param name="id">Poi Id</param>
        ///// <param name="poi">Poi Object</param>
        ///// <returns>HttpResponseMessage</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,PoiManager,PoiModify")]
        //[HttpPut, Route("api/Poi/{id}")]
        //public HttpResponseMessage Put(string id, [FromBody]GBLTSPoi poi)
        //{
        //    try
        //    {
        //        if (poi != null && id != null)
        //        {
        //            using (var conn = new NpgsqlConnection(GlobalPGConnection.PGConnectionString))
        //            {

        //                conn.Open();

        //                PostgresSQLHelper.UpdateDataFromTable(conn, "pois", JsonConvert.SerializeObject(poi), poi.Id.ToUpper());

        //                return Request.CreateResponse(HttpStatusCode.Created, new GenericResult() { Message = "UPDATE Poi succeeded, Id:" + poi.Id.ToUpper() }, "application/json");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("No Poi Data provided");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new GenericResult() { Message = ex.Message }, "application/json");
        //    }
        //}

        ///// <summary>
        ///// DELETE Poi by Id
        ///// </summary>
        ///// <param name="id">Poi Id</param>
        ///// <returns>HttpResponseMessage</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,PoiManager,PoiDelete")]
        //[HttpDelete, Route("api/Poi/{id}")]
        //public HttpResponseMessage Delete(string id)
        //{
        //    try
        //    {
        //        if (id != null)
        //        {

        //            using (var conn = new NpgsqlConnection(GlobalPGConnection.PGConnectionString))
        //            {

        //                conn.Open();

        //                PostgresSQLHelper.DeleteDataFromTable(conn, "pois", id.ToUpper());

        //                return Request.CreateResponse(HttpStatusCode.Created, new GenericResult() { Message = "DELETE Poi succeeded, Id:" + id.ToUpper() }, "application/json");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("No Poi Id provided");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new GenericResult() { Message = ex.Message }, "application/json");
        //    }
        //}

        //#endregion       
    }

}