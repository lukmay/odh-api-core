﻿using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIAG;
using DataModel;
using Newtonsoft.Json;

namespace OdhApiImporter.Helpers
{
    public class SIAGImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;

        public SIAGImportHelper(ISettings settings, QueryFactory queryfactory)
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
        }

        public async Task<string> SaveWeatherToHistoryTable()
        {
            var weatherresponsetaskde = await SIAG.GetWeatherData.GetSiagWeatherData("de", settings.SiagConfig.Username, settings.SiagConfig.Password, true);
            var weatherresponsetaskit = await SIAG.GetWeatherData.GetSiagWeatherData("it", settings.SiagConfig.Username, settings.SiagConfig.Password, true);
            var weatherresponsetasken = await SIAG.GetWeatherData.GetSiagWeatherData("en", settings.SiagConfig.Username, settings.SiagConfig.Password, true);

            //Save all Responses to rawdata table

            var siagweatherde = JsonConvert.DeserializeObject<SIAG.WeatherModel.SiagWeather>(weatherresponsetaskde);
            var siagweatherit = JsonConvert.DeserializeObject<SIAG.WeatherModel.SiagWeather>(weatherresponsetaskit); 
            var siagweatheren = JsonConvert.DeserializeObject<SIAG.WeatherModel.SiagWeather>(weatherresponsetasken);

            RawDataStore rawData = new RawDataStore();
            rawData.importdate = DateTime.Now;
            rawData.type = "weather";
            rawData.sourceid = siagweatherde.id.ToString();
            //rawData.id = 0;
            rawData.datasource = "siag";
            rawData.sourceinterface = "http://daten.buergernetz.bz.it/services/weather/bulletin";
            rawData.raw = JsonConvert.SerializeObject(new { de = siagweatherde, it = siagweatherit, en = siagweatheren });
            

            var insertresultraw = await QueryFactory.Query("rawdata")
                  .InsertGetIdAsync<int>(rawData);

            var rawdataid = insertresultraw.ToString();
            
            //Save parsed Response to measurement history table
            var odhweatherresultde = await SIAG.GetWeatherData.ParseSiagWeatherDataToODHWeather("de", settings.XmlConfig.XmldirWeather, weatherresponsetaskde, true);
            var odhweatherresultit = await SIAG.GetWeatherData.ParseSiagWeatherDataToODHWeather("it", settings.XmlConfig.XmldirWeather, weatherresponsetaskit, true);
            var odhweatherresulten = await SIAG.GetWeatherData.ParseSiagWeatherDataToODHWeather("en", settings.XmlConfig.XmldirWeather, weatherresponsetasken, true);

            //Insert into Measuringhistorytable
            //var insertresultde = await QueryFactory.Query("weatherdatahistory")
            //      .InsertAsync(new JsonBDataRaw { id = odhweatherresultde.Id + "_de", data = new JsonRaw(odhweatherresultde), raw = weatherresponsetaskde });
            //var insertresultit = await QueryFactory.Query("weatherdatahistory")
            //      .InsertAsync(new JsonBDataRaw { id = odhweatherresultde.Id + "_it", data = new JsonRaw(odhweatherresultit), raw = weatherresponsetaskit });
            //var insertresulten = await QueryFactory.Query("weatherdatahistory")
            //      .InsertAsync(new JsonBDataRaw { id = odhweatherresultde.Id + "_en", data = new JsonRaw(odhweatherresulten), raw = weatherresponsetasken });

            var myweatherhistory = new WeatherHistory();
            myweatherhistory.Weather.Add("de", odhweatherresultde);
            myweatherhistory.Weather.Add("it", odhweatherresultit);
            myweatherhistory.Weather.Add("en", odhweatherresulten);
            myweatherhistory.LicenseInfo = new LicenseInfo() { License = "", ClosedData = false, LicenseHolder = "https://provinz.bz.it/wetter" };

            var insertresult = await QueryFactory.Query("weatherdatahistory")
                  .InsertAsync(new JsonBDataRaw { id = odhweatherresultde.Id.ToString(), data = new JsonRaw(myweatherhistory), raw = JsonConvert.SerializeObject(new { de = siagweatherde, it = siagweatherit, en = siagweatheren }) });            

            return String.Format("id: {0}, datetime: {1}", rawdataid, DateTime.Now.ToShortDateString());
        }
    }

    public class WeatherHistory
    {
        public WeatherHistory()
        {
            Weather = new Dictionary<string, Weather>();
        }
        public IDictionary<string, Weather> Weather { get; set; }
        public LicenseInfo LicenseInfo { get; set; }
    }
}
