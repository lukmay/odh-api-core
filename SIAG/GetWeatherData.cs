﻿using DataModel;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SIAG
{
    public class GetWeatherData
    {
        //public const string source = "siag";
        public const string source = "opendata";

        #region Weather general
 
        //Gets SIAG Weather Data (RAW DATA)
        public static async Task<string> GetSiagWeatherData(string lang, string siaguser, string siagpswd, bool json, string? id = null)
        {
            //Request on Siag Service
            HttpResponseMessage weatherresponse = await GetWeatherFromSIAG.RequestAsync(lang, siaguser, siagpswd, source, json, id);
            //Reading Content and returning
            return await weatherresponse.Content.ReadAsStringAsync();
        }

        //Parses to ODH Format
        public static Task<Weather> ParseSiagWeatherDataToODHWeather(string lang, string xmldir, string weatherresponsetask, bool json)
        {
            var weatherinfo = XDocument.Load(xmldir + "Weather.xml");

            if (json)
            {
                return Task.FromResult(ParseWeather.ParsemyWeatherJsonResponse(lang, weatherinfo, weatherresponsetask));
            }

            XDocument myweatherresponse = XDocument.Parse(weatherresponsetask);
            return Task.FromResult(ParseWeather.ParsemyWeatherResponse(lang, weatherinfo, myweatherresponse, source));
        }

        #endregion

        public static async Task<Weather> GetCurrentStationWeatherAsync(string lang, string stationid, string stationidtype, string xmldir, string siaguser, string siagpswd)
        {
            try
            {
                HttpResponseMessage weatherresponse = await GetWeatherFromSIAG.RequestAsync(lang, siaguser, siagpswd, source);

                //Content auslesen und XDocument Parsen
                var weatherresponsetask = await weatherresponse.Content.ReadAsStringAsync();
                XDocument myweatherresponse = XDocument.Parse(weatherresponsetask);

                var weatherinfo = XDocument.Load(xmldir + "Weather.xml");

                //Testen
                //string mystationid = (from x in weatherinfo.Root.Elements("Station")
                //                      let y = x.Elements(stationidtype + "s").SelectMany(y => y.Elements(stationidtype).Elements("ID").Select(u => u.Value))                                      
                //                      where y == stationid
                //                      select x.Attribute("Id").Value).FirstOrDefault();

                string mystationid = null;

                if (stationidtype == "Id")
                    mystationid = stationid;
                else
                    mystationid = weatherinfo.Root.Elements("Station").Where(x => x.Elements(stationidtype + "s").Elements(stationidtype).Attributes("ID").Select(y => y.Value).Contains(stationid)).Select(u => u.Attribute("Id").Value).FirstOrDefault();


                if (mystationid != null)
                {
                    var myweather = ParseWeather.ParsemyStationWeatherResponse(lang, weatherinfo, myweatherresponse, mystationid, source);

                    return myweather;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<BezirksWeather> GetCurrentBezirkWeatherAsync(string lang, string bezirksid, string tvrid, string regid, string xmldir, string siaguser, string siagpswd)
        {
            string currentbezirksid = "";

            if (!String.IsNullOrEmpty(bezirksid))
                currentbezirksid = bezirksid;
            else if (!String.IsNullOrEmpty(tvrid))
            {
                var bezirkweatherinfo = XDocument.Load(xmldir + "BezirkWeather.xml");

                currentbezirksid = (from x in bezirkweatherinfo.Root.Elements("Bezirk")
                                    from y in x.Element("TVList").Elements("TV")
                                    where y.Attribute("ID").Value == tvrid
                                    select x.Attribute("ID").Value).FirstOrDefault();
            }
            else if (!String.IsNullOrEmpty(regid))
            {
                var bezirkweatherinfo = XDocument.Load(xmldir + "BezirkWeather.xml");

                currentbezirksid = (from x in bezirkweatherinfo.Root.Elements("Bezirk")
                                    from y in x.Element("RegionList").Elements("Region")
                                    where y.Attribute("ID").Value == regid
                                    select x.Attribute("ID").Value).FirstOrDefault();
            }

            if (!String.IsNullOrEmpty(currentbezirksid))
            {
                HttpResponseMessage weatherresponse = await GetWeatherFromSIAG.RequestBezirksWeatherAsync(lang, currentbezirksid, siaguser, siagpswd, source);

                //Content auslesen und XDocument Parsen
                var weatherresponsetask = await weatherresponse.Content.ReadAsStringAsync();
                XDocument myweatherresponse = XDocument.Parse(weatherresponsetask);

                var myweather = ParseWeather.ParsemyBezirksWeatherResponse(lang, myweatherresponse, source);
                return myweather;
            }
            else
                return null;
        }

        public static async Task<IEnumerable<WeatherRealTime>> GetCurrentRealTimeWEatherAsync(string lang)
        {
            HttpResponseMessage weatherresponse = await GetWeatherFromSIAG.RequestRealtimeWeatherAsync(lang);

            //Content auslesen und XDocument Parsen
            var weatherresponsetask = await weatherresponse.Content.ReadAsStringAsync();

            dynamic stuff = JsonConvert.DeserializeObject(weatherresponsetask);

            List<WeatherRealTime> myrealtimeweatherlist = new List<WeatherRealTime>();

            CultureInfo myculture = new CultureInfo("de-DE");

            foreach (var row in stuff.rows)
            {
                if (row.t != "--")
                {

                    WeatherRealTime myrealtimeweather = new WeatherRealTime();

                    myrealtimeweather.altitude = Convert.ToDouble(row.altitude, CultureInfo.InvariantCulture);
                    myrealtimeweather.latitude = Convert.ToDouble(row.latitude.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    myrealtimeweather.longitude = Convert.ToDouble(row.longitude.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    myrealtimeweather.categoryId = row.categoryId;
                    myrealtimeweather.code = row.code;
                    myrealtimeweather.dd = row.dd;
                    myrealtimeweather.ff = row.ff;
                    myrealtimeweather.hs = row.hs;
                    myrealtimeweather.id = row.id;
                    myrealtimeweather.lastUpdated = Convert.ToDateTime(row.lastUpdated);
                    myrealtimeweather.lwdType = row.lwdType;
                    myrealtimeweather.n = row.n;
                    myrealtimeweather.p = row.p;
                    myrealtimeweather.q = row.q;
                    myrealtimeweather.rh = row.rh;
                    myrealtimeweather.sd = row.sd;
                    myrealtimeweather.gs = row.gs;
                    myrealtimeweather.wt = row.wt;
                    myrealtimeweather.t = row.t;
                    myrealtimeweather.vaxcode = row.vaxcode;
                    myrealtimeweather.visibility = row.visibility;
                    myrealtimeweather.w = row.w;
                    myrealtimeweather.wMax = row.wMax;
                    myrealtimeweather.zoomLevel = row.zoomlevel;

                    List<RealTimeMeasurements> myrealtimemeasurments = new List<RealTimeMeasurements>();

                    foreach (var measurement in row.measurements)
                    {
                        RealTimeMeasurements mymeasurement = new RealTimeMeasurements();
                        mymeasurement.code = measurement.code;
                        mymeasurement.description = measurement.description;
                        mymeasurement.imageUrl = measurement.imageUrl;

                        myrealtimemeasurments.Add(mymeasurement);
                    }
                    myrealtimeweather.measurements = myrealtimemeasurments.ToList();

                    myrealtimeweather.name = row.name;

                    myrealtimeweatherlist.Add(myrealtimeweather);
                }
            }

            return myrealtimeweatherlist.ToList();
        }

        public static async Task<WeatherRealTime> GetCurrentRealTimeWEatherSingleAsync(string lang, string stationid)
        {
            HttpResponseMessage weatherresponse = await GetWeatherFromSIAG.RequestRealtimeWeatherAsync(lang);

            //Content auslesen und XDocument Parsen
            var weatherresponsetask = await weatherresponse.Content.ReadAsStringAsync();

            dynamic stuff = JsonConvert.DeserializeObject(weatherresponsetask);

            List<WeatherRealTime> myrealtimeweatherlist = new List<WeatherRealTime>();

            CultureInfo myculture = new CultureInfo("de-DE");

            var row = ((IEnumerable)stuff.rows).Cast<dynamic>()
                            .Where(p => p.id == stationid).FirstOrDefault();

            if (row.t != "--")
            {

                WeatherRealTime myrealtimeweather = new WeatherRealTime();

                myrealtimeweather.altitude = Convert.ToDouble(row.altitude, CultureInfo.InvariantCulture);
                myrealtimeweather.latitude = Convert.ToDouble(row.latitude.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                myrealtimeweather.longitude = Convert.ToDouble(row.longitude.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                myrealtimeweather.categoryId = row.categoryId;
                myrealtimeweather.code = row.code;
                myrealtimeweather.dd = row.dd;
                myrealtimeweather.ff = row.ff;
                myrealtimeweather.hs = row.hs;
                myrealtimeweather.id = row.id;
                myrealtimeweather.lastUpdated = Convert.ToDateTime(row.lastUpdated);
                myrealtimeweather.lwdType = row.lwdType;
                myrealtimeweather.n = row.n;
                myrealtimeweather.p = row.p;
                myrealtimeweather.q = row.q;
                myrealtimeweather.rh = row.rh;
                myrealtimeweather.sd = row.sd;
                myrealtimeweather.gs = row.gs;
                myrealtimeweather.wt = row.wt;
                myrealtimeweather.t = row.t;
                myrealtimeweather.vaxcode = row.vaxcode;
                myrealtimeweather.visibility = row.visibility;
                myrealtimeweather.w = row.w;
                myrealtimeweather.wMax = row.wMax;
                myrealtimeweather.zoomLevel = row.zoomlevel;

                List<RealTimeMeasurements> myrealtimemeasurments = new List<RealTimeMeasurements>();

                foreach (var measurement in row.measurements)
                {
                    RealTimeMeasurements mymeasurement = new RealTimeMeasurements();
                    mymeasurement.code = measurement.code;
                    mymeasurement.description = measurement.description;
                    mymeasurement.imageUrl = measurement.imageUrl;

                    myrealtimemeasurments.Add(mymeasurement);
                }
                myrealtimeweather.measurements = myrealtimemeasurments.ToList();

                myrealtimeweather.name = row.name;

                return myrealtimeweather;
            }
            else
                return null;

        }
    }
}
