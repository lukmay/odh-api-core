﻿using DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class ODHTypeHelper
    {
        #region TypeObject2TypeStringANDPGTable
     
        /// <summary>
        /// Translates a ODH Type Object to the Type (Metadata) as String
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string TranslateType2TypeString<T>(T odhtype)
        {
            return odhtype switch
            {
                Accommodation or AccommodationLinked => "accommodation",
                AccoRoom or AccommodationRoomLinked => "accommodationroom",
                GBLTSActivity or LTSActivityLinked => "ltsactivity",
                GBLTSPoi or LTSPoiLinked => "ltspoi",
                Gastronomy or GastronomyLinked => "ltsgastronomy",
                Event or EventLinked => "event",
                ODHActivityPoi or ODHActivityPoiLinked => "odhactivitypoi",
                Package or PackageLinked => "package",
                Measuringpoint or MeasuringpointLinked => "measuringpoint",
                WebcamInfo or WebcamInfoLinked => "webcam",
                Article or ArticlesLinked => "article",
                DDVenue => "venue",
                EventShort or EventShortLinked => "eventshort",
                ExperienceArea or ExperienceAreaLinked => "experiencearea",
                MetaRegion or MetaRegionLinked => "metaregion",
                Region or RegionLinked => "region",
                Tourismverein or TourismvereinLinked => "tourismassociation",
                Municipality or MunicipalityLinked => "municipality",
                District or DistrictLinked => "district",
                SkiArea or SkiAreaLinked => "skiarea",
                SkiRegion or SkiRegionLinked => "skiregion",
                Area or AreaLinked => "area",
                Wine or WineLinked => "wineaward",
                SmgTags or ODHTagLinked => "odhtag",
                _ => throw new Exception("not known odh type")
            };
        }

        /// <summary>
        /// Translates ODH Type Object to PG Table Name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string TranslateType2Table<T>(T odhtype)
        {
            return odhtype switch
            {
                Accommodation or AccommodationLinked => "accommodations",
                AccoRoom or AccommodationRoomLinked => "accommodationrooms",
                GBLTSActivity or LTSActivityLinked => "activities",
                GBLTSPoi or LTSPoiLinked => "pois",
                Gastronomy or GastronomyLinked => "gastronomies",
                Event or EventLinked => "events",
                ODHActivityPoi or ODHActivityPoiLinked => "smgpois",
                Package or PackageLinked => "packages",
                Measuringpoint or MeasuringpointLinked => "measuringpoints",
                WebcamInfo or WebcamInfoLinked => "webcams",
                Article or ArticlesLinked => "articles",
                DDVenue => "venues",
                EventShort or EventShortLinked => "eventeuracnoi",
                ExperienceArea or ExperienceAreaLinked => "experienceareas",
                MetaRegion or MetaRegionLinked => "metaregions",
                Region or RegionLinked => "regions",
                Tourismverein or TourismvereinLinked => "tvs",
                Municipality or MunicipalityLinked => "municipalities",
                District or DistrictLinked => "districts",
                SkiArea or SkiAreaLinked => "skiareas",
                SkiRegion or SkiRegionLinked => "skiregions",
                Area or AreaLinked => "areas",
                Wine or WineLinked => "wines",
                ODHTags or ODHTagLinked => "smgtags",
                _ => throw new Exception("not known odh type")
            };
        }

        #endregion

        #region TypeString2TypeObjectANDPGTable

        /// <summary>
        /// Translates Type (Metadata) as String to PG Table Name
        /// </summary>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string TranslateTypeString2Table(string odhtype)
        {
            return odhtype switch
            {
                "accommodation" => "accommodations",
                "accommodationroom" => "accommodationrooms",
                "ltsactivity" => "activities",
                "ltspoi" => "pois",
                "ltsgastronomy" => "gastronomies",
                "event" => "events",
                "odhactivitypoi" => "smgpois",
                "package" => "packages",
                "measuringpoint" => "measuringpoints",
                "webcam" => "webcams",
                "article" => "articles",
                "venue" => "venues",
                "eventshort" => "eventeuracnoi",
                "experiencearea" => "experienceareas",
                "metaregion" => "metaregions",
                "region" => "regions",
                "tourismassociation" => "tvs",
                "municipality" => "municipalities",
                "district" => "districts",
                "skiarea" => "skiareas",
                "skiregion" => "skiregions",
                "area" => "areas",
                "wineaward" => "wines",
                "odhtag" => "smgtags",
                _ => throw new Exception("not known odh type")
            };
        }

        /// <summary>
        /// Translates Type (Metadata) as String to Type Object
        /// </summary>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static Type TranslateTypeString2Type(string odhtype)
        {
            return odhtype switch
            {
                "accommodation" => typeof(AccommodationLinked),
                "accommodationroom" => typeof(AccommodationRoomLinked),
                "ltsactivity" => typeof(LTSActivityLinked),
                "pois" => typeof(LTSPoiLinked),
                "ltsgastronomy" => typeof(GastronomyLinked),
                "event" => typeof(EventLinked),
                "odhactivitypoi" => typeof(ODHActivityPoiLinked),
                "package" => typeof(PackageLinked),
                "measuringpoint" => typeof(MeasuringpointLinked),
                "webcam" => typeof(WebcamInfoLinked),
                "article" => typeof(ArticlesLinked),
                "venue" => typeof(DDVenue),
                "eventshort" => typeof(EventShortLinked),
                "experiencearea" => typeof(ExperienceAreaLinked),
                "metaregion" => typeof(MetaRegionLinked),
                "region" => typeof(RegionLinked),
                "tourismassociation" => typeof(TourismvereinLinked),
                "municipality" => typeof(MunicipalityLinked),
                "district" => typeof(DistrictLinked),
                "skiarea" => typeof(SkiAreaLinked),
                "skiregion" => typeof(SkiRegionLinked),
                "area" => typeof(AreaLinked),
                "wineaward" => typeof(WineLinked),
                "odhtag" => typeof(ODHTagLinked),
                _ => throw new Exception("not known odh type")
            };
        }

        #endregion

        #region PGTable2TypeObjectANDString

        /// <summary>
        /// Translates PG Table Name to Type (Metadata) as String
        /// </summary>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string TranslateTable2TypeString(string odhtype)
        {
            return odhtype switch
            {
                "accommodations" => "accommodation",
                "accommodationrooms" => "accommodationroom",
                "activities" => "ltsactivity",
                "pois" => "ltspoi",
                "gastronomies" => "ltsgastronomy",
                "events" => "event",
                "smgpois" => "odhactivitypoi",
                "packages" => "package",
                "measuringpoints" => "measuringpoint",
                "webcams" => "webcam",
                "articles" => "article",
                "venues" => "venue",
                "eventeuracnoi" => "eventshort",
                "experienceareas" => "experiencearea",
                "metaregions" => "metaregion",
                "regions" => "region",
                "tvs" => "tourismassociation",
                "municipalities" => "municipality",
                "districts" => "district",
                "skiareas" => "skiarea",
                "skiregions" => "skiregion",
                "areas" => "area",
                "wines" => "wineaward",
                "smgtags" => "odhtag",
                _ => throw new Exception("not known odh type")
            };
        }

        /// <summary>
        /// Translates PG Table Name to Type Object
        /// </summary>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static Type TranslateTable2Type(string odhtype)
        {
            return odhtype switch
            {
                "accommodations" => typeof(AccommodationLinked),
                "accommodationrooms" => typeof(AccommodationRoomLinked),
                "activities" => typeof(LTSActivityLinked),
                "pois" => typeof(LTSPoiLinked),
                "gastronomies" => typeof(GastronomyLinked),
                "events" => typeof(EventLinked),
                "smgpois" => typeof(ODHActivityPoiLinked),
                "packages" => typeof(PackageLinked),
                "measuringpoints" => typeof(MeasuringpointLinked),
                "webcams" => typeof(WebcamInfoLinked),
                "articles" => typeof(ArticlesLinked),
                "venues" => typeof(DDVenue),
                "eventeuracnoi" => typeof(EventShortLinked),
                "experienceareas" => typeof(ExperienceAreaLinked),
                "metaregions" => typeof(MetaRegionLinked),
                "regions" => typeof(RegionLinked),
                "tvs" => typeof(TourismvereinLinked),
                "municipalities" => typeof(MunicipalityLinked),
                "districts" => typeof(DistrictLinked),
                "skiareas" => typeof(SkiAreaLinked),
                "skiregions" => typeof(SkiRegionLinked),
                "areas" => typeof(AreaLinked),
                "wines" => typeof(WineLinked),
                "smgtags" => typeof(ODHTagLinked),
                _ => throw new Exception("not known table name")
            };
        }

        #endregion

        #region Type2MetaGeneratorFunction

        //TODO Migrate from Metagenerator

        #endregion

        #region TypeIdConverter

        public static string ConvertIdbyTypeString(string odhtype, string id)
        {
            return odhtype switch
            {
                "accommodation" => id.ToUpper(),
                "accommodationroom" => id.ToUpper(),
                "ltsactivity" => id.ToUpper(),
                "ltspoi" => id.ToUpper(),
                "ltsgastronomy" => id.ToUpper(),
                "event" => id.ToUpper(),
                "odhactivitypoi" => id.ToLower(),
                "package" => id.ToUpper(),
                "measuringpoint" => id.ToUpper(),
                "webcam" => id.ToUpper(),
                "article" => id.ToUpper(),
                "venue" => id.ToUpper(),
                "eventshort" => id.ToLower(),
                "experiencearea" => id.ToUpper(),
                "metaregion" => id.ToUpper(),
                "region" => id.ToUpper(),
                "tourismassociation" => id.ToUpper(),
                "municipality" => id.ToUpper(),
                "district" => id.ToUpper(),
                "skiarea" => id.ToUpper(),
                "skiregion" => id.ToUpper(),
                "area" => id.ToUpper(),
                "wineaward" => id.ToUpper(),
                "odhtag" => id.ToLower(),
                _ => throw new Exception("not known odh type")
            };
        }

        #endregion

        #region JsonRaw2Type

        public static IIdentifiable ConvertJsonRawToObject(string odhtype, JsonRaw raw)
        {
            return odhtype switch
            {
                "accommodation" => JsonConvert.DeserializeObject<AccommodationLinked>(raw.Value),
                "accommodationroom" => JsonConvert.DeserializeObject<AccommodationRoomLinked>(raw.Value),
                "ltsactivity" => JsonConvert.DeserializeObject<LTSActivityLinked>(raw.Value),
                "ltspoi" => JsonConvert.DeserializeObject<LTSPoiLinked>(raw.Value),
                "ltsgastronomy" => JsonConvert.DeserializeObject<GastronomyLinked>(raw.Value),
                "event" => JsonConvert.DeserializeObject<EventLinked>(raw.Value),
                "odhactivitypoi" => JsonConvert.DeserializeObject<ODHActivityPoiLinked>(raw.Value),
                "package" => JsonConvert.DeserializeObject<PackageLinked>(raw.Value),
                "measuringpoint" => JsonConvert.DeserializeObject<MeasuringpointLinked>(raw.Value),
                "webcam" => JsonConvert.DeserializeObject<WebcamInfoLinked>(raw.Value),
                "article" => JsonConvert.DeserializeObject<ArticlesLinked>(raw.Value),
                "venue" => JsonConvert.DeserializeObject<DDVenue>(raw.Value),
                "eventshort" => JsonConvert.DeserializeObject<EventShortLinked>(raw.Value),
                "experiencearea" => JsonConvert.DeserializeObject<ExperienceAreaLinked>(raw.Value),
                "metaregion" => JsonConvert.DeserializeObject<MetaRegionLinked>(raw.Value),
                "region" => JsonConvert.DeserializeObject<RegionLinked>(raw.Value),
                "tourismassociation" => JsonConvert.DeserializeObject<TourismvereinLinked>(raw.Value),
                "municipality" => JsonConvert.DeserializeObject<MunicipalityLinked>(raw.Value),
                "district" => JsonConvert.DeserializeObject<DistrictLinked>(raw.Value),
                "skiarea" => JsonConvert.DeserializeObject<SkiAreaLinked>(raw.Value),
                "skiregion" => JsonConvert.DeserializeObject<SkiRegionLinked>(raw.Value),
                "area" => JsonConvert.DeserializeObject<AreaLinked>(raw.Value),
                "wineaward" => JsonConvert.DeserializeObject<WineLinked>(raw.Value),
                "odhtag" => JsonConvert.DeserializeObject<ODHTagLinked>(raw.Value),
                _ => throw new Exception("not known odh type")
            };
        }

        #endregion

        #region Search

        /// <summary>
        /// Gets all ODH Types to search on
        /// </summary>
        /// <param name="getall"></param>
        /// <returns></returns>
        public static string[] GetAllSearchableODHTypes(bool getall)
        {
            var odhtypes = new List<string>();

            odhtypes.Add("accommodation");
            odhtypes.Add("odhactivitypoi");
            odhtypes.Add("event");
            odhtypes.Add("region");
            odhtypes.Add("skiarea");
            odhtypes.Add("tourismassociation");
            odhtypes.Add("webcam");
            odhtypes.Add("venue");

            if (getall)
            {
                odhtypes.Add("accommodationroom");
                odhtypes.Add("package");
                odhtypes.Add("ltsactivity");
                odhtypes.Add("ltspoi");
                odhtypes.Add("ltsgastronomy");
                odhtypes.Add("measuringpoint");
                odhtypes.Add("article");
                odhtypes.Add("municipality");
                odhtypes.Add("district");
                odhtypes.Add("skiregion");
                odhtypes.Add("eventshort");
                odhtypes.Add("experiencearea");
                odhtypes.Add("metaregion");
                odhtypes.Add("area");
                odhtypes.Add("wineaward");
            }

            return odhtypes.ToArray();
        }


        public static Func<string, string[]> TranslateTypeToSearchField(string odhtype)
        {
            return odhtype switch
            {
                "accommodation" => PostgresSQLWhereBuilder.AccoTitleFieldsToSearchFor,
                "accommodationroom" => PostgresSQLWhereBuilder.AccoRoomNameFieldsToSearchFor,
                "ltsactivity" or "ltspoi" or "ltsgastronomy" or "event" or "odhactivitypoi" or "metaregion" or "region" or "tourismassociation" or "municipality"
                or "district" or "skiarea" or "skiregion" or "article" or "experiencearea"
                => PostgresSQLWhereBuilder.TitleFieldsToSearchFor,
                //"measuringpoint" => PostgresSQLWhereBuilder.,
                "webcam" => PostgresSQLWhereBuilder.WebcamnameFieldsToSearchFor,
                "venue" => PostgresSQLWhereBuilder.VenueTitleFieldsToSearchFor,
                //"eventshort" => "eventeuracnoi",           
                //"area" => "areas",
                //"wineaward" => "wines",
                _ => throw new Exception("not known odh type")
            };
        }

        #endregion


        //public static Func<string, string[]> TranslateTypeToSearchField(string odhtype, bool searchontext = false)
        //{
        //    return odhtype switch
        //    {
        //        "accommodation" => !searchontext ? PostgresSQLWhereBuilder.AccoTitleFieldsToSearchFor : AddToStringArray(PostgresSQLWhereBuilder.AccoTitleFieldsToSearchFor, "en"),
        //        "accommodationroom" => PostgresSQLWhereBuilder.AccoRoomNameFieldsToSearchFor,
        //        "ltsactivity" or "ltspoi" or "ltsgastronomy" or "event" or "odhactivitypoi" or "metaregion" or "region" or "tourismassociation" or "municipality"
        //        or "district" or "skiarea" or "skiregion" or "article" or "experiencearea"
        //        => PostgresSQLWhereBuilder.TitleFieldsToSearchFor,
        //        //"measuringpoint" => PostgresSQLWhereBuilder.,
        //        "webcam" => PostgresSQLWhereBuilder.WebcamnameFieldsToSearchFor,
        //        "venue" => PostgresSQLWhereBuilder.VenueTitleFieldsToSearchFor,
        //        //"eventshort" => "eventeuracnoi",           
        //        //"area" => "areas",
        //        //"wineaward" => "wines",
        //        _ => throw new Exception("not known odh type")
        //    };
        //}



        public static string TranslateTypeToTitleField(string odhtype, string language)
        {
            return odhtype switch
            {
                "accommodation" => $"AccoDetail.{language}.Name",
                "accommodationroom" => $"AccoRoomDetail.{language}.Name",
                "ltsactivity" or "ltspoi" or "ltsgastronomy" or "event" or "odhactivitypoi" or "metaregion" or "region" or "tourismassociation" or "municipality"
                or "district" or "skiarea" or "skiregion" or "article" or "experiencearea"
                => $"Detail.{language}.Title",
                "measuringpoint" => $"Shortname",
                "webcam" => $"Webcamname.{language}",
                "venue" => $"attributes.name.{PostgresSQLWhereBuilder.TransformLanguagetoDDStandard(language)}",
                //"eventshort" => "eventeuracnoi",           
                //"area" => "areas",
                //"wineaward" => "wines",
                _ => throw new Exception("not known odh type")
            };
        }

        public static string TranslateTypeToBaseTextField(string odhtype, string language)
        {
            return odhtype switch
            {
                "accommodation" => $"AccoDetail.{language}.Longdesc",
                "accommodationroom" => $"AccoRoomDetail.{language}.Longdesc",
                "ltsactivity" or "ltspoi" or "ltsgastronomy" or "event" or "odhactivitypoi" or "metaregion" or "region" or "tourismassociation" or "municipality"
                or "district" or "skiarea" or "skiregion" or "article" or "experiencearea"
                => $"Detail.{language}.BaseText",
                "measuringpoint" => "notextfield",
                "webcam" => "notextfield",
                "venue" => "notextfield",
                //"eventshort" => "eventeuracnoi",           
                //"area" => "areas",
                //"wineaward" => "wines",
                _ => throw new Exception("not known odh type")
            };
        }
    }
}
